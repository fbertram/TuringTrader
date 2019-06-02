//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        IndicatorsTrend
// Description: test trend indicators
// History:     2018xi27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TuringTrader.Indicators;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class IndicatorsTrend
    {
        #region public void Test_SMA()
        [TestMethod]
        public void Test_SMA()
        {
            double[,] testVectors =
            {
                {100.00000, 100.00000, 100.00000, 100.00000, 100.00000,},
                {105.00000, 101.25000, 100.00000, 100.00000, 100.00000,},
                {102.50000, 101.87500, 101.25000, 100.00000, 100.00000,},
                {107.50000, 103.75000, 101.87500, 101.25000, 100.00000,},
                {105.00000, 105.00000, 103.75000, 101.87500, 101.25000,},
                {110.00000, 106.25000, 105.00000, 103.75000, 101.87500,},
                {107.50000, 107.50000, 106.25000, 105.00000, 103.75000,},
                {112.50000, 108.75000, 107.50000, 106.25000, 105.00000,},
                {110.00000, 110.00000, 108.75000, 107.50000, 106.25000,},
                {115.00000, 111.25000, 110.00000, 108.75000, 107.50000,},
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                stimulus.Value = testVectors[row, 0];
                ITimeSeries<double> response = stimulus.SMA(4);
                //Output.Write("{{{0:F5},", testVectors[row, 0]);

                for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 1;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    //Output.Write("{0:F5},", responseValue);

                    Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                }
                //Output.WriteLine("},");
            }
        }
        #endregion
        #region public void Test_EMA()
        [TestMethod]
        public void Test_EMA()
        {
            double[,] testVectors =
            {
                {100.00000, 100.00000, 100.00000, 100.00000, 100.00000,},
                {105.00000, 102.00000, 100.00000, 100.00000, 100.00000,},
                {102.50000, 102.20000, 102.00000, 100.00000, 100.00000,},
                {107.50000, 104.32000, 102.20000, 102.00000, 100.00000,},
                {105.00000, 104.59200, 104.32000, 102.20000, 102.00000,},
                {110.00000, 106.75520, 104.59200, 104.32000, 102.20000,},
                {107.50000, 107.05312, 106.75520, 104.59200, 104.32000,},
                {112.50000, 109.23187, 107.05312, 106.75520, 104.59200,},
                {110.00000, 109.53912, 109.23187, 107.05312, 106.75520,},
                {115.00000, 111.72347, 109.53912, 109.23187, 107.05312,},
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                stimulus.Value = testVectors[row, 0];
                ITimeSeries<double> response = stimulus.EMA(4);
                //Output.Write("{{{0:F5},", testVectors[row, 0]);

                for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 1;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    //Output.Write("{0:F5},", responseValue);

                    Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                }
                //Output.WriteLine("},");
            }

        }
        #endregion
    }
}

//==============================================================================
// end of file