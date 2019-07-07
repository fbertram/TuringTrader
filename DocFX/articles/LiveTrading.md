# Live Trading

Unfortunately, we didn't write this article yet. Here are some hints:

* TuringTrader can be used to perform live trading, and we successfully do so for our own portfolios
* Unlike other simulators, there is no automatic transition from historical to live mode
* Instead, we recommend the following approach
  * run through the historical simulation, just like you would for a backtest
  * on the last bar, pull the account status from the broker, and calculate the target allocation at the broker from the allocation in the backtest
  * submit trades to the broker as required. We like to write out a basket file and import that into Interactive Broker's TWS, but you can also submit the trades directly.