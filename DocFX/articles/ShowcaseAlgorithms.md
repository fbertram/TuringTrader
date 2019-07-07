# Showcase Algorithms

TuringTrader comes with a number of showcase algorithms. These are meant to serve as more complete real-life examples for implementing strategies with TuringTrader.

We implement these strategies as close to the original publication as possible. Often some simple changes to the code and the parameters can significantly improve their performance. We believe these strategies to be great assets for learning, as well as useful starting points for your own experiments.

Here are the strategies we implemented, in no particular order:

* Tim Pearson and Dave Thomas: 'Parking Trade', as published and discussed on [Aeoromir](https://aeromir.com/).
  * A simple strategy selling far out of the money put credit spreads.
* Cesar Alvarez: 'ETF Sector Rotation', as published on his [blog]( https://alvarezquanttrading.com/blog/etf-sector-rotation/). 
  * A simple strategy, rotating instruments based on their momentum.
* Gary Antonacci: 'Dual Momentum Investing', as published in his [book](https://www.amazon.com/Dual-Momentum-Investing-Innovative-Strategy/dp/0071849440/).
  * A portfolio strategy, ranking instruments by both their relative and their absolute momentum.
* Laurens Bensdorp, 'The 30-Minute Stock Trader', as published in his [book](https://www.amazon.com/30-Minute-Stock-Trader-Stress-Free-Financial/dp/1619615738/).
  * WR: a long-only strategy rotating through a large universe of individual stocks based on their rate-of-change, the 200-day moving average, and the 3-day RSI.
  * MRL (MRS): a long-only (short-only) mean-reversion strategy, selecting stocks from a large universe of individual stocks, based on their 150-day moving average, 7-day ADX, 10-day ATR, and 3-day RSI.
* Andreas F. Clenow, 'Stocks on the Move', as published in his [book](https://www.amazon.com/Stocks-Move-Beating-Momentum-Strategies/dp/1511466146/).
  * A portfolio strategy, ranking instruments by their volatility-adjusted momentum, and using risk-parity for position sizing.
* Larry Connors and Cesar Alvarez: 'High Probability ETF Trading', as published in their [book](https://www.amazon.com/High-Probability-ETF-Trading-Professional/dp/0615297412/).
  * A collection of 7 strategies, using various techniques to identify short-term mean-reversal opportunities.
* Larry Connors and Cesar Alvarez: 'Short Term Trading Strategies That Work', as published in their [book](https://www.amazon.com/Short-Term-Trading-Strategies-That-ebook/dp/B007RSLN7M/).
  * A collection of 8 strategies, demonstrating various techniques to identify short-term mean-reversal opportunities.
* Mebane Faber: 'The Ivy Portfolio', as published in his [book](https://www.amazon.com/Ivy-Portfolio-Invest-Endowments-Markets/dp/1118008855/).
  * Four variants of portfolio strategies, selecting from a small universe of ETFs on a monthly basis, based on their relative momentum.
* Wouter J. Keller, Adam Butler, and Ilya Kipnis:  'Momentum and Markowitz: a Golden Combination', as published on [SSRN](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2606884).
  * The 'Classical Asset Allocation' strategy (CAA) aims to create an efficient portfolio of ETFs with a defined target risk using Markowitz' Critical Line Algorithm.
* Wouter J. Keller and Jan Willem Keuning: 'Breadth Momentum and the Canary Universe', as published on [SSRN](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=3212862).
  * The 'Defensive Asset Allocation' strategy (DAA) selects instruments from a small universe of ETFs based on their relative momentum, and the breadth-momentum of a canary universe.
* Brian Livingston: 'Muscular Portfolios', as published in his [book](https://www.amazon.com/Muscular-Portfolios-Investing-Revolution-Superior/dp/194688538X/).
  * Two variants of portfolio strategies, selecting instruments from a small universe of ETFs based on their relative momentum.