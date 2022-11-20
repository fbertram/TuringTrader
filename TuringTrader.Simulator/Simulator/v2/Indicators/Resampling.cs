//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Resampling
// Description: Resampling indicators.
// History:     2022xi13, FUB, created
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
using System.Linq;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of resampling indicators.
    /// </summary>
    public static class Resampling
    {
        private static List<bool> Clock(this TimeSeriesFloat series, Func<DateTime, DateTime, bool> trigger, int offset = 0)
        {
            var src = series.Data.Result;
            var dst = new List<bool>();

            for (int idx = 0; idx < src.Count; idx++)
            {
                var current = src[Math.Min(src.Count - 1, idx + offset)].Date;
                var next = src[Math.Min(src.Count - 1, idx + offset + 1)].Date;

                dst.Add(trigger(current, next));
            }

            if (offset > 0)
            {
                for (var i = 0; i < offset; i++)
                {
                    dst.Insert(0, false); // this may be incorrect
                    dst.RemoveAt(dst.Count - 1);
                }
            }
            else
            {
                for (var i = offset; i < 0; i ++)
                {
                    dst.RemoveAt(0);
                    dst.Add(false); // this may be incorrect
                }
            }

            return dst;
        }

        /// <summary>
        /// Resample time series to monthly bars.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="offset">offset in trading days</param>
        /// <returns>output series of monthly bars</returns>
        public static TimeSeriesFloat Monthly(this TimeSeriesFloat series, int offset = 0)
        {
            var name = string.Format("{0}.Monthly({1})", series.Name, offset);

            var data = series.Algorithm.Cache(name, () =>
            {
                var src = series.Data.Result;
                var dst = new List<BarType<double>>();

                var clock = series.Clock((current, next) => current.Month != next.Month, offset);

                for (int idx = 0; idx < src.Count; idx++)
                {
                    if (clock[idx])
                    {
                        dst.Add(src[idx]);
                    }
                }

                return dst;
            });


            return new TimeSeriesFloat(
                series.Algorithm,
                name,
                data);
        }

        /// <summary>
        /// Resample time series to monthly bars.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="offset">offset in trading days</param>
        /// <returns>output series of monthly bars</returns>
        public static TimeSeriesAsset Monthly(this TimeSeriesAsset series, int offset = 0)
        {
            var name = string.Format("{0}.Monthly({1})", series.Name, offset);

            var data = series.Algorithm.Cache(name, () =>
            {
                var src = series.Data.Result;
                var dst = new List<BarType<OHLCV>>();

                var clock = series.Close.Clock((current, next) => current.Month != next.Month, offset);

                for (int idx = 0; idx < src.Count; idx++)
                {
                    if (clock[idx])
                    {
                        // BUGBUG: this returns the last OHLCV bar
                        // FIXME: calculate open, high, low and volume here
                        dst.Add(src[idx]);
                    }
                }

                return dst;
            });

            var meta = series.Meta;

            return new TimeSeriesAsset(
                series.Algorithm,
                name,
                data,
                meta);
        }

        /// <summary>
        /// Resample time series to weekly bars.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="offset">offset in trading days</param>
        /// <returns>output series of weekly bars</returns>
        public static TimeSeriesFloat Weekly(this TimeSeriesFloat series, int offset = 0)
        {
            var name = string.Format("{0}.Weekly({1})", series.Name, offset);

            var data = series.Algorithm.Cache(name, () =>
            {
                var src = series.Data.Result;
                var dst = new List<BarType<double>>();

                var clock = series.Clock((current, next) => current.DayOfWeek >= next.DayOfWeek, offset);

                for (int idx = 0; idx < src.Count; idx++)
                {
                    if (clock[idx])
                    {
                        dst.Add(src[idx]);
                    }
                }

                return dst;
            });

            return new TimeSeriesFloat(
                series.Algorithm,
                name,
                data);
        }

        /// <summary>
        /// Resample time series to weekly bars.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="offset">offset in trading days</param>
        /// <returns>output series of weekly bars</returns>
        public static TimeSeriesAsset Weekly(this TimeSeriesAsset series, int offset = 0)
        {
            var name = string.Format("{0}.Weekly({1})", series.Name, offset);

            var data = series.Algorithm.Cache(name, () =>
            {
                var src = series.Data.Result;
                var dst = new List<BarType<OHLCV>>();

                var clock = series.Close.Clock((current, next) => current.DayOfWeek >= next.DayOfWeek, offset);

                for (int idx = 0; idx < src.Count; idx++)
                {
                    if (clock[idx])
                    {
                        // BUGBUG: this returns the last OHLCV bar
                        // FIXME: calculate open, high, low and volume here
                        dst.Add(src[idx]);
                    }
                }

                return dst;
            });

            var meta = series.Meta;

            return new TimeSeriesAsset(
                series.Algorithm,
                name,
                data,
                meta);
        }
    }
}

//==============================================================================
// end of file