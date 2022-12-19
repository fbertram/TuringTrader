//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T103_Exceptions
// Description: Unit test for exception handling.
// History:     2022xii19, FUB, created
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
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    [TestClass]
    public class T103_Exceptions
    {
        private class Testbed1 : Algorithm
        {
            public OrderType OrderType;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-12-06T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-16T16:00-05:00");

                throw new Exception("Throw in Run");
            }
        }
        private class Testbed2 : Algorithm
        {
            public OrderType OrderType;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-12-06T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-16T16:00-05:00");

                SimLoop(() =>
                {
                    throw new Exception("Throw in SimLoop");
                });
            }
        }

        [TestMethod]
        public void Test_Exceptions()
        {
            var exception1 = (Exception)null;
            try
            {
                var algo1 = new Testbed1();
                algo1.Run();
            }
            catch (Exception ex)
            {
                exception1 = ex;
            }
            Assert.IsTrue(exception1.Message == "Throw in Run");

            var exception2 = (Exception)null;
            try
            {
                var algo2 = new Testbed2();
                algo2.Run();
            }
            catch (Exception ex)
            {
                exception2 = ex;
            }
            Assert.IsTrue(exception2.Message == "Throw in SimLoop");
        }
    }
}

//==============================================================================
// end of file