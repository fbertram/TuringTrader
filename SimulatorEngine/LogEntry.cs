//==============================================================================
// Project:     Trading Simulator
// Name:        LogEntry
// Description: log entry
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Entry to trading log.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Symbol traded. This is required, as the Instrument is cleared from
        /// the OrderTicket field to preserve memory.
        /// </summary>
        public string Symbol;

        /// <summary>
        /// Original order ticket. Please note that the Instrument is set to
        /// null, to preserve memory. Use the Symbol field instead.
        /// </summary>
        public Order OrderTicket;

        /// <summary>
        /// Bar of trade execution.
        /// </summary>
        public Bar BarOfExecution;

        /// <summary>
        /// Net asset value at trade execution.
        /// </summary>
        public double NetAssetValue;

        /// <summary>
        /// Fill price of trade.
        /// </summary>
        public double FillPrice;

        /// <summary>
        /// Commission paid for trade.
        /// </summary>
        public double Commission;
    }
}

//==============================================================================
// end of file