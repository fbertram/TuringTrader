//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        DataFiles
// Description: test data file integrity
// History:     2019iv11, FUB, created
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

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuringTrader.Simulator;
#endregion

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class DataFiles
    {
        [TestMethod]
        public void Test_DataFiles()
        {
            var saveHomePath = GlobalSettings.HomePath;

            try
            {
                var executableDir = Directory.GetParent(
                    System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
                var homePath = Path.Combine(
                    executableDir,
                    "..",
                    "..",
                    "..");
                GlobalSettings.HomePath = homePath;

                var nicknames = Directory.GetFiles(GlobalSettings.DataPath)
                    .Select(p => Path.GetFileName(p))
                    .Where(f => f.EndsWith(".inf")
                        && f != "_defaults_.inf")
                    .Select(f => f.Substring(0, f.Length - 4))
                    .ToList();

                foreach (var nick in nicknames)
                {
                    var dataSource = DataSource.New(nick);

                    // BUGBUG: need to fix this eventually, but for now we ignore options
                    if (dataSource.IsOption)
                        continue;

                    dataSource.LoadData(DateTime.Parse("01/01/2018"), DateTime.Now.Date - TimeSpan.FromDays(5));

                    Assert.IsTrue(dataSource.Data.Count() > 100);

                    Thread.Sleep(1000); // make sure Yahoo doesn't shut us off
                }
            }

            finally
            {
                GlobalSettings.HomePath = saveHomePath;
            }
        }
    }
}

//==============================================================================
// end of file