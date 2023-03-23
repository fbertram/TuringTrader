# Demo 05: Running the Optimizer

This article needs to be rewritten for the v2 engine. In the meantime, check out the demo code below and the previous [article for the v1 engine](../v1/Demo05.md).

```C#
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
using TuringTrader.SimulatorV2.Assets;

namespace TuringTrader.Demos
{
    public class Demo05_Optimizer : Algorithm
    {
        // these are the parameters to optimize. note that
        // we can optimize fields and properties alike
        [OptimizerParam(0, 90, 10)]
        public int X { get; set; } = 40;

        [OptimizerParam(0, 9, 1)]
        public int Y = 2;

        override public void Run()
        {
            // this is just a dummy for the algorithm's internal functionality.
            Thread.Sleep(250);

            // we can set the FitnessValue manually
            // as a default, TuringTrader will use the
            // return on maximum drawdown
            FitnessValue = X + Y;

            // avoid printing to the log while optimizing
            if (!IsOptimizing)
                Output.WriteLine("Run: {0}", OptimizerParamsAsString);
        }

        // dummy report - typically, we would create a pretty chart here
        public override void Report()
        {
            Output.WriteLine("Report: Fitness={0}", FitnessValue);
        }
    }
}
```
