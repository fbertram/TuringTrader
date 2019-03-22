//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        TimeSeries
// Description: unit test for simulator core
// History:     2019iii21, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuringTrader.Simulator;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class SimulatorCore
    {
        #region private class Test_Trade_Simulator
        private class Test_Trade_Simulator : TuringTrader.Simulator.Algorithm
        {
            private List<Bar> spxBars = new List<Bar>
            {
                Bar.NewOHLC("SPX", DateTime.Parse("1/02/2019"), 2476.96, 2519.49, 2467.47, 2510.03, 2257700000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/03/2019"), 2491.92, 2493.14, 2443.96, 2447.89, 2782400000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/04/2019"), 2474.33, 2538.07, 2474.33, 2531.94, 2682000000),
                //---
                Bar.NewOHLC("SPX", DateTime.Parse("1/07/2019"), 2535.61, 2566.16, 2524.56, 2549.69, 2553900000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/08/2019"), 2568.11, 2579.82, 2547.56, 2574.41, 2497200000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/09/2019"), 2580.00, 2595.32, 2568.89, 2584.96, 2485100000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/10/2019"), 2573.51, 2597.82, 2562.02, 2596.64, 2346300000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/11/2019"), 2588.11, 2596.27, 2577.40, 2596.26, 2075200000),
                //---
                Bar.NewOHLC("SPX", DateTime.Parse("1/14/2019"), 2580.31, 2589.32, 2570.41, 2582.61, 2264300000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/15/2019"), 2585.10, 2613.08, 2585.10, 2610.30, 2316500000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/16/2019"), 2614.75, 2625.76, 2612.68, 2616.10, 2439000000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/17/2019"), 2609.28, 2645.06, 2606.36, 2635.96, 2480500000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/18/2019"), 2651.27, 2675.47, 2647.58, 2670.71, 2634600000),
                //---
                Bar.NewOHLC("SPX", DateTime.Parse("1/22/2019"), 2657.88, 2657.88, 2617.27, 2632.90, 2614600000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/23/2019"), 2643.48, 2653.19, 2612.86, 2638.70, 2269000000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/24/2019"), 2638.84, 2647.20, 2627.01, 2642.33, 2462500000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/25/2019"), 2657.44, 2672.38, 2657.33, 2664.76, 2537100000),
                //---
                Bar.NewOHLC("SPX", DateTime.Parse("1/28/2019"), 2644.97, 2644.97, 2624.06, 2643.85, 2326800000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/29/2019"), 2644.89, 2650.93, 2631.05, 2640.00, 2183700000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/30/2019"), 2653.62, 2690.44, 2648.34, 2681.05, 2576400000),
                Bar.NewOHLC("SPX", DateTime.Parse("1/31/2019"), 2685.49, 2708.95, 2678.65, 2704.10, 3423600000),
            };

            private Dictionary<DataSourceValue, string> spxInfo = new Dictionary<DataSourceValue, string>
                {
                    { DataSourceValue.nickName, "SPX" },
                    { DataSourceValue.name, "S&P 500 Index" },
                };

            public override void Run()
            {
                StartTime = DateTime.Parse("01/02/2019");
                EndTime = DateTime.Parse("01/31/2019");

                AddDataSource(new DataSourceFromBars(spxBars, spxInfo));

                foreach (var s in SimTimes)
                {
                    var spx = FindInstrument("SPX");

                    //===== market order, open next bar
                    if (SimTime[0] == DateTime.Parse("01/02/2019"))
                    {
                        spx.Trade(10, OrderType.openNextBar).Comment = "110";
                        spx.Trade(-10, OrderType.openNextBar).Comment = "111";
                    }

                    //===== market order, close this bar
                    if (SimTime[0] == DateTime.Parse("01/03/2019"))
                    {
                        spx.Trade(10, OrderType.closeThisBar).Comment = "120";
                        spx.Trade(-10, OrderType.closeThisBar).Comment = "121";
                    }

                    //===== stop order, next bar
                    if (SimTime[0] == DateTime.Parse("01/04/2019"))
                    {
                        spx.Trade(10, OrderType.stopNextBar, 2540).Comment = "130";
                        spx.Trade(-10, OrderType.stopNextBar, 2530).Comment = "131";

                        spx.Trade(10, OrderType.stopNextBar, 2600).Comment = "132";
                        spx.Trade(-10, OrderType.stopNextBar, 2600).Comment = "133";

                        spx.Trade(10, OrderType.stopNextBar, 2400).Comment = "134";
                        spx.Trade(-10, OrderType.stopNextBar, 2400).Comment = "135";
                    }

                    //===== limit order, next bar
                    if (SimTime[0] == DateTime.Parse("01/07/2019"))
                    {
                        spx.Trade(10, OrderType.limitNextBar, 2560).Comment = "140";
                        spx.Trade(-10, OrderType.limitNextBar, 2570).Comment = "141";

                        spx.Trade(10, OrderType.limitNextBar, 2600).Comment = "142";
                        spx.Trade(-10, OrderType.limitNextBar, 2600).Comment = "143";

                        spx.Trade(10, OrderType.limitNextBar, 2400).Comment = "144";
                        spx.Trade(-10, OrderType.limitNextBar, 2400).Comment = "145";
                    }
                }

                //===== market order, open next bar
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "110").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2491.92);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "111").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2491.92);
                }

                //===== market order, close this bar
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "120").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2447.89);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "121").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2447.89);
                }

                //===== stop order, next bar
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "130").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2540.00);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "131").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2530.00);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "132").FirstOrDefault();
                    Assert.IsTrue(order == null);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "133").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2535.61);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "134").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2535.61);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "135").FirstOrDefault();
                    Assert.IsTrue(order == null);
                }

                //===== limit order, next bar
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "140").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2560.00);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "141").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2570.00);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "142").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2568.11);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "143").FirstOrDefault();
                    Assert.IsTrue(order == null);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "144").FirstOrDefault();
                    Assert.IsTrue(order == null);
                }
                {
                    var order = Log.Where(e => e.OrderTicket.Comment == "145").FirstOrDefault();
                    Assert.IsTrue(order != null);
                    Assert.IsTrue(order.FillPrice == 2568.11);
                }
            }
        }
        #endregion

        #region Tests_Trade()
        [TestMethod]
        public void Test_Trade()
        {
            var t = new Test_Trade_Simulator();
            t.Run();
        }
        #endregion
    }
}

//==============================================================================
// end of file