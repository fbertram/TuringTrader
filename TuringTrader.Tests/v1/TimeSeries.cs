//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        TimeSeries
// Description: unit test for time series
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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class TimeSeries
    {
        #region public void Test_TimeSeries()
        [TestMethod]
        public void Test_TimeSeries()
        {
            double[,] testVectors =
            {
                { 100.00000, 1.00000, 100.00000, 100.00000, 100.00000, },
                { 105.00000, 2.00000, 100.00000, 100.00000, 100.00000, },
                { 102.50000, 3.00000, 105.00000, 100.00000, 100.00000, },
                { 107.50000, 4.00000, 102.50000, 105.00000, 100.00000, },
                { 105.00000, 5.00000, 107.50000, 102.50000, 105.00000, },
                { 110.00000, 6.00000, 105.00000, 107.50000, 102.50000, },
                { 107.50000, 7.00000, 110.00000, 105.00000, 107.50000, },
                { 112.50000, 8.00000, 107.50000, 110.00000, 105.00000, },
                { 110.00000, 9.00000, 112.50000, 107.50000, 110.00000, },
                { 115.00000, 256.00000, 999.00000, 110.00000, 112.50000, },
            };

            TimeSeries<double> stimulus = new TimeSeries<double>();

            for (int row = 0; row <= testVectors.GetUpperBound(0); row++)
            {
                if (row == testVectors.GetUpperBound(0))
                    for (int i = 0; i < stimulus.MaxBarsBack - 3; i++)
                        stimulus.Value = 999.0;

                stimulus.Value = testVectors[row, 0];
                //Output.Write("{{ {0:F5}, ", testVectors[row, 0]);
                var response = stimulus;

                for (int col = 1; col <= testVectors.GetUpperBound(1); col++)
                {
                    int t = row == testVectors.GetUpperBound(0)
                        ? stimulus.MaxBarsBack - 5 + col
                        : col - 1;
                    double responseValue = col == 1
                        ? response.BarsAvailable
                        : response[t];
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