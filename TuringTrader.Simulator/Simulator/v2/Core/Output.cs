//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Output
// Description: Algorithm output capabilities.
// History:     2018ix11, FUB, created
//              2023ii13, FUB, new info/ warning/ error methods for v2 engine.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Class to provide output capabilities. Output can be filtered
    /// by errors, warnings, and general info. 
    /// </summary>
    public class Output
    {
        #region private stuff
        private static Dictionary<string, bool> _showOnce = new Dictionary<string, bool>();

        private static string _formatMessage(string format, params object[] args)
            => (args.Count() > 0 ? string.Format(format, args) : format);

        private static void _printMessage(string message)
        {
            if (WriteEvent == null)
                Debug.Write(message);
            else
                WriteEvent(message);
        }
        #endregion

        /// <summary>
        /// Debug output event. The application should attach an event
        /// handler here to redirect the messages to a console.
        /// </summary>
        public static Action<string> WriteEvent;

        /// <summary>
        /// Enumeration of output modes.
        /// </summary>
        public enum DisplayModeType
        {
            /// <summary>
            /// show errors (and throw an exception), but suppress warnings 
            /// and informational messages.
            /// </summary>
            errorsOnly,
            /// <summary>
            /// show errors and first occurrence of warnings, but suppress 
            /// repeated warnings and all informational messages.
            /// </summary>
            errorsAndWarningsOnce,
            /// <summary>
            /// show errors and all warnings, but suppress informational messages.
            /// </summary>
            errorsAndWarnings,
            /// <summary>
            /// show all messages including errors, warnings, and info.
            /// </summary>
            errorsWarningsAndInfo,
        };

        /// <summary>
        /// Current output mode.
        /// </summary>
        public static DisplayModeType DisplayMode => GlobalSettings.DisplayMode;

        /// <summary>
        /// Display informational message. These messages will only be shown
        /// if DisplayMode is set to errorsWarningsAndInfo.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void ShowInfo(string format, params object[] args)
        {
            switch (DisplayMode)
            {
                case DisplayModeType.errorsWarningsAndInfo:
                    _printMessage(_formatMessage(format, args) + Environment.NewLine);
                    break;

                case DisplayModeType.errorsAndWarnings:
                case DisplayModeType.errorsAndWarningsOnce:
                case DisplayModeType.errorsOnly:
                    // ignore output for all other
                    break;
            }
        }

        /// <summary>
        /// Display warning message. These messages will only be shown
        /// if DisplayMode is set to errorsWarningsAndInfo, errorsAndWarnings,
        /// or errorsAndWarningsOnce. For errorsAndWarningsOnce, the message
        /// will only be shown the first time, repeated warnings with the
        /// same text will be suppressed.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void ShowWarning(string format, params object[] args)
        {
            var warning = _formatMessage(format + Environment.NewLine, args);

            switch (DisplayMode)
            {
                case DisplayModeType.errorsWarningsAndInfo:
                case DisplayModeType.errorsAndWarnings:
                    _printMessage(warning);
                    break;

                case DisplayModeType.errorsAndWarningsOnce:
                    if (!_showOnce.ContainsKey(warning))
                        _printMessage(warning);
                    _showOnce[warning] = true;
                    break;

                case DisplayModeType.errorsOnly:
                    // ignore output for all other
                    break;
            }
        }

        /// <summary>
        /// Display error message. Note that this method will also throw an exception.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <exception cref="Exception"></exception>
        public static void ThrowError(string format, params object[] args)
        {
            var error = _formatMessage(format + Environment.NewLine, args);
            _printMessage(error);
            throw new Exception(error);
        }

        /// <summary>
        /// Display output message. This is a legacy method, and will be removed
        /// from the API soon. Use Info, Warning, or Error instead.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Write(string format, params object[] args)
            => _printMessage(_formatMessage(format, args));

        /// <summary>
        /// Display output message on a new line. This is a legacy method, 
        /// and will be removed from the API soon. Use Info, Warning, or 
        /// Error instead.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(string format, params object[] args)
            => _printMessage(_formatMessage(format + Environment.NewLine, args));
    }
}

//==============================================================================
// end of file
