//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        PortfolioSupport
// Description: unit test for portfolio support class
// History:     2019iii06, FUB, created
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

#region libraries
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Simulator;
#endregion

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class PortfolioSupport
    {
        #region Test_MarkowitzCLA
        // test vectors taken from here: 
        // https://github.com/lequant40/portfolio_allocation_js/blob/master/test/tests_allocation_dev.js
        // also check implementation here, around line #1900
        // https://github.com/lequant40/portfolio_allocation_js/blob/master/lib/allocation/mean-variance.js#L112
        [TestMethod]
        public void Test_MarkowitzCLA()
        {
            // the instruments serves no function, other than as a key
            Dictionary<Instrument, int> instruments = Enumerable.Range(0, 10)
                .ToDictionary(i =>
                {
                    Dictionary<DataSourceParam, string> info = new Dictionary<DataSourceParam, string>
                    {
                        { DataSourceParam.name, string.Format("X{0}", i) },
                        { DataSourceParam.nickName, string.Format("X{0}", i) },
                    };
                    var dataSource = new DataSourceFromBars(null, info);

                    return new Instrument(null, dataSource);
                },
                i => i);

            #region test vector #1 (William F. Sharpe)
            {
                // taken from https://web.stanford.edu/~wfsharpe/mia/opt/mia_opt3.htm
                double[] mean =
                {
                    2.8000,
                    6.3000,
                    10.8000
                };

                /*double[] sd =
                {   // from source above, translated to covar matrix
                    1.0000,
                    7.4000,
                    15.4000,
                };

                double[,] corr =
                {   // from source above, translated to covar matrix
                    { 1.0000, 0.4000, 0.1500 },
                    { 0.4000, 1.0000, 0.3500 },
                    { 0.1500, 0.3500, 1.0000 },
                };*/

                double[,] covar =
                {   // from lequant40, should equal sd and corr above
                    { 1, 2.96, 2.31 },
                    { 2.96, 54.76, 39.886 },
                    { 2.31,  39.886, 237.16 },
                };

                double[,] expectedWeights =
                {   // => from source above
                    { 0.2, 0.30000000000000004, 0.5 },
                    { 0.2, 0.5, 0.30000000000000004 },
                    { 0.22180737780348653, 0.5, 0.27819262219651353},
                    { 0.451915610952186, 0.348084389047814, 0.2},
                    { 0.5, 0.2999999999999999, 0.2},
                };

                /* double[] expectedIDontKnow =
                {   // from lequant40
                    20.898844444444443,
                    11.1475,
                    10.51088812347172,
                    7.55192170004087,
                    0,
                };*/

                /* double[] expectedRiskTolerance =
                {   // => see source above, unclear how this is calculated
                    41.80,
                    22.94,
                    22.30,
                    21.02,
                    15.10,
                    13.73,
                };*/

                double[] expectedReturn =
                {   // added by FUB
                    7.85,
                    6.95000000000006,
                    6.77554097757211,
                    5.61829536166734,
                    5.45,
                };

                double[] expectedRisk =
                {   // added by FUB
                    8.77732305432584,
                    6.92166165021094,
                    6.64310912002921,
                    4.81952189552386,
                    4.56082448686638,
                };

                var cla = new TuringTrader.Support.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(3),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.2,
                    i => 0.5);

                var turningPoints = cla.TurningPoints().ToList();

                for (int i = 0; i < turningPoints.Count(); i++)
                {
                    var turningPoint = turningPoints[i];

                    foreach (var j in turningPoint.Weights.Keys)
                    {
                        Assert.IsTrue(Math.Abs(turningPoint.Weights[j]
                            - expectedWeights[i, instruments[j]]) < 1e-5);
                    }

                    Assert.IsTrue(Math.Abs(turningPoint.Risk
                        - expectedRisk[i]) < 1e-5);

                    Assert.IsTrue(Math.Abs(turningPoint.Return
                        - expectedReturn[i]) < 1e-5);
                }
            }
            #endregion
            #region test vector #2 (H. Markowitz)
            {
                // Reference: Portfolio Selection, H. Markowitz example, chapter VIII "The computing procedure"
                double[] mean =
                {
                    0.062,
                    0.146,
                    0.128
                };

                double[,] covar =
                {
                    { 0.0146, 0.0187, 0.0145 },
                    { 0.0187, 0.0854, 0.0104 },
                    { 0.0145, 0.0104, 0.0289 },
                };

                double[,] expectedWeights =
                {
                    { 0, 1, 0 },
                    { 0, 0.22496808316614988, 0.7750319168338501 },
                    { 0.8414051841746248, 0, 0.15859481582537516 },
                    { 0.9931034482758623, 0, 0.006896551724137813 },
                };

                /* double[] expectedIDontKnow =
                {
                    4.16666666666667,
                    0.1408064320019454,
                    0.03332764893133244,
                    0,
                };*/

                double[] expectedReturn =
                {
                    0.146,
                    0.132049425496991,
                    0.0724672578444748,
                    0.0624551724137931,
                };

                double[] expectedRisk =
                {
                    0.292232783924048,
                    0.159085749259056,
                    0.122200612163491,
                    0.120827605888835,
                };


                var cla = new TuringTrader.Support.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(3),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.0,
                    i => 1.0);

                var turningPoints = cla.TurningPoints().ToList();

                for (int i = 0; i < turningPoints.Count(); i++)
                {
                    var turningPoint = turningPoints[i];

                    foreach (var j in turningPoint.Weights.Keys)
                    {
                        Assert.IsTrue(Math.Abs(turningPoint.Weights[j]
                            - expectedWeights[i, instruments[j]]) < 1e-5);
                    }

                    Assert.IsTrue(Math.Abs(turningPoint.Risk
                        - expectedRisk[i]) < 1e-5);

                    Assert.IsTrue(Math.Abs(turningPoint.Return
                        - expectedReturn[i]) < 1e-5);
                }
            }
            #endregion
            #region test vector #3 (Clarence C. Kwan)
            {
                // Reference: A Simple Spreadsheet-Based Exposition of the Markowitz Critical Line Method for Portfolio Selection, Clarence C. Kwan
                double[] mean =
                {
                    0.05,
                    0.08,
                    0.12
                };

                double[,] covar =
                {
                    { 0.0004, 0.0004, 0.0002 },
                    { 0.0004, 0.0025,0.001 },
                    { 0.0002, 0.001, 0.01 },
                };

                double[,] expectedWeights =
                {
                    { 0, 0, 1 },
                    { 0, 0.6485013623978204, 0.3514986376021796 },
                    //{ 0.9754098360655736, 0, 0.024590163934426246 }, // FUB removed
                    { 0.9799999999999999, 0, 0.02000000000000001 },
                };

                /* double[] expectedIDontKnow =
                {
                    0.22500000000000006,
                    0.05476839237057218,
                    0.0006557377049180337,
                    0,
                };*/

                double[] expectedReturn =
                {
                    0.12,
                    0.0940599455040872,
                    0.0514,
                };

                double[] expectedRisk =
                {
                    0.1,
                    0.052371677991768,
                    0.0198997487421324,
                };

                var cla = new TuringTrader.Support.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(3),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.0,
                    i => 1.0);

                var turningPoints = cla.TurningPoints().ToList();

                for (int i = 0; i < turningPoints.Count(); i++)
                {
                    var turningPoint = turningPoints[i];

                    foreach (var j in turningPoint.Weights.Keys)
                    {
                        Assert.IsTrue(Math.Abs(turningPoint.Weights[j]
                            - expectedWeights[i, instruments[j]]) < 1e-5);
                    }

                    Assert.IsTrue(Math.Abs(turningPoint.Risk
                        - expectedRisk[i]) < 1e-5);

                    Assert.IsTrue(Math.Abs(turningPoint.Return
                        - expectedReturn[i]) < 1e-5);
                }
            }
            #endregion
            #region test vector #4 (Clarence C. Kwan)
            {
                // Reference: A Simple Spreadsheet-Based Exposition of the Markowitz Critical Line Method for Portfolio Selection, Clarence C. Kwan
                double[] mean =
                {
                    0.05,
                    0.08,
                    0.12
                };

                double[,] covar =
                {
                    { 0.0004, 0.0004, 0.0002 },
                    { 0.0004, 0.0025,0.001 },
                    { 0.0002, 0.001, 0.01 },
                };

                double[,] expectedWeights =
                {
                    { 0, 0.30000000000000004, 0.7 },
                    { 0, 0.6485013623978203, 0.3514986376021798 },
                    //{ 0.7, 0.18310626702997274, 0.11689373297002724 }, // FUB removed
                    { 0.7, 0.2438095238095238, 0.05619047619047619 },
                };

                /* double[] expectedIDontKnow =
                {
                0.14625
                0.05476839237057221
                0.015934604904632152
                0
                };*/

                double[] expectedReturn =
                {   // FUB
                    0.108,
                    0.094059945504087,
                    0.061247619047619,
                };

                double[] expectedRisk =
                {   // FUB
                    0.0744647567645259,
                    0.0523716779917679,
                    0.0235764208277597,
                };

                var cla = new TuringTrader.Support.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(3),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.0,
                    i => 0.7);

                var turningPoints = cla.TurningPoints().ToList();

                for (int i = 0; i < turningPoints.Count(); i++)
                {
                    var turningPoint = turningPoints[i];

                    foreach (var j in turningPoint.Weights.Keys)
                    {
                        Assert.IsTrue(Math.Abs(turningPoint.Weights[j]
                            - expectedWeights[i, instruments[j]]) < 1e-5);
                    }

                    Assert.IsTrue(Math.Abs(turningPoint.Risk
                        - expectedRisk[i]) < 1e-5);

                    Assert.IsTrue(Math.Abs(turningPoint.Return
                        - expectedReturn[i]) < 1e-5);
                }
            }
            #endregion
            #region test vector #5 (David H. Bailey, Marcos Lopez de Prado)
            {
                // Reference: An Open-Source Implementation of the Critical-Line Algorithm for Portfolio Optimization, David H. Bailey and Marcos Lopez de Prado
                // section 5, A Numerical Example
                double[] mean =
                {
                    1.175,
                    1.19,
                    0.396,
                    1.12,
                    0.346,
                    0.679,
                    0.089,
                    0.73,
                    0.481,
                    1.08
                };

                double[,] covar =
                {
                    { 0.40755159,0.03175842,0.05183923,0.05663904,0.0330226,0.00827775,0.02165938,0.01332419,0.0343476,0.02249903 },
                    { 0.03175842,0.9063047,0.03136385,0.02687256,0.01917172,0.00934384,0.02495043,0.00761036,0.02874874,0.01336866 },
                    { 0.05183923,0.03136385,0.19490901,0.04408485,0.03006772,0.01322738,0.03525971,0.0115493,0.0427563,0.02057303 },
                    { 0.05663904,0.02687256,0.04408485,0.19528471,0.02777345,0.00526665,0.01375808,0.00780878,0.02914176,0.01640377 },
                    { 0.0330226,0.01917172,0.03006772,0.02777345,0.34059105,0.00777055,0.02067844,0.00736409,0.02542657,0.01284075 },
                    { 0.00827775,0.00934384,0.01322738,0.00526665,0.00777055,0.15983874,0.02105575,0.00518686,0.01723737,0.00723779 },
                    { 0.02165938,0.02495043,0.03525971,0.01375808,0.02067844,0.02105575,0.68056711,0.01377882,0.04627027,0.01926088 },
                    { 0.01332419,0.00761036,0.0115493,0.00780878,0.00736409,0.00518686,0.01377882,0.95526918,0.0106553,0.00760955 },
                    { 0.0343476,0.02874874,0.0427563,0.02914176,0.02542657,0.01723737,0.04627027,0.0106553,0.31681584,0.01854318 },
                    { 0.02249903,0.01336866,0.02057303,0.01640377,0.01284075,0.00723779,0.01926088,0.00760955,0.01854318,0.11079287 },
                };

                double[,] expectedWeights =
                {
                    { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
                    { 0.6493694070931811, 0.3506305929068189, 0, 0, 0, 0, 0, 0, 0, 0 },
                    { 0.4339841341086239, 0.23124750065448754, 0, 0.334768365236889, 0, 0, 0, 0, 0, 0 },
                    { 0.12688785385570883, 0.07234334721032556, 0, 0.28125374926334057, 0, 0, 0, 0, 0, 0.5195150496706249 },
                    { 0.12320100405906734, 0.07044407130753655, 0, 0.2789935668090118, 0, 0, 0, 0.006435564362887149, 0, 0.5209257934614971 },
                    { 0.0869215492990579, 0.050451042268558385, 0, 0.22359401742288823, 0, 0.17383161507156486, 0, 0.03017301555135618, 0, 0.4350287603865743 },
                    { 0.0846709411996219, 0.049253858741118525, 0, 0.21963390336360733, 0, 0.18003923464176064, 0, 0.03102980185535347, 0.006485702415438152, 0.42888655778310003 },
                    { 0.07378925302280315, 0.043828660769718863, 0, 0.19897560805881487, 0.026158159857441972, 0.19815187227970524, 0, 0.03341958639919798, 0.027902966026643668, 0.3977738935856743 },
                    { 0.06834400480527462, 0.041387026820649334, 0.015215259551836627, 0.18813443107045838, 0.03416248599274816, 0.20231943214747125, 0, 0.0339293235595669, 0.03363264959172938, 0.38287538646026537 },
                    { 0.03696858147921504, 0.02690083780081047, 0.0949424305647986, 0.1257759521946726, 0.0767460810325476, 0.21935567131616898, 0.029987096882220312, 0.035963284621386274, 0.06134983772972688, 0.29201022637845325 },
                };

                /* double[] expectedIDontKnow =
                {
                58.30308533333371
                4.1742728458857385
                1.9455661414558894
                0.16458117494477595
                0.1473887508934171
                0.056172204002751545
                0.05204819067458028
                0.03652161374727064
                0.030971168861678777
                0
                };*/

                double[] expectedReturn =
                {   // Table 2
                    1.18999999999995,
                    1.1802594588936,
                    1.16005645242179,
                    1.11126226427996,
                    1.10836023837479,
                    1.02248388944319,
                    1.01530592052246,
                    0.972720434034985,
                    0.949936815755027,
                    0.803215359876534,
                };

                double[] expectedRisk =
                {   // Table 2
                    0.952000367646947,
                    0.545656874268239,
                    0.41725565037332,
                    0.266719614211323,
                    0.265016995301471,
                    0.229680073596095,
                    0.22798274842085,
                    0.219554880082679,
                    0.216024571686891,
                    0.205237619813865,
                };

                var cla = new TuringTrader.Support.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(10),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.0,
                    i => 1.0);

                var turningPoints = cla.TurningPoints().ToList();

                for (int i = 0; i < turningPoints.Count(); i++)
                {
                    var turningPoint = turningPoints[i];

                    foreach (var j in turningPoint.Weights.Keys)
                    {
                        Assert.IsTrue(Math.Abs(turningPoint.Weights[j]
                            - expectedWeights[i, instruments[j]]) < 1e-5);
                    }

                    Assert.IsTrue(Math.Abs(turningPoint.Risk
                        - expectedRisk[i]) < 1e-5);

                    Assert.IsTrue(Math.Abs(turningPoint.Return
                        - expectedReturn[i]) < 1e-5);
                }
            }
            #endregion
            #region vector #6 (FUB, MarkowitzCLA: aborted after 201 iterations)
            {
                double[] mean = {
                    1.924775229385712E-001,
                    3.047470098437341E-002,
                    3.600448981380029E-002,
                    1.771499702814230E-002
                };
                double[,] covar = {
                    { 5.173614384902067E-003, 5.563709795663105E-003, 6.879256076222439E-005, 1.046411951839147E-006, },
                    { 5.563709795663105E-003, 1.192747981284741E-002, -2.159113135212455E-004, 8.210747330824326E-006, },
                    { 6.879256076222439E-005, -2.159113135212455E-004, 3.263193293253446E-004, 8.122710323505884E-006, },
                    { 1.046411951839147E-006, 8.210747330824326E-006, 8.122710323505884E-006, 8.273639320440565E-007, },
                };
                double[] lbound = {
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                };
                double[] ubound = {
                    7.500000000000000E-001,
                    3.000000000000000E-001,
                    1.000000000000000E+000,
                    1.000000000000000E+000,
                };

                var cla = new TuringTrader.Support.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(mean.Count()),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => lbound[instruments[i]],
                    i => ubound[instruments[i]]);

                var turningPoints = cla.TurningPoints().ToList();

                for (int i = 0; i < turningPoints.Count(); i++)
                {
                    var turningPoint = turningPoints[i];

                    foreach (var j in turningPoint.Weights.Keys)
                    {
                        //Assert.IsTrue(Math.Abs(turningPoint.Weights[j]
                        //    - expectedWeights[i, instruments[j]]) < 1e-5);
                    }

                    //Assert.IsTrue(Math.Abs(turningPoint.Risk
                    //    - expectedRisk[i]) < 1e-5);

                    //Assert.IsTrue(Math.Abs(turningPoint.Return
                    //    - expectedReturn[i]) < 1e-5);
                }
            }
            #endregion
            #region vector #6 (FUB, MarkowitzCLA: aborted after 601 iterations)
            {
                double[] mean = {
                    -1.452452351127411E+000,
                    -1.790134620283237E+000,
                    -1.748238206213537E+000,
                    2.763028800375313E-001,
                    1.219050250899282E-001,
                    -2.371535989891006E+000,
                    -2.362385628920209E+000,
                    -4.345226550223316E-001,
                    -1.917733811334479E+000,
                    -1.886615646689930E+000,
                    -1.016593945085099E+000,
                    -2.180839753535210E+000,
                };
                double[,] covar = {
                    { 4.107667557689510E-001, 3.532770338212708E-001, 3.920114515455653E-001, -6.397854059553235E-002, -2.342593174622205E-002, 6.235865058863653E-001, 5.103235914057721E-001, 5.769579319939092E-002, 4.637175699581909E-001, 2.234971094290169E-001, 3.931823752513509E-001, 4.236514050136496E-001, },
                    { 3.532770338212708E-001, 3.304477192804682E-001, 3.452738727085928E-001, -6.222848366575079E-002, -2.751843965308908E-002, 5.748535972479941E-001, 4.692451254532845E-001, 3.967304767223456E-002, 4.054424818170270E-001, 1.997384020454393E-001, 3.323921922263131E-001, 3.411631700478977E-001, },
                    { 3.920114515455653E-001, 3.452738727085928E-001, 4.317189033893861E-001, -7.400102899188042E-002, -2.850117040169742E-002, 6.039546143920506E-001, 5.630294416931898E-001, 2.936752803539871E-002, 4.389400300421475E-001, 1.982114609578972E-001, 3.294655288657495E-001, 4.162231314394415E-001, },
                    { -6.397854059553235E-002, -6.222848366575079E-002, -7.400102899188042E-002, 4.770878570857436E-002, 2.335142331965847E-002, -1.189553139504919E-001, -1.106745510298056E-001, -1.269822420551352E-002, -7.111001726644241E-002, -3.499608798785736E-002, -5.336890446270736E-002, -6.281607416760732E-002, },
                    { -2.342593174622205E-002, -2.751843965308908E-002, -2.850117040169742E-002, 2.335142331965847E-002, 1.452926950496856E-002, -5.995475036431692E-002, -4.293730406468507E-002, -8.729972561009053E-003, -2.665821670530143E-002, -1.524705100923784E-002, -1.997829761215913E-002, -2.015972460232533E-002, },
                    { 6.235865058863653E-001, 5.748535972479941E-001, 6.039546143920506E-001, -1.189553139504919E-001, -5.995475036431692E-002, 1.221232689432789E+000, 8.118393849671881E-001, 1.019587546988631E-001, 7.579084955172553E-001, 3.448708981352991E-001, 6.186443380390989E-001, 6.503743963305754E-001, },
                    { 5.103235914057721E-001, 4.692451254532845E-001, 5.630294416931898E-001, -1.106745510298056E-001, -4.293730406468507E-002, 8.118393849671881E-001, 8.447658647509614E-001, 7.376862934900393E-002, 5.941244050822955E-001, 2.682387280553992E-001, 4.306022120966404E-001, 5.040965017479671E-001, },
                    { 5.769579319939092E-002, 3.967304767223456E-002, 2.936752803539871E-002, -1.269822420551352E-002, -8.729972561009053E-003, 1.019587546988631E-001, 7.376862934900393E-002, 1.836339363503214E-001, 8.670894228186039E-002, 8.068006861263177E-002, 8.570350597393994E-002, 4.944293677683691E-002, },
                    { 4.637175699581909E-001, 4.054424818170270E-001, 4.389400300421475E-001, -7.111001726644241E-002, -2.665821670530143E-002, 7.579084955172553E-001, 5.941244050822955E-001, 8.670894228186039E-002, 5.672084581519490E-001, 2.681297372418839E-001, 4.554381142124682E-001, 4.934809498023794E-001, },
                    { 2.234971094290169E-001, 1.997384020454393E-001, 1.982114609578972E-001, -3.499608798785736E-002, -1.524705100923784E-002, 3.448708981352991E-001, 2.682387280553992E-001, 8.068006861263177E-002, 2.681297372418839E-001, 1.820220307339481E-001, 2.240698610401698E-001, 2.150697944198728E-001, },
                    { 3.931823752513509E-001, 3.323921922263131E-001, 3.294655288657495E-001, -5.336890446270736E-002, -1.997829761215913E-002, 6.186443380390989E-001, 4.306022120966404E-001, 8.570350597393994E-002, 4.554381142124682E-001, 2.240698610401698E-001, 4.827661636654928E-001, 4.158546001993272E-001, },
                    { 4.236514050136496E-001, 3.411631700478977E-001, 4.162231314394415E-001, -6.281607416760732E-002, -2.015972460232533E-002, 6.503743963305754E-001, 5.040965017479671E-001, 4.944293677683691E-002, 4.934809498023794E-001, 2.150697944198728E-001, 4.158546001993272E-001, 5.559629287257121E-001, },
                };
                double[] lbound = {
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                };
                double[] ubound = {
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    3.000000000000000E-001,
                    1.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                    0.000000000000000E+000,
                };

                var cla = new TuringTrader.Support.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(mean.Count()),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => lbound[instruments[i]],
                    i => ubound[instruments[i]]);

                var turningPoints = cla.TurningPoints().ToList();

                for (int i = 0; i < turningPoints.Count(); i++)
                {
                    var turningPoint = turningPoints[i];

                    foreach (var j in turningPoint.Weights.Keys)
                    {
                        //Assert.IsTrue(Math.Abs(turningPoint.Weights[j]
                        //    - expectedWeights[i, instruments[j]]) < 1e-5);
                    }

                    //Assert.IsTrue(Math.Abs(turningPoint.Risk
                    //    - expectedRisk[i]) < 1e-5);

                    //Assert.IsTrue(Math.Abs(turningPoint.Return
                    //    - expectedReturn[i]) < 1e-5);
                }
            }
            #endregion

            // TODO: random test, w/ target volatility or target return
            /*
	        var covMat =[[0.0146, 0.0187, 0.0145],
				        [0.0187, 0.0854, 0.0104],
				        [0.0145, 0.0104, 0.0289]];
	        var returns = [0.062, 0.146, 0.128];
            */
        }
        #endregion
    }
}

//==============================================================================
// end of file
