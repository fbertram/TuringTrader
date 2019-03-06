//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        PortfolioSupport
// Description: unit test for portfolio support class
// History:     2019ii03, FUB, created
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
            var ef = cla.EfficientFrontier(100);
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

            Assert.IsTrue(Math.Abs((double)maxSR.Sharpe - expectedSharpe) < 1e-5);
            //Assert.IsTrue(Math.Abs((double)maxSR.Risk - expectedVolatility) < 1e-5);

            // TODO: we are currently failing this test
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
    }
}

//==============================================================================
// end of file