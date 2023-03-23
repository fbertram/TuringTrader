//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Order
// Description: order ticket
// History:     2018ix11, FUB, created
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

#region libraries
using System;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Enumeration of order types.
    /// </summary>
    public enum OrderType
    {
        //----- user transactions

        /// <summary>
        /// deposit/ withdraw cash
        /// </summary>
        cash,

        /// <summary>
        /// execute order at close of current bar
        /// </summary>
        closeThisBar,

        /// <summary>
        /// execute order at open of next bar
        /// </summary>
        openNextBar,

        /// <summary>
        /// execute stop order on next bar
        /// </summary>
        stopNextBar,

        /// <summary>
        /// execute limit order on next bar
        /// </summary>
        limitNextBar,

        //----- simulator-internal transactions

        /// <summary>
        /// expire option at close of current bar. this order type is
        /// reserved for internal use by the simulator engine.
        /// </summary>
        optionExpiryClose,

        /// <summary>
        /// close out a position in an inactive stock
        /// </summary>
        instrumentDelisted,

        /// <summary>
        /// fake close at end of simulation
        /// </summary>
        endOfSimFakeClose,
    };

    /// <summary>
    /// Order ticket
    /// </summary>
    public class Order
    {
        /// <summary>
        /// instrument this order is for
        /// </summary>
        public Instrument Instrument;

        /// <summary>
        /// type of order
        /// </summary>
        public OrderType Type;

        /// <summary>
        /// quantity of order
        /// </summary>
        public int Quantity;

        /// <summary>
        /// price of order, only required for stop orders
        /// </summary>
        public double Price;

        /// <summary>
        /// user-defined comment
        /// </summary>
        public string Comment;

        /// <summary>
        /// time stamp of queuing this order
        /// </summary>
        public DateTime QueueTime;

        /// <summary>
        /// exec condition
        /// </summary>
        public Func<Instrument, bool> Condition = null;
    }
}

//==============================================================================
// end of file