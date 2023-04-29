# Creating Custom Indicators

TuringTrader offers a [range of typical indicators](xref:TuringTrader.SimulatorV2.Indicators), and we will add more over time. Nonetheless, you will sooner or later want to create your own indicators, either because we didn't implement the indicator you are looking for, or because it is proprietary. Luckily, this is easy to do.

In this article, we demonstrate the following key concepts:
* Combining existing indicators to create new ones
* Using lambda functions to create indicators

## Combining Existing Indicators to Create New Ones

TuringTrader's 'automagic' indicators allow for a fluid way of creating chains of indicators. This is the ideal basis for creating new indicators by writing them as a combination of existing ones. All that nees to be done, is to wrap that chain of indicators into a new extension method:

```C#
public static TimeSeriesFloat MyVolatility(this TimeSeriesFloat series, int period)
{
    var returns = series.LogReturn();
    var mean = returns.SMA(period);
    return returns.Sub(mean).Square().Sum(period).Div(period - 1).Sqrt();
}
```

## Using Lambda Functions to Create Indicators

The previous method has its limitations, and there are indicators that cannot be expressed through a chain of TuringTrader's built-in indicators. This is where TuringTrader's [Lambda](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_Lambda_System_String_System_Func_System_Double__) function comes in.

TuringTrader's `Lambda` creates a new `SimLoop`, and calls the function passed in for every trading day. `Lambda` exists in two flavors. The first variant aims at indicators with a finite impulse response, e.g., a simple moving average:

```C#
public static TimeSeriesFloat MySMA(this TimeSeriesFloat series, int period)
{
    var name = string.Format("{0}.MySMA({1})", series.Name, period);

    return series.Owner.Lambda(
        name,
        () => Enumerable.Range(0, period).Average(t => series[t]));
}
```

Indicator results will be placed into TuringTrader's cache. Therefore, it is crucial to make sure all indicator instances have unique names. Make sure that your input series' name and any indicator parameters are part of the name.

The function passed into `Lambda` can perform its required operation by referencing the inputs and calculating the output series, one sample at a time.

`Lambda`'s second variant aims at indicators with infinite impulse response, e.g., exponential moving averages:

```C#
public static TimeSeriesFloat MyEMA(this TimeSeriesFloat series, int period)
{
    var name = string.Format("{0}.MyEMA({1})", series.Name, period);

    var alpha = 2.0 / (1.0 + period);
    return series.Owner.Lambda(
        name,
        (prevEMA) => series.Owner.IsFirstBar
            ? series[0]
            : prevEMA + alpha * (series[0] - prevEMA),
        -999.99);
}
```

In this case, `Lambda` expects to receive a function taking the indicator's previous value as an input, and returning the indicator's next value. Also, `Lambda` expects the initial value of the series as a parameter. It is worth noting that in this example, we use [IsFirstBar](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_IsFirstBar) instead to initialize the series.

This brings us to the end of this TuringTrader demo. Please find the [full code for this demo in our repository](https://github.com/fbertram/TuringTrader/blob/develop/Algorithms/Demo%20Algorithms%20(V2)/Demo10_CustomIndicators.cs).
