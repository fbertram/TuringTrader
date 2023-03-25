# Frequently Asked Questions

On this page, we have collected answers to frequently asked questions about TuringTrader. If we didn’t answer your question here, please [give us a holler](https://www.turingtrader.org/about/)!

## Status

### Is TuringTrader production quality?

Yes. We have used TuringTrader to develop and test more than 300 algorithms, covering many different styles. We are using TuringTrader in production at [Bertram Solutions](http://www.bertram.solutions) and [TuringTrader.com](http://www.turingtrader.com), where we trade tactical portfolios day in and day out. Further, we have consulted for other financial professionals, researching investment solutions for them. But most important of all, we are fully committed to keep this going for the foreseeable future.

## Features

### Can TuringTrader run on intraday bars?

Ultimately, TuringTrader does not know about bar length. In general, it can run on any defined time frame, but not ticks. However, any orders in TuringTrader are only valid for on ebar, which makes trading on shorter bars tediuous - and there are no plans to change that. In fact, we doubled down on the end-of-day concept with the v2 engine.

If you need to simulate intraday bars, this is most likely easier with the v1 engine, which will remain included in the TuringTrader package.

### Can TuringTrader perform live trading?

Yes. We actually use TuringTrader at [Bertram Solutions](http://www.bertram.solutions/) to run all of our model portfolios. However, TuringTrader’s model is different from what you’ve seen with other platforms, as the transition from backtest to live trading is performed explicitly: (1) Run a backtest from the past, all the way to today’s close. (2) Watch the *IsLastBar* flag, and transition to live when it’s set. (3) Pull the current account status and positions through the broker interface, and calculate orders based on the account, and the status of the backtest.

### Does TuringTrader run on Linux or Mac?

We coded TuringTrader in C#, using [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/), which potentially runs on Linux or Mac. However, TuringTrader is a WPF application using the Windows Desktop SDK, which is not available on other platforms. We currently don't have the resources to work on changing that. If you'd like to help, [please reach out](https://www.turingtrader.org/about/)!

### Will there be an interactive charting front-end for TuringTrader?

No, we are not planning to do that, as we are quite happy with the way TuringTrader runs and creates reports. However, we might add a feature in the not too distant future, allowing strategies to ask for user input. If you are interested in starting a separate open-source project for an interactive charting frontend, we would certainly endorse that project, and contribute to it. [Please reach out!](https://www.turingtrader.org/about/)

### Is there a binary version?

Yes, there is. Did you check the [download section](https://www.turingtrader.org/download/)?

### Can you add feature XYZ?

That depends. If it is a feature that is on our shortlist of things to do, we will appreciate your input, and probably just do it. If its a feature that is further down on our list, and you’d like us to change our roadmap, this will require some sponsorship. If it’s not even on our roadmap, this becomes a [consulting project](https://www.turingtrader.org/help/articles/overview/FAQ.html#consulting). It all starts with [reaching out to us](https://www.turingtrader.org/about/).

## Licensing

### How can I contribute to the TuringTrader project?

We love to hear from you, and include your contributions into the project. However, in order for us to keep the ability to also grant license exceptions, we need to properly isolate 3rd party contributions from code we own. It's not difficult to do, just something we need to talk about. Please [reach out](https://www.turingtrader.org/about/)!

### Can I use TuringTrader for commercial projects?

Yes, TuringTrader’s open-source license does not restrict your ability to derive commercial projects from it. However, if you are making this derivative work available to a broader audience (be it as a software package, or as a web service), you will need to open-source your code as well. Please see the license terms of the [GNU Affero General Public License](https://www.gnu.org/licenses/agpl-3.0.en.html). If this is does not work for you, we can grant license exceptions through [Bertram Solutions](https://www.bertram.solutions/company/contact/).

## Consulting

### Do you provide development services?

Yes we do, and we’d love to help implementing your strategies! Please head over to Bertram Solutions, and check our [Research & Development](https://www.bertram.solutions/research-development/) section.