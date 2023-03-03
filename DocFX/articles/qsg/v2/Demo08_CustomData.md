# Creating Custom Data Sources

The current implementation of custom data sources is a bit clunky; we will fix that asap. In the meantime, check out the demo code below and the previous [article for the v1 engine](../v1/CustomData.md).

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

namespace Demos
{
    // algorithm acting as a custom data source
    // we are aware that this solution is not ideal
    // and will be releasing a more streamlined solution soon
    class CustomData : Algorithm
    {
        public override void Run()
        {
            //---------- initialization

            // note that we are not setting the simulation period  here
            // the parent algorithm will set StartDate and EndDate for us

            //---------- simulation

            SimLoop(() =>
            {
                var t = (SimDate - DateTime.Parse("1970-01-01")).TotalDays;                
                var v = Math.Sin(Math.PI * t / 180.0);

                // return custom data here,
                // each timestamped with SimDate
                return new OHLCV(v, v, v, v, 0.0);
            });
        }
    }

    public class Demo08_CustomData : Algorithm
    {
        public override void Run()
        {
            //---------- initialization

            // set the simulation period
            StartDate = DateTime.Parse("2007-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2022-12-31T16:00-05:00");

            //---------- simulation
                
            SimLoop(() =>
            {
                Plotter.SelectChart("custom data source", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot("custom data", Asset("algorithm:CustomData").Close[0]);
            });
        }

        // minimalistic chart
        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}
```
