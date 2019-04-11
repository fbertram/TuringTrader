//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        LogAnalysis
// Description: unit test for log analysis
// History:     2019iv04, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
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