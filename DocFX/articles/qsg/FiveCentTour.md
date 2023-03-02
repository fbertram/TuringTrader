# A Five Cent Tour

TuringTrader is a powerful open-source backtesting engine/ market simulator for stocks and options. It is designed to do the following: 

- run interactive sessions with a simple Windows Desktop UI
- load data from various data feeds, or CSV files
- calculate a comprehensive set of standard indicators
- simulate trading of single stocks, stock portfolios, and stock options
- create fully customized reports, rendered either natively, or using Excel, or R
- provide intuitive time-series APIs, making coding a breeze
- run multi-threaded parameter optimization
- allow live trading through Interactive Brokers

## Download and Install

TuringTrader is designed to run on 64-bit Windows 10, using WPF on .NET Core 3. And while we are big fans of Macs and Linux, we currently don't have the resources to test and debug on other platforms.

To install TuringTrader, we recommend our binary distribution. This distribution might trail a little behind the repository head, but is generally more stable and better tested than the many commits we make to the development branch.

The first step is to download the installer file. TuringTrader comes as a plain vanilla Windows Installer Package. You can find this package on the [TuringTrader download page](https://www.turingtrader.org/download/).

Once you downloaded the installer, double-click the file, to start the Setup Wizard. By default, TuringTrader will install to `C:\Program Files\TuringTrader`. Because this destination is a protected location, you might need to enter your administrator password to complete the installation.

![](~/images/tour/installer1.jpg)

TuringTrader installs the following items:

* the TuringTrader application
* a Start Menu shortcut to launch TuringTrader
* demo algorithms discussed in this quick start guide
* real-life algorithms from books and publications
* standard report templates

TuringTrader will store most of the user-specific data in your `Documents\TuringTrader` folder.

## Running a Demo

Now that we have data set up, it is time to run a demo, and see that everything is working as expected. To do so, open the `File/Algorithm` menu, and click `Demo01_Indicators.cs`:

![](~/images/tour/openDemo01.png)

The demo algorithms are provided as C# source code. Therefore, TuringTrader will first compile the source code. Once this has finished, we can run the demo by clicking the `Run` button. 

![](~/images/tour/demo01Run.jpg)

Depending on your data feed, TuringTrader might take a little moment to load data from the internet. Once the simulation has finished, a report window will open, displaying a chart with some indicators:

![](~/images/tour/demo01Chart.jpg)

The exact look of the plot depends on the rendering template in use for the algorithm. By the time you run this demo, the template might look a little different.

## Editing Source Code

As the demos are provided as C# source code, you can edit them at any time, and start your own experiments. To do so, open the `Edit` menu and click `Algorithm Source`. The Windows shell will use the file association for `.cs` to open your favorite editor. 

![](~/images/tour/editSource.PNG)

Having said that, we recommend installing [Microsoft Visual Studio](https://visualstudio.microsoft.com/). It comes for free, and provides all the features you expect from a powerful development environment.

You can switch back and forth between TuringTrader and the editor at any time. TuringTrader will re-compile your source code to reflect the code changes you just made whenever you click the `Run` button.

## Learning the API

TuringTrader is a tool written for developers. We believe the best way to learn to code, and use an API is to experiment and explore. We recommend going through the demos one by one, to gain an understanding of the main concepts. We also encourage you to experiment with these demos: make some changes, and see if you really understand what is happening.

Once you mastered the demos, we recommend moving on to our [Showcase Algorithms](ShowcaseAlgorithms.md). These are real-life algorithms taken from books and publications, which we tried to code verbatim according to their origin. These algorithms are not only great resources for learning the TuringTrader API, but may also serve as good starting points for your own development.

TuringTrader requires data to run any simulations. Out of the box, these data will be downloaded from Yahoo! Finance. As your requirements for accuracy and reliability increase, you will probably want to switch to a more professional data feed. We strongly encourage you to learn about data, by reading through [this topic](DataSetup.md).

This concludes our short tour through TuringTrader. We hope you enjoy developing with TuringTrader as much as we do. Feel free to [reach out](http://www.turingtrader.org/about/) with comments and questions. Happy coding! 