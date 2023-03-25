# Demo 03: Trading Portfolios

In this article, we demonstrate the following key concepts:
* Setting the rebalancing schedule
* Using universes
* Ranking and selecting assets

## Setting a Rebalancing Schedule

Most trading schedules follow a predefined rebalancing schedule. For simpler strategies, this is often monthly, but any other schedule is possible. Following such a schedule is often a bit trickier than expected. For example, you might want to trade on the last day of the week. This will typically be Fridays, but in the Thanksgiving week, it is a Wednesday.

TuringTrader makes it easy to do this, by using [SimDate](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_SimDate), [NextSimDate](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_NextSimDate), and the [TradingCalendar](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_TradingCalendar). Here is an example for setting a weekly schedule:

```C#
    SimLoop(() => {
        // common prerequisites

        if (SimDate.DayOfWeek > NextSimDate.DayOfWeek)
        {
            // rebalancing code
        }

        // reporting code
    })
```

If the next simulator timestamp has a lower day of week than the current timestamp, this must be the last trading day of the week. When you put this in your code, be mindful that you probably still want to have daily reports. As a consequence, your reporting code should be outside of that conditional block.

## Using Universes

Many strategies trade common universes, e.g., the S&P 500, the Nasdaq-100, or the Dow-30. TuringTrader supports such universes. If your data feed supports it, the constituents of these universes adjust when stocks are added or removed from the index.

```C#
SimLoop(() => {
    ...
    var universe = Universe("$DJI");
    ...
})
```

When called inside the `SimLoop`, [Universe](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_Universe_System_String_) returns the current constituents of the index.

`Universe` will also work for data feeds that do not provide dynamic constituents. However, TuringTrader will simply return a static list then. This poses two issues. For once, that list may be outdated, as TuringTrader's release schedule is not influenced by index reconstitution. Further, the static list will introduce survivorship bias into your simulation. Nonetheless, this will help you get started.

## Ranking and Selecting Assets

At the core of any portfolio strategy is a mechanism for ranking and selecting assets. C#'s [LINQ](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/) is the perfect feature to do this. We highly recommend learning this approach of functional programming, instead of using a combination of loops and conditional statements for the job.

Here is a code example:

```C#
var topAssets = universe
    .OrderByDescending(name => Asset(name).Close.LogReturn().EMA(126)[0])
    .Take((int)Math.Round(universe.Count * 0.33));
```

We start with a list of tickers, our current universe. Next, we sort this list by today's 6-months momentum for each of the tickers. Of this list, we take the top-ranking 33% and voila: we picked our top assets. This code is not only very concise, it is also easy to read and maintain.

With TuringTrader's v2 engine, there are virtually no limits how you can use indicators in expressions, making it extremely easy to write code for ranking and selecting assets.

Now that we have select the top assets to hold, we are almost ready to trade them. However, there is one pitfall that we need to think about when dealing with dynamic universes. Assets may be added and removed at any time, and it is quite possible that an asset we are currently holding gets removed from the index.

To avoid that this position gets stuck, we need to create a list of all current assets, which is the combination of our universe, and any currently open positions. Using LINQ, we can create that list with a single expression. Note how we are removing any duplicates from that list:

```C#
var allAssets = universe
    .Concat(Positions.Keys)
    .Distinct()
    .ToList();
```

With that list of all assets in hand, trading the assets becomes trivial. Here is the code:

```C#
foreach (var name in allAssets)
    Asset(name).Allocate(
        topAssets.Contains(name) ? 1.0 / topAssets.Count() : 0.0,
        OrderType.openNextBar);
```

We simply loop through all assets, and allocate capital to them, if they are on the top-ranking list. Otherwise, we allocate zero to them.

This brings us to the end of this TuringTrader demo. Please find the [full code for this demo in our repository](https://github.com/fbertram/TuringTrader/blob/develop/Algorithms/Demo%20Algorithms%20(V2)/Demo03_Portfolios.cs).
