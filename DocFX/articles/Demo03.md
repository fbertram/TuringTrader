# Demo 03: Trading Portfolios

In this demo, we load data for several sector ETFs, calculate indicators, create a portfolio based on the indicator values, and re-balance this portfolio. We demonstrate the following key concepts: 
•    initialization for multiple instruments
•    determining active instruments
•    calculating indicators for multiple instruments
•    portfolio selection
•    placing orders
•    creating a trading log

The strategy bases its decisions on ranking the instrument's Return over Maximum Drawdown. Please don't use this for actual trading. 

## Initialization for Multiple Instruments

The first lines of our strategy initialization are identical to what we have done for <demo01>: 
 <code>

To access instruments during simulation, we need their nicknames. When trading multiple instruments, this quickly gets confusing. To make things easier, it is a good idea to set up some helper fields:
<code>

Using these fields, we can conveniently add our data sources:
<code>

## Determining the Active Instruments

When trading portfolios, we need to keep track of the instruments available at any given bar. Our universe might change over time, with companies having their IPO after the simulation started, and merging or delisting their stock before the simulation ends.

TuringTrader adds instruments to its <Instruments> collection when they start receiving bars and removes them after they become stale. This code snippet provides an enumerable of active instruments:
<code>

## Calculating Indicators for Multiple instruments

Typical portfolio strategies perform a ranking of instruments, based on indicator values. TuringTrader can conveniently calculate indicators for many instruments with just a single LINQ expression. To make sure we call our indicators exactly once per bar and for each instrument, it is best practice to calculate them at the top of the simulation loop and store the results in a collection:
<code>

## Portfolio Selection

With the indicators calculated, we are now ready to select our portfolio. Again, the code is very concise thanks to using LINQ. In this example, we are looking for the RoMaD to be positive, and are limiting our portfolio to a subset of instruments with the highest RoMaD: 
<code>

## Trading the Portfolio

Now that we have our portfolio instruments selected, we are ready to trade them. We start by determining the target equity per instrument. Next, we loop through all instruments, determine the current position and target position, and trade the difference: 
<code>

For analysis of trading logs, it might be helpful to attach a comment to the order ticket. Here is a simple example:
<code> 

Our algorithm is now ready to run. Here is how the equity curve looks like, compared to a benchmark: 
 <image>

## Creating a Trading Log

For a detailed analysis of an algorithm, we need a trading log. TuringTrader keeps information on all transactions in the Log property, which we can collect after finishing the event loop:
<code>

Excel works best for analyzing trade logs, thanks to its filtering features. Here is a snippet of the trading log rendered with Excel:
<image>

As always, find the full code for this demo in our <repository>.