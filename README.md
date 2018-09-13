# FUB Trading Simulator
This project implements a trading simulator for stocks and options with the following features:

* import data in CSV format, either as plain files, or zipped archives
* simulate stock trades
* simulate option trades. Currently this is limited to cash-settled European-style options
* export results to Excel, where you can fully customize reports with a few lines of VBA
* export results to R, for further research

The following features are planned for the near future:

* automatic download/ update of data files
* customizable optimizer engine
* API for creating custom indicators
* collection of standard indicators

The following environment is required for building and running the simulator:

* Microsoft Visual Studio, Community 2015 or better
* Excel, and Microsoft.Office.Interop.Excel for exporting results to Excel. If your environment does not meet these requirements, comment the line #define ENABLE_EXCEL in Logger.cs
* R, RDotNet, RDotNet.NativeLibrary, and DynamicInterop for exporting results to R. If your environment does not meet these requirements, comment the line #define ENABLE_R in Logger.cs
* data files for the instruments to simulate


