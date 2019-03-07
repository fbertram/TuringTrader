//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PortfolioSupport
// Description: portfolio support functionality
// History:     2019iii06, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Support class for portfolio construction
    /// </summary>
    public class PortfolioSupport
    {
        #region public class MarkowitzPortfolio
        /// <summary>
        /// Container to hold Markowitz Portfolio
        /// </summary>
        public class MarkowitzPortfolio
        {
            /// <summary>
            /// Portfolio return (mu)
            /// </summary>
            public double Return;
            /// <summary>
            /// Portfolio risk (sigma)
            /// </summary>
            public double Risk;
            /// <summary>
            /// Instrument weights
            /// </summary>
            public Dictionary<Instrument, double> Weights;
        }
        #endregion
        #region public class MarkowitzCLA
        /// <summary>
        /// Class encapsulating Markowitz CLA algorithm to calculate the
        /// the efficient frontier.
        /// </summary>
        public class MarkowitzCLA
        {
            #region internal data
            private _MarkowitzCLA _cla;
            private List<Instrument> _instruments;
            #endregion

            #region public MarkowitzCLA(...)
            /// <summary>
            /// Create new CLA object.
            /// </summary>
            /// <param name="universe">instrument universe</param>
            /// <param name="meanFunc">instrument mean vector</param>
            /// <param name="covarianceFunc">instrument covariance matrix</param>
            /// <param name="lowerBoundFunc">portfolio lower bound vector</param>
            /// <param name="upperBoundFunc">portfolio upper bound vector</param>
            public MarkowitzCLA(
                IEnumerable<Instrument> universe,
                Func<Instrument, double> meanFunc,
                Func<Instrument, Instrument, double> covarianceFunc,
                Func<Instrument, double> lowerBoundFunc,
                Func<Instrument, double> upperBoundFunc)
            {
                _instruments = universe
                    .ToList();

                var mean = Vector<double>.Build.Dense(
                    _instruments.Count,
                    idx => meanFunc(_instruments[idx]));

                var covar = Matrix<double>.Build.Dense(
                    _instruments.Count, _instruments.Count,
                    (row, col) => covarianceFunc(_instruments[row], _instruments[col]));

                var lowerBound = Vector<double>.Build.Dense(
                    _instruments.Count,
                    idx => lowerBoundFunc(_instruments[idx]));

                var upperBound = Vector<double>.Build.Dense(
                    _instruments.Count,
                    idx => upperBoundFunc(_instruments[idx]));

                _cla = new _MarkowitzCLA(mean, covar, lowerBound, upperBound);
            }
            #endregion
            #region public IEnumerable<MarkowitzPortfolio> TurningPoints()
            /// <summary>
            /// Return all turning points for efficient frontier.
            /// </summary>
            /// <returns>enumerable of portfolios</returns>
            public IEnumerable<MarkowitzPortfolio> TurningPoints()
            {
                foreach (var w in _cla.turningPoints())
                {
                    var pf = new MarkowitzPortfolio
                    {
                        Return = _cla.calcReturn(w),
                        Risk = _cla.calcVolatility(w),
                        Weights = Enumerable.Range(0, w.Count)
                            .ToDictionary(
                                idx => _instruments[idx],
                                idx => w[idx]),
                    };

                    yield return pf;
                }

                yield break;
            }
            #endregion

            #region public IEnumerable<MarkowitzPortfolio> EfficientFrontier(int points)
            /// <summary>
            /// Return efficient frontier, w/ specified # of points
            /// </summary>
            /// <param name="points">number of points</param>
            /// <returns>portfolios at each point</returns>
            public IEnumerable<MarkowitzPortfolio> EfficientFrontier(int points = 100)
            {
                foreach (var t in _cla.efFrontier(points))
                {
                    var mu = t.Item1;
                    var sigma = t.Item2;
                    var w = t.Item3;

                    var pf = new MarkowitzPortfolio
                    {
                        Return = mu,
                        Risk = sigma,
                        Weights = Enumerable.Range(0, w.Count)
                            .ToDictionary(
                                idx => _instruments[idx],
                                idx => w[idx])
                    };

                    yield return pf;
                }

                yield break;
            }
            #endregion
            #region public MarkowitzPortfolio MaximumSharpeRatio()
            /// <summary>
            /// Return portfolio w/ maximum sharpe ratio.
            /// </summary>
            /// <returns>portfolio</returns>
            public MarkowitzPortfolio MaximumSharpeRatio()
            {
                var p = _cla.getMaxSR();

                var pf = new MarkowitzPortfolio
                {
                    Return = _cla.calcReturn(p.Item2),
                    Risk = _cla.calcVolatility(p.Item2),
                    //Sharpe = p.Item1,
                    Weights = Enumerable.Range(0, p.Item2.Count)
                        .ToDictionary(
                            idx => _instruments[idx],
                            idx => p.Item2[idx])
                };

                return pf;
            }
            #endregion
            #region public MarkowitzPortfolio MinimumVariance()
            /// <summary>
            /// Return portfolio w/ minimum variance.
            /// </summary>
            /// <returns>portfolio</returns>
            public MarkowitzPortfolio MinimumVariance()
            {
                var p = _cla.getMinVar();

                var pf = new MarkowitzPortfolio
                {
                    Return = _cla.calcReturn(p.Item2),
                    Risk = Math.Sqrt(p.Item1),
                    Weights = Enumerable.Range(0, p.Item2.Count)
                        .ToDictionary(
                            idx => _instruments[idx],
                            idx => p.Item2[idx])
                };

                return pf;
            }
            #endregion
        }
        #endregion

        #region private class _MarkowitzCLA
        /// <summary>
        /// Markowitz CLA algorithm. Based on Python code, from paper byu
        /// David H. Bailey and Marcos Lopez de Prado.
        /// <see href="https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2197616"/>
        /// </summary>
        private class _MarkowitzCLA
        {
            #region internal data
            /// <summary>
            /// Vector w/ mean returns
            /// </summary>
            private readonly Vector<double> _mean;
            /// <summary>
            /// Covariance matrix
            /// </summary>
            private readonly Matrix<double> _covar;
            /// <summary>
            /// Lower bounds
            /// </summary>
            private readonly Vector<double> _lb;
            /// <summary>
            /// Upper bounds
            /// </summary>
            private readonly Vector<double> _ub;

            /// <summary>
            /// Solution
            /// </summary>
            private List<Vector<double>> _w;
            /// <summary>
            /// Lambdas
            /// </summary>
            private List<double?> _l;
            /// <summary>
            /// Gammas
            /// </summary>
            private List<double?> _g;
            /// <summary>
            /// Free weights
            /// </summary>
            private List<List<int>> _f;
            #endregion
            #region internal helpers
            #region private void solve()
            private void solve()
            {
                // compute the turning points, free sets and weights

                var f_w = initAlgo();
                var f = f_w.Item1;
                var w = f_w.Item2;

                _w.Add(w.Clone());
                _l.Add(null);
                _g.Add(null);
                _f.Add(new List<int>(f));

                //
                double? l_in = null;
                double? l_out = null;
                int i_in = 0;
                int i_out = 0;
                double? bi_in = null;
                Matrix<double> covarF = null;
                Matrix<double> covarF_inv = null;
                Matrix<double> covarFB = null;
                Vector<double> meanF = null;
                Vector<double> wB = null;

                while (true)
                {
                    //----------
                    // #1) case a) Bound one free weight
                    l_in = null;
                    if (f.Count > 1)
                    {
                        var m = getMatrices(f);
                        covarF = m.Item1;
                        covarFB = m.Item2;
                        meanF = m.Item3;
                        wB = m.Item4;

                        int j = 0;
                        foreach (var i in f)
                        {
                            var l_bi = computeLambda(covarF_inv, covarFB, meanF, wB, j, new List<double> { _lb[i], _ub[i] });
                            var l = l_bi.Item1;
                            var bi = l_bi.Item2;
                            if (l > l_in)
                            {
                                l_in = l;
                                i_in = i;
                                bi_in = bi;
                            }
                            j++;
                        }
                    }

                    //----------
                    // #2) case b): Free one bounded weight
                    l_out = null;
                    if (f.Count() < _mean.Count())
                    {
                        var b = getB(f);

                        foreach (int i in b)
                        {
                            var f2 = new List<int>(f);
                            f2.Add(i);
                            var m = getMatrices(f2);
                            covarF = m.Item1;
                            covarFB = m.Item2;
                            meanF = m.Item3;
                            wB = m.Item4;
                            covarF_inv = covarF.Inverse();
                            var ll = computeLambda(covarF_inv, covarFB, meanF, wB, meanF.Count - 1, new List<double> { _w.Last()[i] });
                            var l = ll.Item1;
                            var bi = ll.Item2;
                            if ((_l.Last() == null || l < _l.Last())
                            && (l_out == null || l > l_out))
                            {
                                l_out = l;
                                i_out = i;
                            }
                        }
                    }

                    if ((l_in == null || l_in < 0.0)
                    && (l_out == null || l_out < 0.0))
                    {
                        //----------
                        // #3) compute minimum variance solution
                        _l.Add(0.0);
                        var m = getMatrices(f);
                        covarF = m.Item1;
                        covarFB = m.Item2;
                        meanF = m.Item3;
                        wB = m.Item4;
                        covarF_inv = covarF.Inverse();
                        meanF = Vector<double>.Build.Dense(meanF.Count, 0.0);
                    }
                    else
                    {
                        //----------
                        // #4) decide lambda
                        if (l_in > l_out)
                        {
                            throw new Exception("TODO: not implemented, yet");
                        }
                        else
                        {
                            _l.Add(l_out);
                            f.Add(i_out);
                        }
                        var m = getMatrices(f);
                        covarF = m.Item1;
                        covarFB = m.Item2;
                        meanF = m.Item3;
                        wB = m.Item4;
                        covarF_inv = covarF.Inverse();
                    }

                    //----------
                    // #5) compute solution vector
                    var wF_g = computeW(covarF_inv, covarFB, meanF, wB);
                    var wf = wF_g.Item1;
                    var g = wF_g.Item2;
                    for (var i = 0; i < f.Count; i++)
                        w[f[i]] = wf[i];
                    _w.Add(w.Clone());
                    _g.Add(g);
                    _f.Add(new List<int>(f));
                    if (_l.Last() == 0.0)
                        break;
                }

                //----------
                // #6) Purge turning points
                purgeNumErr(10e-10);
                purgeExcess();
            }
            #endregion
            #region private Tuple<List<int>, Vector<double>> initAlgo()
            private Tuple<List<int>, Vector<double>> initAlgo()
            {
                // initialize all weights to lower bounds,
                // assume all assets are free
                var w = _lb.Clone();

                // increase weights from lower bound to upper bound
                var indicesDescendingMean = Enumerable.Range(0, _mean.Count)
                    .OrderByDescending(idx => _mean[idx])
                    .ToList();

                foreach (var i in indicesDescendingMean)
                {
                    w[i] = _ub[i];

                    // exceeding total weight of 1.0
                    if (w.Sum() >= 1.0)
                    {
                        // reduce weight to comply w/ constraints
                        w[i] += 1.0 - w.Sum();

                        // return first turning point
                        return new Tuple<List<int>, Vector<double>>
                        (
                            new List<int> { i },
                            w
                        );
                    }
                }

                return null;
            }
            #endregion
            #region private double computeBi(double c, List<double> bi)
            private double computeBi(double c, List<double> bi)
            {
                return c > 0 ? bi[1] : bi[0];
            }
            #endregion
            #region private Tuple<Vector<double>, double> computeW(...)
            private Tuple<Vector<double>, double> computeW(
                Matrix<double> covarF_inv, Matrix<double> covarFB,
                Vector<double> meanF, Vector<double> wB)
            {
                // #1) compute gamma
                var onesF = Vector<double>.Build.Dense(meanF.Count, 1.0);
                double g1 = (onesF.ToRowMatrix().Multiply(covarF_inv).Multiply(meanF)).Single();
                double g2 = (onesF.ToRowMatrix().Multiply(covarF_inv).Multiply(onesF)).Single();

                double g;
                Vector<double> w1 = null;

                if (wB == null)
                {
                    g = -(double)_l.Last() * g1 / g2 + 1 / g2;
                    w1 = Vector<double>.Build.Dense(onesF.Count, 0.0);
                }
                else
                {
                    var onesB = Vector<double>.Build.Dense(wB.Count, 1.0);
                    var g3 = onesB.ToRowMatrix().Multiply(wB).Single();
                    var g4x = covarF_inv.Multiply(covarFB);
                    w1 = g4x.Multiply(wB);
                    var g4 = onesF.ToRowMatrix().Multiply(w1).Single();
                    g = -(double)_l.Last() * g1 / g2 + (1.0 - g3 + g4) / g2;
                }

                // #2) compute weights
                var w2 = covarF_inv.Multiply(onesF);
                var w3 = covarF_inv.Multiply(meanF);

                var w = -w1 + g * w2 + (double)_l.Last() * w3;

                return new Tuple<Vector<double>, double>(
                    w,
                    g);
            }
            #endregion
            #region private Tuple<double?, double?> computeLambda(...)
            private Tuple<double?, double?> computeLambda(
                Matrix<double> covarF_inv, Matrix<double> covarFB,
                Vector<double> meanF, Vector<double> wB,
                int i, List<double> bix)
            {
                // #1) C
                var onesF = Vector<double>.Build.Dense(meanF.Count, 1.0);
                var c1 = onesF.ToRowMatrix().Multiply(covarF_inv).Multiply(onesF);
                var c2 = covarF_inv.Multiply(meanF);
                var c3 = onesF.ToRowMatrix().Multiply(covarF_inv).Multiply(meanF);
                var c4 = covarF_inv.Multiply(onesF);
                var c = (-c1 * c2[i] + c3 * c4[i]).Single();
                if (c == 0.0)
                {
                    return new Tuple<double?, double?>(null, null);
                }

                // #2) bi
                double bi = bix.Count > 1
                    ? computeBi(c, bix)
                    : bix[0];

                // #3) Lambda
                if (wB == null)
                {
                    // All free assets
                    return new Tuple<double?, double?>(
                        ((c4[i] - c1 * bi) / c).Single(),
                        bi);
                }
                else
                {
                    var onesB = Vector<double>.Build.Dense(wB.Count, 1.0);
                    var l1 = onesB.ToRowMatrix().Multiply(wB);
                    var l2x = covarF_inv.Multiply(covarFB);
                    var l3 = l2x.Multiply(wB);
                    var l2 = onesF.ToRowMatrix().Multiply(l3);

                    return new Tuple<double?, double?>(
                        (double?)(((1 - l1 + l2) * c4[i] - c1 * (bi + l3[i])) / c).Single(),
                        (double?)bi);
                }
            }
            #endregion
            #region private Tuple<Matrix<double>, Matrix<double>, Vector<double>, Vector<double>> getMatrices(...)
            private Tuple<Matrix<double>, Matrix<double>, Vector<double>, Vector<double>> getMatrices(List<int> f)
            {
                var covarF = Matrix<double>.Build.Dense(
                    f.Count(), f.Count(),
                    (i, j) => _covar[f[i], f[j]]);

                var meanF = Vector<double>.Build.Dense(
                    f.Count(),
                    i => _mean[f[i]]);

                var b = getB(f);

                Matrix<double> covarFB = null;
                Vector<double> wB = null;

                if (b.Count > 0)
                {
                    covarFB = Matrix<double>.Build.Dense(
                        f.Count(), b.Count(),
                        (i, j) => _covar[f[i], b[j]]);

                    wB = Vector<double>.Build.Dense(
                        b.Count(),
                        i => _w.Last()[b[i]]);
                }

                return new Tuple<Matrix<double>, Matrix<double>, Vector<double>, Vector<double>>(
                    covarF,
                    covarFB,
                    meanF,
                    wB);
            }
            #endregion
            #region private List<Instrument> getB(List<Instrument> f)
            private List<int> getB(List<int> f)
            {
                return Enumerable.Range(0, _mean.Count)
                    .Where(idx => !f.Contains(idx))
                    .ToList();
            }
            #endregion
            #region private void purgeNumErr(double tol)
            private void purgeNumErr(double tol)
            {
                // # Purge violations of inequality constraints (associated with ill-conditioned covar matrix)
                int i = 0;
                while (true)
                {
                    var flag = false;

                    if (i == _w.Count())
                        break;

                    if (_w[i].Sum() - 1.0 > tol)
                    {
                        flag = true;
                    }
                    else
                    {
                        foreach (var j in Enumerable.Range(0, _w[i].Count))
                        {
                            if (_w[i][j] - _lb[j] < -tol
                            || _w[i][j] - _ub[j] > tol)
                            {
                                flag = true;
                                break;
                            }
                        }
                    }

                    if (flag)
                    {
                        throw new Exception("purgeNumErr is doing something!");
                        _w.RemoveAt(i);
                        _l.RemoveAt(i);
                        _g.RemoveAt(i);
                        _f.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            #endregion
            #region private void purgeExcess()
            private void purgeExcess()
            {
                // # Remove violations of the convex hull

                var i = 0;
                var repeat = false;

                while (true)
                {
                    if (!repeat)
                        i++;

                    if (i == _w.Count() - 1)
                        break;

                    var w1 = _w[i];
                    var mu1 = w1.ToRowMatrix().Multiply(_mean).Single();

                    var j = i + 1;
                    repeat = false;

                    while (true)
                    {
                        if (j == _w.Count())
                            break;

                        var w2 = _w[j];
                        var mu2 = w2.ToRowMatrix().Multiply(_mean).Single();

                        if (mu1 < mu2)
                        {
                            throw new Exception("purgeExcess is doing something!");
                            _w.RemoveAt(i);
                            _l.RemoveAt(i);
                            _g.RemoveAt(i);
                            _f.RemoveAt(i);
                            repeat = true;
                            break;
                        }
                        else
                        {
                            j++;
                        }
                    }
                }
            }
            #endregion
            #region private void evalSR()
            private double evalSR(double a, Vector<double> w0, Vector<double> w1)
            {
                // Evaluate SR of the portfolio within the convex combination
                var w = w0.Multiply(a).Add(w1.Multiply(1.0 - a));

                return calcReturn(w) / calcVolatility(w);
            }
            #endregion
            #region private void goldenSection()
            private Tuple<double, double> goldenSection(Func<double, double> obj, double a, double b, bool minimum = false)
            {
                double tol = 1e-9;
                double sign = minimum ? 1.0 : -1.0;
                int numIter = (int)(Math.Ceiling(-2.078087 * Math.Log(tol / Math.Abs(b - a))));
                var r = 0.618033989;
                var c = 1.0 - r;

                // Initialize
                var x1 = r * a + c * b;
                var x2 = c * a + r * b;
                var f1 = sign * obj(x1);
                var f2 = sign * obj(x2);

                // Loop
                for (var i = 0; i < numIter; i++)
                {
                    if (f1 > f2)
                    {
                        a = x1;
                        x1 = x2;
                        f1 = f2;
                        x2 = c * a + r * b;
                        f2 = sign * obj(x2);
                    }
                    else
                    {
                        b = x2;
                        x2 = x1;
                        f2 = f1;
                        x1 = r * a + c * b;
                        f1 = sign * obj(x1);
                    }
                }

                return f1 < f2
                    ? new Tuple<double, double>(x1, sign * f1)
                    : new Tuple<double, double>(x2, sign * f2);
            }
            #endregion
            #endregion

            #region public _MarkowitzCLA(...)
            public _MarkowitzCLA(
                Vector<double> means,
                Matrix<double> covariances,
                Vector<double> lowerBounds,
                Vector<double> upperBounds)
            {
                _mean = means;
                _covar = covariances;
                _lb = lowerBounds;
                _ub = upperBounds;

                // TODO: not sure what this does
                // if (mean == np.ones(mean.shape) * mean.mean()).all():mean[-1, 0] += 1e-5

                _w = new List<Vector<double>>();
                _l = new List<double?>();
                _g = new List<double?>();
                _f = new List<List<int>>();

                solve();
            }
            #endregion

            #region public double calcReturn(Vector<double> w)
            public double calcReturn(Vector<double> w)
            {
                return w.ToRowMatrix().Multiply(_mean).Single();
            }
            #endregion
            #region public double calcVolatility(Vector<double> w)
            public double calcVolatility(Vector<double> w)
            {
                var variance = w.ToRowMatrix().Multiply(_covar).Multiply(w).Single();
                return Math.Sqrt(variance);
            }
            #endregion

            #region public IEnumerable<Vector<double>> turningPoints()
            public IEnumerable<Vector<double>> turningPoints()
            {
                foreach (var w in _w)
                {
                    yield return w;
                }

                yield break;
            }
            #endregion
            #region public IEnumerable<Tuple<double, double, Vector<double>>> efFrontier(int points)
            public IEnumerable<Tuple<double, double, Vector<double>>> efFrontier(int points)
            {
                var n = points / _w.Count;
                var a = Enumerable.Range(0, n)
                    .Take(n - 1) // remove the 1, to avoid duplications
                    .Select(i => (double)i / (n - 1))
                    .ToList();
                var b = Enumerable.Range(0, _w.Count - 1)
                    .ToList();

                foreach (var i in b)
                {

                    var w0 = _w[i];
                    var w1 = _w[i + 1];

                    if (i == b.Last())
                        a.Add(1.0); // include the 1 in the last iteration

                    foreach (var j in a)
                    {
                        var w = w1.Multiply(j).Add(w0.Multiply(1.0 - j));
                        var mu = w.ToRowMatrix().Multiply(_mean).Single();
                        var sigma = Math.Sqrt(w.ToRowMatrix().Multiply(_covar).Multiply(w).Single());

                        yield return new Tuple<double, double, Vector<double>>(
                            mu,
                            sigma,
                            w);
                    }
                }

                yield break;
            }
            #endregion
            #region public Tuple<double, Vector<double>> getMaxSR()
            public Tuple<double, Vector<double>> getMaxSR()
            {
                var portfolioCandidates = new List<Tuple<double, Vector<double>>>();

                for (var i = 0; i < _w.Count - 1; i++)
                {
                    var w0 = _w[i];
                    var w1 = _w[i + 1];

                    var a_b = goldenSection(
                        x => evalSR(x, w0, w1),
                        0.0, 1.0,
                        false);
                    var a = a_b.Item1;
                    var b = a_b.Item2;

                    var w = w0.Multiply(a).Add(w1.Multiply(1.0 - a));

                    var portfolio = new Tuple<double, Vector<double>>(b, w);
                    portfolioCandidates.Add(portfolio);
                }

                return portfolioCandidates
                    .OrderByDescending(p => p.Item1)
                    .First();
            }
            #endregion
            #region public Tuple<double, Vector<double>> getMinVar()
            public Tuple<double, Vector<double>> getMinVar()
            {
                var variance = new List<double>();

                foreach (var w in _w)
                {
                    var a = w.ToRowMatrix().Multiply(_covar).Multiply(w).Single();
                    variance.Add(a);
                }

                var min = variance.Min();
                var index = variance.FindIndex(v => v == min);

                return new Tuple<double, Vector<double>>(min, _w[index]);
            }
            #endregion
        }
        #endregion
    }
}

//==============================================================================
// end of file