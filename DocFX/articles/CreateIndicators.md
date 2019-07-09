# Creating Custom Indicators

unfortunately, we didn't write this article yet. 

Here are some quick hints:

* an indicator is an extension method, typically running on TimeSeries< double>.
* if your indicator is a simple combination of existing indicators, have a look at the implementation of [DEMA](xref:TuringTrader.Indicators.IndicatorsTrend#TuringTrader_Indicators_IndicatorsTrend_DEMA_TuringTrader_Simulator_ITimeSeries_System_Double__System_Int32_TuringTrader_Simulator_CacheId_System_String_System_Int32_)
* if your indicator does not need to buffer its results, have a look at the implementation of [Add](xref:TuringTrader.Indicators.IndicatorsArithmetic#TuringTrader_Indicators_IndicatorsArithmetic_Add_TuringTrader_Simulator_ITimeSeries_System_Double__TuringTrader_Simulator_ITimeSeries_System_Double__TuringTrader_Simulator_CacheId_System_String_System_Int32_)
* if your indicator needs to buffer its results, have a look at the implementation of [EMA](xref:TuringTrader.Indicators.IndicatorsTrend#TuringTrader_Indicators_IndicatorsTrend_EMA_TuringTrader_Simulator_ITimeSeries_System_Double__System_Int32_TuringTrader_Simulator_CacheId_System_String_System_Int32_)