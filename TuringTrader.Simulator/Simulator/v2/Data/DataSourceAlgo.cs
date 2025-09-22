//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceAlgo
// Description: Virtual data source to use data from algorithms.
// History:     2022xi25, EFB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2025, Bertram Enterprises LLC dba TuringTrader.
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        #region V1 algorithm wrapper
        class V1AlgoWrapper : Algorithm
        {
            private Algorithm _v2Owner;
            private Simulator.Algorithm _v1Generator;

            // v1 algorithms run in the exchange's timezone
            // while v2 algorithms run in the local timezone
            private DateTime convertTimeFromV1(DateTime v1Time)
                => TimeZoneInfo.ConvertTimeToUtc(v1Time, _v2Owner.TradingCalendar.ExchangeTimeZone)
                    .ToLocalTime();

            // BUGBUG: convertTimeToV1 not implemented
            private DateTime convertTimeToV1(DateTime v2Time)
                => TimeZoneInfo.ConvertTime(v2Time, _v2Owner.TradingCalendar.ExchangeTimeZone);

            private class V1AccountDummy : IAccount
            {
                //--- we convert positions and trade log from the v1
                //    algorithm and make them available here
                public Dictionary<string, double> Positions { get; set; }
                public List<IAccount.OrderReceipt> TradeLog { get; set; }

                //--- these are not required for operation
                public double NetAssetValue => throw new NotImplementedException();
                public double Cash => throw new NotImplementedException();
                public OHLCV ProcessBar() { throw new NotImplementedException(); }
                public void SubmitOrder(string Name, double weight, OrderType orderType, double orderPrice = 0.0) { throw new NotImplementedException(); }
            }
            public V1AlgoWrapper(Algorithm v2Owner, Simulator.Algorithm v1Generator)
            {
                _v2Owner = v2Owner;
                _v1Generator = v1Generator;
                Account = new V1AccountDummy();
            }

            public override string Name => _v1Generator.Name;

            public override void Run()
            {
#if false
                // retired 2024viiii05: this code does not support nested v1 strategies

                //--- prepare v1 algo for execution
                _v1Generator.IsDataSource = true;

                //--- run v1 algo and capture positions at end of sim
                var v1Bars = new List<Simulator.Bar>();
                var v1Positions = (Dictionary<Simulator.Instrument, int>)null;
                foreach (var v1Bar in _v1Generator.Run(
                    convertTimeToV1((DateTime)StartDate),
                    convertTimeToV1((DateTime)EndDate)))
                {
                    v1Bars.Add(v1Bar);
                    // NOTE: _v1Generator.IsLastBar may be unreliable.
                    //       As a workaround, we grab the positions on each bar,
                    //       once we get close to the simulation end.
                    // NOTE2: We copy the position dictionary, as the V1 engine
                    //        clears all positions at the end of its sim loop.
                    if (_v1Generator.IsLastBar || ((DateTime)EndDate - convertTimeFromV1(v1Bar.Time)).TotalDays < 21)
                        v1Positions = new Dictionary<Simulator.Instrument, int>(_v1Generator.Positions);
                }

                //--- convert bars from v1 to v2 format
                var v2Bars = new List<BarType<OHLCV>>();
                foreach (var bar in v1Bars)
                    v2Bars.Add(new BarType<OHLCV>(
                        convertTimeFromV1(bar.Time),
                        new OHLCV(bar.Open, bar.High, bar.Low, bar.Close, bar.Volume)));

                if (v2Bars.Count == 0)
                    throw new Exception(string.Format("no bars received from algorithm '{0}'", _v1Generator.Name));

                //--- convert positions from v1 to v2 format
                var v2Positions = new Dictionary<string, double>();
                foreach (var pos in v1Positions)
                {
                    v2Positions.Add(
                        pos.Key.Nickname,
                        pos.Value * pos.Key.Close[0] / _v1Generator.NetAssetValue[0]);
                }

                //--- convert order log from v1 to v2 format
                var v2Log = new List<IAccount.OrderReceipt>();
                foreach (var entry in _v1Generator.Log)
                {
                    switch (entry.OrderTicket.Type)
                    {
                        case Simulator.OrderType.closeThisBar:
                        case Simulator.OrderType.openNextBar:
                            v2Log.Add(new IAccount.OrderReceipt(
                                new IAccount.OrderTicket(
                                    convertTimeFromV1(entry.OrderTicket.QueueTime),
                                    entry.OrderTicket.Instrument.Nickname,
                                    entry.TargetPercentageOfNav,
                                    OrderType.openNextBar),
                                convertTimeFromV1(entry.BarOfExecution.Time),
                                0.0,   // order size
                                entry.FillPrice,
                                0.0,   // order amount
                                0.0)); // friction amount
                            break;
                    }
                }

                //--- provide converted v1 results to host
                EquityCurve = v2Bars;
                ((V1AccountDummy)Account).TradeLog = v2Log;
                ((V1AccountDummy)Account).Positions = v2Positions;
#else
                // new code 2024viiii05: now supporting nested v1 strategies

                //--- prepare v1 algo for execution
                _v1Generator.IsDataSource = true;
                _v1Generator.IsRunningInsideV2 = true;

                //--- run v1 algo and capture positions at end of sim
                var v2Bars = new List<BarType<OHLCV>>();
                var v2Positions = new Dictionary<DateTime, Dictionary<string, double>>();

                foreach (var v1Bar in _v1Generator.Run(
                    convertTimeToV1((DateTime)StartDate),
                    convertTimeToV1((DateTime)EndDate < DateTime.Now ? (DateTime)EndDate : DateTime.Now)))
                {
                    //--- collect result bars and convert to v2 format
                    v2Bars.Add(new BarType<OHLCV>(
                        convertTimeFromV1(v1Bar.Time),
                        new OHLCV(v1Bar.Open, v1Bar.High, v1Bar.Low, v1Bar.Close, v1Bar.Volume)));

                    //--- collect all end-of-day holdings and convert to v2 format
                    // NOTE: We need to handle the fact that the V1 engine
                    //       clears all positions at the end of its sim loop.
                    //if (_v1Generator.Log.Count() > 0 && _v1Generator.Log.Last().OrderTicket.Type != Simulator.OrderType.endOfSimFakeClose)
                    {
                        var v2PositionsToday = new Dictionary<string, double>();

                        void getNestedPositions(Simulator.Algorithm v1Algo, double pcntMultiplier)
                        {
                            var v1Positions = v1Algo.Positions;
                            var v1Nav = v1Algo.NetAssetValue[0];

                            foreach (var kv in v1Positions)
                            {
                                var v1PosValue = kv.Key.Close[0] * kv.Value;
                                var v1PosPcnt = v1PosValue / v1Nav;

                                if (kv.Key.DataSource.Algorithm == null)
                                {
                                    // position is simple asset
                                    var nickname = kv.Key.Nickname;

                                    if (!v2PositionsToday.ContainsKey(nickname))
                                        v2PositionsToday[nickname] = 0.0;
                                    v2PositionsToday[nickname] += pcntMultiplier * v1PosPcnt;
                                }
                                else
                                {
                                    // position is child strategy
                                    getNestedPositions(kv.Key.DataSource.Algorithm, pcntMultiplier * v1PosPcnt);
                                }
                            }

                            // treat pending orders as executed
                            foreach (var order in v1Algo.PendingOrders)
                            {
                                var v1OrderValue = order.Instrument.Close[0] * order.Quantity;
                                var v1OrderPcnt = v1OrderValue / v1Nav;

                                if (order.Instrument.DataSource.Algorithm == null)
                                {
                                    // position is simple asset
                                    var nickname = order.Instrument.Nickname;

                                    if (!v2PositionsToday.ContainsKey(nickname))
                                        v2PositionsToday[nickname] = 0.0;
                                    v2PositionsToday[nickname] += pcntMultiplier * v1OrderPcnt;
                                }
                                else
                                {
                                    // position is child strategy
                                    getNestedPositions(order.Instrument.DataSource.Algorithm, pcntMultiplier * v1OrderPcnt);
                                }
                            }

                            // clean up v2PositionsToday
                            v2PositionsToday = v2PositionsToday
                                .Where(kv => Math.Abs(kv.Value) > 1e-3)
                                .ToDictionary(kv => kv.Key, kv => kv.Value);
                        }
                        getNestedPositions(_v1Generator, 1.0);

                        // append to list of positions
                        v2Positions[v2Bars.Last().Date] = v2PositionsToday;
                    }
                }

                if (v2Bars.Count == 0)
                    throw new Exception(string.Format("no bars received from algorithm '{0}'", _v1Generator.Name));

                //--- get all order dates
                // NOTE: we are ignoring the actual order tickets,
                //       and only collect the queuing dates
                var v2OrderDates = new HashSet<DateTime>();
                var v1AlgoDone = new HashSet<int>();
                void getNestedOrderDates(Simulator.Algorithm v1Algo)
                {
                    void processOrderTicket(Simulator.Order order)
                    {
                        var v2QueueTime = convertTimeFromV1(order.QueueTime);
                        if (v2QueueTime < v2Bars.First().Date || v2QueueTime > v2Bars.Last().Date)
                            return;

                        switch (order.Type)
                        {
                            case Simulator.OrderType.closeThisBar:
                            case Simulator.OrderType.openNextBar:
                                if (v2QueueTime >= StartDate && v2QueueTime <= EndDate)
                                    v2OrderDates.Add(v2QueueTime);
                                var v1Child = order.Instrument.DataSource.Algorithm;
                                if (v1Child != null)
                                {
                                    var v1ChildHash = v1Child.GetHashCode();
                                    if (!v1AlgoDone.Contains(v1ChildHash))
                                        getNestedOrderDates(v1Child);
                                    v1AlgoDone.Add(v1ChildHash);
                                }
                                break;
                        }
                    }

                    // process executed orders
                    foreach (var entry in v1Algo.Log)
                        processOrderTicket(entry.OrderTicket);

                    // include pending orders
                    foreach (var order in v1Algo.PendingOrders)
                        processOrderTicket(order);
                }
                getNestedOrderDates(_v1Generator);

                //--- create v2 order log from EOD positions and order dates
                // NOTE: this log has nothing to do with the simulator's order log.
                //       instead, we create a fake log, based on the historical holdings.
                var v2Log = new List<IAccount.OrderReceipt>();
                var previousPositions = new Dictionary<string, double>();
                foreach (var v2OrderDate in v2OrderDates.OrderBy(d => d))
                {
                    var newPositions = v2Positions[v2OrderDate];

                    // combine new positions w/ previous positions to capture all orders
                    var orderPositions = new Dictionary<string, double>(newPositions);
                    foreach (var kv in previousPositions)
                        if (!orderPositions.ContainsKey(kv.Key))
                            orderPositions[kv.Key] = 0.0;

                    // create orders for each position
                    foreach (var kv in orderPositions)
                    {
                        v2Log.Add(new IAccount.OrderReceipt(
                            new IAccount.OrderTicket(
                                v2OrderDate,            // order date
                                kv.Key,                 // nickname
                                kv.Value,               // target percentage
                                OrderType.openNextBar), // order type
                            v2OrderDate, // fill date
                            0.0,         // order size
                            0.0,         // fill price
                            0.0,         // order amount
                            0.0));       // friction amount
                    }

                    previousPositions = newPositions;
                }

                //--- provide converted v1 results to host
                EquityCurve = v2Bars;
                ((V1AccountDummy)Account).TradeLog = v2Log;
                ((V1AccountDummy)Account).Positions = v2Positions.Count > 0
                    ? v2Positions[v2Positions.Keys.OrderByDescending(d => d).First()]
                    : new Dictionary<string, double>(); // TODO: can we return null here?
#endif
            }
        }
        #endregion

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> AlgoGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
        {
            var algoName = info[DataSourceParam.nickName2];
            var algoInstance = Simulator.AlgorithmLoader.InstantiateAlgorithm(algoName);

            if (algoInstance == null)
                throw new Exception(string.Format("failed to instantiate algorithm '{0}'", algoName));

            return AlgoGetAssetInstance(owner, algoInstance, info[DataSourceParam.nickName]);
        }

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> AlgoGetAssetInstance(Algorithm owner, Simulator.IAlgorithm generator, string nickname)
        {
            var tradingDays = owner.TradingCalendar.TradingDays;
            var startDate = tradingDays.First(); // includes warmup
            //var endDate = tradingDays.Last(); // includes cooldown
            var endDate = owner.EndDate;

            var instanceV1 = (generator as Simulator.Algorithm);
            var instanceV2 = (generator as Algorithm);

            if (instanceV1 != null)
                instanceV2 = new V1AlgoWrapper(owner, instanceV1);

            if (instanceV2 != null)
            {
                instanceV2.StartDate = startDate;
                instanceV2.EndDate = endDate;
                instanceV2.IsDataSource = true;

#if true
                // new 2023iv29 - share data w/ child algos
                // because the owner's data cache is typically
                // a bypass, this only kicks in while optimizing
                instanceV2.DataCache = owner.DataCache;
#endif

                instanceV2.Run();

                return Tuple.Create(
                    instanceV2.EquityCurve,
                    new TimeSeriesAsset.MetaType
                    {
                        Ticker = nickname,
                        Description = generator.Name,
                        Generator = instanceV2,
                    });
            }

            return null;
        }
    }
}

//==============================================================================
// end of file
