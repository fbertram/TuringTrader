//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        SimulatorCore
// Description: Simulator engine core
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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

// PRINT_ORDERS: if true, print orders to Output. Use this to debug some
// hard-to-find algorithm issues.
//#define PRINT_ORDERS

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
        private readonly Dictionary<string, Instrument> _instruments = new Dictionary<string, Instrument>();
        private readonly List<DataSource> _dataSources = new List<DataSource>();
        #endregion
        #region internal helpers
        // IDE1006: Naming rule violation: Prefix '_' is not expected
#pragma warning disable IDE1006
        private void _execOrder(Order ticket)
        {
#if PRINT_ORDERS
            if (ticket.Type != OrderType.cash)
                Output.WriteLine("{0:MM/dd/yyyy}, {1}: {2} ({3}) {4}x {5}", 
                    SimTime[0], Name,
                    ticket.Quantity > 0 ? "Buy" : "Sell", ticket.Type.ToString(), Math.Abs(ticket.Quantity), ticket.Instrument.Symbol);
#endif

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

            // no trades on delisted assets
            if (!Instruments.Contains(ticket.Instrument))
                return;

            // conditional orders: cancel, if condition not met
            if (ticket.Condition != null
            && !ticket.Condition(ticket.Instrument))
                return;

            Instrument instrument = ticket.Instrument;
            Bar execBar = null;
            DateTime execTime = default;
            double price = 0.00;
            switch (ticket.Type)
            {
                //----- user transactions
                case OrderType.closeThisBar:
                    execBar = instrument[1];
                    execTime = SimTime[1];
                    price = execBar.HasBidAsk
                        ? (ticket.Quantity > 0 ? execBar.Ask : execBar.Bid)
                        : execBar.Close;
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
                    // execBar = instrument[0]; // option bar
                    execBar = _instruments[instrument.OptionUnderlying][0]; // underlying bar
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
            var fillPrice = ticket.Type == OrderType.cash
                || ticket.Type == OrderType.optionExpiryClose
                    ? price
                    : FillModel(ticket, execBar, price);

            // calculate target allocation of asset as
            // achieved by executing the order. This is
            // required for interaction with v2 hosts
            var positionValueAfterOrder = ((Positions.ContainsKey(instrument) ? Positions[instrument] : 0) + ticket.Quantity) * price;
            var positionPercentageAfterOrder = positionValueAfterOrder / _calcNetAssetValue();

            // adjust position, unless its the end-of-sim order
            // this is to ensure that the Positions collection can
            // be queried after the simulation finished
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
                ? Math.Abs(numberOfShares * CommissionPerShare)
                : 0.00;

            // pay for it, unless it's the end-of-sim order
            // same reasoning as for adjustment of position applies
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
                TargetPercentageOfNav = positionPercentageAfterOrder,
            };
            // do not remove instrument here, is required for MFE/ MAE analysis
            //ticket.Instrument = null; // the instrument holds the data source... which consumes lots of memory
            Log.Add(log);
        }
#pragma warning restore IDE1006

        // IDE1006: Naming rule violation: Prefix '_' is not expected
#pragma warning disable IDE1006
        private void _expireOption(Instrument instrument)
        {
            Instrument underlying = _instruments[instrument.OptionUnderlying];
            double spotPrice = underlying.Close[0];
            double optionValue = instrument.OptionIsPut
                    ? Math.Max(0.00, instrument.OptionStrike - spotPrice)
                    : Math.Max(0.00, spotPrice - instrument.OptionStrike);

            // create order ticket
            Order ticket = new Order()
            {
                Instrument = instrument,
                Quantity = -Positions[instrument],
                Type = OrderType.optionExpiryClose,
                Price = optionValue,
                Comment = string.Format("spot = {0:C2}", spotPrice)
            };

            // force execution
            _execOrder(ticket);

            _instruments.Remove(instrument.Symbol);
        }
#pragma warning restore IDE1006

        // IDE1006: Naming rule violation: Prefix '_' is not expected
#pragma warning disable IDE1006
        private void _delistInstrument(Instrument instrument)
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
                _execOrder(ticket);
            }

            _instruments.Remove(instrument.Symbol);
        }
#pragma warning restore IDE1006

        private bool _navInvalidFirst = true;

        // IDE1006: Naming rule violation: Prefix '_' is not expected
#pragma warning disable IDE1006
        /// <summary>
        /// calculate algorithm's net asset value.
        /// </summary>
        /// <returns>nav</returns>
        protected virtual double _calcNetAssetValue()
        {
#if true
            var debugNavCalc = false;
#else
            var debugNavCalc = (EndTime - SimTime[0]).TotalDays < 5;
#endif
            var debugNavCalcMsg = debugNavCalc ? string.Format("{0:MM/dd/yyyy}: cash=${1:C2}", SimTime[0], Cash) : "";

            double nav = Cash;

            bool navValid = true;
            string invalidInstrument = "";

            foreach (var instrument in Positions.Keys)
            {
                double price = 0.00;

                if (IsValidBar(instrument[0]))
                {
                    if (instrument.HasBidAsk)
                    {
                        price = Positions[instrument] > 0
                            ? instrument.Bid[0]
                            : instrument.Ask[0];
                    }
                    else if (instrument.HasOHLC)
                    {
                        price = instrument.Close[0];
                    }
                }
                else
                {
                    // price is bad
                    navValid = false;
                    invalidInstrument = instrument.Symbol;
                }

                double quantity = instrument.IsOption
                    ? 100.0 * Positions[instrument]
                    : Positions[instrument];

                nav += quantity * price;

                debugNavCalcMsg += debugNavCalc ? string.Format(", {0}={1}@{2:C2}={3:C2}", instrument.Symbol, quantity, price, quantity * price) : "";
            }

            if (!navValid && _navInvalidFirst)
            {
                Output.WriteLine("{0:MM/dd/yyyy}: NAV invalid, instrument {1}", SimTime[0], invalidInstrument);
                _navInvalidFirst = false;
            }

            if (debugNavCalcMsg.Length > 0)
                Output.WriteLine(debugNavCalcMsg);

            return navValid
                ? nav
                : NetAssetValue[0]; // yesterday's value
        }
#pragma warning restore IDE1006
        #endregion

        #region protected SimulatorCore()
        /// <summary>
        /// Initialize simulator engine. Only very little is happening here,
        /// most of the engine initialization is performed in SimTimes, to
        /// allow multiple runs of the same algorithm instance.
        /// </summary>
        protected SimulatorCore()
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
        private string _Name = null;
        /// <summary>
        /// Return class type name. This method will return the name of the
        /// derived class, typically a proprietary algorithm derived from
        /// Algorithm.
        /// </summary>
        public virtual string Name
        {
            get
            {
                _Name = _Name ?? GetType().Name;
                return _Name;
            }
            set
            {
                _Name = value;
            }
        }
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
        #region protected DateTime? WarmupStartTime
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
                    enumData[source] = source.LoadData((DateTime)WarmupStartTime, (DateTime)EndTime)
                        .GetEnumerator();
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
                // any indicators depending on it are also re-created
                Cash = 0.0;
                NetAssetValue = new TimeSeries<double>(-1)
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
                    // FIXME: this is most likely an issue. We do not
                    // want to advance 'SimTime' for bars that we ignore
                    // further down w/ 'IsValidSimTime'.
                    // fixing this might be non-trivial, because
                    // we might not be able to set 'LastSimTime' properly.
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
                            var theBar = enumData[source].Current;

                            // algorithm data sources might send dummy bars.
                            // these dummy bars have their symbol set to null
                            // and we should not create any instruments for these.
                            if (theBar.Symbol != null)
                            {
                                if (!_instruments.ContainsKey(theBar.Symbol))
                                    _instruments[theBar.Symbol] = new Instrument(this, source);
                                Instrument instrument = _instruments[enumData[source].Current.Symbol];

                                // we shouldn't need to check for duplicate bars here. unfortunately, this
                                // happens with options having multiple roots. it is unclear what the best
                                // course of action is here, for now we just skip the duplicates.
                                // it seems that the duplicate issue stops 11/5/2013???
                                if (instrument.BarsAvailable == 0 || instrument.Time[0] != SimTime[0])
                                    instrument.Value = theBar;
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
                        NextSimTime = CalcNextSimTime(SimTime[0]);

                    // execute orders
                    foreach (Order order in PendingOrders)
                        _execOrder(order);
                    PendingOrders.Clear();

                    // handle option expiry on bar following expiry
                    List<Instrument> optionsToExpire = Positions.Keys
                            .Where(i => i.IsOption && i.OptionExpiry.Date < NextSimTime.Date)
                            .ToList();

                    foreach (Instrument instr in optionsToExpire)
                        _expireOption(instr);

                    // update net asset value
                    NetAssetValue.Value = _calcNetAssetValue();
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
                            _execOrder(ticket);
                        }
                    }

                    // run user algorithm here
                    // FIXME: this is most likely an issue. to fix this,
                    // we want to 'continue' the loop before doing any
                    // processing (e.g. order execution) on the bar
                    if (SimTime[0] >= (DateTime)WarmupStartTime
                            && SimTime[0] <= EndTime
                            && IsValidSimTime(SimTime[0]))
                        yield return SimTime[0];

                    // handle instrument de-listing
                    if (!IsLastBar)
                    {
                        IEnumerable<Instrument> instrumentsToDelist = Instruments
                            .Where(i => !hasData[i.DataSource])
                            .ToList();

                        foreach (Instrument instr in instrumentsToDelist)
                            _delistInstrument(instr);
                    }

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
        #region public bool IsLastBar
        /// <summary>
        /// Flag, indicating the last bar processed by the simulator. Algorithms
        /// may use this to implement special handling of this last bar, e.g.
        /// setting up live trades.
        /// </summary>
        public bool IsLastBar = false;
        #endregion

        #region protected DataSource AddDataSource(...)
        /// <summary>
        /// Create new data source and attach it to the simulator. If the 
        /// simulator already has a data source with the given nickname
        /// attached, the call is ignored.
        /// </summary>
        /// <param name="nickname">nickname to create data source for</param>
        /// <returns>data source created and attached</returns>
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

        /// <summary>
        /// Create new data source and attach it to the simulator. If the
        /// simulator already has a data source representing the given algo
        /// attached, the call call is ignored.
        /// </summary>
        /// <param name="algo">algorithm to create data source for</param>
        /// <returns>data source created and attached</returns>
        protected DataSource AddDataSource(Algorithm algo)
        {
            foreach (DataSource source in _dataSources)
                if (source.Algorithm == algo)
                    return source;

            DataSource newSource = DataSource.New(algo);
            _dataSources.Add(newSource);
            return newSource;
        }

        /// <summary>
        /// Attach existing data source to the simulator. If this data source
        /// has already been attached to the simulator, the call is ignored.
        /// This call is typically used to attach custom data sources, which
        /// have been created without using TuringTrader's object factory.
        /// </summary>
        /// <param name="dataSource">new data source</param>
        /// <returns>data source attached</returns>
        protected DataSource AddDataSource(DataSource dataSource)
        {
            if (_dataSources.Contains(dataSource))
                return dataSource;

            var ds = _dataSources
                .Where(ds => ds.Info[DataSourceParam.nickName] == dataSource.Info[DataSourceParam.nickName])
                .FirstOrDefault();

            if (ds != null)
                return ds;

            _dataSources.Add(dataSource);

            return dataSource;
        }

        /// <summary>
        /// Create new data source and attach it to the simulator. This 
        /// overload allows to flexibly create data sources from various types.
        /// </summary>
        /// <param name="obj">nickname, algorithm object, or data source object</param>
        /// <returns>data source created and attached</returns>
        protected DataSource AddDataSource(object obj)
        {
            if (obj as string != null)
                return AddDataSource((string)obj);
            else if (obj as Algorithm != null)
                return AddDataSource((Algorithm)obj);
            else if (obj as DataSource != null)
                return AddDataSource((DataSource)obj);

            throw new Exception("AddDataSource: invalid type for parameter 'obj'");
        }
        #endregion
        #region protected IEnumerable<DataSource> AddDataSources(IEnumerable<string> nicknames)
        /// <summary>
        /// Add multiple data sources at once and return an enumeration
        /// of data sources. If the simulator already has data sources
        /// with any of the given nicknames, those data sources will be
        /// re-used.
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
        #region protected bool HasInstrument(...)
        /// <summary>
        /// Check, if the we have an instrument with the given nickname. Use this
        /// to check if an instrument is available for a given data source.
        /// </summary>
        /// <param name="nickname">nickname to check</param>
        /// <returns>true, if instrument exists</returns>
        protected bool HasInstrument(string nickname)
        {
            return Instruments.Where(i => i.Nickname == nickname).Count() > 0;
        }

        // CA1822: Member HasInstrument does not access instance data an can be marked as static
#pragma warning disable CA1822
        /// <summary>
        /// Check if we have an instrument for the given datasource.
        /// </summary>
        /// <param name="ds">data source to check</param>
        /// <returns>true, if instrument exists</returns>
        protected bool HasInstrument(DataSource ds)
        {
            // FIXME: the code below would be better. 
            // however, we are concerned of subtle differences
            // in behavior and did not want to change this
            // right now.
            //return Instruments.Where(i => i.DataSource == ds).Count() > 0;

            return ds.Instrument != null;
        }
#pragma warning restore CA1822
        #endregion
        #region protected bool HasInstruments(...)
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
        #region protected List<Instrument> OptionChain(...)
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

                        // NOTE: by filtering out those w/ invalid bid/ask,
                        // algos are discouraged from opening new positions
                        // with these contracts. however, we can still
                        // trade them, if we know the Instrument object
                        && IsValidBar(i[0]))
                    .ToList();

            return optionChain;
        }

        /// <summary>
        /// Retrieve option chain by its data source. This will return a list of
        /// all instruments with the given data source, marked as options, and with 
        /// bars available at the current simulation time.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        protected List<Instrument> OptionChain(DataSource ds)
        {
            List<Instrument> optionChain = _instruments.Values
                    .Where(i => i.DataSource == ds      // check data source
                        && i[0].Time == SimTime[0]      // current bar
                        && i.IsOption                   // is option
                        && i.OptionExpiry > SimTime[0]  // future expiry

                        // NOTE: by filtering out those w/ invalid bid/ask,
                        // algos are discouraged from opening new positions
                        // with these contracts. however, we can still
                        // trade them, if we know the Instrument object
                        && IsValidBar(i[0]))
                    .ToList();

            return optionChain;
        }
        #endregion

        #region public void QueueOrder(Order order)
        /// <summary>
        /// Queue order ticket for execution. Typically, algorithms won't
        /// use this function directly, but use Instrument.Trade instead.
        /// </summary>
        /// <param name="order"></param>
        public void QueueOrder(Order order)
        {
            order.QueueTime = SimTime.BarsAvailable > 0
                ? SimTime[0] : default;
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
        /// Deposit cash into account. Note that the deposit amount
        /// must be positive.
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
        /// Withdraw cash from account. Note that the withdrawal
        /// amount must be positive.
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
        /// Total net liquidation value of all positions plus cash.
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
        /// Commision to be paid per share. The default value is zero, equivalent
        /// to no commissions. Algorithms should set this to match the commissions
        /// paid on high account values/ large numbers of shares traded.
        /// </summary>
        protected double CommissionPerShare = 0.00;
        #endregion

        #region protected virtual double FillModel(Order orderTicket, Bar barOfExecution, double theoreticalPrice)
        /// <summary>
        /// Order fill model. This method is only called for those orders
        /// which are executed, but not for those which expired. The default
        /// implementation fills orders at their theoretical price. Algorithms
        /// can override this method to implement more realistic fill models
        /// reflecting slippage.
        /// </summary>
        /// <param name="orderTicket">original order ticket</param>
        /// <param name="barOfExecution">bar of order execution</param>
        /// <param name="theoreticalPrice">theoretical fill price</param>
        /// <returns>custom fill price. default: theoretical fill price</returns>
        protected virtual double FillModel(Order orderTicket, Bar barOfExecution, double theoreticalPrice)
        {
            return theoreticalPrice;
        }
        #endregion
        #region protected virtual bool IsValidSimTime(DateTime timestamp)
        /// <summary>
        /// Validate simulator timestamp. Timestamps deemed invalid will be
        /// skipped and not passed on to the user algorithm. The default 
        /// implementation is geared at the U.S. stock market and skips 
        /// all bars not within regular trading hours of the NYSE.
        /// </summary>
        /// <param name="timestamp">simulator timestamp</param>
        /// <returns>true, if valid</returns>
        protected virtual bool IsValidSimTime(DateTime timestamp)
        {
            if (timestamp.DayOfWeek >= DayOfWeek.Monday
            && timestamp.DayOfWeek <= DayOfWeek.Friday)
            {
                if (timestamp.TimeOfDay.TotalHours >= 9.5
                && timestamp.TimeOfDay.TotalHours <= 16.0)
                    return true;
            }

            return false;
        }
        #endregion
        #region protected virtual DateTime CalcNextSimTime(DateTime timestamp)
        /// <summary>
        /// Determine next sim time. This hook is used by the simulator to
        /// determine the value for NextSimTime, after reaching the end of 
        /// the available historical bars. The default implementation assumes
        /// the trading calendar for U.S. stock exchanges.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns>next simulator timestamp</returns>
        protected virtual DateTime CalcNextSimTime(DateTime timestamp)
        {
            return HolidayCalendar.NextLiveSimTime(timestamp);
        }
        #endregion
        #region protected virtual bool IsValidBar(Bar bar)
        /// <summary>
        /// Validate bar. This hook is used by the simulator to validate
        /// bars. Invalid bars are relevant in the following situations:
        /// (1) option chain: Simulator.OptionChain will not return contracts
        /// with invalid bars at the current sim time. 
        /// (2) nav calculation: The simulator will ignore invalid bars when
        /// calculating Simulator.NetAssetValue
        /// The default implementation marks bars invalid under the following 
        /// conditions:
        /// (1) bid volume or ask volume is zero.
        /// (2) bid price is less than 20% of ask price.
        /// </summary>
        /// <param name="bar"></param>
        /// <returns></returns>
        protected virtual bool IsValidBar(Bar bar)
        {
            if (bar.HasBidAsk)
            {
                if (bar.BidVolume <= 0
                        || bar.AskVolume <= 0
                        || bar.Bid < 0.2 * bar.Ask)
                    return false;
            }

            return true;
        }
        #endregion
    }
}
//==============================================================================
// end of file