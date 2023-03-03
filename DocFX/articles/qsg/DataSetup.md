# Data Setup

Unfortunately, this article needs to updated for the v2 engine. In the meantime, please refer to this [article for the v1 engine](v1/DataSetup.md), which is still (mostly) applicable.

To run its simulations, TuringTrader requires market data, most notably price quotations. This section describes the data sources built into TuringTrader, and how to set them up.

## Supported Data Feeds
TuringTrader supports a number of high-quality data feeds, providing a solid foundation for your simulations. In particular:
* [Norgate Data](https://norgatedata.com/): A high-quality paid feed with fully-adjusted end-of-day quotes, and historical constituents of major indices.
* [Tiingo](https://www.tiingo.com/): A high-quality freemium feed with generous limits for free accounts.
* [FRED](https://fred.stlouisfed.org/): A high-quality free feed with thousands of US and international macro-economic time series.
* [Stooq](https://fred.stlouisfed.org/): A lesser known freemium service from Poland, offering quotes from around the world, including the US and Europe.
* [Yahoo! Finance](https://finance.yahoo.com/): A free source with quotes for virtually any security traded in the US.
* CSV: A highly configurable method to bring in custom data.
* Splice and Join: A unique method to combine multiple quotes to a single time series, especially to create backfills for assets lacking sufficient history.
* Algorithms: You can bring in algorithms as assets. This is an extremely powerful way of structuring your trading strategies in a hierarchical way.
* Custom: If none of the methods above work for you, you can provide custom code to load your data.

## Nomenclature
When it comes to data, TuringTrader distinguishes between the following entities:
* _Database_: this is the collection of all data available to TuringTrader. In the default installation, it resides in your Documents/TuringTrader/Data folder and contains a large number of files in various formats.
* _Data Feed_: this is a service or protocol, through which TuringTrader receives its data. Examples of data feeds are .csv files, Tiingo, FRED, or Norgate Data.
* [DataSource](xref:TuringTrader.SimulatorV2.DataSource): this is the code used to connect to the various data feeds.
* [TimeSeriesAsset](xref:TuringTrader.SimulatorV2.TimeSeriesAsset): this is TuringTrader's representation of the market data, a time series of [OHLCV](xref:TuringTrader.SimulatorV2.OHLCV) bars, enriched with additional [meta data](xref:TuringTrader.SimulatorV2.TimeSeriesAsset.MetaType).
* _Nickname_: this is the name used to load an asset. In most cases, this is just the ticker symbol, but in other cases it may include the name of the data source to use, or refer to a data source descriptor.
* _Ticker_: this is the unique identifier for the asset, as used by the data source or brokerage.
* _Data Source Descriptor_: this is a .ini file, associating a nickname with all the information required for TuringTrader to locate data for the requested asset.

## How TuringTrader Interacts with Data
TuringTrader handles data differently than most other backtesting engine. Most importantly, all data brought into TuringTrader follow the algorithm's [TradingCalendar](xref:TuringTrader.SimulatorV2.ITradingCalendar). This makes it possible to mix and match data from conflicting regions and time zones without issue. This often happens when importing economic data or using price indices to backfill security quotes.

Importing data involves the following steps:
* At any time, your [Algorithm](xref:TuringTrader.SimulatorV2.Algorithm) can call the [Asset](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_Asset_System_String_) method to bring in market data.
* To load the data, you use a _nickname_, which may or may not match the ticker symbol of the data you aim to load.
* TuringTrader checks if a _data source descriptor_ exists, which provides clarifies what the _nickname_ might refer to. Information stored in the _data source descriptor_ includes the _data source_ to use, or the _ticker symbol_ to assign.
* In the absence of a _data source descriptor_ TuringTrader will extract the required information directly from the _nickname_. To do so, the _ticker symbol_ may be prefixed with the name of the _data source_, separated with a colon. If no _data source_ is specified, TuringTrader will use the default _data source_.
* TuringTrader passes all the information required to the _data source_, which will then go out and connect to the _data feed_ to retrieve the requested data.
* Many _data sources_ cache their data in the _database_, to speed up subsequent requests.
* The data timestamps are expected to the in the exchange's time zone. In contrast to that, TuringTrader algorithms run in the user's local time zone. TuringTrader uses the algorithm's [TradingCalendar](xref:TuringTrader.SimulatorV2.ITradingCalendar) to convert the time as required, and to resample as needed. When finished, the data have exactly one bar for each trading day in the simulation period.
* The data are stored in the algorithm's [Cache](xref:TuringTrader.SimulatorV2.Cache), so that subsequent calls to `Asset` do not result in reloads. The data in the cache are keyed with the _nickname_, so it is important to keep using identical nicknames whenever referencing the same data.

## Setting up Data Sources
### Default Data Feed
Typically, we want to specify the default data feed TuringTrader is using to bring in its data. For the default data source, the _nickname_ is identical to the _ticker symbol_ used by this _data feed_.

![](~/images/dataSetup/defaultDataSource.jpg)

To specify the default data feed, start TuringTrader, open the `Edit` menu, click on `Settings` and select the `Data Feeds` tab. Select your favorite data feed, click `OK`, and restart TuringTrader for the changes to take effect.

### Norgate Data (paid)
Norgate Data comes as a paid subscription. What makes them unique is how their tools mirror a fully adjusted database to your computer, so that applications can access it without accessing the network.

![](~/images/dataSetup/norgate-1.jpg)

To set things up, first, register for a free trial at https://norgatedata.com/freetrial.php. Then install the Norgate Data Updater from https://norgatedata.com/ndu-installation.php. Next, launch the Norgate Data Updater and click Check for Updates. Doing so initializes your computer with initial snapshot of the Norgate database.

![](~/images/dataSetup/norgate-2.jpg)

Downloading the initial snapshot takes a while. Once this is completed, make sure to set your default data feed to Norgate.

### Tiingo (freemium)
Tiingo provides quite generous limits for their free accounts, which should be enough to get you started with algorithmic trading. If at some point you need more data, you can upgrade to a paid account. This makes Tiingo an excellent choice for getting started with TuringTrader.

![](~/images/dataSetup/tiingo-1.jpg)

To use Tiingo, you first need to sign-up for a free account with them. Once you have signed up, go to https://api.tiingo.com/documentation/end-of-day. Click on `Click here to see your API Token`, and copy the token.

![](~/images/dataSetup/tiingo-2.jpg)

Next, open the `Settings` dialog enter your API token. It is likely also a good idea to set Tiingo as your default data feed.

### FRED (free)
FRED is a fantastic source of economic data. To find the data series of interest, we recommend searching https://fred.stlouisfed.org/. Then, use the last part of the series URL as the ticker symbol. Example: The Civilian Unemployment Rate can be found at https://fred.stlouisfed.org/series/UNRATE. You can load this data series into TuringTrader using FRED:UNRATE as the nickname.

The FRED data series often come with weekly, monthly, or quarterly sampling frequencies. Because TuringTrader resamples all data to match its internal trading calender, we can use the FRED data series side by side with any other data.

We personally feel it would be quite unusual to set FRED as your default data feed.

### Stooq (freemium)
This section needs to be written. If you use Stooq and read Polish, [please reach out](https://turingtrader.org/about).

### Yahoo Finance (free)
By default, TuringTrader uses data from Yahoo Finance. The advantage of doing so is that no accounts or additional settings are required. However, there are also some significant disadvantages, which is why you should try to move away from them as soon as possible:

* the implementation is a bit of a hack. Please be prepared for this data feed to cease functioning at any time
* Yahoo seems to actively taint their data: Be prepared to find missing quotes or values set to zero.

### CSV Files
TuringTrader includes a highly customizable .csv file reader, able to map virtually any .csv file. TuringTrader can read data from a single .csv file, from a folder containing many .csv files, and .csv files compressed as .zip.

As a default, when not providing a _data source descriptor_, TuringTrader expects files of this format:

```csv
Date,Open,High,Low,Close,Volume
01/02/1970,989.1251,1000.0000,985.2941,1000.0000,0.0000
01/05/1970,1000.0000,1002.6075,994.5625,1002.6075,0.0000
01/06/1970,1002.5951,993.1537,987.0242,993.1537,0.0000
```

While it is fast and convenient to bring in CSV without creating _data source descriptors_, there are also some drawbacks. Most importantly, there is no way to provide any meta data, e.g., the company name, change the data path, or import files not following the format above. If any of these are required, see the section explaining _data source descriptors_ below.

### Algorithms
Conquering complexity is one of the primary goals behind the TuringTrader project. One of the ways how we address this is by allowing algorithms to be used as assets. This way it is possible to build a complex trading strategy as a hierarchy of simpler strategies. For example, you could implement a high-powered stock-trading strategy, that uses a separate managed-bond strategy for any idle capital.

To learn more about this, check out [Demo 07](v2/Demo07_ChildAlgos.md).

### Custom Data
When none of the built-in data sources suit your needs, you can bring in custom data. To learn more about this, check out [Demo 08](v2/Demo08_CustomData.md).

## Nicknames
Whenever making calls to the [Asset](xref:TuringTrader.SimulatorV2.Algorithm#TuringTrader_SimulatorV2_Algorithm_Asset_System_String_) method, you need to specify a nickname for your data. Here are some examples:

* `Asset("MSFT")`: this will load quotes for the symbol `MSFT`. If a _data source descriptor_ with that name exists, the data to load will be clarified there. Otherwise, `MSFT` is passed on to the default data source and will, most likely, result in loading quotes for _Microsoft Corporation_.
* `Asset("yahoo:MSFT")`: this will load symbol `MSFT` from Yahoo! Finance.
* `Asset("splice:SPY,$SPXTR")`: this will load quotes for symbol `SPY` from the default data source. Most likely, this will result in loaded quotes for _SPDR's S&P 500 ETF_. Quotes for this ETF reach back to January 1993. Before that time, TuringTrader will use `$SPXTR`, the _S&P 500 Total Return Index_, as a proxy. Both are spliced together seemlessly.
* `Asset(ETF.SPY)`: this will load quotes and backfills for the ETF with the ticker symbol SPY. TuringTrader will use quotes from the default data source and splice them together with its internal backfills. This is typically the preferred method of loading assets.

## Data Source Descriptors
Data source descriptors provide a powerful way to centralize the mapping of _nicknames_ to _data feeds_ and _symbols_. Most users will not need to think about them, but it is helpful to keep in mind that this feature exists and might help you solve specific issues. Instead of explaining the various fields, we focus on specific examples here.

### Symbol Mapping
The _S&P 500 Index_ is often used as a benchmark. Unfortunately, the ticker symbol used varies between the various data feeds. TuringTrader follows Norgate's convention, which will fail with other feeds, e.g., Yahoo Finance.

**`$SPXTR.inf`**
```inf
name=S&P 500 Total Return Index
symbolYahoo=^SP500TR
```

The data source descriptor above fixes that, if saved in the `Data` folder of TuringTrader's home directory. In the file, the `name` field is optional, but helps to identify the purpose of the descriptor file. The `symbolYahoo` field sets the ticker symbol to `^SP500TR`, if the default data source is Yahoo. It is now possible to use the same `Asset("$SPXTR")`, regardless if the data source is set to Norgate or to Yahoo. Unfortunately, Tiingo does not offer index quotes, which is why the file does not contain specific lines for Tiingo.

### Setting Data Feeds
Let's assume that you would like to load unemployment data using `Asset("UNRATE")`. This data series comes from FRED, which is unlikely to being your default data feed.

**`UNRATE.inf`**
```inf
dataFeed=FRED
```

The data source descriptor above redirects the data feed to FRED, whenever your code attempts to load the asset `UNRATE`.

### Custom CSV Formats
Let's assume you have a CSV file stored under `Data/POT.csv` in TuringTrader's home directory and with the following format:

**`Data/POT.csv`**
```csv
Date,Open,High,Low,Close,TotalVolume
1/9/2006,9.63,10.05,9.62,9.97,9058500
1/10/2006,9.97,9.98,9.73,9.83,4243500
```

To parse this file, we can use the following data source descriptor:

**`POT.inf`**
```inf
name=POTASH CORP OF SASKATCHEWAN
ticker=POT
dataPath=POT.csv
date={1:MM/dd/yyyy}
time=16:00
open={2:F2}
high={3:F2}
low={4:F2}
close={5:F2}
volume={6}
```

This data source descriptor associates the ticker symbol and descriptive name with the .csv file, information that would otherwise be filled with default values from the nickname used during loading.

Further, the file points the parser to the right columns for the date, open, high, low, close, and volume columns. Note that the time at the end of each bar is also given, a time that is specified in the exchange's time zone. As POT is traded in the US, this is at 4 pm.

Check out TuringTrader's home directory for some [additional examples](https://github.com/fbertram/TuringTrader/tree/develop/Data).
