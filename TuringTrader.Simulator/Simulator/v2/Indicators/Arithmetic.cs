//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Arithmetic
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

using System.Collections.Generic;
using TuringTrader.SimulatorV2;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of indicators performing arithmetic on time series.
    /// </summary>
    public static class Arithmetic
    {
        #region Add
        #endregion
        #region Subtract
        #endregion
        #region Multiply
        #endregion
        #region Divide
        /// <summary>
        /// Divide one time series by another.
        /// </summary>
        /// <param name="dividend">input series</param>
        /// <param name="divisor">series by which to divide</param>
        /// <returns>Div series</returns>
        public static TimeSeriesFloat Div(this TimeSeriesFloat dividend, TimeSeriesFloat divisor)
        {
            List<BarType<double>> calcIndicator()
            {
                var src = dividend.Data.Result;
                var dst = new List<BarType<double>>();

                foreach (var it in src)
                {
                    dst.Add(new BarType<double>(it.Date, it.Value / divisor[it.Date]));
                }

                return dst;
            }

            var name = string.Format("{0}.Div({1})", dividend.Name, divisor.Name);
            return new TimeSeriesFloat(
                dividend.Algorithm,
                name,
                dividend.Algorithm.Cache(name, calcIndicator));
        }
        /// <summary>
        /// Divide time series by constant value.
        /// </summary>
        /// <param name="dividend">input series</param>
        /// <param name="divisor">series by which to divide</param>
        /// <returns>Div series</returns>
        public static TimeSeriesFloat Div(this TimeSeriesFloat dividend, float divisor)
        {
            List<BarType<double>> calcIndicator()
            {
                var src = dividend.Data.Result;
                var dst = new List<BarType<double>>();

                foreach (var it in src)
                {
                    dst.Add(new BarType<double>(it.Date, it.Value / divisor));
                }

                return dst;
            }

            var name = string.Format("{0}.Div({1})", dividend.Name, divisor);
            return new TimeSeriesFloat(
                dividend.Algorithm,
                name,
                dividend.Algorithm.Cache(name, calcIndicator));
        }
        #endregion
        #region Min
        #endregion
        #region Max
        #endregion
    }
}

//==============================================================================
// end of file
