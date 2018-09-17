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

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    public enum ReportType { FitnessValue, Plot, Excel };

    public abstract partial class Algorithm
    {
        //---------- internal data
        private DateTime _simTime;

        //---------- internal helpers
        private void ExecOrder(Order ticket)
        {
            Instrument instrument = ticket.Instrument;
            Bar execBar = null;
            double netAssetValue = 0.0;
            double price = 0.00;
            switch(ticket.Execution)
            {
                case OrderExecution.closeThisBar:
                    execBar = instrument[1];
                    netAssetValue = NetAssetValue[1];
                    if (execBar.HasBidAsk)
                        price = ticket.Quantity > 0 ? execBar.Ask : execBar.Bid;
                    else
                        price = execBar.Close;
                    break;
                case OrderExecution.openNextBar:
                    execBar = instrument[0];
                    netAssetValue = NetAssetValue[0];
                    price = execBar.Open;
                    break;
                case OrderExecution.optionExpiryClose:
                    // execBar = instrument[1]; // option bar
                    execBar = Instruments[instrument.OptionUnderlying][1]; // underlying bar
                    netAssetValue = NetAssetValue[0];
                    price = ticket.Price;
                    break;
            }

            // add position
            if (!Positions.ContainsKey(instrument))
                Positions[instrument] = 0;
            Positions[instrument] += ticket.Quantity;
            if (Positions[instrument] == 0)
                Positions.Remove(instrument);

            // pay for it
            Cash -= (instrument.IsOption ? 100.0 : 1.0)
                * ticket.Quantity * price;

            // add log entry
            LogEntry log = new LogEntry()
            {
                OrderTicket = ticket,
                BarOfExecution = execBar,
                NetAssetValue = netAssetValue,
                FillPrice = price,
                Commission = 0.00
            };
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
                Execution = OrderExecution.optionExpiryClose,
                PriceSpec = OrderPriceSpec.market,
                Price = instr.OptionIsPut
                    ? Math.Max(0.00, instr.OptionStrike - price) 
                    : Math.Max(0.00, price - instr.OptionStrike),
            };

            // force execution
            ExecOrder(ticket);
        }

        //---------- for use by trading applications
        public Algorithm()
        {
        }
        virtual public void Run()
        {

        }
        virtual public object Report(ReportType reportType)
        {
            return FitnessValue;
        }
        public double FitnessValue
        {
            get;
            protected set;
        }

        //---------- for use by algorithms
        protected string DataPath
        {
            set
            {
                if (!Directory.Exists(value))
                    throw new Exception(string.Format("invalid data path {0}", value));

                DataSource.DataPath = value;
            }

            get
            {
                return DataSource.DataPath;
            }
        }

        protected List<DataSource> DataSources = new List<DataSource>();

        protected DateTime StartTime;
        protected DateTime EndTime;

        protected IEnumerable<DateTime> SimTime
        {
            get
            {
                // save the status of our enumerators here
                Dictionary<DataSource, bool> hasData = new Dictionary<DataSource, bool>();

                // reset all enumerators
                foreach (DataSource instr in DataSources)
                {
                    instr.LoadData(StartTime);
                    instr.BarEnumerator.Reset();
                    hasData[instr] = instr.BarEnumerator.MoveNext();
                }

                // loop, until we've consumed all data
                while (hasData.Select(x => x.Value ? 1 : 0).Sum() > 0)
                {
                    _simTime = DataSources
                        .Where(i => hasData[i])
                        .Min(i => i.BarEnumerator.Current.Time);

                    // go through all data sources
                    foreach (DataSource source in DataSources)
                    {
                        // while timestamp is current, keep adding bars
                        // options have multiple bars with identical timestamps!
                        while (hasData[source] && source.BarEnumerator.Current.Time == _simTime)
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
                            .Where(i => i.IsOption && i.OptionExpiry.Date < _simTime.Date)
                            .ToList();

                    foreach (Instrument instr in optionsToExpire)
                        ExpireOption(instr);

                    // update net asset value
                    double nav = Cash;
                    foreach (var instrument in Positions.Keys)
                        nav += Positions[instrument] * instrument.Close[0];
                    NetAssetValue.Value = nav;

                    // run our algorithm here
                    if (_simTime >= StartTime && _simTime <= EndTime)
                        yield return _simTime;
                }

                yield break;
            }
        }
        protected Dictionary<string, Instrument> Instruments = new Dictionary<string, Instrument>();
        protected Instrument FindInstrument(string nickname)
        {
            return Instruments.Values
                .Where(i => i.Nickname == nickname)
                .First();
        }
        protected List<Instrument> OptionChain(string nickname)
        {
            List<Instrument> optionChain = Instruments
                    .Select(kv => kv.Value)
                    .Where(i => i.Nickname == nickname // check nickname
                        && i[0].Time == _simTime       // current bar
                        && i.IsOption                  // is option
                        && i.OptionExpiry > _simTime)  // future expiry
                    .ToList();

            return optionChain;
        }

        public List<Order> PendingOrders = new List<Order>();
        public Dictionary<Instrument, int> Positions = new Dictionary<Instrument, int>();
        public List<LogEntry> Log = new List<LogEntry>();

        protected double Cash;
        public TimeSeries<double> NetAssetValue = new TimeSeries<double>();
    }
}
//==============================================================================
// end of file