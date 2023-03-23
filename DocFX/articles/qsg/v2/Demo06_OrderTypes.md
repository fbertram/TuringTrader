# Demo 06: Order Types

This article needs to be rewritten for the v2 engine. In the meantime, check the out the demo code below and the previous [article for the v1 engine](../v1/Demo06.md).

```C#
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
using TuringTrader.SimulatorV2.Assets;

namespace TuringTrader.Demos
{
        public class Demo06_OrderTypes : Algorithm
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
                    switch (numDays)
                    {
                        //--- market this close
                        // Tue, 1/3/2023, o=3853.29, h=3878.46, l=3794.33, c=3824.14
                        case 0:
                            asset.Allocate(1.0, OrderType.closeThisBar); // fill #0 @ 3828.14
                            break;
                        // Wed, 1/4/2023, o=3840.36, h=3873.16, l=3815.77, c=3852.97
                        case 1:
                            asset.Allocate(0.0, OrderType.closeThisBar); // fill #1 @ 3852.97
                            break;
                        // Thu, 1/5/2023, o=3839.74, h=3839.74, l=3802.42, c=3808.10
                        case 2:
                            asset.Allocate(-1.0, OrderType.closeThisBar); // fill #2 @ 3808.10
                            break;
                        // Fri, 1/6/2023, o=3823.37, h=3906.19, l=3809.56, c=3895.08
                        case 3:
                            asset.Allocate(0.0, OrderType.closeThisBar); // fill #3 @ 3895.08
                            break;

                        //  Mon, 1/9/2023, o=3910.82, h=3950.57, l=3890.42, c=3892.09

                        //--- market next open
                        // Tue, 1/10/2023, o=3888.57, h=3919.83, l=3877.29, c=3919.25
                        case 4: // submit 01/09/2023
                            asset.Allocate(1.0, OrderType.openNextBar); // fill #4 @ 3888.57
                            break;
                        // Wed, 1/11/2023, o=3932.35, h=3970.07, l=3928.54, c=3969.61
                        case 5: // submit Tue, 01/10/2023
                            asset.Allocate(0.0, OrderType.openNextBar); // fill #5 @ 3932.35
                            break;

                        //--- buy limit next day
                        // Thu, 1/12/2023, o=3977.57, h=3997.76, l=3937.56, c=3983.17
                        case 6: // submit Wed, 01/11/2023
                            asset.Allocate(1.0, OrderType.buyLimitNextBar, 0.0); // won't fill
                            break;
                        // Fri, 1/13/2023, o=3960.60, h=4003.95, l=3947.67, c=3999.09
                        case 7: // submit Thu, 01/12/2023
                            asset.Allocate(0.5, OrderType.buyLimitNextBar, 1e99); // fill #6 @ 3960.60
                            break;
                        // Tue, 1/17/2023, o=3999.28, h=4015.39, l=3984.57, c=3990.97
                        case 8: // submit Fri, 01/13/2023
                            asset.Allocate(0.1, OrderType.buyLimitNextBar, 1e99); // won't fill
                            break;
                        // Wed, 1/18/2023, o=4002.25, h=4014.16, l=3926.59, c=3928.86
                        case 9: // submit Tue, 01/17/2023
                            asset.Allocate(1.0, OrderType.buyLimitNextBar, 4000.0); // fill #7 @ 4000.00
                            break;

                        //--- sell stop next day
                        // Thu, 1/19/2023, o=3911.84, h=3922.94, l=3885.54, c=3898.85
                        case 10: // submit Wed, 01/18/2023
                            asset.Allocate(0.0, OrderType.sellStopNextBar, 0.0); // won't fill
                            break;
                        // Fri, 1/20/2023, o=3909.04,  h=3972.96, l=3897.86, c=3972.61
                        case 11: // submit Thu, 01/19/2023
                            asset.Allocate(0.5, OrderType.sellStopNextBar, 1e99); // fill #8 @ 3909.04
                            break;
                        // Mon, 1/23/2023, o=3978.14, h=4039.31, l=3971.64, c=4019.81
                        case 12: // submit Fri, 01/20/2023
                            asset.Allocate(0.9, OrderType.sellStopNextBar, 1e99); // won't fill
                            break;
                        // Tue, 1/24/2023, o=4001.74, h=4023.92, l=3989.79, c=4016.95
                        case 13: // submit Mon, 01/23/2023
                            asset.Allocate(0.0, OrderType.sellStopNextBar, 4000.0); // fill #9 @ 4000.00
                            break;

                        //--- buy stop next day
                        // Wed, 1/25/2023, o=3982.71,  h=4019.55, l=3949.06, c=4016.22
                        case 14: // submit Tue, 01/24/2023
                            asset.Allocate(1.0, OrderType.buyStopNextBar, 1e99); // won't fill
                            break;
                        // Thu, 1/26/2023, o=4036.08, h=4061.57, l=4013.29, c=4060.43
                        case 15: // submit Wed, 01/25/2023
                            asset.Allocate(0.5, OrderType.buyStopNextBar, 0.0); // fill #10 @ 4036.08
                            break;
                        // Fri, 1/27/2023, o=4053.72, h=4094.21, l=4048.70, c=4070.56
                        case 16: // submit Thu, 01/26/2023
                            asset.Allocate(0.1, OrderType.buyStopNextBar, 0.0); // won't fill
                            break;
                        // Mon, 1/30/2023, o=4049.27, h=4063.85, l=4015.55, c=4017.77
                        case 17: // submit Fri, 01/27/2023
                            asset.Allocate(1.0, OrderType.buyStopNextBar, 4060.0); // fill #11 @ 4060.00
                            break;

                        //--- sell limit next day
                        // Tue, 1/31/2023, o=4020.85, h=4077.16, l=4020.44, c=4076.60
                        case 18: // submit Mon, 01/30/2023
                            asset.Allocate(0.0, OrderType.sellLimitNextBar, 1e99); // won't fill
                            break;
                        // Wed, 2/1/2023, o=4070.07, h=4148.95, l=4037.20, c=4119.21
                        case 19: // submit Tue, 01/31/2023
                            asset.Allocate(0.5, OrderType.sellLimitNextBar, 0.0); // fill #12 @ 4070.07
                            break;
                        // Thu, 2/2/2023, o=4158.68, h=4195.44, l=4141.88, c=4179.76
                        case 20: // submit Wed, 02/01/2023
                            asset.Allocate(0.9, OrderType.sellLimitNextBar, 0.0); // won't fill
                            break;
                        // Fri, 2/3/2023, o=4136.69, h=4182.36, l=4123.36, c=4136.48
                        case 21: // submit Thu, 02/02/2023
                            asset.Allocate(0.0, OrderType.sellLimitNextBar, 4160.0); // fill #13 @ 4160.00
                            break;
                    }

                    numDays++;
                });

                // add trade log to verify the orders
                Plotter.AddTradeLog();
            }

            public override void Report() => Plotter.OpenWith("SimpleChart");
        }
}
```