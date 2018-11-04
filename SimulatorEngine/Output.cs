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
    /// <summary>
    /// Class providing formatted text output.
    /// </summary>
    public class Output
    {
        /// <summary>
        /// Delegate type to write debug message.
        /// </summary>
        /// <param name="message">debug message</param>
        public delegate void WriteEventDelegate(string message);

        /// <summary>
        /// Debug output event.
        /// </summary>
        public static WriteEventDelegate WriteEvent;

        /// <summary>
        /// Write formatted debug output.
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">list or arguments</param>
        public static void Write(string format, params object[] args)
        {
            string message = string.Format(format, args);

            if (WriteEvent == null)
                Debug.Write(message);
            else
                WriteEvent(message);
        }

        /// <summary>
        /// Write formatted debug output, and start new line.
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">list of arguments</param>
        public static void WriteLine(string format, params object[] args)
        {
            Write(format, args);
            Write(Environment.NewLine);
        }
    }
}

//==============================================================================
// end of file