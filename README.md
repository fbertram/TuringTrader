# TuringTrader

![](https://www.gnu.org/graphics/agplv3-155x51.png)

An open-source backtesting engine/ market simulator, licensed under [AGPL 3.0](https://www.gnu.org/licenses/agpl-3.0).

For news, updates, and  more information about the project, please visit our website at https://www.turingtrader.org/.

## Installing

To install TuringTrader, download the setup file from https://www.turingtrader.org/download/.

## Documentation

Find the TuringTrader documentation at https://www.turingtrader.org/documentation/.

## Building from Source

### Prerequisites

see https://dotnet.microsoft.com/download/dotnet-core/3.0

- Microsoft Visual Studio Community 2019
    - due to using .NET Core 3, we need at least version 16.2.0 Preview 3
    - installation must include Workloads for .NET desktop development and Universal Windows Platform development
- .NET Core 3 SDK, Preview 6
- WiX Toolset
- WiX Toolset Visual Studio 2019 Extension

### Build Steps

- Install TuringTrader from binary distribution
- Open TuringTrader solution in Visual Studio
- Build release version of TuringTrader project
- Publish TuringTrader application
- Build BooksAndPubs project (will link against the binary version installed)
- Build setup project





Happy coding!

Felix Bertram
info@TuringTrader.org