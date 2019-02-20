//==============================================================================
// Project:     Trading Simulator
// Name:        Output
// Description: output methods
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
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
        /// Debug output event.
        /// </summary>
        public static Action<string> WriteEvent;

        /// <summary>
        /// Write formatted debug output.
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">list or arguments</param>
        public static void Write(string format, params object[] args)
        {
            string message = args.Count() > 0
                ? string.Format(format, args)
                : format;

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