//==============================================================================
// Project:     TuringTrader: SimulatorEngine
// Name:        DataSourceHelper
// Description: Helper functionality for data sources.
// History:     2019v15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Helper functionality for data sources
    /// </summary>
    public static class DataSourceHelper
    {
        #region public static List<Bar> AlignWithMarket(List<Bar> rawBars, DateTime startTime, DateTime endTime)
        private class _alignWithMarket : Algorithm
        {
            private List<Bar> _rawBars;
            private List<Bar> _alignedBars;
            private static readonly string SPX = "$SPX";

            public _alignWithMarket(DateTime startTime, DateTime endTime, List<Bar> rawBars, List<Bar> alignedBars)
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

        /// <summary>
        /// Align bars with market dates
        /// </summary>
        /// <param name="rawBars">raw bars, unaligned</param>
        /// <param name="startTime">start time</param>
        /// <param name="endTime">end time</param>
        /// <returns>bars, aligned with US trading days</returns>
        public static List<Bar> AlignWithMarket(List<Bar> rawBars, DateTime startTime, DateTime endTime)
        {
            List<Bar> alignedBars = new List<Bar>();

            var converter = new _alignWithMarket(startTime, endTime, rawBars, alignedBars);
            converter.Run();

            return alignedBars;
        }
        #endregion
    }
}

//==============================================================================
// end of file