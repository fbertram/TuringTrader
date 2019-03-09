//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        PortfolioSupport
// Description: unit test for portfolio support class
// History:     2019iii06, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuringTrader.Simulator;
#endregion

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class PortfolioSupport
    {
        #region Test_MarkowitzCLA
        [TestMethod]
        public void Test_MarkowitzCLA()
        {
            // the instruments serves no function, other than as a key
            List<Instrument> instruments = Enumerable.Range(0, 10)
                .Select(i =>
                {
                    Dictionary<DataSourceValue, string> info = new Dictionary<DataSourceValue, string>
                    {
                        { DataSourceValue.name, string.Format("X{0}", i) },
                        { DataSourceValue.nickName, string.Format("X{0}", i) },
                    };
                    var dataSource = new DataSourceFromBars(null, info);

                    return new Instrument(null, dataSource);
                })
                .ToList();

            Dictionary<string, double> mean = new Dictionary<string, double>
            {
                { "X0", 1.175 },
                { "X1", 1.19  },
                { "X2", 0.396 },
                { "X3", 1.12  },
                { "X4", 0.346 },
                { "X5", 0.679 },
                { "X6", 0.089 },
                { "X7", 0.73  },
                { "X8", 0.481 },
                { "X9", 1.08  },
            };

            Dictionary<string, Dictionary<string, double>> covariance = new Dictionary<string, Dictionary<string, double>>
            {
                { "X0", new Dictionary<string, double> { { "X0", 0.40755159 }, { "X1", 0.03175842 }, { "X2", 0.05183923 }, { "X3", 0.05663904 }, { "X4", 0.0330226  }, { "X5", 0.00827775 }, { "X6", 0.02165938 }, { "X7", 0.01332419 }, { "X8", 0.0343476  }, { "X9", 0.02249903 }, } },
                { "X1", new Dictionary<string, double> { { "X0", 0.03175842 }, { "X1", 0.9063047  }, { "X2", 0.03136385 }, { "X3", 0.02687256 }, { "X4", 0.01917172 }, { "X5", 0.00934384 }, { "X6", 0.02495043 }, { "X7", 0.00761036 }, { "X8", 0.02874874 }, { "X9", 0.01336866 }, } },
                { "X2", new Dictionary<string, double> { { "X0", 0.05183923 }, { "X1", 0.03136385 }, { "X2", 0.19490901 }, { "X3", 0.04408485 }, { "X4", 0.03006772 }, { "X5", 0.01322738 }, { "X6", 0.03525971 }, { "X7", 0.0115493  }, { "X8", 0.0427563  }, { "X9", 0.02057303 }, } },
                { "X3", new Dictionary<string, double> { { "X0", 0.05663904 }, { "X1", 0.02687256 }, { "X2", 0.04408485 }, { "X3", 0.19528471 }, { "X4", 0.02777345 }, { "X5", 0.00526665 }, { "X6", 0.01375808 }, { "X7", 0.00780878 }, { "X8", 0.02914176 }, { "X9", 0.01640377 }, } },
                { "X4", new Dictionary<string, double> { { "X0", 0.0330226  }, { "X1", 0.01917172 }, { "X2", 0.03006772 }, { "X3", 0.02777345 }, { "X4", 0.34059105 }, { "X5", 0.00777055 }, { "X6", 0.02067844 }, { "X7", 0.00736409 }, { "X8", 0.02542657 }, { "X9", 0.01284075 }, } },
                { "X5", new Dictionary<string, double> { { "X0", 0.00827775 }, { "X1", 0.00934384 }, { "X2", 0.01322738 }, { "X3", 0.00526665 }, { "X4", 0.00777055 }, { "X5", 0.15983874 }, { "X6", 0.02105575 }, { "X7", 0.00518686 }, { "X8", 0.01723737 }, { "X9", 0.00723779 }, } },
                { "X6", new Dictionary<string, double> { { "X0", 0.02165938 }, { "X1", 0.02495043 }, { "X2", 0.03525971 }, { "X3", 0.01375808 }, { "X4", 0.02067844 }, { "X5", 0.02105575 }, { "X6", 0.68056711 }, { "X7", 0.01377882 }, { "X8", 0.04627027 }, { "X9", 0.01926088 }, } },
                { "X7", new Dictionary<string, double> { { "X0", 0.01332419 }, { "X1", 0.00761036 }, { "X2", 0.0115493  }, { "X3", 0.00780878 }, { "X4", 0.00736409 }, { "X5", 0.00518686 }, { "X6", 0.01377882 }, { "X7", 0.95526918 }, { "X8", 0.0106553  }, { "X9", 0.00760955 }, } },
                { "X8", new Dictionary<string, double> { { "X0", 0.0343476  }, { "X1", 0.02874874 }, { "X2", 0.0427563  }, { "X3", 0.02914176 }, { "X4", 0.02542657 }, { "X5", 0.01723737 }, { "X6", 0.04627027 }, { "X7", 0.0106553  }, { "X8", 0.31681584 }, { "X9", 0.01854318 }, } },
                { "X9", new Dictionary<string, double> { { "X0", 0.02249903 }, { "X1", 0.01336866 }, { "X2", 0.02057303 }, { "X3", 0.01640377 }, { "X4", 0.01284075 }, { "X5", 0.00723779 }, { "X6", 0.01926088 }, { "X7", 0.00760955 }, { "X8", 0.01854318 }, { "X9", 0.11079287 }, } },
            };

            Dictionary<string, double> lowerBound = instruments.Select(i => i.Nickname)
                .ToDictionary(nick => nick, nick => 0.0);

            Dictionary<string, double> upperBound = instruments.Select(i => i.Nickname)
                .ToDictionary(nick => nick, nick => 1.0);

            //----- calculate efficient frontier
            var cla = new TuringTrader.Simulator.PortfolioSupport.MarkowitzCLA(
                instruments,
                i => mean[i.Nickname],
                (i, j) => covariance[i.Nickname][j.Nickname],
                i => lowerBound[i.Nickname],
                i => upperBound[i.Nickname]);

            //----- check turning points
            List<Dictionary<string, double>> expectedTurningPoints = new List<Dictionary<string, double>>
            {
                new Dictionary<string, double> { { "X0", 0.00000000e+00 }, { "X1", 1.00000000e+00 }, { "X2", 0.00000000e+00 }, { "X3", 0.00000000e+00  }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.00000000e+00  }, { "X8", 0.00000000e+00 }, { "X9", 0.00000000e+00  } },
                new Dictionary<string, double> { { "X0", 0.00000000e+00 }, { "X1", 1.00000000e+00 }, { "X2", 0.00000000e+00 }, { "X3", 0.00000000e+00  }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.00000000e+00  }, { "X8", 0.00000000e+00 }, { "X9", 0.00000000e+00  } },
                new Dictionary<string, double> { { "X0", 6.49369407e-01 }, { "X1", 3.50630593e-01 }, { "X2", 0.00000000e+00 }, { "X3", -3.55271368e-15 }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.00000000e+00  }, { "X8", 0.00000000e+00 }, { "X9", 0.00000000e+00  } },
                new Dictionary<string, double> { { "X0", 4.33984134e-01 }, { "X1", 2.31247501e-01 }, { "X2", 0.00000000e+00 }, { "X3", 3.34768365e-01  }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.00000000e+00  }, { "X8", 0.00000000e+00 }, { "X9", -3.55271368e-15 } },
                new Dictionary<string, double> { { "X0", 1.26887854e-01 }, { "X1", 7.23433472e-02 }, { "X2", 0.00000000e+00 }, { "X3", 2.81253749e-01  }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", -1.38777878e-17 }, { "X8", 0.00000000e+00 }, { "X9", 5.19515050e-01  } },
                new Dictionary<string, double> { { "X0", 1.23201004e-01 }, { "X1", 7.04440713e-02 }, { "X2", 0.00000000e+00 }, { "X3", 2.78993567e-01  }, { "X4", 0.00000000e+00  }, { "X5", 1.11022302e-16 }, { "X6", 0.00000000e+00 }, { "X7", 6.43556436e-03  }, { "X8", 0.00000000e+00 }, { "X9", 5.20925793e-01  } },
                new Dictionary<string, double> { { "X0", 8.69215493e-02 }, { "X1", 5.04510423e-02 }, { "X2", 0.00000000e+00 }, { "X3", 2.23594017e-01  }, { "X4", 0.00000000e+00  }, { "X5", 1.73831615e-01 }, { "X6", 0.00000000e+00 }, { "X7", 3.01730156e-02  }, { "X8", 6.93889390e-18 }, { "X9", 4.35028760e-01  } },
                new Dictionary<string, double> { { "X0", 8.46709412e-02 }, { "X1", 4.92538587e-02 }, { "X2", 0.00000000e+00 }, { "X3", 2.19633903e-01  }, { "X4", -1.73472348e-18 }, { "X5", 1.80039235e-01 }, { "X6", 0.00000000e+00 }, { "X7", 3.10298019e-02  }, { "X8", 6.48570242e-03 }, { "X9", 4.28886558e-01  } },
                new Dictionary<string, double> { { "X0", 7.37892530e-02 }, { "X1", 4.38286608e-02 }, { "X2", 3.46944695e-18 }, { "X3", 1.98975608e-01  }, { "X4", 2.61581599e-02  }, { "X5", 1.98151872e-01 }, { "X6", 0.00000000e+00 }, { "X7", 3.34195864e-02  }, { "X8", 2.79029660e-02 }, { "X9", 3.97773894e-01  } },
                new Dictionary<string, double> { { "X0", 0.06834400e+00 }, { "X1", 0.04138703e+00 }, { "X2", 0.01521526e+00 }, { "X3", 0.18813443e+00  }, { "X4", 0.03416249e+00  }, { "X5", 0.20231943e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.03392932e+00  }, { "X8", 0.03363265e+00 }, { "X9", 0.38287539e+00  } },
                new Dictionary<string, double> { { "X0", 0.03696858e+00 }, { "X1", 0.02690084e+00 }, { "X2", 0.09494243e+00 }, { "X3", 0.12577595e+00  }, { "X4", 0.07674608e+00  }, { "X5", 0.21935567e+00 }, { "X6", 0.02998710e+00 }, { "X7", 0.03596328e+00  }, { "X8", 0.06134984e+00 }, { "X9", 0.29201023e+00  } }
            };

            var turningPoints = cla.TurningPoints().ToList();
            for (var i = 0; i < turningPoints.Count; i++)
            {
                var turningPoint = turningPoints[i];

                foreach (var instrument in turningPoint.Weights.Keys)
                {
                    Assert.IsTrue(Math.Abs(turningPoint.Weights[instrument] - expectedTurningPoints[i][instrument.Nickname]) < 1e-5);
                }
            }

            //---------- efficient frontier
            var ef = cla.EfficientFrontier(100).ToList();
            /*
            var plotter = new Plotter();
            plotter.SelectChart("Efficient Frontier", "risk");
            foreach (var p in ef)
            {
                plotter.SetX(p.Risk);
                plotter.Plot("return", p.Return);
            }
            plotter.OpenWith("SimpleChart");
            */

            //---------- max sharpe ratio
            var maxSR = cla.MaximumSharpeRatio();

            Dictionary<string, double> expectedWeights = new Dictionary<string, double>
            {
                { "X0",  8.39731880e-02 }, { "X1", 4.89059854e-02 }, { "X2", 2.22467109e-19 }, { "X3", 2.18309257e-01 }, { "X4", 1.67730773e-03 },
                { "X5", 1.81200649e-01 }, { "X6", 0.00000000e+00 }, { "X7", 3.11830391e-02 }, { "X8", 7.85901545e-03 }, { "X9", 4.26891558e-01 },
            };
            double expectedSharpe = 4.45353347664641;
            double expectedVolatility = 0.22736446659771808;

            Assert.IsTrue(Math.Abs(maxSR.Return / maxSR.Risk - expectedSharpe) < 1e-5);
            Assert.IsTrue(Math.Abs((double)maxSR.Risk - expectedVolatility) < 1e-5);

            foreach (var instrument in maxSR.Weights.Keys)
                Assert.IsTrue(Math.Abs(maxSR.Weights[instrument] - expectedWeights[instrument.Nickname]) < 1e-5);


            //---------- min variance
            var minVar = cla.MinimumVariance();

            Dictionary<string, double> expectedWeights2 = new Dictionary<string, double>
            {
                { "X0",  0.03696858 }, { "X1", 0.02690084 }, { "X2", 0.09494243 }, { "X3", 0.12577595 }, { "X4", 0.07674608 },
                { "X5", 0.21935567 }, { "X6", 0.0299871 }, { "X7", 0.03596328 }, { "X8", 0.06134984 }, { "X9", 0.29201023 },
            };
            double expectedVariance = 0.20523762;

            Assert.IsTrue(Math.Abs((double)minVar.Risk - expectedVariance) < 1e-5);

            foreach (var instrument in minVar.Weights.Keys)
                Assert.IsTrue(Math.Abs(minVar.Weights[instrument] - expectedWeights2[instrument.Nickname]) < 1e-5);
        }
        #endregion
        #region Test_MarkowitzCLA_2
        // test vectors taken from here: 
        // https://github.com/lequant40/portfolio_allocation_js/blob/master/test/tests_allocation_dev.js
        // also check implementation here, around line #1900
        // https://github.com/lequant40/portfolio_allocation_js/blob/master/lib/allocation/mean-variance.js#L112
        [TestMethod]
        public void Test_MarkowitzCLA_2()
        {
            // the instruments serves no function, other than as a key
            Dictionary<Instrument, int> instruments = Enumerable.Range(0, 10)
                .ToDictionary(i =>
                {
                    Dictionary<DataSourceValue, string> info = new Dictionary<DataSourceValue, string>
                    {
                        { DataSourceValue.name, string.Format("X{0}", i) },
                        { DataSourceValue.nickName, string.Format("X{0}", i) },
                    };
                    var dataSource = new DataSourceFromBars(null, info);

                    return new Instrument(null, dataSource);
                },
                i => i);

            {
                // taken from https://web.stanford.edu/~wfsharpe/mia/opt/mia_opt3.htm
                double[] mean =
                {
                    2.8000,
                    6.3000,
                    10.8000
                };

                /*double[] sd =
                {
                    1.0000,
                    7.4000,
                    15.4000,
                };

                double[,] corr =
                {
                    { 1.0000, 0.4000, 0.1500 },
                    { 0.4000, 1.0000, 0.3500 },
                    { 0.1500, 0.3500, 1.0000 },
                };*/

                double[,] covar =
                {
                    { 1, 2.96, 2.31 },
                    { 2.96, 54.76, 39.886 },
                    { 2.31,  39.886, 237.16 },
                };

                double[,] expectedWeights =
                {
                    { 0.2, 0.30000000000000004, 0.5 },
                    { 0.2, 0.30000000000000004, 0.5 }, // FUB added
                    { 0.2, 0.5, 0.30000000000000004 },
                    { 0.22180737780348653, 0.5, 0.27819262219651353},
                    { 0.451915610952186, 0.348084389047814, 0.2},
                    { 0.451915610952186, 0.348084389047814, 0.2}, // FUB added
                    { 0.5, 0.2999999999999999, 0.2},
                };

                /* double[] expectedIDontKnow =
                {
                    20.898844444444443,
                    11.1475,
                    10.51088812347172,
                    7.55192170004087,
                    0,
                };*/

                var cla = new TuringTrader.Simulator.PortfolioSupport.MarkowitzCLA(
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
                }
            }
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

                var cla = new TuringTrader.Simulator.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(3),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.0,
                    i => 1.0);

                var turningPoints = cla.TurningPoints();

                throw new Exception("not implemented, yet");
            }
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
                    { 0.9754098360655736, 0, 0.024590163934426246 },
                    { 0.9799999999999999, 0, 0.02000000000000001 },
                };

                /* double[] expectedIDontKnow =
                {
                    0.22500000000000006,
                    0.05476839237057218,
                    0.0006557377049180337,
                    0,
                };*/

                var cla = new TuringTrader.Simulator.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(3),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.0,
                    i => 1.0);

                var turningPoints = cla.TurningPoints();
                throw new Exception("not implemented, yet");
            }
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
                    { 0.7, 0.18310626702997274, 0.11689373297002724 },
                    { 0.7, 0.2438095238095238, 0.05619047619047619 },
                };

                /* double[] expectedIDontKnow =
                {
                0.14625
                0.05476839237057221
                0.015934604904632152
                0
                };*/

                var cla = new TuringTrader.Simulator.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(3),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.0,
                    i => 1.0);

                var turningPoints = cla.TurningPoints();
                throw new Exception("not implemented, yet");
            }
            {
                // Reference: An Open-Source Implementation of the Critical-Line Algorithm for Portfolio Optimization, David H. Bailey and Marcos Lopez de Prado
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

                var cla = new TuringTrader.Simulator.PortfolioSupport.MarkowitzCLA(
                    instruments.Keys.Take(3),
                    i => mean[instruments[i]],
                    (i, j) => covar[instruments[i], instruments[j]],
                    i => 0.0,
                    i => 1.0);

                var turningPoints = cla.TurningPoints();
                throw new Exception("not implemented, yet");
            }
            {
                // TODO: random test, w/ target volatility or target return
                /*
		var covMat =[[0.0146, 0.0187, 0.0145],
					[0.0187, 0.0854, 0.0104],
					[0.0145, 0.0104, 0.0289]];
		var returns = [0.062, 0.146, 0.128];
                */
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file