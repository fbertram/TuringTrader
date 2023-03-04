# Demo 01: Calculating Indicators

This article needs to be rewritten for the v2 engine. In the meantime, check out the fully-functional demo code below and the previous [article for the v1 engine](../v1/Demo01.md).

The v2 engine is still a little light regarding the available indicators. We will be expanding that library asap. Check our [available indicators](xref:TuringTrader.SimulatorV2.Indicators). Also, check our article describing [how to create custom indicators](Demo09_CustomIndicators).

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
    public class Demo01_Indicators : Algorithm
    {
        override public void Run()
        {
            //---------- initialization

            // we start by setting a simulation range
            // note that this range is specified in the local time zone
            StartDate = DateTime.Parse("2015-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2016-12-31T16:00-05:00");

            // the warmup period makes sure indicators have valid values
            // throughout the simulation range StartDate... EndDate
            WarmupPeriod = TimeSpan.FromDays(50);            

            //---------- simulation

            // SimLoop loops through all timestamps in the range
            SimLoop(() =>
            {
                // we bring in quotes for an instrument
                // the output of Asset is a time series
                // note that we can also use the Asset function 
                // outside the SimLoop
                var asset = Asset(ETF.SPY);

                // assets have open, high, low, and closing prices
                // these are, of course, also time series
                var prices = asset.Close;

                // indicators can be applied to any time series
                // they are calculated as separate tasks when first 
                // called, and cached for subsequent use
                // note that we can also calculate indicators
                // outside the SimLoop
                var ema26 = prices.EMA(26);
                var ema12 = prices.EMA(12);

                // indicators can also be applied on top of indicators
                var macd = ema12.Sub(ema26);
                var signal = macd.EMA(9);

                // data from time series can be accessed relative to the
                // simulator's current position using square brackets
                // we can create custom charts, with the Plotter object
                var offset = -150;
                Plotter.SelectChart("indicators vs time", "date");
                Plotter.SetX(SimDate);
                Plotter.Plot(asset.Description, prices[0] + offset);
                Plotter.Plot("ema26", ema26[0] + offset);
                Plotter.Plot("ema12", ema12[0] + offset);
                Plotter.Plot("macd", macd[0]);
                Plotter.Plot("signal", signal[0]);
            });
        }

        // to render the charts, we use pre-defined templates
        public override void Report() => Plotter.OpenWith("SimpleChart");
    }
}
```
