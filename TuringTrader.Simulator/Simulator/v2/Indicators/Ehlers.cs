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
using static TuringTrader.SimulatorV2.Indicators.Trend;

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of indicators from John F. Ehlers's
    /// book 'Rocket Science for Traders.'
    /// </summary>
    public static class Ehlers_RocketScienceForTraders
    {
        #region Detrend
        /// <summary>
        /// Detrend input signal with a Hilbert Transformer, according
        /// to John F. Ehlers's book 'Rocket Science for Traders'. 
        /// Note that the detrender's frequency response is not flat. 
        /// To remedy this, Ehlers typically corrects the output by a 
        /// factor of 0.075 * Period + 0.54.
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Detrend(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.Detrend", series.Name);

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

                            var lookback = new LookbackGroup();
                            var input = lookback.NewLookback(0.0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();
                                input.Value = src[idx].Value;

                                var detrender = (0.0962 * input + 0.5769 * input[2]
                                    - 0.5769 * input[4] - 0.0962 * input[6])
                                    * (0.075 * input[1] + 0.54);

                                dst.Add(new BarType<double>(
                                    src[idx].Date, detrender));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region Distance
        /// <summary>
        /// Distance indicator, according to John F. Ehlers's 
        /// book 'Rocket Science for Traders'. Ehlers uses this
        /// indicator as coefficients for an Ehlers Filter.
        /// </summary>
        /// <param name="series"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static TimeSeriesFloat Distance(this TimeSeriesFloat series, int n)
        {
            var name = string.Format("{0}.Distance({1})", series.Name, n);

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

                            var lookback = new LookbackGroup();
                            var input = lookback.NewLookback(0.0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();
                                input.Value = src[idx].Value;

                                // this logic taken from
                                // Ehlers's book, see fig 18.6., page 193.
                                var distance = Enumerable.Range(1, n - 1)
                                    .Sum(t => Math.Pow(input[0] - input[t], 2.0));

                                dst.Add(new BarType<double>(
                                    src[idx].Date, distance));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region DominantCyclePeriod
        /// <summary>
        /// Calculate the dominant cycle period. The method is based
        /// on John F. Ehlers's book 'Rocket Science for Traders' and
        /// uses complex arithmetic and a homodyne discriminator.
        /// </summary>
        /// <param name="series">input series</param>
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

                            var lookback = new LookbackGroup();

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
        /// on John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">input series</param>
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

                            var lookback = new LookbackGroup();
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
        #region SinewaveIndicator
        /// <summary>
        /// Calculate the Sinewave Indicator. The method is based
        /// on John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>Sinewave indicator container</returns>
        public static SinewaveIndicatorT SinewaveIndicator(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.SinewaveIndicator()", series.Name);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dcp = series.DominantCyclePeriod().Data;
                            var dstPhase = new List<BarType<double>>();
                            var dstSine = new List<BarType<double>>();
                            var dstLead = new List<BarType<double>>();

                            var lookback = new LookbackGroup();
                            var Price = lookback.NewLookback(0);
                            var SmoothPeriod = lookback.NewLookback(0);
                            var SmoothPrice = lookback.NewLookback(0);
                            var RealPart = lookback.NewLookback();
                            var ImagPart = lookback.NewLookback();
                            var DCPhase = lookback.NewLookback();

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();

                                Price.Value = src[idx].Value;
                                // a lot of code removed here, using
                                // DominantCyclePeriod indicator instead
                                SmoothPeriod.Value = dcp[idx].Value;

                                // this code is taken (almost) verbatim from
                                // Ehlers's book, see fig 9.3., page 101ff.

                                //--- compute dominant cycle phase
                                SmoothPrice.Value = (4 * Price + 3 * Price[1]
                                    + 2 * Price[2] + Price[3]) / 10;

                                var DCPeriod = (int)Math.Floor(SmoothPeriod + 0.5);

                                RealPart.Value = Enumerable.Range(0, DCPeriod)
                                    .Sum(t => Math.Cos(2 * Math.PI / DCPeriod * t) * SmoothPrice[t]);

                                ImagPart.Value = Enumerable.Range(0, DCPeriod)
                                    .Sum(t => Math.Sin(2 * Math.PI / DCPeriod * t) * SmoothPrice[t]);

                                DCPhase.Value = 180 / Math.PI * Math.Atan2(ImagPart, RealPart) + 90;

                                //--- compensate for one bar lag of the weighted moving average
                                DCPhase.Value = DCPhase + 360 / SmoothPeriod;

                                // coerce phase between -45 and +315 degrees
                                if (DCPhase.Value < -45) DCPhase.Value = DCPhase + 360;
                                if (DCPhase.Value > 315) DCPhase.Value = DCPhase - 360;

                                var sine = Math.Sin(Math.PI / 180 * DCPhase);
                                var lead = Math.Sin(Math.PI / 180 * (DCPhase + 45));

                                dstPhase.Add(new BarType<double>(
                                    src[idx].Date, DCPhase));

                                dstSine.Add(new BarType<double>(
                                    src[idx].Date, sine));

                                dstLead.Add(new BarType<double>(
                                    src[idx].Date, lead));
                            }

                            return (object)Tuple.Create(dstPhase, dstSine, dstLead);
                        }));

                    return new SinewaveIndicatorT(
                        new TimeSeriesFloat(
                            series.Owner, name + ".Phase",
                            data,
                            (data) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)data).Item1),
                        new TimeSeriesFloat(
                            series.Owner, name + ".Sine",
                            data,
                            (data) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)data).Item2),
                        new TimeSeriesFloat(
                            series.Owner, name + ".LeadSine",
                            data,
                            (data) => ((Tuple<List<BarType<double>>, List<BarType<double>>, List<BarType<double>>>)data).Item3));
                });
        }

        /// <summary>
        /// Container for Sinewave indicator result.
        /// </summary>
        public class SinewaveIndicatorT
        {
            /// <summary>
            /// Dominant cycle phase. Will hover near 0 in downtrends and
            /// near 180 degrees in uptrends
            /// </summary>
            public TimeSeriesFloat Phase;
            /// <summary>
            /// Dominant cycle's sine wave output.
            /// </summary>
            public TimeSeriesFloat Sine;
            /// <summary>
            /// Dominant cycle's leading sine wave output.
            /// </summary>
            public TimeSeriesFloat LeadSine;

            /// <summary>
            /// Create new container.
            /// </summary>
            /// <param name="phase"></param>
            /// <param name="sine"></param>
            /// <param name="leadsine"></param>
            public SinewaveIndicatorT(TimeSeriesFloat phase, TimeSeriesFloat sine, TimeSeriesFloat leadsine)
            {
                Phase = phase;
                Sine = sine;
                LeadSine = leadsine;
            }
        }

        #endregion
        #region InstantaneousTrendline
        /// <summary>
        /// Calculate the Instantaneous Trendline. The method is based
        /// on John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>variance time series</returns>
        public static TimeSeriesFloat InstantaneousTrendline(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.InstantaneousTrendline()", series.Name);

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
                            var dcp = series.DominantCyclePeriod().Data;

                            var lookback = new LookbackGroup();
                            var Price = lookback.NewLookback(0);
                            var Smooth = lookback.NewLookback(0);
                            var SmoothPeriod = lookback.NewLookback(0);
                            var ITrend = lookback.NewLookback(0);
                            var Trendline = lookback.NewLookback(0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();

                                Price.Value = src[idx].Value;

                                // a lot of code removed here, using
                                // DominantCyclePeriod indicator instead
                                SmoothPeriod.Value = dcp[idx].Value;

                                // this code is taken (almost) verbatim from
                                // Ehlers's book, see fig 10.1., page 109ff.

                                //--- compute trendline as a simple average over
                                //    the measured dominant cycle period
                                var DCPeriod = (int)Math.Floor(SmoothPeriod + 0.5);

                                if (DCPeriod > 0)
                                    ITrend.Value = Enumerable.Range(0, DCPeriod)
                                        .Average(t => Price[t]);

                                Trendline.Value = (4 * ITrend + 3 * ITrend[1]
                                    + 2 * ITrend[2] + ITrend[3]) / 10;

                                dst.Add(new BarType<double>(
                                    src[idx].Date, Trendline));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region MarketMode
        /// <summary>
        /// Calculate the market mode. The method is based
        /// on John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">input series</param>
        /// <returns>variance time series</returns>
        public static TimeSeriesFloat MarketMode(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.MarketMode()", series.Name);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var srcSeries = series.Data;
                            var srcPeriod = series.DominantCyclePeriod().Data;
                            var srcPhase = series.SinewaveIndicator().Phase.Data;
                            var srcSine = series.SinewaveIndicator().Sine.Data;
                            var srcLeadSine = series.SinewaveIndicator().LeadSine.Data;
                            var srcTrendline = series.InstantaneousTrendline().Data;
                            var dst = new List<BarType<double>>();

                            var lbg = new LookbackGroup();
                            var Price = lbg.NewLookback(0);
                            var SmoothPrice = lbg.NewLookback(0);
                            var SmoothPeriod = lbg.NewLookback(0);
                            var ITrend = lbg.NewLookback(0);
                            var Trendline = lbg.NewLookback(0);
                            var Trend = lbg.NewLookback();
                            var DaysInTrend = lbg.NewLookback();
                            var DCPhase = lbg.NewLookback();
                            var Sine = lbg.NewLookback();
                            var LeadSine = lbg.NewLookback();

                            for (int idx = 0; idx < srcSeries.Count; idx++)
                            {
                                lbg.Advance();

                                Price.Value = srcSeries[idx].Value;

                                // a lot of code removed here, using
                                // DominantCyclePeriod indicator instead
                                SmoothPeriod.Value = srcPeriod[idx].Value;

                                // code removed here, using
                                // Sinewave indicator instead
                                SmoothPrice.Value = (4 * Price + 3 * Price[1]
                                    + 2 * Price[2] + Price[3]) / 10;

                                DCPhase.Value = srcPhase[idx].Value;
                                Sine.Value = srcSine[idx].Value;
                                LeadSine.Value = srcLeadSine[idx].Value;

                                // more code removed here, using 
                                // InstantaneousTrendline indicator instead
                                Trendline.Value = srcTrendline[idx].Value;

                                // this code is taken (almost) verbatim from
                                // Ehlers's book, see fig 11.1., page 114ff.

                                //--- assume trend mode
                                Trend.Value = 1;

                                //--- measure days in trend from last crossing of the
                                //    sinewave indicator lines

                                if ((Sine > LeadSine && Sine[1] <= LeadSine[1])
                                    || (Sine < LeadSine && Sine[1] >= LeadSine[1]))
                                {
                                    DaysInTrend.Value = 0;
                                    Trend.Value = 0;
                                }
                                DaysInTrend.Value = DaysInTrend + 1;
                                if (DaysInTrend < 0.5 * SmoothPeriod) Trend.Value = 0;

                                //--- cycle mode if delta phase is +/- 50% of
                                //    dominant cycle change of phase
                                if (SmoothPeriod != 0
                                    && DCPhase - DCPhase[1] > 0.67 * 360 / SmoothPeriod
                                    && DCPhase - DCPhase[1] < 1.5 * 360 / SmoothPeriod)
                                    Trend.Value = 0;

                                //--- trend mode if prices are widely separated
                                //    from the trend line
                                if (Math.Abs((SmoothPrice - Trendline) / Trendline) > 0.015)
                                    Trend.Value = 1;

                                dst.Add(new BarType<double>(
                                    srcSeries[idx].Date, Trend));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region EhlersFilter
        /// <summary>
        /// Calculate Ehlers Filter, as described in
        /// John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">source series</param>
        /// <param name="coefficients">coefficient series</param>
        /// <param name="n">filter length</param>
        /// <returns>Ehlers Filter time series</returns>
        public static TimeSeriesFloat EhlersFilter(this TimeSeriesFloat series, TimeSeriesFloat coefficients, int n)
        {
            var name = string.Format("{0}.EhlersFilter({1},{2})", series.Name, coefficients.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var flt = coefficients.Data;
                            var dst = new List<BarType<double>>();

                            var lookback = new LookbackGroup();
                            var input = lookback.NewLookback(src[0].Value);
                            var filter = lookback.NewLookback(flt[0].Value);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();

                                input.Value = src[idx].Value;
                                filter.Value = flt[idx].Value;

                                var output = Enumerable.Range(0, n)
                                    .Sum(t => input[t] * filter[t])
                                    / Enumerable.Range(0, n)
                                    .Sum(t => filter[t]);

                                dst.Add(new BarType<double>(
                                    src[idx].Date, output));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });

        }
        #endregion
        #region DistanceCoefficientEhlersFilter
        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static TimeSeriesFloat DistanceCoefficientEhlersFilter(this TimeSeriesFloat series, int n)
            // see Ehlers's book, see fig 18.6., page 193.
            => series.EhlersFilter(series.Distance(n), n);
        #endregion
        #region OptimumPredictor
        /// <summary>
        /// Calculate Optimum Predictor, as described in
        /// John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">source series</param>
        /// <returns>Optimum Predictor time series</returns>
        public static OptimumPredictorT OptimumPredictor(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.OptimumPredictor", series.Name);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.Data;
                            var dcp = series.DominantCyclePeriod().Data;
                            var predict = new List<BarType<double>>();
                            var signal = new List<BarType<double>>();

                            var lookback = new LookbackGroup();
                            var Price = lookback.NewLookback(0);
                            var Smooth = lookback.NewLookback(0);
                            var SmoothPeriod = lookback.NewLookback(0);
                            var Detrender2 = lookback.NewLookback(0);
                            var Smooth2 = lookback.NewLookback(0);
                            var DetrendEMA = lookback.NewLookback(0);
                            var Predict = lookback.NewLookback(0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();

                                Price.Value = src[idx].Value;
                                Smooth.Value = (4 * Price + 3 * Price[1] + 2 * Price[2] + Price[3]) / 10;

                                // a lot of code removed here, using
                                // DominantCyclePeriod indicator instead
                                SmoothPeriod.Value = dcp[idx].Value;

                                // this code is taken (almost) verbatim from
                                // Ehlers's book, see fig 20.2., page 209ff.

                                //--- optimum predictor
                                Detrender2.Value = 0.5 * Smooth - 0.5 * Smooth[2];
                                Smooth2.Value = (4 * Detrender2 + 3 * Detrender2[1]
                                    + 2 * Detrender2[2] + Detrender2[3]) / 10;

                                // FIXME: Ehlers uses Period here, which is hidden
                                //        inside the DominantCyclePeriod indicator.
                                //        This will likely introduce some lag.
                                var alpha = 1 - Math.Exp(-6.28 / SmoothPeriod);

                                DetrendEMA.Value = alpha * Smooth2
                                    + (1 - alpha) * DetrendEMA[1];

                                Predict.Value = 1.4 * (Smooth2 - DetrendEMA);

                                predict.Add(new BarType<double>(
                                    src[idx].Date, Predict));

                                signal.Add(new BarType<double>(
                                    src[idx].Date, Smooth2));
                            }

                            return (object)Tuple.Create(predict, signal);
                        }));

                    return new OptimumPredictorT(
                        new TimeSeriesFloat(
                            series.Owner, name + ".Predict",
                            data,
                            (data) => ((Tuple<List<BarType<double>>, List<BarType<double>>>)data).Item1),
                        new TimeSeriesFloat(
                            series.Owner, name + ".Signal",
                            data,
                            (data) => ((Tuple<List<BarType<double>>, List<BarType<double>>>)data).Item2));
                });

        }

        /// <summary>
        /// Container for Optimum Predictor indicator
        /// </summary>
        public class OptimumPredictorT
        {
            /// <summary>
            /// Optimum predictor output
            /// </summary>
            public TimeSeriesFloat Predict;
            /// <summary>
            /// Optimum predictor signal line
            /// </summary>
            public TimeSeriesFloat Signal;

            /// <summary>
            /// Create Optimum Predictor container.
            /// </summary>
            /// <param name="predict"></param>
            /// <param name="signal"></param>
            public OptimumPredictorT(TimeSeriesFloat predict, TimeSeriesFloat signal)
            {
                Predict = predict;
                Signal = signal;
            }
        };
        #endregion
        #region PredictiveMovingAverage
        /// <summary>
        /// Calculate predictive moving average as described in
        /// John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static PredictiveMovingAverageT PredictiveMovingAverage(this TimeSeriesFloat series, int n = 7)
        {
            var name = string.Format("{0}.PredictiveMovingAverage({1})", series.Name, n);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    // see Ehlers's book, see fig 20.4., page 212ff.
                    var wma1 = series.WMA(7);
                    var wma2 = wma1.WMA(7);
                    var predict = wma1.Mul(2).Sub(wma2);
                    var trigger = predict.WMA(4);

                    return new PredictiveMovingAverageT(predict, trigger);
                });
        }
        public class PredictiveMovingAverageT
        {
            public TimeSeriesFloat Trigger;
            public TimeSeriesFloat Predict;

            public PredictiveMovingAverageT(TimeSeriesFloat predict, TimeSeriesFloat trigger)
            {
                Trigger = trigger;
                Predict = predict;
            }
        }
        #endregion
        #region AdaptiveRSI
        /// <summary>
        /// Calculate adaptive RSI. The method is based
        /// on John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="CycPart"></param>
        /// <returns>variance time series</returns>
        public static TimeSeriesFloat AdaptiveRSI(this TimeSeriesFloat series, double CycPart = 0.5)
        {
            var name = string.Format("{0}.AdaptiveRSI({1})", series.Name, CycPart);

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
                            var dcp = series.DominantCyclePeriod().Data;

                            var lookback = new LookbackGroup();
                            var Close = lookback.NewLookback(0);
                            var RSI = lookback.NewLookback(0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();

                                Close.Value = src[idx].Value;

                                // a lot of code removed here, using
                                // DominantCyclePeriod indicator instead
                                var SmoothPeriod = dcp[idx].Value;

                                // this code is adapted from
                                // Ehlers's book, see fig 22.1., page 230ff.

                                var CU = Enumerable.Range(0, (int)Math.Floor(CycPart * SmoothPeriod))
                                    .Sum(t => Math.Max(0.0, Close[t] - Close[t + 1]));

                                var CD = Enumerable.Range(0, (int)Math.Floor(CycPart * SmoothPeriod))
                                    .Sum(t => Math.Max(0.0, Close[t + 1] - Close[t]));

                                if (CU + CD != 0) RSI.Value = 100 * CU / (CU + CD);

                                dst.Add(new BarType<double>(
                                    src[idx].Date, RSI[0]));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region AdaptiveStochastic
        /// <summary>
        /// Calculate adaptive Stochastic. The method is based
        /// on John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="CycPart"></param>
        /// <returns>variance time series</returns>
        public static TimeSeriesFloat AdaptiveStochastic(this TimeSeriesAsset series, double CycPart = 0.5)
        {
            var name = string.Format("{0}.AdaptiveStochastic({1})", series.Name, CycPart);

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
                            var dcp = series.TypicalPrice().DominantCyclePeriod().Data;

                            var lookback = new LookbackGroup();
                            var Close = lookback.NewLookback(0);
                            var H = lookback.NewLookback(0);
                            var L = lookback.NewLookback(0);
                            var HH = lookback.NewLookback(0);
                            var LL = lookback.NewLookback(0);
                            var Stochastic = lookback.NewLookback(0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();

                                Close.Value = src[idx].Value.Close;
                                H.Value = src[idx].Value.High;
                                L.Value = src[idx].Value.Low;

                                // a lot of code removed here, using
                                // DominantCyclePeriod indicator instead
                                var SmoothPeriod = dcp[idx].Value;

                                // this code is adapted from
                                // Ehlers's book, see fig 22.2., page 233ff.

                                var r = (int)Math.Floor(CycPart * SmoothPeriod);

                                if (r > 0)
                                {
                                    HH.Value = Enumerable.Range(0, r).Max(t => H[t]);
                                    LL.Value = Enumerable.Range(0, r).Min(t => L[t]);
                                }

                                if (HH - LL != 0) Stochastic.Value = (Close - LL) / (HH - LL);

                                dst.Add(new BarType<double>(
                                    src[idx].Date, 100 * Stochastic));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region AdaptiveCCI
        /// <summary>
        /// Calculate adaptive CCI. The method is based
        /// on John F. Ehlers's book 'Rocket Science for Traders'.
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="CycPart"></param>
        /// <returns>variance time series</returns>
        public static TimeSeriesFloat AdaptiveCCI(this TimeSeriesAsset series, double CycPart = 1.0)
        {
            var name = string.Format("{0}.AdaptiveCCI({1})", series.Name, CycPart);

            return series.Owner.ObjectCache.Fetch(
                name,
                () =>
                {
                    var data = series.Owner.DataCache.Fetch(
                        name,
                        () => Task.Run(() =>
                        {
                            var src = series.TypicalPrice().Data;
                            var dcp = series.TypicalPrice().DominantCyclePeriod().Data;
                            var dst = new List<BarType<double>>();

                            var lookback = new LookbackGroup();
                            var MedianPrice = lookback.NewLookback(0);
                            var Avg = lookback.NewLookback(0);
                            var MD = lookback.NewLookback(0);
                            var CCI = lookback.NewLookback(0);

                            for (int idx = 0; idx < src.Count; idx++)
                            {
                                lookback.Advance();

                                MedianPrice.Value = src[idx].Value;

                                // a lot of code removed here, using
                                // DominantCyclePeriod indicator instead
                                var SmoothPeriod = dcp[idx].Value;

                                // this code is adapted from
                                // Ehlers's book, see fig 22.2., page 233ff.

                                // FIXME: Ehlers is using Period here, which is
                                //        hidden inside DominantCyclePeriod
                                var Length = (int)Math.Floor(CycPart * SmoothPeriod);

                                if (Length > 0)
                                {
                                    Avg.Value = Enumerable.Range(0, Length)
                                        .Average(t => MedianPrice[t]);

                                    MD.Value = Enumerable.Range(0, Length)
                                        .Average(t => Math.Abs(MedianPrice[t] - Avg));
                                }

                                if (MD != 0) CCI.Value = (MedianPrice - Avg) / (0.015 * MD);

                                dst.Add(new BarType<double>(
                                    src[idx].Date, CCI));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
    }

    /// <summary>
    /// Collection of indicators from John F. Ehlers's
    /// website, see
    /// <href="https://www.mesasoftware.com/papers/"/>
    /// and
    /// <href="https://www.mesasoftware.com/TechnicalArticles.htm"/>
    /// </summary>
    public static class Ehlers_TechnicalPapers
    {
        #region NoiseEliminationTechnology
        /// <summary>
        /// Calculate Noise Elimination Techology (NET) as proposed by
        /// John F. Ehlers in his paper 'NET - Noise Eliminating Technolgy,
        /// Clarify Your Indicators using Kendall Correlation.'
        /// see <href="https://www.mesasoftware.com/papers/Noise%20Elimination%20Technology.pdf"/>
        /// and <href="https://en.wikipedia.org/wiki/Kendall_rank_correlation_coefficient"/>
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="n">NET length</param>
        /// <returns>noise-reduced time series, ranging between -1 and +1</returns>
        public static TimeSeriesFloat NoiseEliminationTechnology(this TimeSeriesFloat series, int n = 14)
        {
            var name = string.Format("{0}.NoiseEliminationTechnology({1})", series.Name, n);

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

                            var lbg = new LookbackGroup();
                            var x = lbg.NewLookback(src[0].Value);

                            foreach (var it in src)
                            {
                                lbg.Advance();
                                x.Value = it.Value;

#if false
                                var num = Enumerable.Range(2, n - 1)
                                    .Sum(j => Enumerable.Range(1, j - 1)
                                        .Sum(i => Math.Sign(x[i] - x[j])));
#else

                                // code taken (almost) verbatim
                                // from Ehlers's paper
                                var num = 0.0;
                                for (var count = 2; count <= n; count++)
                                    for (var k = 1; k <= count - 1; k++)
                                        num = num - Math.Sign(x[count] - x[k]);
#endif
                                var denom = 0.5 * n * (n - 1);

                                var net = num / denom;

                                dst.Add(new BarType<double>(it.Date, net));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region ErrorCorrectedEMA
        /// <summary>
        /// Calculate Error-Corrected Exponential Moving Average, as 
        /// proposed by John F. Ehlers and Ric Way in their paper
        /// 'Zero Lag (well, almost).'
        /// see <href="https://www.mesasoftware.com/papers/ZeroLag.pdf"/>
        /// </summary>
        /// <param name="series">input series</param>
        /// <param name="length">filter length</param>
        /// <param name="gainLimit">max gain for error term, scaled by 10x</param>
        /// <returns>Error-Corrected EMA series</returns>
        public static TimeSeriesFloat ErrorCorrectedEMA(this TimeSeriesFloat series, int length = 20, int gainLimit = 50)
        {
            var name = string.Format("{0}.ErrorCorrectedEMA({1},{2})", series.Name, length, gainLimit);

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

                            var lbg = new LookbackGroup();
                            var EMA = lbg.NewLookback(src[0].Value);
                            var EC = lbg.NewLookback(src[0].Value);

                            var alpha = 2.0 / (1.0 + length);

                            foreach (var it in src)
                            {
                                lbg.Advance();
                                var Close = it.Value;

                                // this code is taken (almost) verbatim
                                // from Ehlers paper, fig 5.

                                EMA.Value = alpha * Close + (1 - alpha) * EMA[1];

                                var LeastError = 1e99;
                                var BestGain = 0.0;
                                for (var gain10x = -gainLimit; gain10x <= gainLimit; gain10x++)
                                {
                                    var Gain = gain10x / 10.0;
                                    EC.Value = alpha * (EMA + Gain * (Close - EC[1]))
                                        + (1 - alpha) * EC[1];
                                    var Error = Math.Abs(Close - EC);

                                    if (Error < LeastError)
                                    {
                                        LeastError = Error;
                                        BestGain = Gain;
                                    }
                                }

                                EC.Value = alpha * (EMA + BestGain * (Close - EC[1]))
                                    + (1 - alpha) * EC[1];

                                dst.Add(new BarType<double>(it.Date, EC));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region InverseFisherTransform
        /// <summary>
        /// Calculate Inverse Fisher Transformation, as proposed by
        /// John F. Ehlers in his paper 'The Inverse Fisher Transform'
        /// see <href="https://www.mesasoftware.com/papers/TheInverseFisherTransform.pdf"/>
        /// </summary>
        /// <param name="series">input series, range should be between -5 and +5</param>
        /// <returns></returns>
        public static TimeSeriesFloat InverseFisherTransform(this TimeSeriesFloat series)
        {
            var name = string.Format("{0}.InverseFisherTransform()", series.Name);

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
                                // see Ehlers's paper, fig 4.
                                var inverseFisher = (Math.Exp(2 * it.Value) - 1)
                                    / (Math.Exp(2 * it.Value) + 1);

                                dst.Add(new BarType<double>(it.Date, inverseFisher));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data);
                });
        }
        #endregion
        #region CyberCycle
        // see https://www.mesasoftware.com/papers/TheInverseFisherTransform.pdf
        public static TimeSeriesFloat CyberCycle(this TimeSeriesFloat series, double alpha = 0.07)
        {
            var name = string.Format("{0}.CyberCycle({1})", series.Name, alpha);

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

                            var lbg = new LookbackGroup();
                            var Price = lbg.NewLookback(src[0].Value);
                            var Smooth = lbg.NewLookback(src[0].Value);
                            var Cycle = lbg.NewLookback();

                            foreach (var it in src)
                            {
                                lbg.Advance();
                                Price.Value = it.Value;

                                // this code is taken (almost) verbatim
                                // from Ehlers's paper, fig 4.
                                Smooth.Value = (Price + 2 * Price[1] + 2 * Price[2] + Price[3]) / 6;

                                Cycle.Value = (1 - 0.5 * alpha) * (1 - 0.5 * alpha)
                                    * (Smooth - 2 * Smooth[1] + Smooth[2])
                                    + 2 * (1 - alpha) * Cycle[1]
                                    - (1 - alpha) * (1 - alpha) * Cycle[2];

                                dst.Add(new BarType<double>(it.Date, Cycle));
                            }

                            return dst;
                        }));

                    return new TimeSeriesFloat(series.Owner, name, data)
                        .InverseFisherTransform();
                });
        }
        #endregion
        #region xxx CGOscillator
        // see https://www.mesasoftware.com/papers/TheCGOscillator.pdf
        #endregion
        #region xxx DMH
        // see https://www.mesasoftware.com/papers/DMH.pdf
        #endregion
    }
}

//==============================================================================
