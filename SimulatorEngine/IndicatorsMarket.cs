//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsMarket
// Description: collection of market indicators
// History:     2018ix17, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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
        #region public static CAPMParams CAPM(this IEnumerable<Instrument> market, Instrument benchmark, int n)
        /// <summary>
        /// Calculate Fama/ French Capital Asset Pricing Model parameters.
        /// </summary>
        /// <param name="market">collection of instruments forming market</param>
        /// <param name="benchmark">instrument serving as benchmark</param>
        /// <param name="n">length of rolling window</param>
        /// <returns>CAPM info</returns>
        public static CAPMParams CAPM(this IEnumerable<Instrument> market, Instrument benchmark, int n)
        {
            var functor = Cache<CAPMParams>.GetData(
                    // TODO: does the market need to be included in the unique id?
                    Cache.UniqueId(market.GetHashCode(), benchmark.GetHashCode(), n),
                    () => new CAPMParams(market, benchmark, n));

            functor.Calc();

            return functor;
        }
        /// <summary>
        /// Container holding CAPM parameters.
        /// </summary>
        public class CAPMParams
        {
            private readonly double _alpha;
            private Dictionary<Instrument, double> _avg = new Dictionary<Instrument, double>();
            private Dictionary<Instrument, double> _var = new Dictionary<Instrument, double>();
            private Dictionary<Instrument, double> _cov = new Dictionary<Instrument, double>();

            /// <summary>
            /// Enumerable defining market.
            /// </summary>
            public readonly IEnumerable<Instrument> Market;

            /// <summary>
            /// Benchmark instrument
            /// </summary>
            public readonly Instrument Benchmark;

            /// <summary>
            /// Length of rolling window.
            /// </summary>
            public readonly int N;

            /// <summary>
            /// Alpha time series.
            /// </summary>
            public Dictionary<Instrument, TimeSeries<double>> Alpha = new Dictionary<Instrument, TimeSeries<double>>();

            /// <summary>
            /// Beta time series.
            /// </summary>
            public Dictionary<Instrument, TimeSeries<double>> Beta = new Dictionary<Instrument, TimeSeries<double>>();

            /// <summary>
            /// Create and initialize CAPM functor.
            /// </summary>
            /// <param name="market">Enumerable of instruments defining market</param>
            /// <param name="benchmark">Benchmark instrument</param>
            /// <param name="n">Length of rolling window</param>
            public CAPMParams(IEnumerable<Instrument> market, Instrument benchmark, int n)
            {
                Market = market;
                Benchmark = benchmark;
                N = n;

                _alpha = 2.0 / (1.0 + N);

                //--- initialize benchmark data
                if (!_avg.ContainsKey(Benchmark))
                {
                    _avg[Benchmark] = 0.0;
                    _var[Benchmark] = 0.0;
                    _cov[Benchmark] = 0.0;

                    Alpha[Benchmark] = new TimeSeries<double>();
                    Beta[Benchmark] = new TimeSeries<double>();
                }
            }

            /// <summary>
            /// Calculate new values for CAPM model.
            /// </summary>
            public void Calc()
            {
                // see Tony Finch, Incrmental calculation of weighted mean and variance, February 2009

                //--- calculate benchmark's average and variance
                double benchmarkNew = Benchmark.Close.LogReturn()[0];
                double benchmarkDiff = benchmarkNew - _avg[Benchmark];
                double benchmarkIncr = _alpha * benchmarkDiff;
                _avg[Benchmark] = _avg[Benchmark] + benchmarkIncr;
                _var[Benchmark] = (1.0 - _alpha) * (_var[Benchmark] + benchmarkDiff * benchmarkIncr);

                var check = Market.ToList();

                foreach (Instrument instrument in Market)
                {
                    if (instrument == Benchmark)
                    {
                        Alpha[Benchmark].Value = 0.0;
                        Beta[Benchmark].Value = 1.0;
                        continue;
                    }

                    //--- initialize instrument data
                    // NOTE: this can't be done during object construction, as instruments
                    //       might be added to or removed from the market
                    if (!_avg.ContainsKey(instrument))
                    {
                        _avg[instrument] = 0.0;
                        _var[instrument] = 0.0;
                        _cov[instrument] = 0.0;

                        Alpha[instrument] = new TimeSeries<double>();
                        Beta[instrument] = new TimeSeries<double>();
                    }

                    //--- calculate instrument's average and variance
                    double instrumentNew = instrument.Close.LogReturn()[0];
                    double instrumentDiff = instrumentNew - _avg[instrument];
                    double instrumentIncr = _alpha * instrumentDiff;
                    _avg[instrument] = _avg[instrument] + instrumentIncr;
                    _var[instrument] = (1.0 - _alpha) * (_var[instrument] + instrumentDiff * instrumentIncr);

                    //--- calculate instrument's covariance and CAPM
                    // FUB's own abstraction of Tony Finch for covariance
                    _cov[instrument] = (1.0 - _alpha) * (_cov[instrument] + instrumentDiff * benchmarkIncr);

                    // and now to CAPM
                    Beta[instrument].Value = _cov[instrument] / Math.Max(1e-10, _var[Benchmark]);
                    Alpha[instrument].Value = _avg[instrument] - Beta[instrument][0] * _avg[Benchmark];
                }
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file