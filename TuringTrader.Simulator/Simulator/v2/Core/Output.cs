//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Output
// Description: Algorithm output capabilities.
// History:     2023ii13, FUB, created
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

        private static string _formatOutput(string format, params object[] args)
            => args.Count() > 0 ? string.Format(format, args) : format;

        private static void _printOutput(string output)
            => Simulator.Output.WriteLine(output);
        #endregion

        /// <summary>
        /// Enumeration of output modes.
        /// </summary>
        public enum DisplayModeType
        {
            /// <summary>
            /// show errors (and throw an exception), but suppress warnings and info.
            /// </summary>
            errorsOnly,
            /// <summary>
            /// show errors and first occurrence of warnings, but suppress repeated warnings and info.
            /// </summary>
            errorsAndWarningsOnce,
            /// <summary>
            /// show errors and all warnings, but suppress info.
            /// </summary>
            errorsAndWarnings,
            /// <summary>
            /// show errors, warnings, and general info.
            /// </summary>
            errorsWarningsAndInfo,
        };

        /// <summary>
        /// Current output mode.
        /// </summary>
        public static DisplayModeType DisplayMode { get; set; } = DisplayModeType.errorsWarningsAndInfo;

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
                    _printOutput(_formatOutput(format, args));
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
            var warning = _formatOutput(format, args);

            switch (DisplayMode)
            {
                case DisplayModeType.errorsWarningsAndInfo:
                case DisplayModeType.errorsAndWarnings:
                    _printOutput(warning);
                    break;

                case DisplayModeType.errorsAndWarningsOnce:
                    if (!_showOnce.ContainsKey(warning))
                        _printOutput(warning);
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
            var error = _formatOutput(format, args);
            _printOutput(error);
            throw new Exception(error);
        }

        /// <summary>
        /// Display output message. This is a legacy method, and will be removed
        /// from the API soon. Use Info, Warning, or Error instead.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(string format, params object[] args)
            => _printOutput(_formatOutput(format, args));
    }
}

//==============================================================================
// end of file
