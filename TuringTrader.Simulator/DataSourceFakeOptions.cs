//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceFakeOptions
// Description: Data source providing fake option quotes
// History:     2019i30, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
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
            private class SimFakeOptions : SimulatorCore
            {
                private static readonly string UNDERLYING_NICK = "$SPX";
                private static readonly string VOLATILITY_NICK = "$VIX";

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

                    AddDataSource(UNDERLYING_NICK);
                    AddDataSource(VOLATILITY_NICK);

                    foreach (var simTime in SimTimes)
                    {
                        ITimeSeries<double> underlying = FindInstrument(UNDERLYING_NICK).Close;
                        ITimeSeries<double> volatility = FindInstrument(VOLATILITY_NICK).Close.Divide(100.0);

                        DateTime previousFriday = SimTime[0]
                            - TimeSpan.FromDays((int)SimTime[0].DayOfWeek + 2);

                        for (int weeks = 1; weeks < 4 * 4; weeks++)
                        {
                            DateTime expiry = previousFriday + TimeSpan.FromDays(weeks * 7);
                            double T = (expiry - SimTime[0].Date).TotalDays / 365.25;

                            int lowStrike = 5 * (int)Math.Round(underlying[0] * 0.80 / 5.0);
                            int highStrike = 5 * (int)Math.Round(underlying[0] * 1.20 / 5.0);

                            for (int strike = lowStrike; strike <= highStrike; strike += 5)
                            {
                                for (int putCall = 0; putCall < 2; putCall++) // 0 = put, 1 = call
                                {
                                    double z = (strike - underlying[0])
                                        / (Math.Sqrt(T) * underlying[0] * volatility[0]);

                                    double vol = volatility[0] * (1.0 + 0.30 * Math.Abs(z));

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
            override public void LoadData(DateTime startTime, DateTime endTime)
            {
                DateTime t1 = DateTime.Now;
                Output.WriteLine(string.Format("DataSourceFakeOptions: generating data for {0}...", Info[DataSourceParam.nickName]));

                List<Bar> data = new List<Bar>();
                LoadData(data, startTime, endTime);
                Data = data;

                DateTime t2 = DateTime.Now;
                Output.WriteLine(string.Format("DataSourceFakeOptions: finished after {0:F1} seconds", (t2 - t1).TotalSeconds));
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file