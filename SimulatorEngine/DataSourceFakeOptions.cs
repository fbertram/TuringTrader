//==============================================================================
// Project:     Trading Simulator
// Name:        DataSourceFakeOptions
// Description: Data source providing fake option quotes
// History:     2019i30, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceFakeOptions : DataSource
        {
            #region internal data
            private List<Bar> _data;
            private IEnumerator<Bar> _barEnumerator;
            #endregion
            #region internal helpers
            private class SimFakeOptions : SimulatorCore
            {
                private static readonly string UNDERLYING_NICK = "^SPX.index";
                private static readonly string VOLATILITY_NICK = "^VIX.index";

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
                        Instrument underlying = FindInstrument(UNDERLYING_NICK);
                        Instrument volatility = FindInstrument(VOLATILITY_NICK);

                        DateTime previousFriday = SimTime[0]
                            - TimeSpan.FromDays((int)SimTime[0].DayOfWeek + 2);

                        for (int weeks = 1; weeks < 4 * 4; weeks++)
                        {
                            DateTime expiry = previousFriday + TimeSpan.FromDays(weeks * 7);

                            int lowStrike = 5 * (int)Math.Round(underlying.Close[0] * 0.80 / 5.0);
                            int highStrike = 5 * (int)Math.Round(underlying.Close[0] * 1.20 / 5.0);

                            for (int strike = lowStrike; strike <= highStrike; strike += 5)
                            {
                                for (int putCall = 0; putCall < 2; putCall++)
                                {
                                    double T = (expiry - SimTime[0].Date).TotalDays / 365.25;
                                    double price = OptionSupport.GBlackScholes(
                                        putCall == 0 ? false : true,
                                        underlying.Close[0],
                                        strike,
                                        T,
                                        0.0, // risk-free rate
                                        0.0, // cost-of-carry rate
                                        volatility.Close[0] / 100.0 * 1.10);

                                    Bar bar = new Bar(
                                        "SPX", //symbol,
                                        SimTime[0],
                                        default(double), default(double), default(double), default(double), default(long), false,
                                        price * 0.99, price * 1.01, 100, 100, true,
                                        expiry + DateTime.Parse("4pm").TimeOfDay,
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
            public DataSourceFakeOptions(Dictionary<DataSourceValue, string> info) : base(info)
            {
            }
            #endregion
            #region override public IEnumerator<Bar> BarEnumerator
            /// <summary>
            /// Retrieve enumerator for this data source's bars.
            /// </summary>
            override public IEnumerator<Bar> BarEnumerator
            {
                get
                {
                    if (_barEnumerator == null)
                        _barEnumerator = _data.GetEnumerator();
                    return _barEnumerator;
                }
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
                Output.WriteLine(string.Format("DataSourceFakeOptions: generating data for {0}...", Info[DataSourceValue.nickName]));

                _data = new List<Bar>();
                LoadData(_data, startTime, endTime);

                DateTime t2 = DateTime.Now;
                Output.WriteLine(string.Format("DataSourceFakeOptions: finished after {0:F1} seconds", (t2 - t1).TotalSeconds));
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file