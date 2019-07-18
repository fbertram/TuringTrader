# Demo 06: Order Types

In this demo, we present the various order Types supported by TuringTrader:
* market order on the open of the next bar
* market order on the close of this bar
* stop order on the next bar
* limit order on the next bar

Like others before, this demo is non-sensical, meant as a sandbox to experiment with the various order types.

## Market Orders

If no order type is specified, TuringTrader defaults to a market order, executed on the next bar's open. You can also specify the order type explicitly:

```c#
instrument.Trade(100, OrderType.openNextBar);
```

It is also possible to execute an order on the close of the current bar. Please note that this will introduce some data snooping bias, dependent on how fast the market was moving upon close. Be mindful:

```c#
instrument.Trade(-100, OrderType.closeThisBar);
```

## Stop Orders

Stop orders turn into market orders when the market turns 'worse' than the stop price. A 'stop-loss' for a long position will trigger when the market falls below the stop price:

```c#
instrument.Trade(-100, OrderType.stopNextBar, 2799);
```

## Limit Orders

Limit orders execute at a price equal to or 'better' than the limit price. Traders often use them to ensure reasonable fill prices, risking that they might not fill at all:

```c#
instrument.Trade(100, OrderType.limitNextBar, 2750);
```

## Time in Force

All TuringTrader orders have a time-in-force of **one bar**. This behavior is natural when working with end-of-day bars, which is what we typically do for our applications. We are aware of the confusion created by this behavior when using intra-day bars, and might fix that one day.

## Conditional Orders

TuringTrader supports conditional orders. Using this feature, you can model more sophisticated execution types, e.g., one-cancels-other order groups. The following example buys 100 shares, but only if the position is still flat by the time the order is executed:

```c#
instrument.Trade(100, OrderType.openNextBar, 0.0, i => i.Position == 0);
```

## Order Log

As shown in a [previous demo](Demo03.md), we recommend creating an order log, to analyze the orders placed by your strategy. Here is the output of this demo, when rendered natively:

![](../images/demo06/orderLog.jpg)

The full source code is available in our [repository](https://github.com/fbertram/TuringTrader/blob/master/Algorithms/Demo%20Algorithms/Demo06_OrderTypes.cs).