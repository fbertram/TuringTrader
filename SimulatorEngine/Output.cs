//==============================================================================
// Project:     Trading Simulator
// Name:        Output
// Description: output methods
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    public class Output
    {
        public static void Write(string format)
        {
#if DEBUG
            Debug.Write(format);
#else
            Console.Write(format);
#endif
        }

        public static void WriteLine(string format, params object[] args)
        {
#if DEBUG
            Debug.WriteLine(format, args);
#else
            Console.WriteLine(format, args);
#endif
        }
    }
}

//==============================================================================
// end of file