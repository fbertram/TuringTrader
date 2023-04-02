//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        SimpleTimeSeries
// Description: Simple lookback class. This is helpful for creating indicators
//              and to port strategies from other backtesters and languages.
// History:     2023iv01, FUB, created
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

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Manager class for lookback series.
    /// </summary>
    public class LookbackGroup
    {
        private List<_lookback> _instances = new List<_lookback>();

        #region Lookback
        /// <summary>
        /// Simple lookback class
        /// </summary>
        public abstract class Lookback
        {
            /// <summary>
            /// List of historical values.
            /// </summary>
            protected List<double> values = new List<double>();

            /// <summary>
            /// Create and initialize new loockback object.
            /// </summary>
            /// <param name="init"></param>
            protected Lookback(double init)
                => values.Add(init);

            /// <summary>
            /// Retrieve value from lookback.
            /// </summary>
            /// <param name="offset">lookback offset, 0 is current, 1 is previous</param>
            /// <returns>value at time offset</returns>
            public double this[int offset]
                => values[Math.Min(values.Count - 1, offset)];

            /// <summary>
            /// Get or set the current value of lookback.
            /// </summary>
            public double Value
            {
                get => values[0];
                set => values[0] = value;
            }

            /// <summary>
            /// Get the current value of lookback.
            /// </summary>
            /// <param name="s"></param>
            public static implicit operator double(Lookback s) => s[0];
        }
        #endregion
        #region _lookback
        private class _lookback : Lookback
        {
            public _lookback(double init) : base(init) { }

            public void Advance()
            {
                values.Insert(0, values[0]);

                if (values.Count > 256)
                    values.Remove(values.Count - 1);
            }
        }
        #endregion

        /// <summary>
        /// Create new lookback series.
        /// </summary>
        /// <param name="init">initial value</param>
        /// <returns>newly created lookback</returns>
        public Lookback NewLookback(double init = 0.0)
        {
            var i = new _lookback(init);
            _instances.Add(i);

            return i;
        }

        /// <summary>
        /// Advance all lookbacks managed by this instance.
        /// </summary>
        public void Advance()
        {
            foreach (var i in _instances)
                i.Advance();
        }
    }
    #region helpers to port from TradeStation/ EasyLanguage
    /// <summary>
    /// Helper class to port strategies from TradeStation/ EasyLanguage
    /// </summary>
    public class TradeStation
    {
        #region trigonometry
        public static double Sine(double angle) => Math.Sin(Math.PI / 180.0 * angle);
        public static double Cosine(double angle) => Math.Cos(Math.PI / 180.0 * angle);
        public static double ArcTangent(double f)
            // Ehlers' code expects ArcTangent to return angles between
            // 0 and 180 degrees. In contrast, Math.Atan returns angles
            // between -Pi/2 and +Pi/2.
            => 180.0 / Math.PI * Math.Atan(f) + (f < 0.0 ? 180.0 : 0.0);
        //=> 180.0 / Math.PI * Math.Atan(f);
        #endregion
    }
    #endregion
}

//==============================================================================
// end of file
