//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceValue
// Description: enum for data source info values
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Enum for tags in data source descriptor .inf file.
    /// </summary>
    public enum DataSourceValue
    {
        /// <summary>
        /// error, none of the defined values
        /// </summary>
        error,

        /// <summary>
        /// path to .inf data source descriptor
        /// </summary>
        infoPath,
        /// <summary>
        /// path to data
        /// </summary>
        dataPath,

        /// <summary>
        /// nickname
        /// </summary>
        nickName,
        /// <summary>
        /// full descriptive name
        /// </summary>
        name,
        /// <summary>
        /// stock ticker
        /// </summary>
        ticker,

        /// <summary>
        /// timestamp, data part
        /// </summary>
        date,
        /// <summary>
        /// timestamp, time part
        /// </summary>
        time,

        /// <summary>
        /// opening price
        /// </summary>
        open,
        /// <summary>
        /// high price
        /// </summary>
        high,
        /// <summary>
        /// low price
        /// </summary>
        low,
        /// <summary>
        /// closing price
        /// </summary>
        close,
        /// <summary>
        /// trading volume
        /// </summary>
        volume,

        /// <summary>
        /// bid price
        /// </summary>
        bid,
        /// <summary>
        /// ask price
        /// </summary>
        ask,
        /// <summary>
        /// bid volume
        /// </summary>
        bidSize,
        /// <summary>
        /// ask volume
        /// </summary>
        askSize,

        /// <summary>
        /// option expiration date
        /// </summary>
        optionExpiration,
        /// <summary>
        /// option strike price
        /// </summary>
        optionStrike,
        /// <summary>
        /// option right
        /// </summary>
        optionRight,
        /// <summary>
        /// option underlying symbol
        /// </summary>
        optionUnderlying,

        /// <summary>
        /// symbol for IQFeed/ DTN
        /// </summary>
        symbolIqfeed,
        /// <summary>
        /// symbol for Stooq.com
        /// </summary>
        symbolStooq,
        /// <summary>
        /// symbol for yahoo.com
        /// </summary>
        symbolYahoo,
        /// <summary>
        /// symbol for Interactive Brokers
        /// </summary>
        symbolInteractiveBrokers,
        /// <summary>
        /// symbol for Norgate Data
        /// </summary>
        symbolNorgate,
        /// <summary>
        /// data source to use
        /// </summary>
        dataSource,
        /// <summary>
        /// data updater to use
        /// </summary>
        dataUpdater,
        /// <summary>
        /// price multiplier for data updater
        /// </summary>
        dataUpdaterPriceMultiplier,
    };
}

//==============================================================================
// end of file