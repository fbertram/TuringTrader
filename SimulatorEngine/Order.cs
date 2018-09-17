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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    public enum OrderExecution { closeThisBar, openNextBar, optionExpiryClose };
    public enum OrderPriceSpec { market };

    public class Order
    {
        public Instrument Instrument;
        public int Quantity;
        public OrderExecution Execution;
        public OrderPriceSpec PriceSpec;
        public double Price;
    }
}

//==============================================================================
// end of file