//==============================================================================
// Project:     Trading Simulator
// Name:        DataSourceValue
// Description: enum for data source info values
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

namespace FUB_TradingSim
{
    public enum DataSourceValue
    {
        error,    // to make sure default(DataSourceValue) is none of the defined values

        infoPath,
        dataPath,

        nickName,
        name,
        ticker,
        symbol,

        date,
        time,

        open,
        high,
        low,
        close,
        volume,

        bid,
        ask,
        bidSize,
        askSize,

        optionExpiration,
        optionStrike,
        optionRight,
        optionUnderlying,

        symbolIqfeed,
        symbolStooq,
        symbolYahoo,
        symbolInteractiveBrokers,
        dataUpdater,
    };
}

//==============================================================================
// end of file