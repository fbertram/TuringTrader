//==============================================================================
// Project:     Trading Simulator
// Name:        Algorithm
// Description: Base class for trading algorithms
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
#endregion

namespace FUB_TradingSim
{
    /// <summary>
    /// base class for trading algorithms
    /// </summary>
    public abstract partial class Algorithm
    {
        #region internal helpers
        private void ExecOrder(Order ticket)
        {
            if (SimTime[0] < StartTime)
                return;

            Instrument instrument = ticket.Instrument;
            Bar execBar = null;
            double netAssetValue = 0.0;
            double price = 0.00;
            switch(ticket.Type)
            {
                case OrderType.closeThisBar:
                    execBar = instrument[1];
                    netAssetValue = NetAssetValue[1];
                    if (execBar.HasBidAsk)
                        price = ticket.Quantity > 0 ? execBar.Ask : execBar.Bid;
                    else
                        price = execBar.Close;
                    break;

                case OrderType.openNextBar:
                    execBar = instrument[0];
                    netAssetValue = NetAssetValue[0];
                    price = execBar.Open;
                    break;

                case OrderType.optionExpiryClose:
                    // execBar = instrument[1]; // option bar
                    execBar = Instruments[instrument.OptionUnderlying][1]; // underlying bar
                    netAssetValue = NetAssetValue[0];
                    price = ticket.Price;
                    break;

                case OrderType.stopNextBar:
                    execBar = instrument[0];
                    netAssetValue = NetAssetValue[0];
                    if (ticket.Quantity > 0)
                    {
                        if (ticket.Price > execBar.High)
                            return;

                        price = Math.Max(ticket.Price, execBar.Open);
                    }
                    else
                    {
                        if (ticket.Price < execBar.Low)
                            return;

                        price = Math.Min(ticket.Price, execBar.Open);
                    }
                    break;
            }

            // add position
            if (!Positions.ContainsKey(instrument))
                Positions[instrument] = 0;
            Positions[instrument] += ticket.Quantity;
            if (Positions[instrument] == 0)
                Positions.Remove(instrument);

            // determine # of shares
            int numberOfShares = instrument.IsOption
                ? 100 * ticket.Quantity 
                : ticket.Quantity;

            // pay for it
            Cash = Cash
                - numberOfShares * price
                - Math.Abs(numberOfShares) * CommissionPerShare;

            // add log entry
            LogEntry log = new LogEntry()
            {
                Symbol = ticket.Instrument.Symbol,
                OrderTicket = ticket,
                BarOfExecution = execBar,
                NetAssetValue = netAssetValue,
                FillPrice = price,
                Commission = Math.Abs(numberOfShares) * CommissionPerShare,
            };
            ticket.Instrument = null; // the instrument holds the data source... which consumes lots of memory
            Log.Add(log);
        }
        private void ExpireOption(Instrument instr)
        {
            Instrument underlying = Instruments[instr.OptionUnderlying];
            double price = underlying.Close[1];

            // create order ticket
            Order ticket = new Order()
            {
                Instrument = instr,
                Quantity = -Positions[instr],
                Type = OrderType.optionExpiryClose,
                Price = instr.OptionIsPut
                    ? Math.Max(0.00, instr.OptionStrike - price) 
                    : Math.Max(0.00, price - instr.OptionStrike),
            };

            // force execution
            ExecOrder(ticket);
        }
        private double CalcNetAssetValue()
        {
            double nav = Cash;

            foreach (var instrument in Positions.Keys)
            {
                double price = 0.00;

                if (instrument.HasBidAsk && instrument.IsBidAskValid[0])
                {
                    price = Positions[instrument] > 0
                        ? instrument.Bid[0]
                        : instrument.Ask[0];
                }
                else if (instrument.HasOHLC)
                {
                    price = instrument.Close[0];
                }

                double quantity = instrument.IsOption
                    ? 100.0 * Positions[instrument]
                    : Positions[instrument];

                nav += quantity * price;
            }

            return nav;
        }
        #endregion

        #region public Algorithm()
        public Algorithm()
        {
            // initialize the time series.
            // this is generally not required, but 
            // without this line the optimizer demo
            // will crash, as it has zero bars
            NetAssetValue.Value = 0.0;

            // create a dictionary of optimizer parameters
            OptimizerParams = new Dictionary<string, OptimizerParam>();
            foreach (OptimizerParam param in OptimizerParam.GetParams(this))
                OptimizerParams[param.Name] = param;

            GlobalSettings.MostRecentAlgorithm = Name;
        }
        #endregion
        #region public string Name
        public string Name
        {
            get
            {
                return this.GetType().Name;
            }
        }
        #endregion
        #region virtual public void Run()
        virtual public void Run()
        {
        }
        #endregion
        #region virtual public object Report()
        virtual public void Report()
        {
        }
        #endregion
        #region public double FitnessValue
        public double FitnessValue
        {
            get;
            protected set;
        }
        #endregion

        public /*readonly*/ Dictionary<string, OptimizerParam> OptimizerParams;
        #region public string OptimizerParamsAsString
        public string OptimizerParamsAsString
        {
            get
            {
                string retval = "";
                foreach (var parameter in OptimizerParams.Values.OrderBy(p => p.Name))
                    retval += string.Format("{0}={1} ", parameter.Name, parameter.Value);
                return retval;
            }
        }
        #endregion

        protected List<DataSource> DataSources = new List<DataSource>();

        protected DateTime StartTime;
        protected DateTime? WarmupStartTime = null;
        protected DateTime EndTime;

        public TimeSeries<DateTime> SimTime = new TimeSeries<DateTime>();
        protected bool IsLastBar = false;

        #region protected IEnumerable<DateTime> SimTimes
        protected IEnumerable<DateTime> SimTimes
        {
            get
            {
                // initialization
                DateTime warmupStartTime = WarmupStartTime != null
                    ? (DateTime)WarmupStartTime
                    : StartTime;

                // save the status of our enumerators here
                Dictionary<DataSource, bool> hasData = new Dictionary<DataSource, bool>();

                // reset all enumerators
                foreach (DataSource instr in DataSources)
                {
                    instr.LoadData(warmupStartTime, EndTime);
                    instr.BarEnumerator.Reset();
                    hasData[instr] = instr.BarEnumerator.MoveNext();
                }

                // reset trade log
                Log.Clear();

                // loop, until we've consumed all data
                while (hasData.Select(x => x.Value ? 1 : 0).Sum() > 0)
                {
                    SimTime.Value = DataSources
                        .Where(i => hasData[i])
                        .Min(i => i.BarEnumerator.Current.Time);

                    // go through all data sources
                    foreach (DataSource source in DataSources)
                    {
                        // while timestamp is current, keep adding bars
                        // options have multiple bars with identical timestamps!
                        while (hasData[source] && source.BarEnumerator.Current.Time == SimTime[0])
                        {
                            if (!Instruments.ContainsKey(source.BarEnumerator.Current.Symbol))
                                Instruments[source.BarEnumerator.Current.Symbol] = new Instrument(this, source);
                            Instruments[source.BarEnumerator.Current.Symbol].Value = source.BarEnumerator.Current;
                            hasData[source] = source.BarEnumerator.MoveNext();
                        }
                    }

                    // execute orders
                    foreach (Order order in PendingOrders)
                        ExecOrder(order);
                    PendingOrders.Clear();

                    // handle option expiry on bar following expiry
                    List<Instrument> optionsToExpire = Positions.Keys
                            .Where(i => i.IsOption && i.OptionExpiry.Date < SimTime[0].Date)
                            .ToList();

                    foreach (Instrument instr in optionsToExpire)
                        ExpireOption(instr);

                    // update net asset value
                    NetAssetValue.Value = CalcNetAssetValue();
                    ITimeSeries<double> filteredNAV = NetAssetValue.EMA(3);
                    NetAssetValueHighestHigh = Math.Max(NetAssetValueHighestHigh, filteredNAV[0]);
                    NetAssetValueMaxDrawdown = Math.Max(NetAssetValueMaxDrawdown, 1.0 - filteredNAV[0] / NetAssetValueHighestHigh);

                    // update IsLastBar
                    IsLastBar = hasData.Select(x => x.Value ? 1 : 0).Sum() == 0;

                    // run our algorithm here
                    if (SimTime[0] >= warmupStartTime && SimTime[0] <= EndTime)
                        yield return SimTime[0];
                }

                // attempt to free up resources
#if true
                Instruments.Clear();
                Positions.Clear();
                PendingOrders.Clear();
                DataSources.Clear();
#endif

                yield break;
            }
        }
        #endregion
        public Dictionary<string, Instrument> Instruments = new Dictionary<string, Instrument>();
        #region public Instrument FindInstrument(string)
        public Instrument FindInstrument(string nickname)
        {
            return Instruments.Values
                .Where(i => i.Nickname == nickname)
                .First();
        }
        #endregion
        #region protected List<Instrument> OptionChain(string)
        protected List<Instrument> OptionChain(string nickname)
        {
            List<Instrument> optionChain = Instruments
                    .Select(kv => kv.Value)
                    .Where(i => i.Nickname == nickname  // check nickname
                        && i[0].Time == SimTime[0]      // current bar
                        && i.IsOption                   // is option
                        && i.OptionExpiry > SimTime[0]  // future expiry
                        && i.IsBidAskValid[0])          // bid/ask seems legit
                    .ToList();

            return optionChain;
        }
        #endregion

        public List<Order> PendingOrders = new List<Order>();
        public Dictionary<Instrument, int> Positions = new Dictionary<Instrument, int>();
        public List<LogEntry> Log = new List<LogEntry>();

        protected double Cash;
        public TimeSeries<double> NetAssetValue = new TimeSeries<double>();
        protected double NetAssetValueHighestHigh = 0.0;
        protected double NetAssetValueMaxDrawdown = 1e-10;

        protected double CommissionPerShare = 0.00;

        public bool IsOptimizing = false;
    }
}
//==============================================================================
// end of file