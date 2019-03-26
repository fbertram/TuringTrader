//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataUpdaterFred
// Description: Web data updater, FRED (https://fred.stlouisfed.org/)
// History:     2019iii22, FUB, created
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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
#endregion

// https://fred.stlouisfed.org/graph/fredgraph.csv?bgcolor=%23e1e9f0&chart_type=line&drp=0&fo=open%20sans&graph_bgcolor=%23ffffff&height=450&mode=fred&recession_bars=on&txtcolor=%23444444&ts=12&tts=12&width=1168&nt=0&thu=0&trc=0&show_legend=yes&show_axis_titles=yes&show_tooltip=yes&id=USSLIND&scale=left&cosd=1982-01-01&coed=2018-12-01&line_color=%234572a7&link_values=false&line_style=solid&mark_type=none&mw=3&lw=2&ost=-99999&oet=99999&mma=0&fml=a&fq=Monthly&fam=avg&fgst=lin&fgsnd=2009-06-01&line_index=1&transformation=lin&vintage_date=2019-03-22&revision_date=2019-03-22&nd=1982-01-01

namespace TuringTrader.Simulator
{
    public partial class DataUpdaterCollection
    {
        #region private class AlignWithMarket 
        private class AlignWithMarket : Algorithm
        {
            private List<Bar> _rawBars;
            private List<Bar> _alignedBars;
            private static readonly string SPX = "$SPX.index";

            public AlignWithMarket(DateTime startTime, DateTime endTime, List<Bar> rawBars, List<Bar> alignedBars)
            {
                _rawBars = rawBars;
                _alignedBars = alignedBars;
                StartTime = startTime;
                EndTime = endTime;
            }

            public override void Run()
            {
                AddDataSource(SPX);

                int i = 0;
                foreach (var s in SimTimes)
                {
                    // increment, until raw bar has a timestamp
                    // LARGER (not equal) than the simulator. 
                    while (i < _rawBars.Count
                    && SimTime[0] >= _rawBars[i].Time)
                        i++;

                    // now the raw bar we want is the previous one
                    if (i > 0)
                    {
                        var rawBar = _rawBars[i - 1];
                        var alignedBar = Bar.NewOHLC(
                            rawBar.Symbol, SimTime[0],
                            rawBar.Open, rawBar.High, rawBar.Low, rawBar.Close, rawBar.Volume);

                        _alignedBars.Add(alignedBar);
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Data updater for fred.stlouisfed.org
        /// </summary>
        private class DataUpdaterFred :DataUpdater
        {
            #region internal data & helpers
            // URL can be retrieved by manually downloading from FRED website
            private static readonly string URL_TEMPLATE = @"https://fred.stlouisfed.org/graph/fredgraph.csv?id={0}&cosd={1:yyyy}-{1:MM}-{1:dd}&coed={2:yyyy}-{2:MM}-{2:dd}";
            private static readonly Dictionary<DataSourceValue, string> PARSE_INFO = new Dictionary<DataSourceValue, string>()
            {
                { DataSourceValue.date,     "{1}" },
                { DataSourceValue.time,     "16:00" },
                { DataSourceValue.close,    "{2}" },
            };
            #endregion

            #region public DataUpdaterFred(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            /// <summary>
            /// Create and initialize data updater object.
            /// </summary>
            /// <param name="simulator">parent simulator</param>
            /// <param name="info">info dictionary</param>
            public DataUpdaterFred(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            {
            }
            #endregion

            #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Run data update.
            /// </summary>
            /// <param name="startTime">start of update range</param>
            /// <param name="endTime">end of update range</param>
            /// <returns>enumerable of updated bars</returns>
            override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
            {
                string url = string.Format(URL_TEMPLATE,
                    Info[DataSourceValue.symbolFred], startTime, endTime);

                var rawBars = new List<Bar>();

                using (var client = new WebClient())
                {
                    string rawData = client.DownloadString(url);

                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(rawData)))
                    using (StreamReader reader = new StreamReader(ms))
                    {
                        string header = reader.ReadLine(); // skip header line

                        for (string line; (line = reader.ReadLine()) != null;)
                        {
                            if (line.Length == 0)
                                continue; // to handle end of file

                            string[] items = (Info[DataSourceValue.ticker] + "," + line).Split(',');

                            var timestamp = 
                                DateTime.Parse(string.Format(PARSE_INFO[DataSourceValue.date], items)).Date
                                + DateTime.Parse(string.Format(PARSE_INFO[DataSourceValue.time], items)).TimeOfDay;

#if true
                            // FIXME: this is incomplete. we need to make sure
                            // our bars align with market data. coercing them to the
                            // monday to friday range is not enough.
                            if (timestamp.DayOfWeek == DayOfWeek.Saturday)
                                timestamp += TimeSpan.FromDays(2);

                            if (timestamp.DayOfWeek == DayOfWeek.Sunday)
                                timestamp += TimeSpan.FromDays(1);
#endif

                            var close = double.Parse(string.Format(PARSE_INFO[DataSourceValue.close], items));

                            var bar = Bar.NewOHLC(
                                Info[DataSourceValue.ticker], timestamp,
                                close, close, close, close, default(long));

                            if (bar.Time >= startTime
                            && bar.Time <= endTime)
                                rawBars.Add(bar);
                        }
                    }
                } // using (var client = new WebClient())

                // use a simulator instance to align bars w/ S&P 500
                var alignedBars = new List<Bar>();
                var align = new AlignWithMarket(startTime, endTime, rawBars, alignedBars);
                align.Run();

                // instantiating a new algorithm here will overwrite
                // the most-recent algorithm. need to reset here.
                GlobalSettings.MostRecentAlgorithm = Simulator.Name;

                return alignedBars;
            }
            #endregion

            #region public override string Name
            /// <summary>
            /// Name of updater.
            /// </summary>
            public override string Name
            {
                get
                {
                    return "Fred";
                }
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file