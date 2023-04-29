﻿//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Resampling
// Description: Resampling indicators.
// History:     2022xi13, FUB, created
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of resampling indicators.
    /// </summary>
    public static class Resampling
    {
        private static List<bool> Clock(this TimeSeriesFloat series, Func<DateTime, DateTime, bool> trigger, int offset = 0)
        {
            var src = series.Data;
            var dst = new List<bool>();

            for (int idx = 0; idx < src.Count; idx++)
            {
                var current = src[Math.Min(src.Count - 1, Math.Max(0, idx - offset))].Date;
                var next = src[Math.Min(src.Count - 1, Math.Max(0, idx - offset + 1))].Date;

                dst.Add(trigger(current, next));
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

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
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
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
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

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<OHLCV>>();

                            var clock = series.Close.Clock((current, next) => current.Month != next.Month, offset);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                if (clock[idx])
                                {
                                    // BUGBUG: this returns the last OHLCV bar of each monthly period
                                    // FIXME: calculate open, high, low and volume here
                                    dst.Add(src[idx]);
                                }
                            }

                            return (object)Tuple.Create(dst, series.Meta);
                        }));

                    return new TimeSeriesAsset(
                        series.Owner, name,
                        data,
                        (data) => ((Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType>)data).Item1,
                        (data) => ((Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType>)data).Item2);
                });
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

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
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
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
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

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dst = new List<BarType<OHLCV>>();

                            var clock = series.Close.Clock((current, next) => current.DayOfWeek >= next.DayOfWeek, offset);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                if (clock[idx])
                                {
                                    // BUGBUG: this returns the last OHLCV bar of each weekly period
                                    // FIXME: calculate open, high, low and volume here
                                    dst.Add(src[idx]);
                                }
                            }

                            return (object)Tuple.Create(dst, series.Meta);
                        }));

                    return new TimeSeriesAsset(
                        series.Owner, name,
                        data,
                        (data) => ((Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType>)data).Item1,
                        (data) => ((Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType>)data).Item2);
                });
        }
    }
}

//==============================================================================
// end of file