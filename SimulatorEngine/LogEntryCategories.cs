//==============================================================================
// Project:     Trading Simulator
// Name:        LogEntryCategories
// Description: log entry categories for actions and instruments.
// History:     2018xi16, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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