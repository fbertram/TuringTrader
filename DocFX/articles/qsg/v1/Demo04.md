# Demo 04: Trading Options

In this demo, we will show how to implement a simple options strategy. We will focus on the following key concepts:
* loading option quotes
* retrieving the option chain
* filtering options

The strategy sells out-of-the-money puts for a premium.

## Loading Option Quotes

Before we can simulate any strategy, we need to have quote data available. TuringTrader can import quotes in CSV files, like the data available through the [CBOE Data Shop](https://datashop.cboe.com/). To do so, you will need to create a data source descriptor as described [here](DataSetup.md).

For this demo, we go a different route: TuringTrader includes a generator for 'fake' quotes, calculated from SPX and VIX using the Black-Scholes model included in our [option support package](xref:TuringTrader.Support.OptionSupport). While these quotes won't be a precise mirror of the markets, they still capture the overall vibe.

Regardless of where your quotes come from, they will be brought in as a single data source: 

```c#
AddDataSource("$SPX.options");
```

TuringTrader models each option contract as a separate [Instrument](xref:TuringTrader.Simulator.Instrument) with the following important implications:
* there is a one-to-many relationship between the single data source and the many instruments it creates
* the list of instruments is dynamic, with options contracts added when they become available, and removed when they expire

## Retrieving the Option Chain

The option chain is a list of instruments, available at a given point in time. You can query the option chain in the simulation loop like this:

```c#
List<Instrument> optionChain = OptionChain("$SPX.options");
```

This call will typically return several hundred contracts to choose from, so we need to filter it to find a suitable contract to trade. We can do so, using LINQ.

## Selecting an Option Contract

First, we probably want to create a list of available expiry dates. We can create this list as follows:

```c#
List<DateTime> expiryDates = OptionChain(_optionsNickname)
    .Select(o => o.OptionExpiry)
    .Distinct()
    .ToList();
```

Now, we can pick an expiry with a specific range of days until expiration (DTE):

```c#
DateTime expiryDate = expiryDates
    .Where(d => (d - simTime).TotalDays >= 21
        && (d - simTime).TotalDays <= 28)
    .FirstOrDefault();
```

And finally, we can select a put contract expiring that very day, and with a strike price closest to a given target:

```c#
Instrument shortPut = OptionChain("$SPX.options")
.Where(o => o.OptionIsPut
    && o.Option.Expiry == expiryDate)
.OrderBy(o => Math.Abs(o.OptionStrike - 2800)
.FirstOrDefault(); 
```

Now that we have an [Instrument](xref:TuringTrader.Simulator.Instrument) for the contract to trade, everything else is coded just like we showed for [trading stocks](Demo02.md).

## Putting Everything Together

Our demo strategy includes a few more bells and whistles. In particular:

* only consider options expiring on the 3rd Friday of the month
* calculate strike price based on volatility
* close position, if the option contract held is at risk of expiring in the money
* position sizing based on margin rules

Please find the full source code in our [repository](https://github.com/fbertram/TuringTrader/blob/master/Algorithms/Demo%20Algorithms/Demo04_Options.cs).

TuringTrader is assuming the options traded to be cash-settled and European-style, which has significant consequences:

* assignment of American-style options will not be modeled accurately
* stocks added or removed from the portfolio through option exercise will be replaced with a cash transaction for the same amount

Because of these simulator limitations, it is imperative to watch the extrinsic value of any short option positions closely, when trading American-style options. If an early assignment occurs, the simulator results will be inaccurate.