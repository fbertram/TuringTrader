//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T101_Cache
// Description: Unit test for cache.
// History:     2022xi30, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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
    public class T102_OrderTypes
    {
        private class Testbed : Algorithm
        {
            public OrderType OrderType;
            public override void Run()
            {
                StartDate = DateTime.Parse("2022-12-06T16:00-05:00");
                EndDate = DateTime.Parse("2022-12-16T16:00-05:00");

                SimLoop(() =>
                {
                    if (SimDate.Date == DateTime.Parse("2022-12-13"))
                    {
                        Asset("$SPX").Allocate(1.0, OrderType);
                    }
                    if (SimDate.Date == DateTime.Parse("2022-12-15"))
                    {
                        Asset("$SPX").Allocate(0.0, OrderType);
                    }
                });
            }
        }

        [TestMethod]
        public void Test_OrderOpenNextBar()
        {
            var algo = new Testbed();
            algo.OrderType = OrderType.openNextBar;
            algo.Run();

            Assert.IsTrue(Math.Abs(algo.NetAssetValue - 967.99456831900375) < 1e-5);
            Assert.IsTrue(algo.Account.TradeLog.Count == 2);

            var order1 = algo.Account.TradeLog[0];
            Assert.IsTrue(order1.ExecDate.Date == DateTime.Parse("2022-12-14"));
            Assert.IsTrue(Math.Abs(order1.FillPrice - 4015.5400390625) < 1e-5);
            Assert.IsTrue(Math.Abs(order1.OrderAmount - 999.50024987506242) < 1e-5);
            Assert.IsTrue(Math.Abs(order1.FrictionAmount - 0.49975012493753124) < 1e-5);

            var order2 = algo.Account.TradeLog[1];
            Assert.IsTrue(order2.ExecDate.Date == DateTime.Parse("2022-12-16"));
            Assert.IsTrue(Math.Abs(order2.FillPrice - 3890.909912109375) < 1e-5);
            Assert.IsTrue(Math.Abs(order2.OrderAmount - -968.47880772286521) < 1e-5);
            Assert.IsTrue(Math.Abs(order2.FrictionAmount - 0.4842394038614326) < 1e-5);
        }

        [TestMethod]
        public void Test_OrderCloseThisBar()
        {
            var algo = new Testbed();
            algo.OrderType = OrderType.closeThisBar;
            algo.Run();

            Assert.IsTrue(Math.Abs(algo.NetAssetValue - 968.20775228019545) < 1e-5);
            Assert.IsTrue(algo.Account.TradeLog.Count == 2);

            var order1 = algo.Account.TradeLog[0];
            Assert.IsTrue(order1.ExecDate.Date == DateTime.Parse("2022-12-13"));
            Assert.IsTrue(Math.Abs(order1.FillPrice - 4019.64990234375) < 1e-5);
            Assert.IsTrue(Math.Abs(order1.OrderAmount - 999.50024987506254) < 1e-5);
            Assert.IsTrue(Math.Abs(order1.FrictionAmount - 0.49975012493753129) < 1e-5);

            var order2 = algo.Account.TradeLog[1];
            Assert.IsTrue(order2.ExecDate.Date == DateTime.Parse("2022-12-15"));
            Assert.IsTrue(Math.Abs(order2.FillPrice - 3895.75) < 1e-5);
            Assert.IsTrue(Math.Abs(order2.OrderAmount - -968.69209832936019) < 1e-5);
            Assert.IsTrue(Math.Abs(order2.FrictionAmount - 0.4843460491646801) < 1e-5);
        }

        private class Testbed2 : Algorithm
        {
            public override void Run()
            {
                StartDate = DateTime.Parse("2023-01-01T16:00-05:00");
                EndDate = DateTime.Parse("2023-03-01T16:00-05:00");
                ((Account_Default)Account).Friction = 0.0;

                var asset = Asset("$SPX");

                var numDays = 0;
                SimLoop(() =>
                {
                    Output.WriteLine("#{0}: {1:MM/dd/yyyy}, o={2:C2}, h={3:C2}, l={4:C2}, c={5:C2}",
                        numDays, SimDate, asset.Open[0], asset.High[0], asset.Low[0], asset.Close[0]);

                    switch (numDays)
                    {
                        //--- market on close
                        case 0: // Tue, 01/03/2023
                            asset.Allocate(1.0, OrderType.closeThisBar); // trade log #0
                            break;
                        case 1: // Wed, 01/04/2023
                            asset.Allocate(0.0, OrderType.closeThisBar); // trade log #1
                            break;
                        case 2: // Thu, 01/05/2023
                            asset.Allocate(-1.0, OrderType.closeThisBar); // trade log #2
                            break;
                        case 3: // Fri, 01/06/2023
                            asset.Allocate(0.0, OrderType.closeThisBar); // trade log #3
                            break;

                        //--- market next day
                        case 4: // Mon, 01/09/2023
                            asset.Allocate(1.0, OrderType.openNextBar); // trade log #4
                            break;
                        case 5: // Tue, 01/10/2023
                            asset.Allocate(0.0, OrderType.openNextBar); // trade log #5
                            break;

                        //--- buy limit next day
                        case 6: // Wed, 01/11/2023
                            asset.Allocate(1.0, OrderType.buyLimitNextBar, 0.0); // won't fill
                            break;
                        case 7: // Thu, 01/12/2023
                            asset.Allocate(0.5, OrderType.buyLimitNextBar, 1e99); // trade log #6
                            break;
                        case 8: // Fri, 01/13/2023
                            asset.Allocate(0.1, OrderType.buyLimitNextBar, 1e99); // won't fill
                            break;
                        case 9: // Tue, 01/17/2023
                            asset.Allocate(1.0, OrderType.buyLimitNextBar, 4000.0); // trade log #7
                            break;

                        //--- sell stop next day
                        case 10: // Wed, 01/18/2023
                            asset.Allocate(0.0, OrderType.sellStopNextBar, 0.0); // won't fill
                            break;
                        case 11: // Thu, 01/19/2023
                            asset.Allocate(0.5, OrderType.sellStopNextBar, 1e99); // trade log #8
                            break;
                        case 12: // Fri, 01/20/2023
                            asset.Allocate(0.9, OrderType.sellStopNextBar, 1e99); // won't fill
                            break;
                        case 13: // Mon, 01/23/2023
                            asset.Allocate(0.0, OrderType.sellStopNextBar, 4000.0); // trade log #9
                            break;

                        //--- buy stop next day
                        case 14: // Tue, 01/24/2023
                            asset.Allocate(1.0, OrderType.buyStopNextBar, 1e99); // won't fill
                            break;
                        case 15: // Wed, 01/25/2023
                            asset.Allocate(0.5, OrderType.buyStopNextBar, 0.0); // trade log #10
                            break;
                        case 16: // Thu, 01/26/2023
                            asset.Allocate(0.1, OrderType.buyStopNextBar, 0.0); // won't fill
                            break;
                        case 17: // Fri, 01/27/2023
                            asset.Allocate(1.0, OrderType.buyStopNextBar, 4060.0); // trade log #11
                            break;

                        //--- sell limit next day
                        case 18: // Mon, 01/30/2023
                            asset.Allocate(0.0, OrderType.sellLimitNextBar, 1e99); // won't fill
                            break;
                        case 19: // Tue, 01/31/2023
                            asset.Allocate(0.5, OrderType.sellLimitNextBar, 0.0); // trade log #12
                            break;
                        case 20: // Wed, 02/01/2023
                            asset.Allocate(0.9, OrderType.sellLimitNextBar, 0.0); // won't fill
                            break;
                        case 21: // Thu, 02/02/2023
                            asset.Allocate(0.0, OrderType.sellLimitNextBar, 4160.0); // trade log #13
                            break;
                    }

                    numDays++;
                });
            }
        }

        [TestMethod]
        public void Test_OrderTypes()
        {
            var algo = new Testbed2();
            algo.Run();

            /*
            day       date      open    high    low     close   volume
            Tuesday   1/3/2023  3853.29	3878.46	3794.33	3824.14	2,356,300,000
            Wednesday 1/4/2023  3840.36	3873.16	3815.77	3852.97	2,613,600,000
            Thursday  1/5/2023  3839.74	3839.74	3802.42	3808.10 2,261,200,000
            Friday    1/6/2023  3823.37	3906.19	3809.56	3895.08	2,470,500,000
            Monday    1/9/2023  3910.82	3950.57	3890.42	3892.09	2,503,800,000
            Tuesday   1/10/2023 3888.57	3919.83	3877.29	3919.25	2,144,200,000
            Wednesday 1/11/2023 3932.35	3970.07	3928.54	3969.61	2,358,000,000
            Thursday  1/12/2023 3977.57	3997.76	3937.56	3983.17	2,472,100,000
            Friday    1/13/2023 3960.60 4003.95	3947.67	3999.09	2,309,600,000
            Tuesday   1/17/2023 3999.28	4015.39	3984.57	3990.97	2,504,000,000
            Wednesday 1/18/2023 4002.25	4014.16	3926.59	3928.86	2,549,900,000
            Thursday  1/19/2023 3911.84	3922.94	3885.54	3898.85	2,468,500,000
            Friday    1/20/2023 3909.04	3972.96	3897.86	3972.61	2,628,800,000
            Monday    1/23/2023 3978.14	4039.31	3971.64	4019.81	2,473,600,000
            Tuesday   1/24/2023 4001.74	4023.92	3989.79	4016.95	2,106,800,000
            Wednesday 1/25/2023 3982.71	4019.55	3949.06	4016.22	2,431,100,000
            Thursday  1/26/2023 4036.08	4061.57	4013.29	4060.43	2,441,000,000
            Friday    1/27/2023 4053.72	4094.21	4048.70	4070.56	2,577,500,000
            Monday    1/30/2023 4049.27	4063.85	4015.55	4017.77	2,327,500,000
            Tuesday   1/31/2023 4020.85	4077.16	4020.44	4076.60	2,874,500,000
            Wednesday 2/1/2023	4070.07	4148.95	4037.20	4119.21	2,968,000,000
            Thursday  2/2/2023	4158.68	4195.44	4141.88	4179.76	3,416,600,000
            Friday    2/3/2023	4136.69	4182.36	4123.36	4136.48	2,905,400,000
            Monday    2/6/2023	4119.57	4124.63	4093.38	4111.08	2,289,600,000
            Tuesday   2/7/2023	4105.35	4176.54	4088.39	4164.00	2,627,700,000
            Wednesday 2/8/2023	4153.47	4156.85	4111.67	4117.86	2,567,100,000
            Thursday  2/9/2023	4144.25	4156.23	4069.67	4081.50	2,669,200,000
            Friday    2/10/2023	4068.92	4094.36	4060.79	4090.46	2,314,800,000
             */

            //--- market this close
            Assert.AreEqual(algo.Account.TradeLog[0].ExecDate.Date, DateTime.Parse("01/03/2023"));
            Assert.AreEqual(algo.Account.TradeLog[0].FillPrice, 3824.14, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog[1].ExecDate.Date, DateTime.Parse("01/04/2023"));
            Assert.AreEqual(algo.Account.TradeLog[1].FillPrice, 3852.97, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog[2].ExecDate.Date, DateTime.Parse("01/05/2023"));
            Assert.AreEqual(algo.Account.TradeLog[2].FillPrice, 3808.10, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog[3].ExecDate.Date, DateTime.Parse("01/06/2023"));
            Assert.AreEqual(algo.Account.TradeLog[3].FillPrice, 3895.08, 1e-2);

            //--- market next open
            Assert.AreEqual(algo.Account.TradeLog[4].ExecDate.Date, DateTime.Parse("01/10/2023"));
            Assert.AreEqual(algo.Account.TradeLog[4].FillPrice, 3888.57, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog[5].ExecDate.Date, DateTime.Parse("01/11/2023"));
            Assert.AreEqual(algo.Account.TradeLog[5].FillPrice, 3932.35, 1e-2);

            //--- buy limit next day
            Assert.AreEqual(algo.Account.TradeLog[6].ExecDate.Date, DateTime.Parse("01/13/2023"));
            Assert.AreEqual(algo.Account.TradeLog[6].FillPrice, 3960.60, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog[7].ExecDate.Date, DateTime.Parse("01/18/2023"));
            Assert.AreEqual(algo.Account.TradeLog[7].FillPrice, 4000.00, 1e-2);

            //--- sell stop next day
            Assert.AreEqual(algo.Account.TradeLog[8].ExecDate.Date, DateTime.Parse("01/20/2023"));
            Assert.AreEqual(algo.Account.TradeLog[8].FillPrice, 3909.04, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog[9].ExecDate.Date, DateTime.Parse("01/24/2023"));
            Assert.AreEqual(algo.Account.TradeLog[9].FillPrice, 4000.00, 1e-2);

            //--- buy stop next day
            Assert.AreEqual(algo.Account.TradeLog[10].ExecDate.Date, DateTime.Parse("01/26/2023"));
            Assert.AreEqual(algo.Account.TradeLog[10].FillPrice, 4036.08, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog[11].ExecDate.Date, DateTime.Parse("01/30/2023"));
            Assert.AreEqual(algo.Account.TradeLog[11].FillPrice, 4060.00, 1e-2);

            //--- sell limit next day
            Assert.AreEqual(algo.Account.TradeLog[12].ExecDate.Date, DateTime.Parse("02/01/2023"));
            Assert.AreEqual(algo.Account.TradeLog[12].FillPrice, 4070.07, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog[13].ExecDate.Date, DateTime.Parse("02/03/2023"));
            Assert.AreEqual(algo.Account.TradeLog[13].FillPrice, 4160.00, 1e-2);

            Assert.AreEqual(algo.Account.TradeLog.Count, 14);
        }
    }
}

//==============================================================================
// end of file