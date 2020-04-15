//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        SimulatorCore
// Description: Simulator engine core
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Indicators;
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
                    FillPrice = ticket.Price,
                    Commission = 0.0,
                };
                Log.Add(l);

                return;
            }

            // no trades during warmup phase
            if (SimTime[0] < StartTime)
                return;

            // conditional orders: cancel, if condition not met
            if (ticket.Condition != null
            && !ticket.Condition(ticket.Instrument))
                return;

            Instrument instrument = ticket.Instrument;
            Bar execBar = null;
            DateTime execTime = default(DateTime);
            double price = 0.00;
            switch (ticket.Type)
            {
                //----- user transactions
                case OrderType.closeThisBar:
                    execBar = instrument[1];
                    execTime = SimTime[1];
                    price = execBar.HasBidAsk
                        ? (ticket.Quantity > 0 ? execBar.Ask : execBar.Bid)
                        : execBar.Open;
                    break;

                case OrderType.openNextBar:
                    execBar = instrument[0];
                    execTime = SimTime[0];
                    price = execBar.HasBidAsk
                        ? (ticket.Quantity > 0 ? execBar.Ask : execBar.Bid)
                        : execBar.Open;
                    break;

                case OrderType.stopNextBar:
                    execBar = instrument[0];
                    execTime = SimTime[0];
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

                case OrderType.limitNextBar:
                    execBar = instrument[0];
                    execTime = SimTime[0];
                    if (ticket.Quantity > 0)
                    {
                        if (ticket.Price < execBar.Low)
                            return;

                        price = Math.Min(ticket.Price, execBar.Open);
                    }
                    else
                    {
                        if (ticket.Price > execBar.High)
                            return;

                        price = Math.Max(ticket.Price, execBar.Open);
                    }
                    break;

                //----- simulator-internal transactions

                case OrderType.instrumentDelisted:
                case OrderType.endOfSimFakeClose:
                    execBar = instrument[0];
                    execTime = SimTime[0];
                    price = execBar.HasBidAsk
                        ? (instrument.Position > 0 ? execBar.Bid : execBar.Ask)
                        : execBar.Close;
                    break;

                case OrderType.optionExpiryClose:
                    // execBar = instrument[1]; // option bar
                    execBar = _instruments[instrument.OptionUnderlying][1]; // underlying bar
                    execTime = SimTime[1];
                    price = ticket.Price;
                    break;

                default:
                    throw new Exception("SimulatorCore.ExecOrder: unknown order type");
            }

#if false
            if (execBar.Time != execTime)
                Output.WriteLine("WARNING: {0}: bar time mismatch. expected {1:MM/dd/yyyy}, found {2:MM/dd/yyyy}", 
                    instrument.Symbol, execTime, execBar.Time);
#endif

            // run fill model. default fill is theoretical price
            var fillPrice = FillModel(ticket, execBar, price);

            // adjust position, unless it's the end-of-sim order
            if (ticket.Type != OrderType.endOfSimFakeClose)
            {
                if (!Positions.ContainsKey(instrument))
                    Positions[instrument] = 0;
                Positions[instrument] += ticket.Quantity;
                if (Positions[instrument] == 0)
                    Positions.Remove(instrument);
            }

            // determine # of shares
            int numberOfShares = instrument.IsOption
                ? 100 * ticket.Quantity
                : ticket.Quantity;

            // determine commission (no commission on expiry, delisting, end-of-sim)
            double commission = ticket.Type != OrderType.optionExpiryClose
                            && ticket.Type != OrderType.instrumentDelisted
                            && ticket.Type != OrderType.endOfSimFakeClose
                ? Math.Abs(numberOfShares) * CommissionPerShare
                : 0.00;

            // pay for it, unless it's the end-of-sim order
            if (ticket.Type != OrderType.endOfSimFakeClose)
            {
                Cash = Cash
                    - numberOfShares * fillPrice
                    - commission;
            }

            // add log entry
            LogEntry log = new LogEntry()
            {
                Symbol = ticket.Instrument.Symbol,
                InstrumentType = ticket.Instrument.IsOption
                    ? (ticket.Instrument.OptionIsPut ? LogEntryInstrument.OptionPut : LogEntryInstrument.OptionCall)
                    : LogEntryInstrument.Equity,
                OrderTicket = ticket,
                BarOfExecution = execBar,
                FillPrice = fillPrice,
                Commission = commission,
            };
            //ticket.Instrument = null; // the instrument holds the data source... which consumes lots of memory
            Log.Add(log);
        }
        private void ExpireOption(Instrument instrument)
        {
            Instrument underlying = _instruments[instrument.OptionUnderlying];
            double price = underlying.Close[1];

            // create order ticket
            Order ticket = new Order()
            {
                Instrument = instrument,
                Quantity = -Positions[instrument],
                Type = OrderType.optionExpiryClose,
                Price = instrument.OptionIsPut
                    ? Math.Max(0.00, instrument.OptionStrike - price)
                    : Math.Max(0.00, price - instrument.OptionStrike),
            };

            // force execution
            ExecOrder(ticket);

            _instruments.Remove(instrument.Symbol);
        }
        private void DelistInstrument(Instrument instrument)
        {
            if (instrument.Position != 0)
            {
                // create order ticket
                Order ticket = new Order()
                {
                    Instrument = instrument,
                    Quantity = -instrument.Position,
                    Type = OrderType.instrumentDelisted,
                    Comment = "delisted",
                };

                // force execution
                ExecOrder(ticket);
            }

            _instruments.Remove(instrument.Symbol);
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
            NetAssetValue = new TimeSeries<double>
            {
                Value = 0.0
            };
        }
        #endregion
        #region public string Name
        /// <summary>
        /// Return class type name. This method will return the name of the
        /// derived class, typically a proprietary algorithm derived from
        /// Algorithm.
        /// </summary>
        public virtual string Name
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
                Dictionary<DataSource, IEnumerator<Bar>> enumData = new Dictionary<DataSource, IEnumerator<Bar>>();

                // reset all enumerators
                foreach (DataSource source in _dataSources)
                {
                    source.Simulator = this; // we'd love to do this during construction
                    source.LoadData((DateTime)WarmupStartTime, EndTime);
                    enumData[source] = source.Data.GetEnumerator();
                    hasData[source] = enumData[source].MoveNext();
                }

#if false
                Output.WriteLine("Data source summary:");
                foreach (var ds in _dataSources)
                {
                    Output.WriteLine("    {0}: {1:MM/dd/yyyy} - {2:MM/dd/yyyy}", ds.Info[DataSourceParam.name], ds.FirstTime, ds.LastTime);
                }
#endif

                // reset trade log
                Log.Clear();

                // reset fitness
                TradingDays = 0;

                // reset cash and net asset value
                // we create a new time-series here, to make sure that
                // any indicators depending on it, are also re-created
                Cash = 0.0;
                NetAssetValue = new TimeSeries<double>
                {
                    Value = Cash
                };
                NetAssetValueHighestHigh = 0.0;
                NetAssetValueMaxDrawdown = 1e-10;

                // reset instruments, positions, orders
                // this is also done at the and of SimTimes, to free memory
                // we might find some data here, if we exited SimTimes with
                // an exception
                // TODO: should we use final {} to fix this?
                _instruments.Clear();
                Positions.Clear();
                //PendingOrders.Clear(); // must not do this, initial deposit is pending!

                SimTime.Clear();

                //----- loop, until we've consumed all data
                while (hasData.Select(x => x.Value ? 1 : 0).Sum() > 0)
                {
                    SimTime.Value = _dataSources
                        .Where(s => hasData[s])
                        .Min(s => enumData[s].Current.Time);

                    NextSimTime = SimTime[0] + TimeSpan.FromDays(1000); // any date far in the future

                    // go through all data sources
                    foreach (DataSource source in _dataSources)
                    {
                        // while timestamp is current, keep adding bars
                        // options have multiple bars with identical timestamps!
                        while (hasData[source] && enumData[source].Current.Time == SimTime[0])
                        {
                            if (!_instruments.ContainsKey(enumData[source].Current.Symbol))
                                _instruments[enumData[source].Current.Symbol] = new Instrument(this, source);
                            Instrument instrument = _instruments[enumData[source].Current.Symbol];

                            // we shouldn't need to check for duplicate bars here. unfortunately, this
                            // happens with options having multiple roots. it is unclear what the best
                            // course of action is here, for now we just skip the duplicates.
                            // it seems that the duplicate issue stops 11/5/2013???
                            if (instrument.BarsAvailable == 0 || instrument.Time[0] != SimTime[0])
                                instrument.Value = enumData[source].Current;
                            else
                            {
                                //Output.WriteLine(string.Format("{0}: {1} has duplicate bar on {2}",
                                //        Name, source.BarEnumerator.Current.Symbol, SimTime[0]));
                            }

                            hasData[source] = enumData[source].MoveNext();
                        }

                        if (hasData[source] && enumData[source].Current.Time < NextSimTime)
                            NextSimTime = enumData[source].Current.Time;
                    }

                    // update IsLastBar
                    IsLastBar = hasData.Select(x => x.Value ? 1 : 0).Sum() == 0;

                    // set NextSimTime according to holiday schedule
                    if (IsLastBar)
                        NextSimTime = this.NextSimTime();

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

                    // handle instrument de-listing
                    IEnumerable<Instrument> instrumentsToDelist = Instruments
                        .Where(i => i.DataSource.LastTime + TimeSpan.FromDays(5) < SimTime[0])
                        .ToList();

                    foreach (Instrument instr in instrumentsToDelist)
                        DelistInstrument(instr);

                    // update net asset value
                    NetAssetValue.Value = CalcNetAssetValue();
                    ITimeSeries<double> filteredNAV = NetAssetValue.EMA(3);
                    NetAssetValueHighestHigh = Math.Max(NetAssetValueHighestHigh, filteredNAV[0]);
                    NetAssetValueMaxDrawdown = Math.Max(NetAssetValueMaxDrawdown, 1.0 - filteredNAV[0] / NetAssetValueHighestHigh);

                    // update TradingDays
                    if (TradingDays == 0 && Positions.Count > 0 // start counter w/ 1st position
                    || TradingDays > 0)
                        TradingDays++;

                    // close all positions at end of simulation
                    if (IsLastBar)
                    {
                        List<Instrument> positionsToClose = Positions.Keys.ToList();
                        foreach (Instrument instrument in positionsToClose)
                        {
                            // create order ticket
                            Order ticket = new Order()
                            {
                                Instrument = instrument,
                                Quantity = -instrument.Position,
                                Type = OrderType.endOfSimFakeClose,
                                Comment = "end of simulation",
                            };

                            // force execution
                            ExecOrder(ticket);
                        }
                    }

                    // run our algorithm here
                    if (SimTime[0] >= (DateTime)WarmupStartTime && SimTime[0] <= EndTime)
                        yield return SimTime[0];
                }

                //----- attempt to free up resources
                _dataSources.Clear();
                _instruments.Clear();
                Positions.Clear();
                PendingOrders.Clear();
                SimTime.Clear();

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
        #region public DateTime NextSimTime
        /// <summary>
        /// Next simulator time stamp
        /// </summary>
        public DateTime NextSimTime
        {
            get;
            private set;
        }
        #endregion
        #region protected bool IsLastBar
        /// <summary>
        /// Flag, indicating the last bar processed by the simulator. Algorithms
        /// may use this to implement special handling of this last bar, e.g.
        /// setting up live trades.
        /// </summary>
        protected bool IsLastBar = false;
        #endregion

        #region protected DataSource AddDataSource(string nickname)
        /// <summary>
        /// Create new data source and add to simulator. If the simulator 
        /// already has a data source for the nickname, the call is ignored.
        /// </summary>
        /// <param name="nickname">nickname of data source</param>
        /// <returns>newly created data source</returns>
        protected DataSource AddDataSource(string nickname)
        {
            string nickLower = nickname; //.ToLower();

            foreach (DataSource source in _dataSources)
                if (source.Info[DataSourceParam.nickName] == nickLower)
                    return source;

            DataSource newSource = DataSource.New(nickLower);
            _dataSources.Add(newSource);
            return newSource;
        }
        #endregion
        #region protected IEnumerable<DataSource> AddDataSources(IEnumerable<string> nicknames)
        /// <summary>
        /// Add multiple data sources at once.
        /// </summary>
        /// <param name="nicknames">enumerable of nicknames</param>
        /// <returns>enumerable of newly created data sources</returns>
        protected IEnumerable<DataSource> AddDataSources(IEnumerable<string> nicknames)
        {
            List<DataSource> retval = new List<DataSource>();

            foreach (var nickname in nicknames)
                retval.Add(AddDataSource(nickname));

            return retval;
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
        #region protected bool HasInstrument(string nickname)
        /// <summary>
        /// Check, if the we have an instrument with the given nickname
        /// </summary>
        /// <param name="nickname">nickname to check</param>
        /// <returns>true, if instrument exists</returns>
        protected bool HasInstrument(string nickname)
        {
            return Instruments.Where(i => i.Nickname == nickname).Count() > 0;
        }
        #endregion
        #region protected bool HasInstrument(DataSource ds)
        /// <summary>
        /// Check if we have an instrument for the given datasource
        /// </summary>
        /// <param name="ds">data source to check</param>
        /// <returns>true, if instrument exists</returns>
        protected bool HasInstrument(DataSource ds)
        {
            return ds.Instrument != null;
        }
        #endregion
        #region protected bool HasInstruments(IEnumerable<string> nicknames)
        /// <summary>
        /// Check, if we have instruments for all given nicknames
        /// </summary>
        /// <param name="nicknames">enumerable with nick names</param>
        /// <returns>true, if all instruments exist</returns>
        protected bool HasInstruments(IEnumerable<string> nicknames)
        {
            return nicknames
                .Aggregate(
                    true,
                    (prev, nick) => prev && HasInstrument(nick));
        }
        #endregion
        #region protected bool HasInstruments(IEnumerable<DataSource> sources)
        /// <summary>
        /// Check, if we have instruments for all given data sources
        /// </summary>
        /// <param name="sources">enumerable of data sources</param>
        /// <returns>true, if all instruments exist</returns>
        protected bool HasInstruments(IEnumerable<DataSource> sources)
        {
            return sources
                .Aggregate(
                    true,
                    (prev, ds) => prev && HasInstrument(ds));
        }
        #endregion
        #region protected Instrument FindInstrument(string)
        /// <summary>
        /// Find an instrument in the Instruments collection by its nickname.
        /// In case multiple instruments have the same nickname, the first
        /// match will be returned.
        /// </summary>
        /// <param name="nickname">nickname of instrument to find</param>
        /// <returns>instrument matching nickname</returns>
        protected Instrument FindInstrument(string nickname)
        {
            string nickLower = nickname; //.ToLower();

            try
            {
                return _instruments.Values
                    .Where(i => i.Nickname == nickLower)
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
            string nickLower = nickname; //.ToLower();

            List<Instrument> optionChain = _instruments.Values
                    .Where(i => i.Nickname == nickLower // check nickname
                        && i[0].Time == SimTime[0]      // current bar
                        && i.IsOption                   // is option
                        && i.OptionExpiry > SimTime[0]  // future expiry
                        && i.IsBidAskValid[0])          // bid/ask seems legit
                    .ToList();

            return optionChain;
        }
        #endregion

        #region public void QueueOrder(Order order)
        /// <summary>
        /// Queue order ticket for execution
        /// </summary>
        /// <param name="order"></param>
        public void QueueOrder(Order order)
        {
            order.QueueTime = SimTime.BarsAvailable > 0
                ? SimTime[0] : default(DateTime);
            PendingOrders.Add(order);
        }
        #endregion
        #region public List<Order> PendingOrders
        /// <summary>
        /// List of pending orders.
        /// </summary>
        public List<Order> PendingOrders
        {
            get;
            private set;
        } = new List<Order>();
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
        /// Simulator's order log.
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

                QueueOrder(order);
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

                QueueOrder(order);
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

        #region virtual protected double FillModel(Order orderTicket, Bar barOfExecution, double theoreticalPrice)
        /// <summary>
        /// Order fill model. This method is only called for those orders
        /// which are executed, but not for those which expired.
        /// </summary>
        /// <param name="orderTicket">original order ticket</param>
        /// <param name="barOfExecution">bar of order execution</param>
        /// <param name="theoreticalPrice">theoretical fill price</param>
        /// <returns>custom fill price. default: theoretical fill price</returns>
        virtual protected double FillModel(Order orderTicket, Bar barOfExecution, double theoreticalPrice)
        {
            return theoreticalPrice;
        }
        #endregion
    }
}
//==============================================================================
// end of file