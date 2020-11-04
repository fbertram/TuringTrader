//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PortfolioSupport
// Description: portfolio support functionality
// History:     2019iii06, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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

#region libraries
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Support
{
    /// <summary>
    /// Support class for portfolio construction
    /// </summary>
    public class PortfolioSupport
    {
        #region public class MarkowitzCLA
        /// <summary>
        /// Class encapsulating Markowitz CLA algorithm to calculate the
        /// the efficient frontier.
        /// </summary>
        public class MarkowitzCLA
        {
            #region private class CLA
            /// <summary>
            /// Markowitz CLA algorithm. Based on Python implementation, 
            /// as presended in paper by David H. Bailey and Marcos Lopez de Prado.
            /// <see href="https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2197616"/>
            /// <see href="http://www.quantresearch.info/Software.htm"/>
            /// <see href="https://www.davidhbailey.com/dhbpapers/"/>
            /// In case you are wondering why this code smells a bit like Python:
            /// This implementation follows the paper closely; the overall structure,
            /// as well as the names of methods and fields are (mostly) identical.
            /// For an extensive discussion of the implementation, refere to the paper.
            /// </summary>
            private class CLA
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
                #region private void DumpTestVectors()
                private void DumpTestVectors()
                {
#if true
                    Output.WriteLine("double[] mean = {");
                    for (int i = 0; i < _mean.Count; i++)
                        Output.WriteLine("    {0:E15}, ", _mean[i]);
                    Output.WriteLine("};");

                    Output.WriteLine("double[,] covar = {");
                    for (int i = 0; i < _covar.RowCount; i++)
                    {
                        Output.Write("    { ");
                        for (int j = 0; j < _covar.ColumnCount; j++)
                        {
                            Output.Write("{0:E15}, ", _covar[i, j]);
                        }
                        Output.WriteLine("},");
                    }
                    Output.WriteLine("};");

                    Output.WriteLine("double[] lbound = {");
                    for (int i = 0; i < _lb.Count; i++)
                        Output.WriteLine("    {0:E15},", _lb[i]);
                    Output.WriteLine("};");

                    Output.WriteLine("double[] ubound = {");
                    for (int i = 0; i < _lb.Count; i++)
                        Output.WriteLine("    {0:E15},", _ub[i]);
                    Output.WriteLine("};");
#endif
                }
                #endregion
                #region private void Solve()
                private void Solve()
                {
                    // compute the turning points, free sets and weights

                    var f_w = InitAlgo();

                    var f = f_w.Item1;
                    var w = f_w.Item2;

                    if (f == null)
                    {
                        // InitAlgo failed. The weights returned are 
                        // still the best possible solution.
                        for (int i = 0; i < 2; i++)
                        {
                            _w.Add(w.Clone());
                            _l.Add(null);
                            _g.Add(null);
                            _f.Add(null);
                        }
                        return;
                    }

                    _w.Add(w.Clone());
                    _l.Add(null);
                    _g.Add(null);
                    _f.Add(new List<int>(f));

                    Matrix<double> covarF = null;
                    Matrix<double> covarF_inv = null;
                    Matrix<double> covarFB = null;
                    Vector<double> meanF = null;
                    Vector<double> wB = null;

                    int iter = 0;
                    while (true)
                    {
                        iter++;

                        //----------
                        // #1) case a) Bound one free weight
                        double l_in = -1.0;
                        int i_in = 0;
                        double bi_in = 0.0;

                        if (f.Count > 1)
                        {
                            var m = GetMatrices(f);
                            covarF = m.Item1;
                            covarFB = m.Item2;
                            meanF = m.Item3;
                            wB = m.Item4;

                            covarF_inv = covarF.Inverse();

                            int j = 0;
                            foreach (var i in f)
                            {
                                var l_bi = ComputeLambda(covarF_inv, covarFB, meanF, wB, j, new List<double> { _lb[i], _ub[i] });
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
                        double l_out = -1.0;
                        int i_out = 0;

                        if (f.Count() < _mean.Count())
                        {
                            List<int> b = GetB(f);

                            foreach (int i in b)
                            {
                                var fi = new List<int>(f) { i };
                                var m = GetMatrices(fi);
                                covarF = m.Item1;
                                covarFB = m.Item2;
                                meanF = m.Item3;
                                wB = m.Item4;

                                covarF_inv = covarF.Inverse();

                                var ll = ComputeLambda(covarF_inv, covarFB, meanF, wB, fi.FindIndex(v => v == i), new List<double> { _w.Last()[i] });
                                var l = ll.Item1;
                                var bi = ll.Item2;

                                if ((_l.Last() == null || l < _l.Last())
                                && (l > l_out))
                                {
                                    l_out = l;
                                    i_out = i;
                                }
                            }
                        }

#if true
                        // FIXME: sometimes method doesn't converge. It is unclear why
                        // that is, and it seems the issue can't be reproduced in the
                        // testbench. Probably, the numerical resolution of the test
                        // vectors dumped by the code below, is not sufficient to do so.
                        // It was observed that lambdas have been going in circles,
                        // while it seems they should be monotonically falling?
                        // For now, we just abort the method here.
                        bool noConvergence = _w.Count > 50 * _mean.Count;
#else
                        bool noConvergence = false;
#endif

                        if (l_in < 0.0 && l_out < 0.0
                        || noConvergence)
                        {
                            if (noConvergence)
                            {
                                Output.WriteLine("MarkowitzCLA: aborted after {0} iterations", iter);
                                DumpTestVectors();
                            }

                            //----------
                            // #3) compute minimum variance solution
                            _l.Add(0.0);
                            var m = GetMatrices(f);
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
                                _l.Add(l_in);
                                f.Remove(i_in);
                                w[i_in] = bi_in; // set value at the correct boundary
                            }
                            else
                            {
                                _l.Add(l_out);
                                f.Add(i_out);
                            }

                            var m = GetMatrices(f);
                            covarF = m.Item1;
                            covarFB = m.Item2;
                            meanF = m.Item3;
                            wB = m.Item4;

                            covarF_inv = covarF.Inverse();
                        }

                        //----------
                        // #5) compute solution vector
                        var wF_g = ComputeW(covarF_inv, covarFB, meanF, wB);
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
                    PurgeNumErr(10e-10);
                    PurgeExcess();
                    PurgeDuplicates(10e-10);

                    if (_w.Count <= 1)
                    {
                        Output.WriteLine("MarkowitzCLA: no turning points");
                        DumpTestVectors();
                    }
                }
                #endregion
                #region private Tuple<List<int>, Vector<double>> InitAlgo()
                private Tuple<List<int>, Vector<double>> InitAlgo()
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

                    return new Tuple<List<int>, Vector<double>>
                    (
                        null,
                        w
                    );
                }
                #endregion
                #region private double ComputeBi(double c, List<double> bi)
                private double ComputeBi(double c, List<double> bi)
                {
                    return c > 0 ? bi[1] : bi[0];
                }
                #endregion
                #region private Tuple<Vector<double>, double> ComputeW(...)
                private Tuple<Vector<double>, double> ComputeW(
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
                #region private Tuple<double, double> computeLambda(...)
                private Tuple<double, double> ComputeLambda(
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
                        return new Tuple<double, double>(0.0, 0.0);
                    }

                    // #2) bi
                    double bi = bix.Count > 1
                        ? ComputeBi(c, bix)
                        : bix[0];

                    // #3) Lambda
                    if (wB == null)
                    {
                        // All free assets
                        return new Tuple<double, double>(
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

                        return new Tuple<double, double>(
                            (((1 - l1 + l2) * c4[i] - c1 * (bi + l3[i])) / c).Single(),
                            bi);
                    }
                }
                #endregion
                #region private Tuple<Matrix<double>, Matrix<double>, Vector<double>, Vector<double>> GetMatrices(...)
                private Tuple<Matrix<double>, Matrix<double>, Vector<double>, Vector<double>> GetMatrices(List<int> f)
                {
                    var covarF = Matrix<double>.Build.Dense(
                        f.Count(), f.Count(),
                        (i, j) => _covar[f[i], f[j]]);

                    var meanF = Vector<double>.Build.Dense(
                        f.Count(),
                        i => _mean[f[i]]);

                    var b = GetB(f);

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
                #region private List<Instrument> GetB(List<Instrument> f)
                private List<int> GetB(List<int> f)
                {
                    return Enumerable.Range(0, _mean.Count)
                        .Where(idx => !f.Contains(idx))
                        .ToList();
                }
                #endregion
                #region private void PurgeNumErr(double tol)
                private void PurgeNumErr(double tol)
                {
                    // # Purge violations of inequality constraints (associated with ill-conditioned covar matrix)
                    int i = 0;
                    while (true)
                    {
                        var flag = false;

                        if (i == _w.Count())
                            break;

                        if (Math.Abs(_w[i].Sum() - 1.0) > tol)
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
                            //Output.WriteLine("CLA: purgeNumErr removing turning point");
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
                #region private void PurgeExcess()
                private void PurgeExcess()
                {
                    // # Remove violations of the convex hull

                    var i = 0;
                    var repeat = false;

                    while (true)
                    {
                        if (!repeat)
                            i++;

                        if (i >= _w.Count() - 1)
                            break;

                        var w1 = _w[i];
                        var mu1 = w1.ToRowMatrix().Multiply(_mean).Single();

                        var j = i + 1;
                        repeat = false;

                        while (true)
                        {
                            if (j >= _w.Count())
                                break;

                            var w2 = _w[j];
                            var mu2 = w2.ToRowMatrix().Multiply(_mean).Single();

                            if (mu1 < mu2)
                            {
                                //Output.WriteLine("CLA: purgeExcess removing turning point");
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
                #region private void PurgeDuplicates(double tolerance)
                private void PurgeDuplicates(double tolerance)
                {
                    // added by FUB, not part ofBailey & de Prado's 
                    // original implementation

                    int i = 0;
                    while (i < _w.Count() - 2) // last member is min variance p/f
                    {
                        bool isDuplicate = true;
                        foreach (var j in Enumerable.Range(0, _w[i].Count()))
                        {
                            if (Math.Abs(_w[i][j] - _w[i + 1][j]) > tolerance)
                            {
                                isDuplicate = false;
                                break;
                            }
                        }

                        if (isDuplicate)
                        {
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
                #region private void EvalSR()
                private double EvalSR(double a, Vector<double> w0, Vector<double> w1)
                {
                    // Evaluate SR of the portfolio within the convex combination
                    var w = w0.Multiply(a).Add(w1.Multiply(1.0 - a));

                    return CalcReturn(w) / CalcVolatility(w);
                }
                #endregion
                #region public void GoldenSection(...)
                public Tuple<double, double> GoldenSection(Func<double, double> obj, double a, double b, bool minimum = false)
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
                public CLA(
                    Vector<double> means,
                    Matrix<double> covariances,
                    Vector<double> lowerBounds,
                    Vector<double> upperBounds)
                {
                    _mean = means;
                    _covar = covariances;
                    _lb = lowerBounds;
                    _ub = upperBounds;

#if true
                    for (var i = 0; i < _lb.Count; i++)
                    {
                        // FUB addition
                        // ill-conditioned vector. we assume that it is more likely
                        // for an algorithm to dynamically control the upper bounds,
                        // which is why we set the lower bound to those.
                        if (_lb[i] > _ub[i])
                            _lb[i] = _ub[i];
                    }
#endif

                    // TODO: not sure what this does
                    // if (mean == np.ones(mean.shape) * mean.mean()).all():mean[-1, 0] += 1e-5

                    _w = new List<Vector<double>>();
                    _l = new List<double?>();
                    _g = new List<double?>();
                    _f = new List<List<int>>();

                    Solve();
                }
                #endregion

                //--- APIs to calc risk & return, based on weights
                #region public double CalcReturn(Vector<double> w)
                public double CalcReturn(Vector<double> w)
                {
                    return w.ToRowMatrix().Multiply(_mean).Single();
                }
                #endregion
                #region public double CalcVolatility(Vector<double> w)
                public double CalcVolatility(Vector<double> w)
                {
                    var variance = w.ToRowMatrix().Multiply(_covar).Multiply(w).Single();
                    return Math.Sqrt(variance);
                }
                #endregion
                #region public bool CheckValidity(Vector<double> w)
                public bool CheckValidity(Vector<double> w)
                {
                    for (int i = 0; i < w.Count; i++)
                    {
                        if (w[i] < _lb[i] || w[i] > _ub[i])
                            return false;
                    }
                    return true;
                }
                #endregion

                //--- APIs to return efficient frontier
                #region public IEnumerable<Vector<double>> TurningPoints()
                public IEnumerable<Vector<double>> TurningPoints()
                {
                    foreach (var w in _w)
                    {
                        yield return w;
                    }

                    yield break;
                }
                #endregion
                #region public IEnumerable<Tuple<double, double, Vector<double>>> EfFrontier(int points)
                public IEnumerable<Tuple<double, double, Vector<double>>> EfFrontier(int points)
                {
                    var n = points / (_w.Count - 1); // last is min-variance portfolio
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

                //--- APIs to return specific portfolios
                #region public Tuple<double, Vector<double>> GetMaxSR()
                public Tuple<double, Vector<double>> GetMaxSR()
                {
                    var portfolioCandidates = new List<Tuple<double, Vector<double>>>();

                    for (var i = 0; i < _w.Count - 1; i++)
                    {
                        var w0 = _w[i];
                        var w1 = _w[i + 1];

                        var a_b = GoldenSection(
                            x => EvalSR(x, w0, w1),
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
                #region public Tuple<double, Vector<double>> GetMinVar()
                public Tuple<double, Vector<double>> GetMinVar()
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
            #region public class Portfolio
            /// <summary>
            /// Container to hold Markowitz Portfolio
            /// </summary>
            public class Portfolio
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
                /// <summary>
                /// Weights meeting constraints
                /// </summary>
                public bool IsValid = true;

                /// <summary>
                /// Convert portfolio to human-readable string.
                /// </summary>
                /// <returns>portfolio string</returns>
                override public string ToString()
                {
                    string retvalue = string.Format("Return={0:P2}, Risk={1:P2}", Return, Risk);

                    foreach (var i in Weights.Keys)
                        if (Weights[i] > 0.0)
                            retvalue += string.Format(", {0}={1:P2}", i.Symbol, Weights[i]);

                    return retvalue;
                }
            }
            #endregion

            #region internal data
            private CLA _cla;
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

                int numParams = Enumerable.Range(0, _instruments.Count)
                    .Where(i => upperBound[i] - lowerBound[i] > 0.0)
                    .Count();

                _cla = new CLA(mean, covar, lowerBound, upperBound);
            }
            #endregion
            #region public IEnumerable<Portfolio> TurningPoints()
            /// <summary>
            /// Return all turning points for efficient frontier.
            /// </summary>
            /// <returns>enumerable of portfolios</returns>
            public IEnumerable<Portfolio> TurningPoints()
            {
                foreach (var w in _cla.TurningPoints())
                {
                    var pf = new Portfolio
                    {
                        Return = _cla.CalcReturn(w),
                        Risk = _cla.CalcVolatility(w),
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

            #region public IEnumerable<Portfolio> EfficientFrontier(int points)
            /// <summary>
            /// Return efficient frontier, w/ specified # of points
            /// </summary>
            /// <param name="points">number of points</param>
            /// <returns>portfolios at each point</returns>
            public IEnumerable<Portfolio> EfficientFrontier(int points = 100)
            {
                foreach (var t in _cla.EfFrontier(points))
                {
                    var mu = t.Item1;
                    var sigma = t.Item2;
                    var w = t.Item3;

                    var pf = new Portfolio
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
            #region public Portfolio MaximumSharpeRatio()
            /// <summary>
            /// Return portfolio w/ maximum sharpe ratio.
            /// </summary>
            /// <returns>portfolio</returns>
            public Portfolio MaximumSharpeRatio()
            {
                var p = _cla.GetMaxSR();

                var pf = new Portfolio
                {
                    Return = _cla.CalcReturn(p.Item2),
                    Risk = _cla.CalcVolatility(p.Item2),
                    //Sharpe = p.Item1,
                    Weights = Enumerable.Range(0, p.Item2.Count)
                        .ToDictionary(
                            idx => _instruments[idx],
                            idx => p.Item2[idx])
                };

                return pf;
            }
            #endregion
            #region public Portfolio MinimumVariance()
            /// <summary>
            /// Return portfolio w/ minimum variance.
            /// </summary>
            /// <returns>portfolio</returns>
            public Portfolio MinimumVariance()
            {
                var p = _cla.GetMinVar();

                var pf = new Portfolio
                {
                    Return = _cla.CalcReturn(p.Item2),
                    Risk = Math.Sqrt(p.Item1),
                    Weights = Enumerable.Range(0, p.Item2.Count)
                        .ToDictionary(
                            idx => _instruments[idx],
                            idx => p.Item2[idx])
                };

                return pf;
            }
            #endregion
            #region public Portfolio TargetVolatility(double targetRisk)
            /// <summary>
            /// Return portfolio with the specified risk (or less). Note that
            /// the weights of this portfolio might not add up to 1.0:
            /// This routine will return a portfolio on the capital allocation
            /// line, if the target risk is lower than the risk of the
            /// portfolio with the maximum Sharpe Ratio.
            /// </summary>
            /// <param name="targetRisk">risk setting</param>
            /// <returns>portfolio</returns>
            public Portfolio TargetVolatility(double targetRisk)
            {
                var maxSR = MaximumSharpeRatio();

                if (targetRisk <= maxSR.Risk)
                {
                    //----- return diluted Max-SR portfolio
                    var scaleDown = maxSR.Risk / targetRisk;

                    var cal = new Portfolio
                    {
                        Return = maxSR.Return / scaleDown,
                        Risk = targetRisk,
                        Weights = maxSR.Weights.Keys
                            .ToDictionary(
                                i => i,
                                i => maxSR.Weights[i] / scaleDown),
                    };

                    return cal;
                }
                else
                {
                    //----- desired portfolio is on the efficient frontier

                    // there will always be a wlo, as we handle
                    // low volatilities above
                    var wlo = _cla.TurningPoints()
                        .Select(w => new { weights = w, vol = _cla.CalcVolatility(w) })
                        .Where(w => w.vol < targetRisk)
                        .OrderByDescending(t => t.vol)
                        .Select(t => t.weights)
                        .First();

                    // there might not be a whi with risk 
                    // as high as targetRisk
                    var whi = _cla.TurningPoints()
                        .Select(w => new { weights = w, vol = _cla.CalcVolatility(w) })
                        .Where(t => t.vol >= targetRisk)
                        .OrderBy(t => t.vol)
                        .Select(t => t.weights)
                        .FirstOrDefault();

                    Vector<double> ww = null;

                    if (whi != null)
                    {
                        // interpolation between wlo and whi
                        // this is not a linear interpolation, which is
                        // why we need to use GoldenSection here
                        var xx = _cla.GoldenSection(
                                x =>
                                {
                                    var w = whi != null
                                        ? wlo.Multiply(x).Add(whi.Multiply(1.0 - x))
                                        : wlo;
                                    return Math.Abs(_cla.CalcVolatility(w) - targetRisk);
                                },
                                0.0, 1.0,
                                true)
                            .Item1;

                        ww = wlo.Multiply(xx).Add(whi.Multiply(1.0 - xx));
                    }
                    else
                    {
                        // no whi: simply use wlo
                        ww = wlo;
                    }

                    var pf = new Portfolio
                    {
                        Return = _cla.CalcReturn(ww),
                        Risk = _cla.CalcVolatility(ww),
                        Weights = Enumerable.Range(0, ww.Count)
                        .ToDictionary(
                            idx => _instruments[idx],
                            idx => ww[idx])
                    };

                    return pf;

                    // simple but inefficient method to do this
                    //
                    //var ef = EfficientFrontier(250)
                    //    .ToList();
                    //
                    //return ef
                    //    .Where(pf => pf.Risk <= targetRisk)
                    //    .OrderByDescending(pf => pf.Risk)
                    //    .First();
                }
            }
            #endregion

            #region public Portfolio EvalPositions(double netAssetValue)
            /// <summary>
            /// Evaluate the current positions.
            /// </summary>
            /// <param name="netAssetValue">current net asset value</param>
            /// <returns>portfolio</returns>
            public Portfolio EvalPositions(double netAssetValue)
            {
                var w = Vector<double>.Build.Dense(
                    _instruments.Count,
                    idx => _instruments[idx].Position * _instruments[idx].Close[0] / netAssetValue);

                var pf = new Portfolio
                {
                    Return = _cla.CalcReturn(w),
                    Risk = _cla.CalcVolatility(w),
                    Weights = Enumerable.Range(0, _instruments.Count)
                        .ToDictionary(
                            idx => _instruments[idx],
                            idx => w[idx]),
                    IsValid = _cla.CheckValidity(w),
                };

                return pf;
            }
            #endregion
        }
        #endregion

        #region public class PortfolioCovariance
        /// <summary>
        /// Class encapsulating calculation of covariance matrix.
        /// </summary>
        public class Covariance
        {
            #region internal stuff
            private List<Instrument> _instruments;
            private Dictionary<Instrument, Dictionary<Instrument, double>> _covariance;

            private void calc(IEnumerable<Instrument> universe, int numBars, int barSize, Func<Instrument, ITimeSeries<double>> priceFunc)
            {
                // save instruments, ordered by their hash code
                // this is important, so that we can define a 
                // 'first' and 'second' instrument during lookup
                _instruments = universe
                    .OrderBy(i => i.GetHashCode())
                    .ToList();

                NumBars = numBars;
                BarSize = barSize;

#if false
                // BUGBUG: this code currently does not work

                // save the price series, so that we may use indicators
                // in the priceFunc lambda expression, without evaluating
                // them multiple times per bar
                Dictionary<Instrument, ITimeSeries<double>> priceSeries = universe
                    .ToDictionary(
                        i => i,
                        i => priceFunc(i).LogMomentum(barSize));

                _covariance = new Dictionary<Instrument, Dictionary<Instrument, double>>();

                for (int i1 = 0; i1 < _instruments.Count; i1++)
                {
                    Instrument instrument1 = _instruments[i1];
                    _covariance[instrument1] = new Dictionary<Instrument, double>();

                    for (int i2 = i1; i2 < _instruments.Count; i2++)
                    {
                        Instrument instrument2 = _instruments[i2];
                        _covariance[instrument1][instrument2] =
                            priceSeries[instrument1].Covariance(priceSeries[instrument2], numBars, barSize)[0];
                    }
                }
#else
                // save the price series, so that we may use indicators
                // in the priceFunc lambda expression, without evaluating
                // them multiple times per bar
                Dictionary<Instrument, ITimeSeries<double>> priceSeries = universe
                    .ToDictionary(
                        i => i,
                        i => priceFunc(i));

                _covariance = new Dictionary<Instrument, Dictionary<Instrument, double>>();

                for (int i1 = 0; i1 < _instruments.Count; i1++)
                {
                    Instrument instrument1 = _instruments[i1];

                    var series1 = Enumerable.Range(0, NumBars)
                        .Select(b => Math.Log(
                            priceSeries[instrument1][b * BarSize]
                            / priceSeries[instrument1][(b + 1) * BarSize]))
                        .ToList();
                    var average1 = series1.Average();

                    // create a new row for our matrix
                    if (!_covariance.ContainsKey(instrument1))
                        _covariance[instrument1] = new Dictionary<Instrument, double>();

                    for (int i2 = i1; i2 < _instruments.Count; i2++)
                    {
                        Instrument instrument2 = _instruments[i2];

                        var series2 = Enumerable.Range(0, NumBars)
                            .Select(b => Math.Log(
                                priceSeries[instrument2][b * BarSize]
                                / priceSeries[instrument2][(b + 1) * BarSize]))
                            .ToList();
                        var average2 = series2.Average();

                        _covariance[instrument1][instrument2] = Enumerable.Range(0, NumBars)
                            .Sum(i => (series1[i] - average1) * (series2[i] - average2))
                            / (NumBars - 1.0);
                    }
                }
#endif
            }
            #endregion


            #region public PortfolioCovariance(IEnumerable<Instrument> universe, int numBars, int barSize = 1)
            /// <summary>
            /// Create new covariance object. Subsample the instrument bars, to create 
            /// bars with a larger size, if desired.
            /// <see href="https://en.wikipedia.org/wiki/Covariance"/>
            /// </summary>
            /// <param name="universe">universe of instruments</param>
            /// <param name="numBars"># of bars to calculate</param>
            /// <param name="barSize"># of bars between points, default = 1</param>
            public Covariance(IEnumerable<Instrument> universe, int numBars, int barSize = 1)
            {
                calc(universe, numBars, barSize, i => i.Close);
            }

            /// <summary>
            /// Create new covariance object. Subsample the instrument bars, to create 
            /// bars with a larger size, if desired.
            /// <see href="https://en.wikipedia.org/wiki/Covariance"/>
            /// </summary>
            /// <param name="universe">universe of instruments</param>
            /// <param name="numBars"># of bars to calculate</param>
            /// <param name="barSize"># of bars between points, default = 1</param>
            /// <param name="priceFunc">predicate </param>
            public Covariance(IEnumerable<Instrument> universe, int numBars, int barSize, Func<Instrument, ITimeSeries<double>> priceFunc)
            {
                calc(universe, numBars, barSize, priceFunc);
            }
            #endregion
            #region public int NumBars
            /// <summary>
            /// Number of bars used to calculate covariance
            /// </summary>
            public int NumBars
            {
                get; private set;
            }
            #endregion
            #region public int BarSize
            /// <summary>
            /// Size of bars, as the number of bars from the instruments being subsampled,
            /// in order to calculate co-variance. Default is 1.
            /// </summary>
            public int BarSize
            {
                get; private set;
            }
            #endregion

            #region public double this[Instrument instrument1, Instrument instrument2]
            /// <summary>
            /// Retrieve covariance.
            /// </summary>
            /// <param name="instrument1">instrument #1</param>
            /// <param name="instrument2">instrument #2</param>
            /// <returns>covariance</returns>
            public double this[Instrument instrument1, Instrument instrument2]
            {
                get
                {
                    Instrument x1 = instrument1.GetHashCode() < instrument2.GetHashCode() ? instrument1 : instrument2;
                    Instrument x2 = instrument1.GetHashCode() >= instrument2.GetHashCode() ? instrument1 : instrument2;

                    return _covariance[x1][x2];
                }
            }
            #endregion
        }
        #endregion
    }

#if false
    // DEPRECATED as of 04/2020
    // use functionality from IndicatorsCorrelation instead

    /// <summary>
    /// Collection of portfolio-related indicators
    /// </summary>
    public static class IndicatorsPortfolio
    {
    #region public static ITimeSeries<double> Covariance(this Instrument series, Instrument otherSeries, int n = 10)
        /// <summary>
        /// Calculate historical covariance.
        /// </summary>
        /// <param name="series">primary instrument</param>
        /// <param name="otherSeries">other instrument</param>
        /// <param name="n">length</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>volatility as time series</returns>
        public static ITimeSeries<double> Covariance(this Instrument series, Instrument otherSeries, int n = 10,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), otherSeries.GetHashCode(), n);

            return IndicatorsBasic.BufferedLambda(
                (p) =>
                {
                    List<Instrument> instruments = new List<Instrument> { series, otherSeries };
                    var c = new PortfolioSupport.Covariance(instruments, n, 1);
                    var coVar = c[series, otherSeries];
                    return coVar;
                }, 0.0,
                cacheId);
        }
    #endregion
    #region public static ITimeSeries<double> Correlation(this Instrument series, Instrument otherSeries, int n = 10)
        /// <summary>
        /// Calculate historical correlation.
        /// </summary>
        /// <param name="series">primary instrument</param>
        /// <param name="otherSeries">other instrument</param>
        /// <param name="n">length</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>volatility as time series</returns>
        public static ITimeSeries<double> Correlation(this Instrument series, Instrument otherSeries, int n = 10,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), otherSeries.GetHashCode(), n);

            return IndicatorsBasic.BufferedLambda(
                (p) =>
                {
                    List<Instrument> instruments = new List<Instrument> { series, otherSeries };
                    var c = new PortfolioSupport.Covariance(instruments, n, 1);
                    var coVar = c[series, otherSeries];
                    var var = c[series, series];
                    var otherVar = c[otherSeries, otherSeries];
                    return coVar / Math.Sqrt(var) / Math.Sqrt(otherVar);
                }, 0.0, 
                cacheId);
        }
    #endregion
    }
#endif
}

//==============================================================================
// end of file