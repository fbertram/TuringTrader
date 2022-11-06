//==============================================================================
// Project:     TuringTrader: SimulatorEngine
// Name:        DataSourceHelper
// Description: Helper functionality for data sources.
// History:     2019v15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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

using System;
using System.Collections.Generic;

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