# Demo 03: Trading Portfolios

This article needs to be rewritten for the v2 engine. In the meantime, check the demo code below and the previous [article for the v1 engine](../v1/Demo03.md).

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
    public class Demo03_Portfolio: Algorithm
    {
        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            // note that our simulation starts long before inception 
            // of the ETFs thanks to TuringTrader's backfills
            StartDate = DateTime.Parse("1990-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2022-12-31T16:00-05:00");
            WarmupPeriod = TimeSpan.FromDays(365);

            // setup the trading universe. note that we are 
            // not using strings here, but TuringTrader's pre-defined 
            // constants giving us access to backfills spanning many years
            var universe = new List<string>{
                ETF.XLY, ETF.XLV, ETF.XLK,
                ETF.XLP, ETF.XLE, ETF.XLI,
                ETF.XLF, ETF.XLU, ETF.XLB,
            };

            //---------- simulation

            SimLoop(() =>
            {
                // this algorithm trades only once per week
                if (SimDate.DayOfWeek > NextSimDate.DayOfWeek)
                {
                    // pick the top 3 assets with the highest 1-year momentum
                    var topAssets = universe
                        .OrderByDescending(name => Asset(name).Close[0] / Asset(name).Close[252])
                        .Take(3);

                    // hold only the top-ranking assets, flatten all others
                    foreach (var name in universe)
                        Asset(name).Allocate(
                            topAssets.Contains(name) ? 1.0 / topAssets.Count() : 0.0,
                            OrderType.openNextBar);
                }

                // create a simple report comparing the
                // trading strategy to the S&P 500
                Plotter.SelectChart("simple sector rotation", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot("trading strategy", NetAssetValue);
                Plotter.Plot("s&P 500", Asset(MarketIndex.SPX).Close[0]);
            });

            //---------- post-processing

            // add some optional information to the report
            Plotter.AddTargetAllocation();
            Plotter.AddHistoricalAllocations();
            Plotter.AddTradeLog();
        }
    }
}
```
