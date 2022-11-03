//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Basic
// Description: Dummy indicators for API development.
// History:     2022xi02, FUB, created
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
using TuringTrader.SimulatorV2;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of basic indicators.
    /// </summary>
    public static class Basic
    {
        #region Lambda
        #endregion
        #region Const
        #endregion
        #region Delay
        #endregion

        #region Highest
        public static TimeSeriesFloat Highest(this TimeSeriesFloat series, int n)
        {
            List<BarType<double>> calcIndicator()
            {
                var src = series.Data.Result;
                var dst = new List<BarType<double>>();

                var window = new Queue<double>();
                for (var i = 0; i < n; i++)
                    window.Enqueue(src[0].Value);

                for (int idx = 0; idx < src.Count; idx++)
                {
                    window.Enqueue(src[idx].Value);
                    window.Dequeue();

                    var sma = window.Max(w => w);
                    dst.Add(new BarType<double>(src[idx].Date, sma));
                }

                return dst;
            }

            var name = string.Format("{0}.Highest({1})", series.Name, n);
            return new TimeSeriesFloat(
                series.Algorithm,
                name,
                series.Algorithm.Cache(name, calcIndicator));
        }
        #endregion
        #region Lowest
        #endregion
        #region Range
        #endregion

        #region AbsReturn
        #endregion
        #region LinReturn
        public static TimeSeriesFloat LinReturn(this TimeSeriesFloat series)
        {
            List<BarType<double>> calcIndicator()
            {
                var src = series.Data.Result;
                var dst = new List<BarType<double>>();
                var prev = src[0].Value;

                foreach (var it in src)
                {
                    dst.Add(new BarType<double>(it.Date, it.Value / prev - 1.0));
                    prev = it.Value;
                }

                return dst;
            }

            var name = string.Format("{0}.LinReturn", series.Name);
            return new TimeSeriesFloat(
                series.Algorithm,
                name,
                series.Algorithm.Cache(name, calcIndicator));
        }
        #endregion
        #region LogReturn
        #endregion

        #region AbsValue
        public static TimeSeriesFloat AbsValue(this TimeSeriesFloat series)
        {
            List<BarType<double>> calcIndicator()
            {
                var src = series.Data.Result;
                var dst = new List<BarType<double>>();

                foreach (var it in src)
                {
                    dst.Add(new BarType<double>(it.Date, Math.Abs(it.Value)));
                }

                return dst;
            }

            var name = string.Format("{0}.AbsValue", series.Name);
            return new TimeSeriesFloat(
                series.Algorithm,
                name,
                series.Algorithm.Cache(name, calcIndicator));
        }
        #endregion
        #region Square
        #endregion
        #region Sqrt
        #endregion
        #region Log
        public static TimeSeriesFloat Log(this TimeSeriesFloat series)
        {
            List<BarType<double>> calcIndicator()
            {
                var src = series.Data.Result;
                var dst = new List<BarType<double>>();

                foreach (var it in src)
                {
                    dst.Add(new BarType<double>(it.Date, Math.Log(it.Value)));
                }

                return dst;
            }

            var name = string.Format("{0}.Log", series.Name);
            return new TimeSeriesFloat(
                series.Algorithm,
                name,
                series.Algorithm.Cache(name, calcIndicator));
        }
        #endregion
        #region Exp
        #endregion
    }
}

//==============================================================================
// end of file
