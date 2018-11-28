//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsMomentum
// Description: test momentum indicators
// History:     2018xi27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class IndicatorsMomentum
    {
        #region public void Test_LinearRegression()
        [TestMethod]
        public void Test_LinearRegression()
        {
            double[,] testVectors =
            {
                {100.00000, 0.00000, 0.00000, 0.00000, 0.00000,},
                {105.00000, 1.50000, 0.00000, 0.00000, 0.00000,},
                {102.50000, 1.25000, 1.50000, 0.00000, 0.00000,},
                {107.50000, 2.00000, 1.25000, 1.50000, 0.00000,},
                {105.00000, 0.50000, 2.00000, 1.25000, 1.50000,},
                {110.00000, 2.00000, 0.50000, 2.00000, 1.25000,},
                {107.50000, 0.50000, 2.00000, 0.50000, 2.00000,},
                {112.50000, 2.00000, 0.50000, 2.00000, 0.50000,},
                {110.00000, 0.50000, 2.00000, 0.50000, 2.00000,},
                {115.00000, 2.00000, 0.50000, 2.00000, 0.50000,},
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                stimulus.Value = testVectors[row, 0];
                ITimeSeries<double> response = stimulus.LinRegression(4);
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