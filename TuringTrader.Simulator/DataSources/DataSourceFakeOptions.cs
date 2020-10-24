//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceFakeOptions
// Description: Data source providing fake option quotes
// History:     2019i30, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2020, Bertram Solutions LLC
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

//#define MORE_VIX_PERIODS
// MORE_VIX_PERIODS: define this to use 9d, 30d, 3m, 6, 12m VIX.
// if undefined, uses only 30d VIX

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Support;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceFakeOptions : DataSource
        {
            #region internal helpers
            private class SimFakeOptions : Algorithm
            {
#if MORE_VIX_PERIODS
                private static readonly string UNDERLYING = "$SPX";
                private static readonly string VOLATILITY_9D = "$VIX9D";
                private static readonly string VOLATILITY_30D = "$VIX";
                private static readonly string VOLATILITY_3M = "$VIX3M";
                private static readonly string VOLATILITY_6M = "$VIX6M";
                private static readonly string VOLATILITY_12M = "$VIX1Y";
#else
                private static readonly string UNDERLYING = "$SPX";
                private static readonly string VOLATILITY_9D = "$VIX";
                private static readonly string VOLATILITY_30D = "$VIX";
                private static readonly string VOLATILITY_3M = "$VIX";
                private static readonly string VOLATILITY_6M = "$VIX";
                private static readonly string VOLATILITY_12M = "$VIX";
#endif

                private List<Bar> _data;
                private DateTime _startTime;
                private DateTime _endTime;

                public SimFakeOptions(List<Bar> data, DateTime startTime, DateTime endTime)
                {
                    _data = data;
                    _startTime = startTime;
                    _endTime = endTime;
                }

                public override void Run()
                {
                    StartTime = _startTime;
                    EndTime = _endTime;

                    AddDataSource(UNDERLYING);

#if MORE_VIX_PERIODS
                    var volatilities = new Dictionary<double, DataSource>
                    {
                        { 9 / 365.25, AddDataSource(VOLATILITY_9D) },
                        { 30 / 365.25, AddDataSource(VOLATILITY_30D) },
                        { 91 / 365.25, AddDataSource(VOLATILITY_3M) },
                        { 182 / 365.25, AddDataSource(VOLATILITY_6M) },
                        { 365 / 365.25, AddDataSource(VOLATILITY_12M) },
                    };
#else
                    var volatilities = new Dictionary<double, DataSource>
                    {
                        { 30 / 365.25, AddDataSource(VOLATILITY_30D) },
                    };
#endif

                    var lowStrikes = new Dictionary<DateTime, int>();
                    var highStrikes = new Dictionary<DateTime, int>();

                    foreach (var simTime in SimTimes)
                    {
                        ITimeSeries<double> underlying = FindInstrument(UNDERLYING).Close;

                        DateTime previousFriday = SimTime[0]
                            - TimeSpan.FromDays((int)SimTime[0].DayOfWeek + 2);

                        var expiries = new List<DateTime>();
                        for (int w = 1; w < 60; w++)
                        {
                            DateTime x = previousFriday + TimeSpan.FromDays(w * 7);
                            if (w <= 13)
                                expiries.Add(x);

                            // 3rd Friday of the Month:
                            //   15, if 1st is a Friday
                            //   21, if 1st is a Saturday
                            else if (x.Day <= 21 && x.Day >= 15)
                                expiries.Add(x);
                        }

                        foreach (var expiry in expiries)
                        {
                            double T = (expiry - SimTime[0].Date).TotalDays / 365.25;

                            var volatility = 0.0;
                            if (T < volatilities.Keys.Min())
                            {
                                volatility = volatilities[volatilities.Keys.Min()].Instrument.Close[0] / 100.0;
                            }
                            else if (T > volatilities.Keys.Max())
                            {
                                volatility = volatilities[volatilities.Keys.Max()].Instrument.Close[0] / 100.0;
                            }
                            else
                            {
                                var vLow = volatilities
                                    .Where(v => v.Key <= T)
                                    .OrderByDescending(v => v.Key)
                                    .FirstOrDefault();
                                var vHigh = volatilities
                                    .Where(v => v.Key >= T)
                                    .OrderBy(v => v.Key)
                                    .FirstOrDefault();
                                var p = (T - vLow.Key) / (vHigh.Key - vLow.Key);
                                volatility = 0.01 * (vLow.Value.Instrument.Close[0]
                                    + p * (vHigh.Value.Instrument.Close[0] - vLow.Value.Instrument.Close[0]));
                            }

                            lowStrikes[expiry] = Math.Min(5 * (int)Math.Round(underlying[0] * 0.70 / 5.0),
                                lowStrikes.ContainsKey(expiry) ? lowStrikes[expiry] : int.MaxValue);
                            highStrikes[expiry] = Math.Max(5 * (int)Math.Round(underlying[0] * 1.20 / 5.0),
                                highStrikes.ContainsKey(expiry) ? highStrikes[expiry] : 0);
                            int lowStrike = lowStrikes[expiry];
                            int highStrike = highStrikes[expiry];

                            for (int strike = lowStrike; strike <= highStrike; strike += 5)
                            {
                                for (int putCall = 0; putCall < 2; putCall++) // 0 = put, 1 = call
                                {
                                    double z = (strike - underlying[0])
                                        / (Math.Sqrt(T) * underlying[0] * volatility);

                                    double vol = volatility * (1.0 + 0.30 * Math.Abs(z));

                                    double price = OptionSupport.GBlackScholes(
                                        putCall == 0 ? false : true,
                                        underlying[0],
                                        strike,
                                        T,
                                        0.0, // risk-free rate
                                        0.0, // cost-of-carry rate
                                        vol);

                                    Bar bar = new Bar(
                                        "SPX", //symbol,
                                        SimTime[0],
                                        default(double), default(double), default(double), default(double), default(long), false,
                                        price * 0.99, price * 1.01, 100, 100, true,
                                        expiry.Date,
                                        strike,
                                        putCall == 0 ? true : false);

                                    _data.Add(bar);
                                }
                            }
                        }
                    }
                }
            }

            private void LoadData(List<Bar> data, DateTime startTime, DateTime endTime)
            {
                var sim = new SimFakeOptions(data, startTime, endTime);
                sim.Run();
            }
            #endregion

            //---------- API
            #region public DataSourceFakeOptions(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for fake option quotes.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceFakeOptions(Dictionary<DataSourceParam, string> info) : base(info)
            {
            }
            #endregion
            #region override public void LoadData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Load data into memory.
            /// </summary>
            /// <param name="startTime">start of load range</param>
            /// <param name="endTime">end of load range</param>
            public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            {
                var cacheKey = new CacheId().AddParameters(
                    Info[DataSourceParam.nickName].GetHashCode(),
                    startTime.GetHashCode(),
                    endTime.GetHashCode());

                List<Bar> retrievalFunction()
                {

                    DateTime t1 = DateTime.Now;
                    Output.WriteLine(string.Format("DataSourceFakeOptions: generating data for {0}...", Info[DataSourceParam.nickName]));

                    List<Bar> data = new List<Bar>();
                    LoadData(data, startTime, endTime);

                    DateTime t2 = DateTime.Now;
                    Output.WriteLine(string.Format("DataSourceFakeOptions: finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                    return data;
                }

                var data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction, true);

                CachedData = data;
                return data;
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file