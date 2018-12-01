# Turing Trader
This project implements a trading simulator/ backtester for stocks and options with the following features:

* simple Windows Desktop UI for interactive sessions
* import data in various CSV formats and with configurable column-mapping
* automatic download/ update of data files from IQFeed, Yahoo, and Stooq
* query account summary and positions from Interactive Brokers
* calculate indicators, with a growing library of standard indicators
* simulate stock trades, and portfolios of stocks. Currently market and stop orders are supported
* simulate option trades. Currently this is limited to cash-settled European-style options
* create fully customized Excel reports with just a few lines of VBA
* create fully customized R reports, either in straight R, or with RMarkdown
* strong focus on easy-to-use time-series APIs, to make coding a breeze
* multi-threaded optimizer engine, able to utilize all CPU cores
* demo algorithms to shorten learning curve
* API documentation, and quick start guide, as Windows help file
* Unit tests

The following features are planned for the near future:

* support for limit orders
* Data management tool

The following environment is required for building and running the simulator:

* Microsoft Visual Studio, Community 2017
* Excel, and Microsoft.Office.Interop.Excel for exporting results to Excel. If your environment does not meet these requirements, comment the line #define ENABLE_EXCEL at the top of Plotter.cs
* R, and preferably RMarkdown, for exporting results to R. If your environment does not meet these requirements, comment the line #define ENABLE_R at the top of Plotter.cs
* data files for the instruments to simulate. For convenience, some end-of-day quotes are pre-configured


