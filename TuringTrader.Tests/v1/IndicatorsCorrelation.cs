//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        IndicatorsCorrelation
// Description: unit test for correlation indicators
// History:     2020iv26, FUB, created
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TuringTrader.Indicators;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class IndicatorsCorrelation
    {
        #region Test_Correlation
        [TestMethod]
        public void Test_Correlation()
        {
            double[,] testVectors =
            {
                // instr1     instr2     results
                { 1691.75000,  68.96000, 0.00000, 0.00000, 0.00000, },
                { 1977.80000, 100.11000, 1.00000, 0.00000, 0.00000, },
                { 1884.09000, 109.06000, 0.91773, 1.00000, 0.00000, },
                { 2151.13000, 112.18000, 0.89519, 0.91773, 1.00000, },
                { 2519.36000, 154.12000, 0.95470, 0.89519, 0.91773, },
            };

            TimeSeries<double> x = new TimeSeries<double>();
            TimeSeries<double> y = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                x.Value = testVectors[row, 0];
                y.Value = testVectors[row, 1];

                ITimeSeries<double> response = x.Correlation(y, 5);
                //Output.Write("{{{0:F5},{1:F5},", testVectors[row, 0], testVectors[row, 1]);

                for (int col = 2; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 2;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    //Output.Write("{0:F5},", responseValue);

                    Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                }
                //Output.WriteLine("},");
            }
        }
        #endregion
        #region Test_Covariance
        [TestMethod]
        public void Test_Covariance()
        {
            double[,] testVectors =
            {
                // instr1  instr2     results
                { 1.10000, 3.00000, 0.00000, 0.00000, 0.00000,},
                { 1.70000, 4.20000, 0.14400, 0.00000, 0.00000,},
                { 2.10000, 4.90000, 0.40700, 0.14400, 0.00000,},
                { 1.40000, 4.10000, 0.33850, 0.40700, 0.14400,},
                { 0.20000, 2.50000, 0.66500, 0.33850, 0.40700,},
            };

            TimeSeries<double> x = new TimeSeries<double>();
            TimeSeries<double> y = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                x.Value = testVectors[row, 0];
                y.Value = testVectors[row, 1];

                ITimeSeries<double> response = x.Covariance(y, 5);
                //Output.Write("{{{0:F5},{1:F5},", testVectors[row, 0], testVectors[row, 1]);

                for (int col = 2; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 2;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    //Output.Write("{0:F5},", responseValue);

                    Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                }
                //Output.WriteLine("},");
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file
