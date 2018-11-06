//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsCAPM
// Description: Indicators for Fama/French capital asset pricing model.
// History:     2018xi05, FUB, created
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
    public static class IndicatorsCAPM
    {
        #region public static CAPMFunctor CAPM(this ITimeSeries<double> series, ITimeSeries<double> benchmark, int n)
        public static CAPMFunctor CAPM(this ITimeSeries<double> series, ITimeSeries<double> benchmark, int n)
        {
            var functor = Cache<CAPMFunctor>.GetData(
                    Cache.UniqueId(series.GetHashCode(), benchmark.GetHashCode(), n),
                    () => new CAPMFunctor(series, benchmark, n));

            functor.Calc();

            return functor;
        }

        public class CAPMFunctor
        {
            private readonly double _alpha;
            private double _benchmarkAvg = 0.0;
            private double _benchmarkVar = 0.0;
            private double _instrumentAvg = 0.0;
            private double _instrumentVar = 0.0;
            private double _instrumentCov = 0.0;

            public readonly ITimeSeries<double> Series;
            public readonly ITimeSeries<double> Benchmark;
            public readonly int N;

            public TimeSeries<double> Beta = new TimeSeries<double>();
            public TimeSeries<double> Alpha = new TimeSeries<double>();

            public CAPMFunctor(ITimeSeries<double> series, ITimeSeries<double> benchmark, int n)
            {
                Series = series;
                Benchmark = benchmark;
                N = n;
                _alpha = 2.0 / (1.0 + N);
            }

            public void Calc()
            {
                // see Tony Finch, Incrmental calculation of weighted mean and variance, February 2009

                double benchmarkNew = Benchmark.LogReturn()[0];
                double benchmarkDiff = benchmarkNew - _benchmarkAvg;
                double benchmarkIncr = _alpha * benchmarkDiff;
                _benchmarkAvg = _benchmarkAvg + benchmarkIncr;
                _benchmarkVar = (1.0 - _alpha) * (_benchmarkVar + benchmarkDiff * benchmarkIncr);

                double instrumentNew = Series.LogReturn()[0];
                double instrumentDiff = instrumentNew - _instrumentAvg;
                double instrumentIncr = _alpha * instrumentDiff;
                _instrumentAvg = _instrumentAvg + instrumentIncr;
                _instrumentVar = (1.0 - _alpha) * (_instrumentVar + instrumentDiff * instrumentIncr);

                // FUB's abstraction for covariance
                _instrumentCov = (1.0 - _alpha) * (_instrumentCov + instrumentDiff * benchmarkIncr);

                // and now to CAPM
                Beta.Value = _instrumentCov / Math.Max(1e-10, _benchmarkVar);
                Alpha.Value = _instrumentAvg - Beta[0] * _benchmarkAvg;
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file