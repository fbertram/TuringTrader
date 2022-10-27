//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Account
// Description: Account class.
// History:     2022x25, FUB, created
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringTrader.Simulator.v2
{
    /// <summary>
    /// OrderType is an enumeration of orders supported by the
    /// simulator.
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// buy or sell assets on the next bar's open
        /// </summary>
        BuySellNextOpen,
        /// <summary>
        /// buy or sell assets on this bar's close
        /// </summary>
        BuySellThisClose,
        // SellStopNextBar,
        // BuyLimitNextBar,
    }

    /// <summary>
    /// The OrderTicket class bundles the information for an order.
    /// This class uses an abstract order concept, where the direction of
    /// the trade is typically determined by the difference between the
    /// current and the target allocation.
    /// </summary>
    public class OrderTicket
    {
        /// <summary>
        /// Ticker symbol for this order.
        /// </summary>
        public readonly string tickerSymbol;
        /// <summary>
        /// Order type to use for this order.
        /// </summary>
        public readonly OrderType orderType;
        /// <summary>
        /// Target allocation to achieve with this order
        /// </summary>
        public readonly double targetAllocation;

        /// <summary>
        /// Create new order ticket.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="type"></param>
        /// <param name="allocation"></param>
        public OrderTicket(string symbol, OrderType type, double allocation)
        {
            tickerSymbol = symbol;
            orderType = type;
            targetAllocation = allocation;
        }
    }

    /// <summary>
    /// The Account class maintains the status of the trading account.
    /// </summary>
    public class Account
    {

    }
}

//==============================================================================
// end of file
