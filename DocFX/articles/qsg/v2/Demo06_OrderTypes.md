# Demo 06: Order Types

This article demonstrates the following key concepts:
* placing market orders
* placing stop orders
* placing limit orders

All orders are placed using an asset's [Allocate method](xref:TuringTrader.SimulatorV2.TimeSeriesAsset#TuringTrader_SimulatorV2_TimeSeriesAsset_Allocate_System_Double_TuringTrader_SimulatorV2_OrderType_System_Double_). To learn more about the mechanics of trading, we recommend reviewing our demos for [trading single assets](Demo02_SingleAssets.md) and [trading portfolios](Demo03_Portfolios.md).

## Placing Market Orders

The majority of orders placed by typical trading strategies are market orders, which execute immediately at the available price. Unlike other backtesting engines, TuringTrader's v2 engine is concerned about asset allocations, not positions. As a consequence, there are no separate buy or sell orders. Instead, we allocate capital to an asset, and the simulator will determine if shares need to be bought or sold:

TuringTrader's v2 engine focuses on end-of-day trading. Here we have two options: we can place the order on the close, or we can place it on the next morning's open. In our opinion, placing the order on the next open is more realistic, given that many data feeds have significant publishing delays. However, especially for monthly strategies, trading on the close is very common, as it simplifies the design of the simulator.

```C#
var asset = Asset("$SPX");

// trading on the close
asset.Allocate(1.0, OrderType.closeThisBar); // open long position
asset.Allocate(0.0, OrderType.closeThisBar); // close position

// trading on the next open
asset.Allocate(1.0, OrderType.openNextBar); // open long position
asset.Allocate(0.0, OrderType.openNextBar); // close position
```

As you can see, the code is very clear. All that is required is to pass in the capital allocated to the position, and to decide wether you want to fill this order on the close, or the next open.

```C#
asset.Allocate(-1.0, OrderType.closeThisBar); // open short position
asset.Allocate(0.0, OrderType.closeThisBar);  // close position
```

TuringTrader also supports short positions. The code is self-explanatory: instead of assigning positive capital, a negative number is used.

When TuringTrader executes market orders, it calculates the shares to be bought or sold at the time the order is filled. As a consequence, you can be sure that your allocation is exact, even if the order is filled on the next open, and the market moved significantly over night.

## Placing Stop Orders

Many trading strategies use stop orders to protect the portfolio from undue risk. Of course, TuringTrader also supports stop orders. Unfortunately, stop orders are not directionless. Instead, we need to know if we are buying or selling shares.

All stop orders will be evaluated and filled on the next bar. This makes sense, as we can't really place a stop order on the current bar's close.

```C#
asset.Allocate(0.0, OrderType.sellStopNextBar, 0.0);    // won't fill, keep position as-is
asset.Allocate(0.5, OrderType.sellStopNextBar, 1e99);   // reduce to 50% at next open
asset.Allocate(0.9, OrderType.sellStopNextBar, 1e99);   // won't fill, keep at 50%
asset.Allocate(0.0, OrderType.sellStopNextBar, 4000.0); // reduce to 0% if index drops below 4,000
```

The code above shows the most common stop order, which is to sell assets. TuringTrader will only fill these orders, if the asset position at the time of order evaluation exceeds the capital allocated by the order. Or in other words: a sell-stop order will never buy additional shares, and a buy-stop order will never sell any shares.

It should be clear that a sell order with a stop price of $0.00 won't fill, because equities cannot have negative prices. Similarly, an order with an almost infinite stop price will fill at the next open, making it equivalent to a market order, selling 50% of the position. Only the order with a realistic price, $4,000 in this case, will depend on the development of the asset price. If the price dips below $4,000, we will sell the remainder of the position.

Because TuringTrader works on daily bars, there is no way for the simulator to know about when the stop price was breached and at which price other assets where trading. As a consequence, TuringTrader assumes the asset allocation at the open to be still valid when the order triggers. This may lead to some slight deviations in fast moving markets.

```C#
asset.Allocate(1.0, OrderType.buyStopNextBar, 1e99);   // won't fill, keep position as-is
asset.Allocate(0.5, OrderType.buyStopNextBar, 0.0);    // increase to 50% at next open
asset.Allocate(0.1, OrderType.buyStopNextBar, 0.0);    // won't fill, keep at 50%
asset.Allocate(1.0, OrderType.buyStopNextBar, 4060.0); // increase to 100% if price exceeds $4,060
```

Similarly, can also place buy-stop orders. It is worth noting that stop orders translate to filling at the 'stop price or worse.' For sell orders, that equates to 'or lower,' for buy orders 'or higher.'

## Placing Limit Orders

Many trading strategies also use limit orders, typically with the intent of hunting for a bargain. We can see limit orders as the opposite of stop orders, filling at the 'limit price or better.' For buy orders, that is 'or lower,' while for sell orders this is 'or higher.'

```C#
asset.Allocate(1.0, OrderType.buyLimitNextBar, 0.0);    // won't fill, keep position as-is
asset.Allocate(0.5, OrderType.buyLimitNextBar, 1e99);   // increase to 50% at next open
asset.Allocate(0.1, OrderType.buyLimitNextBar, 1e99);   // won't fill, keep at 50%
asset.Allocate(1.0, OrderType.buyLimitNextBar, 4000.0); // increase to 100%, if price dips below $4,000
```

As with the stop orders, these orders are conditional, and only execute when the asset price meets certain conditions. As a consequence, buy-limit orders with a price of zero will never be filled, and buy-limit orders with an almost infinite price are equivalent to a market order at the next open.

```C#
asset.Allocate(0.0, OrderType.sellLimitNextBar, 1e99);   // won't fill, keep position as-is
asset.Allocate(0.5, OrderType.sellLimitNextBar, 0.0);    // reduce to 50% at next open
asset.Allocate(0.9, OrderType.sellLimitNextBar, 0.0);    // won't fill, keep at 50%
asset.Allocate(0.0, OrderType.sellLimitNextBar, 4160.0); // recuce to 0%, if price exceeds $4,160
```

Or course, there are also sell-limit orders. The example should be self-explanatory.

This brings us to the end of this TuringTrader demo. Please find the [full code for this demo in our repository](https://github.com/fbertram/TuringTrader/blob/develop/Algorithms/Demo%20Algorithms%20(V2)/Demo06_OrderTypes.cs). We recommend taking some time to evaluate the code, and the trading log to understand how exactly TuringTrader processes its orders.
