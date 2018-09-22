# FUB Trading Simulator
This project implements a trading simulator for stocks and options with the following features:

* import data in CSV format, either as plain files, or zipped archives with customizable column-mapping
* calculate indicators, with a growing library of standard indicators
* simulate stock trades, and portfolios of stocks. Currently only market orders are supported
* simulate option trades. Currently this is limited to cash-settled European-style options
* create fully customized Excel reports with just a few lines of VBA
* export results to R, for further research
* strong focus on easy-to-use time-series APIs, to make coding a breeze
* multi-threaded optimizer engine, able to load all your CPU cores
* demo algorithms to shorten your learning curve
* quick start guide

The following features are planned for the near future:

* support for stop and limit orders
* automatic download/ update of data files
* API documentation

The following environment is required for building and running the simulator:

* Microsoft Visual Studio, Community 2015 or better
* Excel, and Microsoft.Office.Interop.Excel for exporting results to Excel. If your environment does not meet these requirements, comment the line #define ENABLE_EXCEL at the top of Logger.cs
* R, RDotNet, RDotNet.NativeLibrary, and DynamicInterop for exporting results to R. If your environment does not meet these requirements, comment the line #define ENABLE_R at the top of Logger.cs
* data files for the instruments to simulate. For convenience, some end-of-day quotes are included


