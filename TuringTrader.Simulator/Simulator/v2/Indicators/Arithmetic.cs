//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Arithmetic
// Description: Arithmetic on time series.
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
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of indicators performing arithmetic on time series.
    /// </summary>
    public static class Arithmetic
    {
        #region Const
        /// <summary>
        /// Create time series with constant value.
        /// </summary>
        /// <param name="series">parent series</param>
        /// <param name="value">value</param>
        /// <returns>time series</returns>
        public static TimeSeriesFloat Const(this TimeSeriesFloat series, double value)
            => series.Owner.Const(value);

        /// <summary>
        /// Create time series with constant value.
        /// </summary>
        /// <param name="algo">parent algorithm</param>
        /// <param name="value">value</param>
        /// <returns>time series</returns>
        public static TimeSeriesFloat Const(this Algorithm algo, double value)
        {
            var name = string.Format("Const({0})", value);

            return algo.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = algo.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var dst = new List<BarType<double>>();

                            foreach (var t in algo.TradingCalendar.TradingDays)
                            {
                                dst.Add(new BarType<double>(
                                    t,
                                    value));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(algo, name, data);
                });
        }
        #endregion
        #region Add
        /// <summary>
        /// Add two time series
        /// </summary>
        /// <param name="summand1"></param>
        /// <param name="summand2"></param>
        /// <returns>sum of time series</returns>
        public static TimeSeriesFloat Add(this TimeSeriesFloat summand1, TimeSeriesFloat summand2)
        {
            var name = string.Format("{0}.Add({1})", summand1.Name, summand2.Name);

            return summand1.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = summand1.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = summand1.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value + summand2[it.Date]));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(summand1.Owner, name, data);
                });
        }

        /// <summary>
        /// Add constant value to time series.
        /// </summary>
        /// <param name="summand1"></param>
        /// <param name="summand2"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Add(this TimeSeriesFloat summand1, double summand2)
        {
            var name = string.Format("{0}.Add({1})", summand1.Name, summand2);

            return summand1.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = summand1.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = summand1.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value + summand2));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(summand1.Owner, name, data);
                });
        }
        #endregion
        #region Sub
        /// <summary>
        /// Subtract two time series
        /// </summary>
        /// <param name="minuend"></param>
        /// <param name="subtrahend"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Sub(this TimeSeriesFloat minuend, TimeSeriesFloat subtrahend)
        {
            var name = string.Format("{0}.Sub({1})", minuend.Name, subtrahend.Name);

            return minuend.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = minuend.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = minuend.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value - subtrahend[it.Date]));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(minuend.Owner, name, data);
                });
        }

        /// <summary>
        /// Add constant value to time series.
        /// </summary>
        /// <param name="minuend"></param>
        /// <param name="subtrahend"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Sub(this TimeSeriesFloat minuend, double subtrahend)
        {
            var name = string.Format("{0}.Sub({1})", minuend.Name, subtrahend);

            return minuend.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = minuend.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = minuend.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value - subtrahend));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(minuend.Owner, name, data);
                });
        }
        #endregion
        #region Mul
        /// <summary>
        /// Multiply two time series.
        /// </summary>
        /// <param name="multiplicand1"></param>
        /// <param name="multiplicand2"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Mul(this TimeSeriesFloat multiplicand1, TimeSeriesFloat multiplicand2)
        {
            var name = string.Format("{0}.Mul({1})", multiplicand1.Name, multiplicand2.Name);

            return multiplicand1.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = multiplicand1.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = multiplicand1.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value * multiplicand2[it.Date]));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(multiplicand1.Owner, name, data);
                });
        }

        /// <summary>
        /// Multiply time series with constant value.
        /// </summary>
        /// <param name="multiplicand1"></param>
        /// <param name="multiplicand2"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Mul(this TimeSeriesFloat multiplicand1, double multiplicand2)
        {
            var name = string.Format("{0}.Mul({1})", multiplicand1.Name, multiplicand2);

            return multiplicand1.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = multiplicand1.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = multiplicand1.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value * multiplicand2));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(multiplicand1.Owner, name, data);
                });
        }
        #endregion
        #region Div
        /// <summary>
        /// Divide one time series by another.
        /// </summary>
        /// <param name="dividend">input series</param>
        /// <param name="divisor">series by which to divide</param>
        /// <returns>Div series</returns>
        public static TimeSeriesFloat Div(this TimeSeriesFloat dividend, TimeSeriesFloat divisor)
        {
            var name = string.Format("{0}.Div({1})", dividend.Name, divisor.Name);

            return dividend.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = dividend.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = dividend.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value / divisor[it.Date]));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(dividend.Owner, name, data);
                });
        }
        /// <summary>
        /// Divide time series by constant value.
        /// </summary>
        /// <param name="dividend">input series</param>
        /// <param name="divisor">series by which to divide</param>
        /// <returns>Div series</returns>
        public static TimeSeriesFloat Div(this TimeSeriesFloat dividend, double divisor)
        {
            var name = string.Format("{0}.Div({1})", dividend.Name, divisor);

            return dividend.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = dividend.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = dividend.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    it.Value / divisor));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(dividend.Owner, name, data);
                });
        }
        #endregion
        #region Min
        /// <summary>
        /// Create new time series with minimum values of two series.
        /// </summary>
        /// <param name="min1"></param>
        /// <param name="min2"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Min(this TimeSeriesFloat min1, TimeSeriesFloat min2)
        {
            var name = string.Format("{0}.Min({1})", min1.Name, min2.Name);

            return min1.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = min1.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = min1.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Min(it.Value, min2[it.Date])));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(min1.Owner, name, data);
                });
        }

        /// <summary>
        /// Calculate minimum of time series and constant value.
        /// </summary>
        /// <param name="min1"></param>
        /// <param name="min2"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Min(this TimeSeriesFloat min1, double min2)
        {
            var name = string.Format("{0}.Min({1})", min1.Name, min2);

            return min1.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = min1.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = min1.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Min(it.Value, min2)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(min1.Owner, name, data);
                });
        }
        #endregion
        #region Max
        /// <summary>
        /// Create new time series w/ maximum values of two series.
        /// </summary>
        /// <param name="max1"></param>
        /// <param name="max2"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Max(this TimeSeriesFloat max1, TimeSeriesFloat max2)
        {
            var name = string.Format("{0}.Max({1})", max1.Name, max2.Name);

            return max1.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = max1.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = max1.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Max(it.Value, max2[it.Date])));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(max1.Owner, name, data);
                });
        }

        /// <summary>
        /// Create new time series w/ maximum of input time series and input value.
        /// </summary>
        /// <param name="max1"></param>
        /// <param name="max2"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Max(this TimeSeriesFloat max1, double max2)
        {
            var name = string.Format("{0}.Max({1})", max1.Name, max2);

            return max1.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = max1.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = max1.Data;
                            var dst = new List<BarType<double>>();

                            foreach (var it in src)
                            {
                                dst.Add(new BarType<double>(
                                    it.Date,
                                    Math.Max(it.Value, max2)));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(max1.Owner, name, data);
                });
        }
        #endregion
    }
}

//==============================================================================
// end of file
