//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceAlgo
// Description: Virtual data source to use data from algorithms.
// History:     2022xi25, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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
                public void SubmitOrder(string Name, double weight, OrderType orderType) { throw new NotImplementedException(); }
            }
            public V1AlgoWrapper(Algorithm v2Owner, Simulator.Algorithm v1Generator)
            {
                _v2Owner = v2Owner;
                _v1Generator = v1Generator;
                Account = new V1AccountDummy();
            }

            public override void Run()
            {
                //--- prepare v1 algo for execution
                _v1Generator.IsDataSource = true;

                //--- run v1 algo
                var v1Bars = new List<Simulator.Bar>();
                var v1Positions = (Dictionary<Simulator.Instrument, int>)null;
                foreach (var v1Bar in _v1Generator.Run(
                    convertTimeToV1((DateTime)StartDate),
                    convertTimeToV1((DateTime)EndDate)))
                {
                    v1Bars.Add(v1Bar);
                    if (_v1Generator.IsLastBar)
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
                        pos.Key.Symbol,
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
                                    entry.OrderTicket.Instrument.Nickname,
                                    entry.TargetPercentageOfNav,
                                    OrderType.openNextBar,
                                    convertTimeFromV1(entry.OrderTicket.QueueTime)),
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
            var startDate = tradingDays.First();
            var endDate = tradingDays.Last();

            var instanceV1 = (generator as Simulator.Algorithm);
            var instanceV2 = (generator as Algorithm);

            if (instanceV1 != null)
                instanceV2 = new V1AlgoWrapper(owner, instanceV1);

            if (instanceV2 != null)
            {
                instanceV2.StartDate = startDate;
                instanceV2.EndDate = endDate;
                instanceV2.IsDataSource = true;
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
