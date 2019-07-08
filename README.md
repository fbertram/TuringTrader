# TuringTrader
An open-source backtesting engine/ market simulator. For news, updates, and 
more information about the project, please visit our website at 
[www.turingtrader.org](http://www.turingtrader.org).

Building from Source
* Prerequisites (see https://dotnet.microsoft.com/download/dotnet-core/3.0)
  - Microsoft Visual Studio Community 2019
    . Due to using .NET Core 3, we need at least
      version 16.2.0 Preview 3
    . install must include Workloads for .NET desktop development,
	  and Universal Windows Platform development
  - .NET Core 3 SDK, Preview 6
  - WiX Toolset
  - WiX Toolset Visual Studio 2019 Extension
* Build Steps
  - Open solution in Visual Studio
  - Build release version of TuringTrader project
  - Publish TuringTrader application
  - Build TuringTrader.Setup project, this will be missing books & pubs
  - Install TuringTrader from setup created in previous step
  - Build BooksAndPubs project
  - Re-build setup project, now including books & pubs


Happy coding!

--
Felix Bertram
info@TuringTrader.org