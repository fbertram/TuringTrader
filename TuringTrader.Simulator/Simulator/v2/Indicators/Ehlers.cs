//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Ehlers
// Description: Indicators from John F. Ehlers
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
using System.Linq;
using System.Threading.Tasks;

namespace TuringTrader.SimulatorV2.Indicators
{
    public static class Ehlers_RocketScienceForTraders
    {
        #region DominantCyclePeriod
        /// <summary>
        /// Calculate the dominant cycle period. The method is based
        /// on John F. Ehler's book 'Rocket Science for Traders' and
        /// uses complex arithmetic and a homodyne discriminator.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">length of calculation window</param>
        /// <returns>variance time series</returns>
        public static TimeSeriesFloat DominantCyclePeriod(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.DominantCyclePeriod()", series.Name);

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

                            var lookback = new LookbackManager();

                            var Price = lookback.NewLookback(src[0].Value);
                            var Smooth = lookback.NewLookback(src[0].Value);
                            var Detrender = lookback.NewLookback(0);
                            var I1 = lookback.NewLookback(0);
                            var Q1 = lookback.NewLookback(0);
                            var jI = lookback.NewLookback(0);
                            var jQ = lookback.NewLookback(0);
                            var I2 = lookback.NewLookback(0);
                            var Q2 = lookback.NewLookback(0);
                            var Re = lookback.NewLookback(0);
                            var Im = lookback.NewLookback(0);
                            var Period = lookback.NewLookback(0);
                            var SmoothPeriod = lookback.NewLookback(0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                // advance all lookbacks
                                lookback.Advance();

                                // note the complicated feedback through the
                                // period value, which makes porting to 
                                // TuringTrader's indicators tricky

                                Price.Value = src[idx].Value;

                                // this code is taken (almost) verbatim from
                                // Ehlers's book, see fig 7.2., page 68ff.

                                Smooth.Value = (4.0 * Price + 3.0 * Price[1]
                                    + 2.0 * Price[2] + Price[3])
                                    / 10.0;

                                Detrender.Value = (0.0962 * Smooth + 0.5769 * Smooth[2]
                                    - 0.5769 * Smooth[4] - 0.0962 * Smooth[6])
                                    * (0.075 * Period[1] + 0.54);

                                //--- compute in-phase and quadrature components
                                Q1.Value = (0.0962 * Detrender + 0.5769 * Detrender[2]
                                    - 0.5769 * Detrender[4] - 0.0962 * Detrender[6])
                                    * (0.075 * Period[1] + 0.54);

                                I1.Value = Detrender[3];

                                //--- advance the phase of I1 and Q1 by 90 degrees
                                jI.Value = (0.0962 * I1 + 0.5769 * I1[2]
                                    - 0.5769 * I1[4] - 0.0962 * I1[6])
                                    * (0.075 * Period[1] + 0.54);

                                jQ.Value = (0.0962 * Q1 + 0.5769 * Q1[2]
                                    - 0.5769 * Q1[4] - 0.0962 * Q1[6])
                                    * (0.075 * Period[1] + 0.54);

                                //--- phasor addition for 3-bar averaging
                                I2.Value = I1 - jQ;
                                Q2.Value = Q1 + jI;

                                //--- smooth the i and q components before applying the discriminator
                                I2.Value = 0.2 * I2 + 0.8 * I2[1];
                                Q2.Value = 0.2 * Q2 + 0.8 * Q2[1];

                                //--- homodyne discriminator
                                Re.Value = I2 * I2[1] + Q2 * Q2[1];
                                Im.Value = I2 * Q2[1] - Q2 * I2[1];

                                Re.Value = 0.2 * Re + 0.8 * Re[1];
                                Im.Value = 0.2 * Im + 0.8 * Im[1];

                                Period.Value = 2.0 * Math.PI / Math.Atan2(Im, Re);

                                if (Period > 1.5 * Period[1]) Period.Value = 1.5 * Period[1];
                                if (Period < 0.67 * Period[1]) Period.Value = 0.67 * Period[1];
                                if (Period < 6.0) Period.Value = 6.0;
                                if (Period > 50.0) Period.Value = 50.0;

                                Period.Value = 0.2 * Period + 0.8 * Period[1];
                                SmoothPeriod.Value = 0.33 * Period + 0.67 * SmoothPeriod[1];

                                dst.Add(new BarType<double>(
                                    src[idx].Date, SmoothPeriod[0]));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region SignalToNoiseRatio
        /// <summary>
        /// Calculate the signal-to-noise ratio. The method is based
        /// on John F. Ehler's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">length of calculation window</param>
        /// <returns>variance time series</returns>
        public static TimeSeriesFloat SignalToNoiseRatio(this TimeSeriesAsset series)
        {
            var name = string.Format("{0}.SignalToNoiseRatio()", series.Name);

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
                            var typ = series.TypicalPrice().Data; // Ehlers is using (H+L)/2 instead
                            var dcp = series.TypicalPrice().DominantCyclePeriod().Data;

                            var lookback = new LookbackManager();
                            var Price = lookback.NewLookback(0);
                            var Smooth = lookback.NewLookback(0);
                            var SmoothPeriod = lookback.NewLookback(0);
                            var Q3 = lookback.NewLookback(0);
                            var I3 = lookback.NewLookback(0);
                            var Signal = lookback.NewLookback(0);
                            var Noise = lookback.NewLookback(0);
                            var SNR = lookback.NewLookback(0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                // advance all lookbacks
                                lookback.Advance();

                                var H = src[idx].Value.High;
                                var L = src[idx].Value.Low;
                                Price.Value = typ[idx].Value;
                                Smooth.Value = (4 * Price + 3 * Price[1] + 2 * Price[2] + Price[3]) / 10;

                                // a lot of code removed here, using
                                // DominantCyclePeriod indicator instead
                                SmoothPeriod.Value = dcp[idx].Value;

                                // this code is taken (almost) verbatim from
                                // Ehlers's book, see fig 8.5., page 87ff.

                                Q3.Value = 0.5 * (Smooth - Smooth[2])
                                    * (0.1759 * SmoothPeriod + 0.4607);

                                var smoothPeriod_2 = (int)Math.Floor(SmoothPeriod / 2);
                                I3.Value = Enumerable.Range(0, smoothPeriod_2)
                                    .Sum(t => Q3[t])
                                    * 1.57 / Math.Max(1, smoothPeriod_2);

                                Signal.Value = I3 * I3 + Q3 * Q3;
                                Noise.Value = 0.1 * (H - L) * (H - L) * 0.25 + 0.9 * Noise[1];

                                if (Noise != 0 && Signal != 0)
                                    SNR.Value = 0.33 * (10 * Math.Log(Signal / Noise) / Math.Log(10))
                                        + 0.67 * SNR[1];

                                dst.Add(new BarType<double>(
                                    src[idx].Date, SNR[0]));
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
