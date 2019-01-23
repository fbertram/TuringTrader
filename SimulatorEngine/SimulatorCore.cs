//==============================================================================
// Project:     Trading Simulator
// Name:        SimulatorCore
// Description: Simulator engine core
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

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Simulator engine core, managing data sources and instruments,
    /// processing a sequence of bars, simulating trades, keeping
    /// track of positions, and maintaining log information.
    /// </summary>
    public abstract class SimulatorCore
    {
        #region internal data
        private Dictionary<string, Instrument> _instruments = new Dictionary<string, Instrument>();
        private List<DataSource> _dataSources = new List<DataSource>();
        #endregion
        #region internal helpers
        private void ExecOrder(Order ticket)
        {
            if (ticket.Type == OrderType.cash)
            {
                // to make things similar to stocks, a positive quantity
                // results in a debit, a negative quantity in a credit
                Cash -= ticket.Quantity * ticket.Price;

                LogEntry l = new LogEntry()
                {
                    Symbol = "N/A",
                    InstrumentType = LogEntryInstrument.Cash,
                    OrderTicket = ticket,
                    BarOfExecution = Instruments
                        .Where(i => i.Time[0] == SimTime[0])
                        .First()[0],
                    NetAssetValue = NetAssetValue[0],
                    FillPrice = ticket.Price,
                    Commission = 0.0,
                };
                Log.Add(l);

                return;
            }

            // no trades during warmup phase
            if (SimTime[0] < StartTime)
                return;

            Instrument instrument = ticket.Instrument;
            Bar execBar = null;
            double netAssetValue = 0.0;
            double price = 0.00;
            switch (ticket.Type)
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

                case OrderType.stockInactiveClose:
                    execBar = instrument[0];
                    netAssetValue = NetAssetValue[5]; // this is probably incorrect
                    price = execBar.Close;
                    break;

                case OrderType.optionExpiryClose:
                    // execBar = instrument[1]; // option bar
                    execBar = _instruments[instrument.OptionUnderlying][1]; // underlying bar
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

            // determine commission (no commission on expiry)
            double commission = ticket.Type != OrderType.optionExpiryClose
                            && ticket.Type != OrderType.stockInactiveClose
                ? Math.Abs(numberOfShares) * CommissionPerShare
                : 0.00;

            // pay for it
            Cash = Cash
                - numberOfShares * price
                - commission;

            // add log entry
            LogEntry log = new LogEntry()
            {
                Symbol = ticket.Instrument.Symbol,
                InstrumentType = ticket.Instrument.IsOption
                    ? (ticket.Instrument.OptionIsPut ? LogEntryInstrument.OptionPut : LogEntryInstrument.OptionCall)
                    : LogEntryInstrument.Equity,
                OrderTicket = ticket,
                BarOfExecution = execBar,
                NetAssetValue = netAssetValue,
                FillPrice = price,
                Commission = commission,
            };
            ticket.Instrument = null; // the instrument holds the data source... which consumes lots of memory
            Log.Add(log);
        }
        private void ExpireOption(Instrument instr)
        {
            Instrument underlying = _instruments[instr.OptionUnderlying];
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
        private void ExpireInstrument(Instrument instrument)
        {
            // create order ticket
            Order ticket = new Order()
            {
                Instrument = instrument,
                Quantity = -instrument.Position,
                Type = OrderType.stockInactiveClose,
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

                // TODO: close any stale positions
            }

            return nav;
        }
        #endregion

        #region public SimulatorCore()
        /// <summary>
        /// Initialize simulator engine. Only very little is happening here,
        /// most of the engine initialization is performed in SimTimes, to
        /// allow multiple runs of the same algorithm instance.
        /// </summary>
        public SimulatorCore()
        {
            // this is not required, a new object will be assigned
            // during SimTime's initialization. we assign an object
            // here, to avoid a crash in Demo05_Optimizer, which does
            // not have any bars, and does not call SimTime
            NetAssetValue = new TimeSeries<double>();
            NetAssetValue.Value = 0.0;
        }
        #endregion

        #region public string Name
        /// <summary>
        /// Return class type name. This method will return the name of the
        /// derived class, typically a proprietary algorithm derived from
        /// Algorithm.
        /// </summary>
        public string Name
        {
            get
            {
                return this.GetType().Name;
            }
        }
        #endregion

        #region abstract public void Run()
        /// <summary>
        /// Hook for proprietary trading logic. This method must be
        /// overriden and implemented by a derived class.
        /// </summary>
        abstract public void Run();
        #endregion

        #region protected DateTime StartTime
        /// <summary>
        /// Time stamp representing the first bar, on which
        /// the simulator will perform trades. Most often, this is
        /// also the earliest bar being processed by the simulator,
        /// unless WarmupStartTime is set to an earlier time.
        /// </summary>
        protected DateTime StartTime;
        #endregion
        # region protected DateTime? WarmupStartTime
        /// <summary>
        /// Optional value, specifying a time stamp earlier than StartTime,
        /// representing the first bar processed by the simulator. Setting
        /// this value allows to warm up indicators and internal calculations
        /// prior to starting trading activity.
        /// </summary>
        protected DateTime? WarmupStartTime = null;
        #endregion
        #region protected DateTime EndTime
        /// <summary>
        /// Time stamp, representing the last bar processed by the simulator.
        /// For simulations reaching into live trading, this should be set
        /// to a future time.
        /// </summary>
        protected DateTime EndTime;
        #endregion
        #region public int TradingDays
        /// <summary>
        /// Number of trading days processed. The first trading day is
        /// considered the bar, on which the very first trade is executed.
        /// This may or may not be the first trade submitted.
        /// </summary>
        public int TradingDays;
        #endregion

        #region protected IEnumerable<DateTime> SimTimes
        /// <summary>
        /// Enumerable of available simulation time stamps. An algorithm
        /// processes bars by iterating through these time stamps using
        /// a foreach loop.
        /// </summary>
        protected IEnumerable<DateTime> SimTimes
        {
            get
            {
                //----- initialization
                if (WarmupStartTime == null || WarmupStartTime > StartTime)
                    WarmupStartTime = StartTime;

                // save the status of our enumerators here
                Dictionary<DataSource, bool> hasData = new Dictionary<DataSource, bool>();

                // reset all enumerators
                foreach (DataSource source in _dataSources)
                {
                    source.Simulator = this; // we'd love to do this during construction
                    source.LoadData((DateTime)WarmupStartTime, EndTime);
                    source.BarEnumerator.Reset();
                    hasData[source] = source.BarEnumerator.MoveNext();
                }

                // reset trade log
                Log.Clear();

                // reset fitness
                TradingDays = 0;

                // reset cash and net asset value
                // we create a new time-series here, to make sure that
                // any indicators depending on it, are also re-created
                Cash = 0.0;
                NetAssetValue = new TimeSeries<double>();
                NetAssetValue.Value = Cash;
                NetAssetValueHighestHigh = 0.0;
                NetAssetValueMaxDrawdown = 1e-10;

                //----- loop, until we've consumed all data
                while (hasData.Select(x => x.Value ? 1 : 0).Sum() > 0)
                {
                    SimTime.Value = _dataSources
                        .Where(i => hasData[i])
                        .Min(i => i.BarEnumerator.Current.Time);

                    // go through all data sources
                    foreach (DataSource source in _dataSources)
                    {
                        // while timestamp is current, keep adding bars
                        // options have multiple bars with identical timestamps!
                        while (hasData[source] && source.BarEnumerator.Current.Time == SimTime[0])
                        {
                            if (!_instruments.ContainsKey(source.BarEnumerator.Current.Symbol))
                                _instruments[source.BarEnumerator.Current.Symbol] = new Instrument(this, source);
                            Instrument instrument = _instruments[source.BarEnumerator.Current.Symbol];

                            // we shouldn't need to check for duplicate bars here. unfortunately, this
                            // happens with options having multiple roots. it is unclear what the best
                            // course of action is here, for now we just skip the duplicates.
                            // it seems that the duplicate issue stops 11/5/2013???
                            if (instrument.BarsAvailable == 0 || instrument.Time[0] != SimTime[0])
                                instrument.Value = source.BarEnumerator.Current;
                            else
                            {
                                //Output.WriteLine(string.Format("{0}: {1} has duplicate bar on {2}",
                                //        Name, source.BarEnumerator.Current.Symbol, SimTime[0]));
                            }

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

                    // handle instrument expiry
                    IEnumerable<Instrument> instrumentsToExpire = Instruments
                        .Where(i => !i.IsOption
                            && i.Time[0] < SimTime[5]
                            && i.Position != 0);

                    foreach (Instrument instr in instrumentsToExpire)
                        ExpireInstrument(instr);

                    // update net asset value
                    NetAssetValue.Value = CalcNetAssetValue();
                    ITimeSeries<double> filteredNAV = NetAssetValue.EMA(3);
                    NetAssetValueHighestHigh = Math.Max(NetAssetValueHighestHigh, filteredNAV[0]);
                    NetAssetValueMaxDrawdown = Math.Max(NetAssetValueMaxDrawdown, 1.0 - filteredNAV[0] / NetAssetValueHighestHigh);

                    // update IsLastBar
                    IsLastBar = hasData.Select(x => x.Value ? 1 : 0).Sum() == 0;

                    // update TradingDays
                    if (TradingDays == 0 && Positions.Count > 0 // start counter w/ 1st position
                    || TradingDays > 0)
                        TradingDays++;

                    // run our algorithm here
                    if (SimTime[0] >= (DateTime)WarmupStartTime && SimTime[0] <= EndTime)
                        yield return SimTime[0];
                }

                //----- attempt to free up resources
                _instruments.Clear();
                Positions.Clear();
                PendingOrders.Clear();
                //DataSources.Clear();

                yield break;
            }
        }
        #endregion
        #region public TimeSeries<DateTime> SimTime
        /// <summary>
        /// Time series of simulation time stamps with the most recent/ current
        /// time stamp at index 0.
        /// </summary>
        public TimeSeries<DateTime> SimTime = new TimeSeries<DateTime>();
        #endregion
        #region protected bool IsLastBar
        /// <summary>
        /// Flag, indicating the last bar processed by the simulator. Algorithms
        /// may use this to implement special handling of this last bar, e.g.
        /// setting up live trades.
        /// </summary>
        protected bool IsLastBar = false;
        #endregion

        #region protected void AddDataSource(string nickname)
        /// <summary>
        /// Add data source. If the data source already exists, the call is ignored.
        /// </summary>
        /// <param name="nickname">nickname of data source</param>
        protected void AddDataSource(string nickname)
        {
            foreach (DataSource source in _dataSources)
                if (source.Info[DataSourceValue.nickName] == nickname)
                    return;

            _dataSources.Add(DataSource.New(nickname));
        }
        #endregion
        #region protected void AddDataSource(DataSource dataSource)
        /// <summary>
        /// Add data source. If the data source already exists, the call is ignored.
        /// </summary>
        /// <param name="dataSource">new data source</param>
        protected void AddDataSource(DataSource dataSource)
        {
            if (_dataSources.Contains(dataSource))
                return;

            _dataSources.Add(dataSource);
        }
        #endregion
        #region public IEnumerable<Instrument> Instruments
        /// <summary>
        /// Enumeration of instruments available to the simulator. It is
        /// important to understand that instruments are created dynamically
        /// during simulation such, that in many cases the number of instruments
        /// held in this collection increases over the course of the simulation.
        /// </summary>
        public IEnumerable<Instrument> Instruments
        {
            get
            {
                return _instruments.Values;
            }
        }
        #endregion
        #region public Instrument FindInstrument(string)
        /// <summary>
        /// Find an instrument in the Instruments collection by its nickname.
        /// In case multiple instruments have the same nickname, the first
        /// match will be returned.
        /// </summary>
        /// <param name="nickname">nickname of instrument to find</param>
        /// <returns>instrument matching nickname</returns>
        public Instrument FindInstrument(string nickname)
        {
            try
            {
                return _instruments.Values
                    .Where(i => i.Nickname == nickname)
                    .First();
            }
            catch
            {
                throw new Exception(string.Format("Instrument {0} not available on {1:MM/dd/yyyy}", nickname, SimTime[0]));
            }
        }
        #endregion
        #region protected List<Instrument> OptionChain(string)
        /// <summary>
        /// Retrieve option chain by its nickname. This will return a list of
        /// all instruments with the given nickname, marked as options, and with 
        /// bars available at the current simulation time.
        /// </summary>
        /// <param name="nickname">option nickname</param>
        /// <returns>list of option instruments</returns>
        protected List<Instrument> OptionChain(string nickname)
        {
            List<Instrument> optionChain = _instruments.Values
                    .Where(i => i.Nickname == nickname  // check nickname
                        && i[0].Time == SimTime[0]      // current bar
                        && i.IsOption                   // is option
                        && i.OptionExpiry > SimTime[0]  // future expiry
                        && i.IsBidAskValid[0])          // bid/ask seems legit
                    .ToList();

            return optionChain;
        }
        #endregion

        #region public List<Order> PendingOrders
        /// <summary>
        /// List of all currently pending orders.
        /// </summary>
        public List<Order> PendingOrders = new List<Order>();
        #endregion
        #region public Dictionary<Instrument, int> Positions
        /// <summary>
        /// Collection of all instrument objects with currently open positions.
        /// Typically, algorithms will use the Positions property of an instrument,
        /// instead of checking this collection for a match.
        /// </summary>
        public Dictionary<Instrument, int> Positions = new Dictionary<Instrument, int>();
        #endregion
        #region public List<LogEntry> Log
        /// <summary>
        /// List of trade log entries.
        /// </summary>
        public List<LogEntry> Log = new List<LogEntry>();
        #endregion

        #region protected void Deposit(double amount)
        /// <summary>
        /// Deposit cash into account.
        /// </summary>
        /// <param name="amount">amount to deposit</param>
        protected void Deposit(double amount)
        {
            if (amount < 0.0)
                throw new Exception("SimulatorCore: Deposit w/ negative amount");

            if (amount > 0.0)
            {
                Order order = new Order()
                {
                    Instrument = null,
                    Quantity = -1,
                    Type = OrderType.cash,
                    Price = amount,
                };

                PendingOrders.Add(order);
            }
        }
        #endregion
        #region protected void Withdraw(double amount)
        /// <summary>
        /// Withdraw cash from account.
        /// </summary>
        /// <param name="amount">amount to withdraw</param>
        protected void Withdraw(double amount)
        {
            if (amount < 0.0)
                throw new Exception("SimulatorCore: Withdraw w/ negative amount");

            if (amount > 0.0)
            {
                Order order = new Order()
                {
                    Instrument = null,
                    Quantity = 1,
                    Type = OrderType.cash,
                    Price = amount,
                };

                PendingOrders.Add(order);
            }
        }
        #endregion
        #region protected double Cash
        /// <summary>
        /// Currently available cash position. Algorithms will typically
        /// initialize this value at the beginning of the simulation.
        /// </summary>
        public double Cash
        {
            get;
            private set;
        }
        #endregion
        #region public TimeSeries<double> NetAssetValue
        /// <summary>
        /// Total net value of all positions, and cash.
        /// </summary>
        public TimeSeries<double> NetAssetValue;
        #endregion
        #region public double NetAssetValueHighestHigh
        /// <summary>
        /// Highest high of net asset value.
        /// </summary>
        public double NetAssetValueHighestHigh;
        #endregion
        #region public double NetAssetValueMaxDrawdown
        /// <summary>
        /// Maximum drawdown of net asset value, expressed
        /// as a fractional value between 0 and 1.
        /// </summary>
        public double NetAssetValueMaxDrawdown;
        #endregion

        #region protected double CommissionPerShare
        /// <summary>
        /// Commision to be paid per share.
        /// </summary>
        protected double CommissionPerShare = 0.00;
        #endregion
    }
}
//==============================================================================
// end of file