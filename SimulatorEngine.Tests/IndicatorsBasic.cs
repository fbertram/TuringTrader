//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        IndicatorsBasic
// Description: unit test for basic indicators
// History:     2018ixi27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class IndicatorsBasic
    {
        #region public void Test_Lambda()
        [TestMethod]
        public void Test_Lambda()
        {
            double[,] testVectors =
            {
                { 100.00000, 100.00000, 101.00000, 102.00000, 103.00000,},
                { 105.00000, 105.00000, 106.00000, 107.00000, 108.00000,},
                { 102.50000, 102.50000, 103.50000, 104.50000, 105.50000,},
                { 107.50000, 107.50000, 108.50000, 109.50000, 110.50000,},
                { 105.00000, 105.00000, 106.00000, 107.00000, 108.00000,},
                { 110.00000, 110.00000, 111.00000, 112.00000, 113.00000,},
                { 107.50000, 107.50000, 108.50000, 109.50000, 110.50000,},
                { 112.50000, 112.50000, 113.50000, 114.50000, 115.50000,},
                { 110.00000, 110.00000, 111.00000, 112.00000, 113.00000,},
                { 115.00000, 115.00000, 116.00000, 117.00000, 118.00000,},
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                stimulus.Value = testVectors[row, 0];
                ITimeSeries<double> response =
                    TuringTrader.Simulator.IndicatorsBasic.Lambda(
                        (t) => t + stimulus[0],
                        new CacheId(0));
                //Output.Write("{{ {0:F5}, ", testVectors[row, 0]);

                for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 1;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    //Output.Write("{0:F5}, ", responseValue);

                    Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                }
                //Output.WriteLine("},");
            }
        }
        #endregion
        #region public void Test_BufferedLambda()
        [TestMethod]
        public void Test_BufferedLambda()
        {
            double[,] testVectors =
            {
                { 100.00000, 100.00000, 100.00000, 100.00000, 100.00000, },
                { 105.00000, 205.00000, 100.00000, 100.00000, 100.00000, },
                { 102.50000, 307.50000, 205.00000, 100.00000, 100.00000, },
                { 107.50000, 415.00000, 307.50000, 205.00000, 100.00000, },
                { 105.00000, 520.00000, 415.00000, 307.50000, 205.00000, },
                { 110.00000, 630.00000, 520.00000, 415.00000, 307.50000, },
                { 107.50000, 737.50000, 630.00000, 520.00000, 415.00000, },
                { 112.50000, 850.00000, 737.50000, 630.00000, 520.00000, },
                { 110.00000, 960.00000, 850.00000, 737.50000, 630.00000, },
                { 115.00000, 1075.00000, 960.00000, 850.00000, 737.50000, },
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                stimulus.Value = testVectors[row, 0];
                ITimeSeries<double> response = 
                    TuringTrader.Simulator.IndicatorsBasic.BufferedLambda(
                        (p) => p + stimulus[0],
                        0.0,
                        new CacheId(0));
                //Output.Write("{{ {0:F5}, ", testVectors[row, 0]);

                for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 1;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    //Output.Write("{0:F5}, ", responseValue);

                    Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                }
                //Output.WriteLine("},");
            }
        }
        #endregion
        #region public void Test_Normalize()
        [TestMethod]
        public void Test_Normalize()
        {
            double[,] testVectors =
            {
                { 100.00000, 1.00000, 1.00000, 1.00000, 1.00000, },
                { 105.00000, 1.02439, 1.00000, 1.00000, 1.00000, },
                { 102.50000, 1.00000, 1.02439, 1.00000, 1.00000, },
                { 107.50000, 1.02381, 1.00000, 1.02439, 1.00000, },
                { 105.00000, 1.00000, 1.02381, 1.00000, 1.02439, },
                { 110.00000, 1.02326, 1.00000, 1.02381, 1.00000, },
                { 107.50000, 1.00000, 1.02326, 1.00000, 1.02381, },
                { 112.50000, 1.02273, 1.00000, 1.02326, 1.00000, },
                { 110.00000, 1.00000, 1.02273, 1.00000, 1.02326, },
                { 115.00000, 1.02222, 1.00000, 1.02273, 1.00000, },
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                stimulus.Value = testVectors[row, 0];
                ITimeSeries<double> response = stimulus.Normalize(3);
                //Output.Write("{{ {0:F5}, ", testVectors[row, 0]);

                for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 1;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    //Output.Write("{0:F5}, ", responseValue);

                    Assert.IsTrue(Math.Abs(responseValue - expectedValue) < 1e-5);
                }
                //Output.WriteLine("},");
            }
        }
        #endregion
        #region public void Test_Range()
        [TestMethod]
        public void Test_Range()
        {
            double[,] testVectors =
            {
                { 100.00000, 0.00000, 0.00000, 0.00000, 0.00000, },
                { 105.00000, 5.00000, 0.00000, 0.00000, 0.00000, },
                { 102.50000, 5.00000, 5.00000, 0.00000, 0.00000, },
                { 107.50000, 7.50000, 5.00000, 5.00000, 0.00000, },
                { 105.00000, 5.00000, 7.50000, 5.00000, 5.00000, },
                { 110.00000, 7.50000, 5.00000, 7.50000, 5.00000, },
                { 107.50000, 5.00000, 7.50000, 5.00000, 7.50000, },
                { 112.50000, 7.50000, 5.00000, 7.50000, 5.00000, },
                { 110.00000, 5.00000, 7.50000, 5.00000, 7.50000, },
                { 115.00000, 7.50000, 5.00000, 7.50000, 5.00000, },
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                stimulus.Value = testVectors[row, 0];
                ITimeSeries<double> response = stimulus.Range(4);
                //Output.Write("{{ {0:F5}, ", testVectors[row, 0]);

                for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = col - 1;
                    double responseValue = response[t];
                    double expectedValue = testVectors[row, col];
                    //Output.Write("{0:F5}, ", responseValue);

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