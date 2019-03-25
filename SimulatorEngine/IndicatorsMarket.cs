//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsMarket
// Description: collection of market indicators
// History:     2018ix17, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Collection of market indicators.
    /// </summary>
    public static class IndicatorsMarket
    {
        #region public static ITimeSeries<double> Benchmark(this IEnumerable<Instrument> market)
        /// <summary>
        /// Calculate equally-weighted market benchmark.
        /// </summary>
        /// <param name="market">enumerable of instruments making up market</param>
        /// <returns>benchmark time series</returns>
        public static ITimeSeries<double> Benchmark(this IEnumerable<Instrument> market)
        {
            // TODO: need to figure out, if we want to include the market in the calcualtion
            //       of the cache id. for now, we decided to _not_ include the market,
            //       as instruments included might change, for the _same_ market
            return IndicatorsBasic.BufferedLambda(
                (p) =>
                {
                    double todaysLogReturn = market
                        .Average(i => i.Close.LogReturn()[0]);
                    return p * Math.Exp(todaysLogReturn);
                },
                1.0,
                CacheId.NewFromStackTraceParameters(0));
        }
        #endregion

        #region public static _CAPM CAPM(this Instrument series, Instrument benchmark, int n)
        /// <summary>
        /// Calculate Capital Asset Pricing Model parameters.
        /// <see href="http://en.wikipedia.org/wiki/Capital_asset_pricing_model"/>
        /// This indicator uses an exponentially-weighted, incremental method of
        /// calculation, based on Tony Finch, which is very fast and efficient.
        /// /// </summary>
        /// <param name="series">input instrument</param>
        /// <param name="benchmark">benchmark time series</param>
        /// <param name="n">length of observation window</param>
        /// <returns>container w/ CAPM parameters</returns>
        public static _CAPM CAPM(this Instrument series, Instrument benchmark, int n)
        {
            return series.Close.CAPM(
                benchmark.Close,
                n);
        }
        #endregion
        #region public static _CAPM CAPM(this ITimeSeries<double> series, ITimeSeries<double> benchmark, int n)
        /// <summary>
        /// Calculate Capital Asset Pricing Model parameters.
        /// <see href="http://en.wikipedia.org/wiki/Capital_asset_pricing_model"/>
        /// This indicator uses an exponentially-weighted, incremental method of
        /// calculation, based on Tony Finch, which is very fast and efficient.
        /// /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="benchmark">benchmark time series</param>
        /// <param name="n">length of observation window</param>
        /// <returns>container w/ CAPM parameters</returns>
        public static _CAPM CAPM(this ITimeSeries<double> series, ITimeSeries<double> benchmark, int n)
        {
            var functor = Cache<CAPMFunctor>.GetData(
                    CacheId.NewFromStackTraceParameters(series.GetHashCode(), benchmark.GetHashCode(), n),
                    () => new CAPMFunctor(series, benchmark, n));

            functor.Calc();

            return functor;
        }

        /// <summary>
        /// Container holding CAPM parameters.
        /// </summary>
        public class _CAPM
        {
            /// <summary>
            /// CAPM alpha time series.
            /// </summary>
            public ITimeSeries<double> Alpha = new TimeSeries<double>();
            /// <summary>
            /// CAPM beta time series.
            /// </summary>
            public ITimeSeries<double> Beta = new TimeSeries<double>();
        }

        private class CAPMFunctor : _CAPM
        {
            private ITimeSeries<double> _series;
            private ITimeSeries<double> _benchmark;
            private int _n;

            private double _alpha;
            private double _avgSeries;
            private double _avgBench;
            private double _varSeries;
            private double _varBench;
            private double _cov;

            public CAPMFunctor(ITimeSeries<double> series, ITimeSeries<double> benchmark, int n)
            {
                _series = series;
                _benchmark = benchmark;
                _n = n;

                _alpha = 2.0 / (1.0 + _n);
                _avgSeries = 0.0;
                _avgBench = 0.0;
                _varSeries = 0.0025; // exact value uncritical, but helps startup behavior
                _varBench = 0.0025;
                _cov = 0.0025;
            }

            public void Calc()
            {
                //--- average & variance for series
                double seriesNew = _series.LogReturn()[0];
                double seriesDiff = seriesNew - _avgSeries;
                double seriesIncr = _alpha * seriesDiff;
                _avgSeries = _avgSeries + seriesIncr;
                _varSeries = (1.0 - _alpha) * (_varSeries + seriesDiff * seriesIncr);

                //--- average & variance for benchmark
                double benchNew = _benchmark.LogReturn()[0];
                double benchDiff = benchNew - _avgBench;
                double benchIncr = _alpha * benchDiff;
                _avgBench = _avgBench + benchIncr;
                _varBench = (1.0 - _alpha) * (_varBench + benchDiff * benchIncr);

                //--- covariance
                _cov = (1.0 - _alpha) * (_cov + seriesDiff * benchIncr);

                //--- CAPM
                (Beta as TimeSeries<double>).Value = _cov / Math.Max(1e-10, _varBench);
                (Alpha as TimeSeries<double>).Value = _avgSeries - Beta[0] * _avgBench;
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file