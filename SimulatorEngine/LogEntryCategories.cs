//==============================================================================
// Project:     Trading Simulator
// Name:        LogEntryCategories
// Description: log entry categories for actions and instruments.
// History:     2018xi16, FUB, created
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
    /// Enumeration of actions for log entries.
    /// </summary>
    public enum LogEntryAction
    {
        /// <summary>
        /// cash deposit into account
        /// </summary>
        Deposit,

        /// <summary>
        /// cash withdrawal from account
        /// </summary>
        Withdrawal,

        /// <summary>
        /// buy equity or option
        /// </summary>
        Buy,

        /// <summary>
        /// sell equity or option
        /// </summary>
        Sell,

        /// <summary>
        /// option expiry
        /// </summary>
        Expiry,
    };

    /// <summary>
    /// Enumeration of instrument classes for log entries.
    /// </summary>
    public enum LogEntryInstrument
    {
        /// <summary>
        /// cash transaction
        /// </summary>
        Cash,

        /// <summary>
        /// stock or index
        /// </summary>
        Equity,

        /// <summary>
        /// put option
        /// </summary>
        OptionPut,

        /// <summary>
        /// call option
        /// </summary>
        OptionCall,
    };
}

//==============================================================================
// end of file