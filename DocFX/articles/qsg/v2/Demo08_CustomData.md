# Creating Custom Data Sources

TuringTrader supports several high-quality data feeds, including [Norgate Data](https://norgatedata.com/), [Tiingo](https://www.tiingo.com/), [FRED](https://fred.stlouisfed.org/), [Yahoo! Finance](https://finance.yahoo.com/), and [CSV files](https://en.wikipedia.org/wiki/Comma-separated_values). While this should be a good starting point for quotes and macroeconomic data for US investors, there will always be situations requiring to bring in data that TuringTrader does not support. Luckily, TuringTrader can easily be expanded with custom data sources.

```C#
public override TimeSeriesAsset Asset(string name)
{
    switch(name)
    {
        case "ernie":
        case "bert":
            return Asset(name, () =>
            {
                var bars = new List<BarType<OHLCV>>();
                foreach (var timestamp in TradingCalendar.TradingDays)
                {
                    var p = name == "ernie" ? 360.0 : 180.0;
                    var t = (timestamp - DateTime.Parse("1970-01-01")).TotalDays;
                    var v = Math.Sin(2.0 * Math.PI * t / p);

                    bars.Add(new BarType<OHLCV>(
                        timestamp,
                        new OHLCV(v, v, v, v, 0.0)));
                }

                return bars;
            });

        default:
            return base.Asset(name);
    }
}
```

The code above shows how an algorithm's [Asset](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_Asset_System_String_) method can be overloaded. Inside of this overloaded method, we make use of a different flavor of [Asset](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_Asset_System_String_System_Func_System_Collections_Generic_List_TuringTrader_SimulatorV2_BarType_TuringTrader_SimulatorV2_OHLCV____), which takes a function returning a list of [OHLCV](xref:TuringTrader.SimulatorV2.OHLCV) bars as a parameter. Here, we are free to retrieve or create data any way we see fit.

The data returned by this code are, like TuringTrader's built-in data sources, subject to resampling to match the currrent `TradingCalendar`. Therefore, we can take some liberties with the timestamps. However, the example shows how we can also access the `TradingCalendar` and reuse those timestamps.

As we are overloading the `Asset` method, the strategy code remains agnostic to the new custom data source. To retain the built-in functionality, it is crucial to default to the base class's implementation of `Asset` for all requests we don't handle in our code.

It is worth noting that custom data could also be brought into TuringTrader through a child algorithm. We have written a [separate article](Demo07_ChildAlgos.md) about that.

This brings us to the end of our first TuringTrader demo. Please find the [full code for this demo in our repository](https://github.com/fbertram/TuringTrader/blob/develop/Algorithms/Demo%20Algorithms%20(V2)/Demo08_CustomData.cs).
