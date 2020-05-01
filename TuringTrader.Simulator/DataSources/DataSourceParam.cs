//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceParam
// Description: enum for data source info values
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Enum for tags in data source descriptor .inf file.
    /// </summary>
    public enum DataSourceParam
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
        /// nickname, w/o data source prefix
        /// </summary>
        nickName2,
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
        /// symbol for fred.stlouisfed.org
        /// </summary>
        symbolFred,
        /// <summary>
        /// symbol for Tiingo
        /// </summary>
        symbolTiingo,
        /// <summary>
        /// data feed to use
        /// </summary>
        dataFeed,
        /// <summary>
        /// data updater to use
        /// </summary>
        dataUpdater,
        /// <summary>
        /// price multiplier for data updater
        /// </summary>
        dataUpdaterPriceMultiplier,

        /// <summary>
        /// symbol list for data splice
        /// </summary>
        symbolSplice,

        /// <summary>
        /// symbol for sub-classed algorithms
        /// </summary>
        symbolAlgo,
    };
}

//==============================================================================
// end of file