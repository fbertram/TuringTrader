//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Basic
// Description: Basic indicators.
// History:     2022xi02, FUB, created
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
using System.Linq;
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of basic indicators.
    /// </summary>
    public static class Basic
    {
        #region Const
        #endregion
        #region Delay
        /// <summary>
        /// Delay time series.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">delay</param>
        /// <returns>delayed time series</returns>
        public static TimeSeriesFloat Delay(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.Delay({1})", series.Name, n);

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

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                var srcIdx = Math.Max(0, idx - n);
                                dst.Add(new BarType<double>(
                                    src[idx].Date,
                                    src[srcIdx].Value));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion

        #region Highest
        /// <summary>
        /// Return highest value in given period.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">observation period</param>
        /// <returns>time series of highest values</returns>
        public static TimeSeriesFloat Highest(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.Highest({1})", series.Name, n);

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

                            var window = new Queue<double>();
                            for (var i = 0; i < n; i++)
                                window.Enqueue(src[0].Value);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                window.Enqueue(src[idx].Value);
                                window.Dequeue();

                                dst.Add(new BarType<double>(
                                    src[idx].Date,
                                    window.Max(w => w)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region Lowest
        /// <summary>
        /// Return lowest value in given period.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">observation period</param>
        /// <returns>time series of highest values</returns>
        public static TimeSeriesFloat Lowest(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.Lowest({1})", series.Name, n);

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

                            var window = new Queue<double>();
                            for (var i = 0; i < n; i++)
                                window.Enqueue(src[0].Value);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                window.Enqueue(src[idx].Value);
                                window.Dequeue();

                                dst.Add(new BarType<double>(
                                    src[idx].Date,
                                    window.Min(w => w)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region Range
        /// <summary>
        /// Return value range in given period.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">observation period</param>
        /// <returns>time series of range values</returns>
        public static TimeSeriesFloat Range(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.Range({1})", series.Name, n);

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

                            var window = new Queue<double>();
                            for (var i = 0; i < n; i++)
                                window.Enqueue(src[0].Value);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                window.Enqueue(src[idx].Value);
                                window.Dequeue();

                                dst.Add(new BarType<double>(
                                    src[idx].Date,
                                    window.Max(w => w) - window.Min(w => w)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion

        #region AbsReturn
        /// <summary>
        /// Calculate absolute return as r = v[0] - v[1].
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>time series of absolute returns</returns>
        public static TimeSeriesFloat AbsReturn(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.AbsReturn", series.Name);

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
                            var prev = src[0].Value;

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value - prev));

                                prev = it.Value;
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region RelReturn
        /// <summary>
        /// Return relative return, calculated as r = v[0] / v[1] - 1.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>time series of linear returns</returns>
        public static TimeSeriesFloat RelReturn(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.RelReturn", series.Name);

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
                            var prev = src[0].Value;

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value / prev - 1.0));

                                prev = it.Value;
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region LogReturn
        /// <summary>
        /// Calculate logarithmic return as r = log(v[0] / v[1]).
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>time series of log returns</returns>
        public static TimeSeriesFloat LogReturn(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.LogReturn", series.Name);

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
                            var prev = src[0].Value;

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Log(it.Value / prev)));

                                prev = it.Value;
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion

        #region AbsValue
        /// <summary>
        /// Calculate absolute value of time series.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>time series of absolute values</returns>
        public static TimeSeriesFloat AbsValue(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.AbsValue", series.Name);

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

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Abs(it.Value)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region Square
        /// <summary>
        /// Calculate square of time series.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>squared time series</returns>
        public static TimeSeriesFloat Square(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.Square", series.Name);

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

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value * it.Value));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region Sqrt
        /// <summary>
        /// Calculate square root of time series.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>square root time series</returns>
        public static TimeSeriesFloat Sqrt(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.Sqrt", series.Name);

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

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Sqrt(it.Value)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region Log
        /// <summary>
        /// Calculate natural logarithm of time series.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>log time series</returns>
        public static TimeSeriesFloat Log(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.Log", series.Name);

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

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Log(it.Value)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region Exp
        /// <summary>
        /// Calculate exp of time series.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>exp time series</returns>
        public static TimeSeriesFloat Exp(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.Sqrt", series.Name);

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

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Exp(it.Value)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
    }
}

//==============================================================================
// end of file
