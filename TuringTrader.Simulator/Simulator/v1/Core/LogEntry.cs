//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        LogEntry
// Description: log entry
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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

#region libraries
#endregion

namespace TuringTrader.Simulator
{

    /// <summary>
    /// Entry to order log.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Symbol traded. This is a shortcut, same as using
        /// Order.Instrument.Symbol
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
        /// Fill price of trade.
        /// </summary>
        public double FillPrice;

        /// <summary>
        /// Commission paid for trade.
        /// </summary>
        public double Commission;

        /// <summary>
        /// Return string with order action. This is for convenience only,
        /// as this information can be reconstructed from the other fields.
        /// </summary>
        public LogEntryAction Action
        {
            get
            {
                switch (OrderTicket.Type)
                {
                    case OrderType.cash:
                        if (OrderTicket.Quantity > 0) return LogEntryAction.Withdrawal;
                        else return LogEntryAction.Deposit;

                    case OrderType.optionExpiryClose:
                    case OrderType.instrumentDelisted:
                        return LogEntryAction.Expiry;

                    default:
                        if (OrderTicket.Quantity > 0) return LogEntryAction.Buy;
                        else return LogEntryAction.Sell;
                }
            }
        }

        /// <summary>
        /// Instrument class. This field is required, as the Instrument
        /// is cleared from the OrderTicket to save memory.
        /// </summary>
        public LogEntryInstrument InstrumentType;
    }
}

//==============================================================================
// end of file