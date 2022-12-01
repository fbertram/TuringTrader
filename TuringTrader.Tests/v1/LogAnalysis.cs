//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        LogAnalysis
// Description: unit test for log analysis
// History:     2019iv04, FUB, created
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class LogAnalysis
    {
        [TestMethod]
        public void Test_LogAnalysis()
        {
            #region test #1: single open, multiple closes
            {
                var orders = new List<LogEntry>
                {
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = 100,
                            Type = OrderType.closeThisBar,
                        },
                    },
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = -50,
                            Type = OrderType.closeThisBar,
                        },
                    },
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = -50,
                            Type = OrderType.closeThisBar,
                        },
                    },
                };

                var positions = TuringTrader.Simulator.LogAnalysis
                    .GroupPositions(orders);

                Assert.IsTrue(positions.Count == 2);
            }
            #endregion
            #region test #2: multiple opens, single close
            {
                var orders = new List<LogEntry>
                {
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = 50,
                            Type = OrderType.closeThisBar,
                        },
                    },
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = 50,
                            Type = OrderType.closeThisBar,
                        },
                    },
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = -100,
                            Type = OrderType.closeThisBar,
                        },
                    },
                };

                var positions = TuringTrader.Simulator.LogAnalysis
                    .GroupPositions(orders);

                Assert.IsTrue(positions.Count == 2);
            }
            #endregion
            #region test #3: open, reverse, close
            {
                var orders = new List<LogEntry>
                {
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = 50,
                            Type = OrderType.closeThisBar,
                        },
                    },
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = -100,
                            Type = OrderType.closeThisBar,
                        },
                    },
                    new LogEntry
                    {
                        Symbol = "TEST",
                        OrderTicket = new Order
                        {
                            Quantity = 50,
                            Type = OrderType.closeThisBar,
                        },
                    },
                };

                var positions = TuringTrader.Simulator.LogAnalysis
                    .GroupPositions(orders);

                Assert.IsTrue(positions.Count == 2);
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file