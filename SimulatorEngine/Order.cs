//==============================================================================
// Project:     Trading Simulator
// Name:        Order
// Description: order ticket
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
    /// Enumeration of order types.
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// execute order at close of current bar
        /// </summary>
        closeThisBar,

        /// <summary>
        /// execute order at open of next bar
        /// </summary>
        openNextBar,

        /// <summary>
        /// expire option at close of current bar. this order type is
        /// reserved for internal use by the simulator engine.
        /// </summary>
        optionExpiryClose,

        /// <summary>
        /// execute stop order during next bar
        /// </summary>
        stopNextBar,

        /// <summary>
        /// deposit/ withdraw cash
        /// </summary>
        cash,
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
    }
}

//==============================================================================
// end of file