//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        IndicatorsMomentum
// Description: test momentum indicators
// History:     2018xi27, FUB, created
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TuringTrader.Indicators;
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
            //----- slope
            {
                double[,] testVectors =
                {
                    {100.00000, 0.00000, 0.00000, 0.00000, 0.00000, },
                    {105.00000, 1.50000, 0.00000, 0.00000, 0.00000, },
                    {102.50000, 1.25000, 1.50000, 0.00000, 0.00000, },
                    {107.50000, 2.00000, 1.25000, 1.50000, 0.00000, },
                    {105.00000, 0.50000, 2.00000, 1.25000, 1.50000, },
                    {110.00000, 2.00000, 0.50000, 2.00000, 1.25000, },
                    {107.50000, 0.50000, 2.00000, 0.50000, 2.00000, },
                    {112.50000, 2.00000, 0.50000, 2.00000, 0.50000, },
                    {110.00000, 0.50000, 2.00000, 0.50000, 2.00000, },
                    {115.00000, 2.00000, 0.50000, 2.00000, 0.50000, },
                };

                TimeSeries<double> stimulus = new TimeSeries<double>();

                string msg = "";
                for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
                {
                    stimulus.Value = testVectors[row, 0];
                    ITimeSeries<double> response = stimulus.LinRegression(4).Slope;
                    msg += string.Format("{{{0:F5}, ", testVectors[row, 0]);

                    for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                    {
                        int t = col - 1;
                        double responseValue = response[t];
                        double expectedValue = testVectors[row, col];
                        msg += string.Format("{0:F5}, ", responseValue);

                        Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                    }
                    msg += "},\n";
                }
                Output.WriteLine(msg);
            }

            //----- intercept
            {
                double[,] testVectors =
                {
                    {100.00000, 100.00000, 100.00000, 100.00000, 100.00000, },
                    {105.00000, 103.50000, 100.00000, 100.00000, 100.00000, },
                    {102.50000, 103.75000, 103.50000, 100.00000, 100.00000, },
                    {107.50000, 106.75000, 103.75000, 103.50000, 100.00000, },
                    {105.00000, 105.75000, 106.75000, 103.75000, 103.50000, },
                    {110.00000, 109.25000, 105.75000, 106.75000, 103.75000, },
                    {107.50000, 108.25000, 109.25000, 105.75000, 106.75000, },
                    {112.50000, 111.75000, 108.25000, 109.25000, 105.75000, },
                    {110.00000, 110.75000, 111.75000, 108.25000, 109.25000, },
                    {115.00000, 114.25000, 110.75000, 111.75000, 108.25000, },
                };

                TimeSeries<double> stimulus = new TimeSeries<double>();

                string msg = "";
                for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
                {
                    stimulus.Value = testVectors[row, 0];
                    ITimeSeries<double> response = stimulus.LinRegression(4).Intercept;
                    msg += string.Format("{{{0:F5}, ", testVectors[row, 0]);

                    for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                    {
                        int t = col - 1;
                        double responseValue = response[t];
                        double expectedValue = testVectors[row, col];
                        msg += string.Format("{0:F5}, ", responseValue);

                        Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                    }
                    msg += "},\n";
                }
                Output.WriteLine(msg);
            }

            //----- R2
            {
                double[,] testVectors =
                {
                    {100.00000, 0.00000, 0.00000, 0.00000, 0.00000, },
                    {105.00000, 0.60000, 0.00000, 0.00000, 0.00000, },
                    {102.50000, 0.45455, 0.60000, 0.00000, 0.00000, },
                    {107.50000, 0.64000, 0.45455, 0.60000, 0.00000, },
                    {105.00000, 0.10000, 0.64000, 0.45455, 0.60000, },
                    {110.00000, 0.64000, 0.10000, 0.64000, 0.45455, },
                    {107.50000, 0.10000, 0.64000, 0.10000, 0.64000, },
                    {112.50000, 0.64000, 0.10000, 0.64000, 0.10000, },
                    {110.00000, 0.10000, 0.64000, 0.10000, 0.64000, },
                    {115.00000, 0.64000, 0.10000, 0.64000, 0.10000, },
                };

                TimeSeries<double> stimulus = new TimeSeries<double>();

                string msg = "";
                for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
                {
                    stimulus.Value = testVectors[row, 0];
                    ITimeSeries<double> response = stimulus.LinRegression(4).R2;
                    msg += string.Format("{{{0:F5}, ", testVectors[row, 0]);

                    for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                    {
                        int t = col - 1;
                        double responseValue = response[t];
                        double expectedValue = testVectors[row, col];
                        msg += string.Format("{0:F5}, ", responseValue);

                        Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                    }
                    msg += "},\n";
                }
                //Output.WriteLine(msg);
            }
        }
        #endregion
        #region public void Test_LogRegression()
        [TestMethod]
        public void Test_LogRegression()
        {
            double[,] testVectors =
            {
                {100.00000, 0.00000, 0.00000, 0.00000, 0.00000, },
                {105.00000, 0.01464, 0.00000, 0.00000, 0.00000, },
                {102.50000, 0.01229, 0.01464, 0.00000, 0.00000, },
                {107.50000, 0.01929, 0.01229, 0.01464, 0.00000, },
                {105.00000, 0.00476, 0.01929, 0.01229, 0.01464, },
                {110.00000, 0.01883, 0.00476, 0.01929, 0.01229, },
                {107.50000, 0.00465, 0.01883, 0.00476, 0.01929, },
                {112.50000, 0.01840, 0.00465, 0.01883, 0.00476, },
                {110.00000, 0.00455, 0.01840, 0.00465, 0.01883, },
                {115.00000, 0.01799, 0.00455, 0.01840, 0.00465, },
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            string msg = "";
            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                stimulus.Value = testVectors[row, 0];
                ITimeSeries<double> response = stimulus.LogRegression(4).Slope;
                msg += string.Format("{{{0:F5}, ", testVectors[row, 0]);

                for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 1;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    msg += string.Format("{0:F5}, ", responseValue);

                    Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                }
                msg += "},\n";
            }
            //Output.WriteLine(msg);
        }
        #endregion
    }
}

//==============================================================================
// end of file