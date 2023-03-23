# Demo 02: Trading Single Assets

This article needs to be rewritten for the v2 engine. In the meantime, check out the fully-functional demo code below and the previous [article for the v1 engine](../v1/Demo02.md).

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
    public class Demo02_Stocks : Algorithm
    {
        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2022-12-31T16:00-05:00");

            // note that the warmup period is specified in calendar days
            // while most indicators express their parameters in trading days
            WarmupPeriod = TimeSpan.FromDays(90);            

            //---------- simulation

            SimLoop(() =>
            {
                // first, we load the asset quotes
                // then, we calculate slow and fast moving averages
                // note that this code can live inside or outside of SimLoop
                var asset = Asset("MSFT");
                var slow = asset.Close.EMA(63);
                var fast = asset.Close.EMA(21);

                // set the asset allocation as a percentage of the NAV
                // no need to worry about number of shares, trade direction
                asset.Allocate(
                    // hold the asset while the fast MA is above the slow MA,
                    fast[0] > slow[0] ? 1.0 : 0.0,
                    // we set the order to fill on tomorrow's open
                    OrderType.openNextBar);

                // create a simple report comparing the
                // trading strategy to buy & hold
                Plotter.SelectChart("moving average crossover", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot("trading strategy", NetAssetValue);
                Plotter.Plot("buy & hold", asset.Close[0]);
            });
        }

        // if we don't override Report here, a default report
        // using the SimpleReport template will be created
    }
}
```
