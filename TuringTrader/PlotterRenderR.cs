//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PlotterRenderR
// Description: Plotter renderer for R templates
// History:     2019vi20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader
{
    static class PlotterRenderR
    {
        public static void Register()
        {
            Plotter.Renderer += Renderer;
        }

        #region public static void Renderer(Plotter plotter, string pathToRscriptTemplate)
        public static void Renderer(Plotter plotter, string pathToRscriptTemplate)
        {
            if (Path.GetExtension(pathToRscriptTemplate).ToLower() != ".r")
                return;

            if (!File.Exists(pathToRscriptTemplate))
            {
                Output.WriteLine("Template '{0}' not found", pathToRscriptTemplate);
                return;
            }

            string defaultR = GlobalSettings.DefaultRCore;

            string rscriptExe = Path.Combine(defaultR, "bin", "Rscript.exe");
            if (!File.Exists(rscriptExe))
                throw new Exception("PlotterRenderR: Rscript.exe not found");

            string csvFileArgs = "";
            foreach (string plotTitle in plotter.AllData.Keys)
            {
                string tmpFile = Path.ChangeExtension(Path.GetTempFileName(), ".csv");

                // use lambda expression, as local functions require VS2017 or later
                // see https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/local-functions
                Func<object, string> _convertToString = (o) =>
                {
                        // see https://stackoverflow.com/questions/298976/is-there-a-better-alternative-than-this-to-switch-on-type/299001#299001
                        if (o.GetType() == typeof(DateTime))
                    {
                        DateTime d = (DateTime)o;
                        return string.Format("{0:MM/dd/yyyy}", d);
                    }

                    return o.ToString();
                };

                plotter.SaveAsCsv(tmpFile, plotTitle, _convertToString);

                // see https://stackoverflow.com/questions/5510343/escape-command-line-arguments-in-c-sharp/6040946#6040946
                Func<string, string> _encodeParameterArgument = (original) =>
                {
                    if (string.IsNullOrEmpty(original))
                        return original;
                    string value = Regex.Replace(original, @"(\\*)" + "\"", @"$1\$0");
                    value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
                    return value;
                };

                // Note that this version does the same but handles new lines in the arugments
                /*string _enncodeParameterArgumentMultiLine(string original)
                {
                    if (string.IsNullOrEmpty(original))
                        return original;
                    string value = Regex.Replace(original, @"(\\*)" + "\"", @"$1\$0");
                    value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"", RegexOptions.Singleline);

                    return value;
                }*/

                // R needs:
                // - path seperators to be forward slashes
                // - blanks removed from arguments
                csvFileArgs += _encodeParameterArgument(plotTitle.Replace(" ", "_")) + " "
                    + _encodeParameterArgument(tmpFile.Replace("\\", "/")) + " ";
                Output.WriteLine("csvFileArgs=>>{0}<<", csvFileArgs);
            }

            try
            {
                var info = new ProcessStartInfo()
                {
                    FileName = rscriptExe,
                    WorkingDirectory = Path.GetDirectoryName(pathToRscriptTemplate),
                    Arguments = pathToRscriptTemplate + " " + csvFileArgs,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (var proc = new Process())
                {
                    proc.StartInfo = info;
                    proc.Start();
                    string result = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();

                    if (result.Length > 0)
                        Output.WriteLine(result);

                    if (error.Length > 0)
                        Output.WriteLine(error);
                }
            }
            catch (Exception)
            {
                throw new Exception("Logger: R script execution failed");
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file