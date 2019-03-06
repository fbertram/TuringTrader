//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PortfolioSupport
// Description: portfolio support functionality
// History:     2019ii03, FUB, created
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
            public double? Return = null;
            /// <summary>
            /// Portfolio risk (sigma)
            /// </summary>
            public double? Risk = null;
            /// <summary>
            /// Portfolio Sharpe Ratio
            /// </summary>
            public double? Sharpe = null;
            /// <summary>
            /// Instrument weights
            /// </summary>
            public Dictionary<Instrument, double> Weights = null;
        }
        #endregion
        #region public class MarkowitzCLA
        public class MarkowitzCLA
        {
            private _MarkowitzCLA _cla;
            private List<Instrument> _instruments;

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

            public IEnumerable<MarkowitzPortfolio> TurningPoints()
            {
                for (int i = 0; i < _cla._w.Count; i++)
                {
                    var pf = new MarkowitzPortfolio
                    {
                        Weights = Enumerable.Range(0, _cla._w[i].Count)
                            .ToDictionary(
                                idx => _instruments[idx],
                                idx => _cla._w[i][idx]),
                    };

                    yield return pf;
                }

                yield break;
            }

            public IEnumerable<MarkowitzPortfolio> EfficientFrontier(int points)
            {
                yield break;
            }

            public MarkowitzPortfolio MaximumSharpeRatio()
            {
                return null;
            }

            public MarkowitzPortfolio MinimumVariance()
            {
                return null;
            }
        }
        #endregion

        #region private class _MarkowitzCLA
        private class _MarkowitzCLA
        {
            public Vector<double> _mean;
            public Matrix<double> _covar;
            public Vector<double> _lb;
            public Vector<double> _ub;

            public List<Vector<double>> _w; // solution
            public List<double?> _l; // lambdas
            public List<double?> _g; // gammas
            public List<List<int>> _f; // free weights

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
                    /*----------
                    In Snippet 3 we saw that the value of 𝜆 which results from 
                    each candidate i is stored in variable l. Among those 
                    values of l, we find the maximum, store it as l_out, and 
                    denote as i_out our candidate to become free. This is only 
                    a candidate for addition into F, because before making that 
                    decision we need to consider the possibility that one item 
                    is removed from F, as follows. 
 
                    After the first run of this iteration, it is also conceivable 
                    that one asset in F moves to one of its boundaries. Should 
                    that be the case, Snippet 7 determines which asset would do 
                    so. Similarly to the addition case, we search for the 
                    candidate that, after removal, maximizes 𝜆 (or to be more 
                    precise, minimizes the reduction in 𝜆, since we know that 𝜆 
                    becomes smaller at each iteration). We store our candidate 
                    for removal in the variable i_in, and the associated 𝜆 in 
                    the variable l_in. 
                    */
                    /*
                            #1) case a): Bound one free weight
                            l_in=None
                            if len(f)>1:
                                covarF,covarFB,meanF,wB=self.getMatrices(f)
                                covarF_inv=np.linalg.inv(covarF)
                                j=0
                                for i in f:
                                    l,bi=self.computeLambda(covarF_inv,covarFB,meanF,wB,j,[self.lB[i],self.uB[i]])
                                    if l>l_in:l_in,i_in,bi_in=l,i,bi
                                    j+=1
                    */

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



                    /*----------
                    The transition from one turning point to the next requires 
                    that one element is either added to or removed from the 
                    subset of free assets, F. Because 𝜆 and 𝜔′𝜇 are linearly 
                    and positively related, this means that each subsequent 
                    turning point will lead to a lower value for 𝜆. This recursion 
                    of adding or removing one asset from F continues until the 
                    algorithm determines that the optimal expected return cannot 
                    be further reduced. In the first run of this iteration, the 
                    choice is simple: F has been initialized with one asset, 
                    and the only option is to add another one (F cannot be an 
                    empty set, or there would be no optimization).
                    */
                    /*
                    In this part of the code, we search within B for a candidate 
                    asset i to be added to F. That search only makes sense if B 
                    is not an empty set, hence the first if. Because F and B are 
                    complementary sets, we only need to keep track of one of them. 
                    In the code, we always derive B from F, thanks to the functions 
                    getB and diffLists.
                    */
                    /*
                            #2) case b): Free one bounded weight
                            l_out=None
                            if len(f)<self.mean.shape[0]:
                                b=self.getB(f)
                                for i in b:
                                    covarF,covarFB,meanF,wB=self.getMatrices(f+[i])
                                    covarF_inv=np.linalg.inv(covarF)
                                    l,bi=self.computeLambda(covarF_inv,covarFB,meanF,wB,meanF.shape[0]-1, \
                                        self.w[-1][i])
                                    if (self.l[-1]==None or l<self.l[-1]) and l>l_out:l_out,i_out=l,i                
                    */

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

                    /*
                            if (l_in==None or l_in<0) and (l_out==None or l_out<0):
                                #3) compute minimum variance solution
                                self.l.append(0)
                                covarF,covarFB,meanF,wB=self.getMatrices(f)
                                covarF_inv=np.linalg.inv(covarF)
                                meanF=np.zeros(meanF.shape)
                            else:
                                #4) decide lambda
                                if l_in>l_out:
                                    self.l.append(l_in)
                                    f.remove(i_in)
                                    w[i_in]=bi_in # set value at the correct boundary
                                else:
                                    self.l.append(l_out)
                                    f.append(i_out)
                                covarF,covarFB,meanF,wB=self.getMatrices(f)
                                covarF_inv=np.linalg.inv(covarF)
                    */
                    /*
                            #5) compute solution vector
                            wF,g=self.computeW(covarF_inv,covarFB,meanF,wB)
                            for i in range(len(f)):w[f[i]]=wF[i]
                            self.w.append(np.copy(w)) # store solution
                            self.g.append(g)
                            self.f.append(f[:])
                            if self.l[-1]==0:break
                    */
                    if ((l_in == null || l_in < 0.0)
                    && (l_out == null || l_out < 0.0))
                    {
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
                /*
                    #6) Purge turning points
                    self.purgeNumErr(10e-10)
                    self.purgeExcess()
                */

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
                /*
                def computeBi(self,c,bi):
                    if c>0:
                        bi=bi[1][0]
                    if c<0:
                        bi=bi[0][0]
                    return bi
                 */

                return c > 0 ? bi[1] : bi[0];
            }
            #endregion
            #region private Tuple<Vector<double>, double> computeW(...)
            private Tuple<Vector<double>, double> computeW(
                Matrix<double> covarF_inv, Matrix<double> covarFB,
                Vector<double> meanF, Vector<double> wB)
            {
                /*
                def computeW(self,covarF_inv,covarFB,meanF,wB):
                    #1) compute gamma
                    onesF=np.ones(meanF.shape)
                    g1=np.dot(np.dot(onesF.T,covarF_inv),meanF)
                    g2=np.dot(np.dot(onesF.T,covarF_inv),onesF)
                    if wB==None:
                        g,w1=float(-self.l[-1]*g1/g2+1/g2),0
                    else:
                        onesB=np.ones(wB.shape)
                        g3=np.dot(onesB.T,wB)
                        g4=np.dot(covarF_inv,covarFB)
                        w1=np.dot(g4,wB)
                        g4=np.dot(onesF.T,w1)
                        g=float(-self.l[-1]*g1/g2+(1-g3+g4)/g2)
                    #2) compute weights
                    w2=np.dot(covarF_inv,onesF)
                    w3=np.dot(covarF_inv,meanF)
                    return -w1+g*w2+self.l[-1]*w3,g
                */

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
                /*----------
                Using the matrices provided by the function getMatrices, 𝜆 can 
                be computed as:   
                <snip>
                A proof of these expressions can be found in [11]. Eq. (4) is 
                implemented in function computeLambda. We have computed some 
                intermediate variables, which can be re-used at various points 
                in order to accelerate the calculations. With the value of 𝜆, 
                this function also returns 𝑏𝑖                
                */
                /*
                def computeLambda(self,covarF_inv,covarFB,meanF,wB,i,bi):
                    #1) C
                    onesF=np.ones(meanF.shape)
                    c1=np.dot(np.dot(onesF.T,covarF_inv),onesF)
                    c2=np.dot(covarF_inv,meanF)
                    c3=np.dot(np.dot(onesF.T,covarF_inv),meanF)
                    c4=np.dot(covarF_inv,onesF)
                    c=-c1*c2[i]+c3*c4[i]
                    if c==0:return None,None
                    #2) bi
                    if type(bi)==list:bi=self.computeBi(c,bi)
                    #3) Lambda
                    if wB==None:
                        # All free assets
                        return float((c4[i]-c1*bi)/c),bi
                    else:
                        onesB=np.ones(wB.shape)
                        l1=np.dot(onesB.T,wB)
                        l2=np.dot(covarF_inv,covarFB)
                        l3=np.dot(l2,wB)
                        l2=np.dot(onesF.T,l3)
                        return float(((1-l1+l2)*c4[i]-c1*(bi+l3[i]))/c),bi
                */

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
                /*
                def purgeNumErr(self,tol):
                    # Purge violations of inequality constraints (associated with ill-conditioned covar matrix)
                    i=0
                    while True:
                        flag=False
                        if i==len(self.w):break
                        if abs(sum(self.w[i])-1)>tol:
                            flag=True
                        else:
                            for j in range(self.w[i].shape[0]):
                                if self.w[i][j]-self.lB[j]<-tol or self.w[i][j]-self.uB[j]>tol:
                                    flag=True;break
                        if flag==True:
                            del self.w[i]
                            del self.l[i]
                            del self.g[i]
                            del self.f[i]
                        else:
                            i+=1
                    return
                */

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
                /*
                def purgeExcess(self):
                    # Remove violations of the convex hull
                    i,repeat=0,False
                    while True:
                        if repeat==False:i+=1
                        if i==len(self.w)-1:break
                        w=self.w[i]
                        mu=np.dot(w.T,self.mean)[0,0]
                        j,repeat=i+1,False
                        while True:
                            if j==len(self.w):break
                            w=self.w[j]
                            mu_=np.dot(w.T,self.mean)[0,0]
                            if mu<mu_:
                                del self.w[i]
                                del self.l[i]
                                del self.g[i]
                                del self.f[i]
                                repeat=True
                                break
                            else:
                                j+=1
                    return
                */

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
            private double evalSR(Vector<double> mean, Matrix<double> covar, double a, Vector<double> w0, Vector<double> w1)
            {
                /*
                def evalSR(self,a,w0,w1):
                    # Evaluate SR of the portfolio within the convex combination
                    w=a*w0+(1-a)*w1
                    b=np.dot(w.T,self.mean)[0,0]
                    c=np.dot(np.dot(w.T,self.covar),w)[0,0]**.5
                    return b/c
                */

                // Evaluate SR of the portfolio within the convex combination
                var w = w0.Multiply(a).Add(w1.Multiply(1.0 - a));
                var b = w.ToRowMatrix().Multiply(mean).Single();
                var c = Math.Sqrt(w.ToRowMatrix().Multiply(covar).Multiply(w).Single());

                return b / c;
            }
            #endregion
            #region private void goldenSection()
            private Tuple<double, double> goldenSection(Func<double, double> obj, double a, double b, bool minimum = false)
            {
                /*
                def goldenSection(self,obj,a,b,**kargs):
                    # Golden section method. Maximum if kargs['minimum']==False is passed 
                    from math import log,ceil
                    tol,sign,args=1.0e-9,1,None
                    if 'minimum' in kargs and kargs['minimum']==False:sign=-1
                    if 'args' in kargs:args=kargs['args']
                    numIter=int(ceil(-2.078087*log(tol/abs(b-a))))
                    r=0.618033989
                    c=1.0-r
                    # Initialize
                    x1=r*a+c*b;x2=c*a+r*b
                    f1=sign*obj(x1,*args);f2=sign*obj(x2,*args)
                    # Loop
                    for i in range(numIter):
                        if f1>f2:
                            a=x1
                            x1=x2;f1=f2
                            x2=c*a+r*b;f2=sign*obj(x2,*args)
                        else:
                            b=x2
                            x2=x1;f2=f1
                            x1=r*a+c*b;f1=sign*obj(x1,*args)
                    if f1<f2:return x1,sign*f1
                    else:return x2,sign*f2
                */

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

#if false
            #region public List<MarkowitzPortfolio> TurningPoints()
            public List<MarkowitzPortfolio> TurningPoints()
            {
                var turningPoints = new List<MarkowitzPortfolio>();

                for (int i = 0; i < _w.Count; i++)
                {
                    var turningPoint = new MarkowitzPortfolio
                    {
                        Return = 0.0,
                        Risk = 0.0,
                        Weights = new Dictionary<Instrument, double>(_w[i]),
                    };

                    turningPoints.Add(turningPoint);
                }

                return turningPoints;
            }
            #endregion
            #region public List<MarkowitzPortfolio> EfficientFrontier(int points)
            public List<MarkowitzPortfolio> EfficientFrontier(int points)
            {
                /*
                def efFrontier(self,points):
                    /# Get the efficient frontier
                    mu,sigma,weights=[],[],[]
                    a=np.linspace(0,1,points/len(self.w))[:-1] # remove the 1, to avoid duplications
                    b=range(len(self.w)-1)
                    for i in b:
                        w0,w1=self.w[i],self.w[i+1]
                        if i==b[-1]:a=np.linspace(0,1,points/len(self.w)) # include the 1 in the last iteration
                        for j in a:
                            w=w1*j+(1-j)*w0
                            weights.append(np.copy(w))
                            mu.append(np.dot(w.T,self.mean)[0,0])
                            sigma.append(np.dot(np.dot(w.T,self.covar),w)[0,0]**.5)
                    return mu,sigma,weights
                */

                var efFrontier = new List<MarkowitzPortfolio>();

                var n = points / _w.Count;
                var a = Enumerable.Range(0, n)
                    .Take(n - 1) // remove the 1, to avoid duplications
                    .Select(i => (double)i / (n - 1))
                    .ToList();
                var b = Enumerable.Range(0, _w.Count - 1)
                    .ToList();

                // create matrix objects
                var instruments = _w[0].Keys.ToList();
                var mean = Vector<double>.Build.Dense(instruments.Count, x => _mean[instruments[x]]);
                var covar = Matrix<double>.Build.Dense(
                    instruments.Count, instruments.Count,
                    (i, j) => _covar[instruments[i]][instruments[j]]);

                foreach (var i in b)
                {

                    var w0 = Vector<double>.Build.Dense(instruments.Count, x => _w[i][instruments[x]]);
                    var w1 = Vector<double>.Build.Dense(instruments.Count, x => _w[i + 1][instruments[x]]);

                    if (i == b.Last())
                        a.Add(1.0); // include the 1 in the last iteration

                    foreach (var j in a)
                    {
                        var w = w1.Multiply(j).Add(w0.Multiply(1.0 - j));
                        var mu = w.ToRowMatrix().Multiply(mean).Single();
                        var sigma = Math.Sqrt(w.ToRowMatrix().Multiply(covar).Multiply(w).Single());

                        var portfolio = new MarkowitzPortfolio
                        {
                            Return = mu,
                            Risk = sigma,
                            Weights = Enumerable.Range(0, instruments.Count)
                                .ToDictionary(
                                    x => instruments[x],
                                    x => w[x]),
                        };

                        efFrontier.Add(portfolio);
                    }
                }

                return efFrontier;
            }
            #endregion
            #region private void MaximumSharpeRatio()
            public MarkowitzPortfolio MaximumSharpeRatio()
            {
                /*
                def getMaxSR(self):
                    /# Get the max Sharpe ratio portfolio
                    /#1) Compute the local max SR portfolio between any two neighbor turning points
                    w_sr,sr=[],[]
                    for i in range(len(self.w)-1):
                        w0=np.copy(self.w[i])
                        w1=np.copy(self.w[i+1])
                        kargs={'minimum':False,'args':(w0,w1)}
                        a,b=self.goldenSection(self.evalSR,0,1,**kargs)
                        w_sr.append(a*w0+(1-a)*w1)
                        sr.append(b)
                    return max(sr),w_sr[sr.index(max(sr))]
                */

                var portfolioCandidates = new List<MarkowitzPortfolio>();

                var instruments = _w[0].Keys.ToList();
                var mean = Vector<double>.Build.Dense(instruments.Count, x => _mean[instruments[x]]);
                var covar = Matrix<double>.Build.Dense(
                    instruments.Count, instruments.Count,
                    (i, j) => _covar[instruments[i]][instruments[j]]);

                for (var i = 0; i < _w.Count - 1; i++)
                {
                    var w0 = Vector<double>.Build.Dense(instruments.Count, x => _w[i][instruments[x]]);
                    var w1 = Vector<double>.Build.Dense(instruments.Count, x => _w[i + 1][instruments[x]]);

                    var a_b = goldenSection(
                        x => evalSR(mean, covar, x, w0, w1),
                        0.0, 1.0,
                        false);
                    var a = a_b.Item1;
                    var b = a_b.Item2;

                    var w = w0.Multiply(a).Add(w1.Multiply(1.0 - a));

                    var portfolio = new MarkowitzPortfolio
                    {
                        Sharpe = b,
                        Weights = Enumerable.Range(0, instruments.Count)
                            .ToDictionary(
                                x => instruments[x],
                                x => w[x]),

                    };

                    portfolioCandidates.Add(portfolio);
                }

                var max = portfolioCandidates.Max(p => p.Sharpe);
                var index = portfolioCandidates.FindIndex(p => p.Sharpe == max);

                return portfolioCandidates[index];
            }
            #endregion
            #region public void MinimumVariance()
            public MarkowitzPortfolio MinimumVariance()
            {
                /*
                def getMinVar(self):
                    /# Get the minimum variance solution
                    var=[]
                    for w in self.w:
                        a=np.dot(np.dot(w.T,self.covar),w)
                        var.append(a)
                    return min(var)**.5,self.w[var.index(min(var))]
                */

                var variance = new List<double>();

                foreach (var wx in _w)
                {
                    var instruments = _w[0].Keys.ToList();
                    var covar = Matrix<double>.Build.Dense(
                        instruments.Count, instruments.Count,
                        (i, j) => _covar[instruments[i]][instruments[j]]);
                    var w = Vector<double>.Build.Dense(instruments.Count, x => wx[instruments[x]]);

                    var a = w.ToRowMatrix().Multiply(covar).Multiply(w).Single();
                    variance.Add(a);
                }

                var min = variance.Min();
                var index = variance.FindIndex(v => v == min);
                return new MarkowitzPortfolio
                {
                    Risk = Math.Sqrt(min),
                    Weights = new Dictionary<Instrument, double>(_w[index]),
                };
            }
            #endregion
#endif
        }
        #endregion
    }
}

//==============================================================================
// end of file