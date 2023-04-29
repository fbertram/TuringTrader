# Demo 02: Trading Single Assets

In this article, we demonstrate the following key concepts:
* Account setup
* Placing orders
* Querying the account status
* Using the SimpleReport template

## Account Setup

Whenever we place an order, some friction occurs, caused by slippage and fees. Because TuringTrader's trading paradigm is focused on asset allocation rather than shares, the trade friction is specified as a fraction of the order volume in currency.

```C#
((Account_Default)Account).Friction = 0.005;
```

Internally, TuringTrader's [Account](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_Account) object is handling all of the market transactions. This property is pre-populated with an [Account_Default](xref:TuringTrader.SimulatorV2.Account_Default) object, which has a [Friction](xref:TuringTrader.SimulatorV2.Account_Default#TuringTrader_SimulatorV2_Account_Default_Friction) field. For starters, 0.005 or 0.5% of the traded volume is a good value.

## Placing Orders

We are now ready to place orders. The following code shows a simple moving-average crossover strategy:

```C#
var asset = Asset("MSFT");
var slow = asset.Close.EMA(63);
var fast = asset.Close.EMA(21);

asset.Allocate(
    fast[0] > slow[0] ? 1.0 : 0.0,
    OrderType.openNextBar);
```

First, we calculate slow and fast moving averages. Then, we use the [Allocate](xref:TuringTrader.SimulatorV2.TimeSeriesAsset#TuringTrader_SimulatorV2_TimeSeriesAsset_Allocate_System_Double_TuringTrader_SimulatorV2_OrderType_System_Double_) method to allocate 100% of the capital to the asset when the fast line is above the slow line. Otherwise, we allocate 0% to the asset, resulting in an all-cash position.

TuringTrader supports all typical [order types](xref:TuringTrader.SimulatorV2.OrderType). In this case, the order will be filled on the next day's open. It is worth noting that for market orders, only the allocation is specified, and the trade direction (buy or sell) is not required.

## Querying the Account Status

All trading strategies need to know about the current account status, and if only for reporting purposes.

```C#
Plotter.SelectChart("moving average crossover", "date");
Plotter.SetX(SimDate);
Plotter.Plot("trading strategy", NetAssetValue);
Plotter.Plot("buy & hold", asset.Close[0]);
```

The code above creates the main chart comparing the strategy's performance to buy & hold of the traded asset. The key here is the [NetAssetValue](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_NetAssetValue) property, which returns the account's net liquidation value. The [SimpleReport](../SimpleReport.md) will create a beautiful equity curve and drawdown chart from these lines:

![](~/images/qsg-v2/demo02/chart.png)

Unlike TuringTrader's v1 engine, the v2 engine does not require an initial deposit. Instead, the `Account_Default` is initialized with $1,000 at the start of the beginning. This amount then fluctuates throughout the simulation and with the algorithm's trading activity. It is important to notice that the account value has no significance to the simulation engine.

```C#
if (IsLastBar)
{
    Output.WriteLine("Final Asset Allocation");
    foreach (var position in Positions)
        Output.WriteLine("{0} ({1})= {2:P2}", 
            Asset(position.Key).Description, 
            Asset(position.Key).Ticker, 
            position.Value);
    Output.WriteLine("Idle Cash = {0:P2}", Cash);
}
```

The code above writes a summary of the account status to the console. For each asset held, a line with the asset's name and ticker symbol, along with the allocation in percent is printed. The final line prints the percentage of cash in the account.

This code makes use of the algorithm's [Positions](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_Positions) property. Similarly, you can also query the asset's [Position](xref:TuringTrader.SimulatorV2.TimeSeriesAsset#TuringTrader_SimulatorV2_TimeSeriesAsset_Position):

```C#
var currentPosition = Asset("MSFT").Position;
```

## Using the SimpleReport template

When evaluating the performance of a trading system, most people are not only interested in the equity curve, and trade logs, but also in:
* strategy metrics
* annual returns
* rolling returns
* Monte Carlo simulations

Creating these manually for each trading strategy would be a lot of repetitive work. Luckily, we won't have to do that. Instead, TuringTrader's `SimpleReport` template creates all these for us, directly from the equity curve we charted with the code above. To use this template, we could do the following:

```C#
public override void Report() => Plotter.OpenWith("SimpleReport");
```

But we don't even have to do that. If we don't override `Report`, the default implementation of the `Report` method will do that for us.

When using a strategy for live trading, we need to know the target asset allocation on the last day of the simulation. TuringTrader has a built-in function that can add that final allocation to the `Plotter`. This code, placed after the `SimLoop`, will do just that:

```C#
Plotter.AddTargetAllocation();
```

Oftentimes, we are not only interested in the last asset allocation, but also the historical allocation throughout the simulation. The following code, again placed after the `SimLoop` will add these to the report:

```C#
Plotter.AddHistoricalAllocations();
Plotter.AddTradeLog();
```

To learn more about TuringTrader's charting and reporting, we encourage you to check the separate article about the features of the [SimpleReport template](../SimpleReport.md).

This concludes this TuringTrader demo. Please find the [full code for this demo in our repository](https://github.com/fbertram/TuringTrader/blob/develop/Algorithms/Demo%20Algorithms%20(V2)/Demo02_SingleAssets.cs).
