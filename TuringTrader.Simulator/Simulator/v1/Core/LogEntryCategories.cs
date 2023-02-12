//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        LogEntryCategories
// Description: log entry categories for actions and instruments.
// History:     2018xi16, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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