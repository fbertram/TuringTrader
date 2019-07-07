//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PlotterRenderR
// Description: Plotter renderer for R templates
// History:     2019vi20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TuringTrader.Simulator;

#if false
#region private void OpenWithRscript(string pathToRscriptTemplate)
#if ENABLE_R
        /// <summary>
        /// Open plot with Rscript. This will launch the default R core,
        /// as found in HKLM/SOFTWARE/R-Core/R/InstallPath
        /// </summary>
        /// <param name="pathToRscriptTemplate"></param>
        private void OpenWithRscript(string pathToRscriptTemplate)
        {
            string defaultR = GlobalSettings.DefaultRCore;
            if (defaultR == null)
                throw new Exception("Logger: no default R installation found");

            string rscriptExe = Path.Combine(defaultR, "bin", "Rscript.exe");
            if (!File.Exists(rscriptExe))
                throw new Exception("Logger: Rscript.exe not found");

            string csvFileArgs = "";
            foreach (string plotTitle in AllData.Keys)
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

                SaveAsCsv(tmpFile, plotTitle, _convertToString);

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
#else
        private void OpenWithRscript(string pathToRscriptTemplate)
        {
				Output.WriteLine("Plotter: OpenWithRscript bypassed w/ ENABLE_R switch");
        }
#endif
#endregion
#endif

namespace TuringTrader
{
    static class PlotterRenderR
    {
        public static void Register()
        {
            Plotter.Renderer += Renderer;
        }

        public static void Renderer(Plotter plotter, string template)
        {
            if (Path.GetExtension(template).ToLower() != ".r")
                return;

            Output.WriteLine("Rendering with R is currently unavailable. Stay tuned");
        }
    }
}

//==============================================================================
// end of file