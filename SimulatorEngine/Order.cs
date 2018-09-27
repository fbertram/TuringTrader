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

namespace FUB_TradingSim
{
    public enum OrderType { closeThisBar, openNextBar, optionExpiryClose, stopNextBar };

    /// <summary>
    /// order ticket
    /// </summary>
    public class Order
    {
        public Instrument Instrument;
        public OrderType Type;
        public int Quantity;
        public double Price;
    }
}

//==============================================================================
// end of file