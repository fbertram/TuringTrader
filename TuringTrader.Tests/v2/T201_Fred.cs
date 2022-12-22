//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T201_Fred
// Description: Unit test for FRED data source.
// History:     2022xi30, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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

#region libraries
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T201_Fred
    {
        private class Testbed : Algorithm
        {
            public string Description;
            public double FirstOpen;
            public double LastClose;
            public double HighestHigh = 0.0;
            public double LowestLow = 1e99;
            public double NumBars;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-01-03T16:00-05:00");
                EndDate = DateTime.Parse("2022-11-25T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var a = Asset("fred:EFFR"); // Effective Federal Funds Rate

                    if (IsFirstBar)
                    {
                        Description = a.Description;
                        FirstOpen = a.Open[0];
                        NumBars = 0;
                    }
                    if (IsLastBar)
                    {
                        LastClose = a.Close[0];
                    }
                    HighestHigh = Math.Max(HighestHigh, a.High[0]);
                    LowestLow = Math.Min(LowestLow, a.Low[0]);
                    NumBars++;
                });
            }
        }

        [TestMethod]
        public void Test_DataRetrieval()
        {
            var cachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TuringTrader", "Cache", "EFFR");
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, true);

            for (int i = 0; i < 2; i++)
            {
                var algo = new Testbed();
                algo.Run();

                Assert.IsTrue(algo.Description.ToLower().Contains("effective federal funds rate"));
                Assert.IsTrue(algo.NumBars == 227);
                Assert.IsTrue(Math.Abs(algo.LastClose / algo.FirstOpen - 3.83 / 0.08) < 1e-3);
                Assert.IsTrue(Math.Abs(algo.HighestHigh / algo.LowestLow - 3.83 / 0.08) < 1e-3);
            }
        }
    }
}

//==============================================================================
// end of file