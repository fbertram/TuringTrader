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

namespace TuringTrader.Simulator
{
    public class Output
    {
        public delegate void WriteEventDelegate(string message);
        public static WriteEventDelegate WriteEvent;

        public static void Write(string format, params object[] args)
        {
            string message = string.Format(format, args);

            if (WriteEvent == null)
                Debug.Write(message);
            else
                WriteEvent(message);
        }

        public static void WriteLine(string format, params object[] args)
        {
            Write(format, args);
            Write(Environment.NewLine);
        }
    }
}

//==============================================================================
// end of file