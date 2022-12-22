//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T203_Yahoo
// Description: Unit test for Yahoo! finance data source.
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
    public class T203_Yahoo
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
                StartDate = DateTime.Parse("2019-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2019-01-12T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    var a = Asset("yahoo:msft");

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
                "TuringTrader", "Cache", "msft");
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, true);

            for (int i = 0; i < 2; i++)
            {
                var algo = new Testbed();
                algo.Run();

                Assert.IsTrue(algo.Description.ToLower().Contains("microsoft"));
                Assert.IsTrue(algo.NumBars == 8);
                Assert.IsTrue(Math.Abs(algo.LastClose / algo.FirstOpen - 98.48418 / 95.37062) < 1e-3);
                Assert.IsTrue(Math.Abs(algo.HighestHigh / algo.LowestLow - 100.47684 / 93.11927) < 1e-3);
            }
        }
    }
}

//==============================================================================
// end of file