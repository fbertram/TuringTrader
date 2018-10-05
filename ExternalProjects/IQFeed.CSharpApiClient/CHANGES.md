### IQFeed.CSharpApiClient 1.4.1 - August 12, 2018
##### Issues Resolved
 * Reversed Open and Close position in IntervalMessage and DailyWeeklyMonthlyMessage
 * Historical requests supporting RequestId parameter
 * IntervalMessage can support larger TotalVolume

### IQFeed.CSharpApiClient 1.4.0 - August 5, 2018
This release contains a significant amount of improvements. More importantly, we added support
for derivative data and now supporting the latest protocol version 6.0. All messages parsing are
 now culture invariant and in the same way, we increased a lot our unit testing.

* Added Derivative data support
* Added support for protocol version 6.0
* Added BaseFacade, BaseMessageHandler and customs IQFeedException
* ChainsFacade now returning chains instances
* Added NumberOfTrades in IntervalMessage
* Added DataDirection enum parameter for all historical request


##### Issues Resolved
 * Messages parsing are now culture independent
 * #16 LookupClient BufferSize can be customized
 * DailyWeeklyMonthlyMessage can handle large value
 * Workaround for test converage