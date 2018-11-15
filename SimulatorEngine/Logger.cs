//==============================================================================
// Project:     Trading Simulator
// Name:        Plotter
// Description: logging class to connect w/ CSV Files, Excel, R
// History:     2017xii08, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#define ENABLE_R
// for R, we need R installed. This has been tested successfully with
// R 3.4.3 from CRAN, and Microsoft R 3.3.2.0.
// To disable R, comment the line above.

#define ENABLE_EXCEL
// for Excel, we need Microsoft office installed. comment the line above to disable Excel
// tested successfully with Excel 2016 via Office 365
// add global assembly references to the following DLLs
// - Microsoft.Office.Interop.Excel.dll

#region libraries
using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

#if ENABLE_EXCEL
using Excel = Microsoft.Office.Interop.Excel;
#endif

#if false
using RDotNet;
using RDotNet.NativeLibrary;
using System.Drawing.Imaging;
#endif

#if false
using Microsoft.R.Host.Client;
#endif
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Class to log data, and save as CSV, or present with Excel or R.
    /// </summary>
    public class Logger
    {
        #region internal data
        /// <summary>
        /// title of current plot
        /// </summary>
        private string CurrentPlot;

        /// <summary>
        /// x-axis label for given plot title
        /// </summary>
        private Dictionary<string, List<string>> AllLabels = new Dictionary<string, List<string>>();
        private List<string> CurrentLabels = null;

        /// <summary>
        /// log data for given plot title. log data is list of dictionaries,
        /// one entry per row, with key being the name of the field.
        /// </summary>
        private Dictionary<string, List<Dictionary<string, object>>> AllData = new Dictionary<string, List<Dictionary<string, object>>>();
        private List<Dictionary<string, object>> CurrentData = null;
        #endregion
        #region internal helpers
        private string AddQuotesAsRequired(string value)
        {
            char[] needsQuotes = " ,".ToCharArray();

            if (value.IndexOfAny(needsQuotes) >= 0)
                return string.Format("\"{0}\"", value);
            else
                return value;
        }
        #endregion
        #region private void OpenWithExcel(string pathToExcelTemplate)
#if ENABLE_EXCEL
        /// <summary>
        /// Open log with existing Excel template, containing UPDATE_ALL macro.
        /// </summary>
        /// <param name="pathToExcelTemplate">path to existing excel file</param>
        private void OpenWithExcel(string pathToExcelTemplate)
        {
            if (AllData == null || AllData.Keys.Count == 0)
                return;

            int rows = AllData.Keys
                .Select(item => AllData[item].Count)
                .Max();
            if (rows <= 1)
            {
                Clear();
                return;
            }

            //
            // https://stackoverflow.com/questions/38542748/visual-c-sharp-setting-multiple-cells-at-once-with-excel-interop
            // https://stackoverflow.com/questions/4811664/set-cell-value-using-excel-interop
            // also: Application.ScreenUpdating = false, calculation to manual
            // https://support.microsoft.com/en-us/help/302096/how-to-automate-excel-by-using-visual-c-to-fill-or-to-obtain-data-in-a
            // https://social.msdn.microsoft.com/Forums/lync/en-US/2e33b8e5-c9fd-42a1-8d67-3d61d2cedc1c/how-to-call-excel-macros-programmatically-in-c?forum=exceldev

            //var excel = new Excel.ApplicationClass();
            var excel = new Excel.Application()
            {
                Visible = true,
            };
            var wbooks = excel.Workbooks;
            //var wbook = wbooks.Open(pathToExcelFile);
            var wbook = wbooks.Add(pathToExcelTemplate); // create new w/ file as template
            Thread.Sleep(500); // this is ugly but prevents Excel from crashing

            List<string> plots = AllData.Keys.ToList();
            for (int i = 0; i < plots.Count; i++)
            {
                string plot = plots[i];
                string tmpFile = Path.GetTempFileName();
                SaveAsCsv(tmpFile, plot);

                //excel.GetType().InvokeMember("Run",
                //    System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.InvokeMethod,
                //    null,
                //    excel,
                //    new Object[]{string.Format("{0}!UPDATE_LOGGER", Path.GetFileName(pathToExcelFile)),
                //                        tmpFile, plots.Count, i});
                excel.Run("UPDATE_LOGGER", tmpFile, plots.Count, i, plot);

                Thread.Sleep(500); // this is ugly but prevents Excel from crashing
            }
        }
#else // ENABLE_EXCEL
			public void OpenWithExcel(string pathToExcelFile = @"C:\ProgramData\TS Support\MultiCharts .NET64\__FUB_Research.xlsm")
			{
				Output.WriteLine("Logger: OpenWithExcel bypassed w/ ENABLE_EXCEL switch");
			}
#endif // ENABLE_EXCEL
        #endregion
#if false
        // for R, we need RDotNet installed. comment the line above to disable R
        // install with the following command: nuget install R.Net.Community
        // tested successfully w/ MSVC 2017, R 3.4.3, and RDotNet 1.7.0
        // add assembly references to the following DLLs
        // - DynamicInterop.dll
        // - RDotNet.dll
        // - RDotNet.NativeLibrary.dll
        #region public void OpenWithR(List<string> RCommands = null)
        /// <summary>
        /// Open and plot log with R.
        /// </summary>
        /// <param name="RCommands">R commands to load and plot</param>
        private void OpenWithR(List<string> RCommands = null)
        {
            if (LogData == null || LogData.Keys.Count == 0)
                return;

            int rows = LogData.Keys
                .Select(item => LogData[item].Count)
                .Min();
            if (rows <= 1)
            {
                Clear();
                return;
            }

            if (RCommands == null)
                RCommands = new List<string>()
            {
                "data<-read.csv(\"{2}\")",
                "x<-data[,1]",
#if true
				"y<-data[,-1]",
#else
				"y<-scale(data[,-1])",
#endif
				"matplot(x, y, type=\"l\", lty=1)",
                "title(main=\"{0}\",xlab=\"{1}\",ylab=\"\")",
				"legend(\"bottom\",legend=colnames(y), col=seq_len(ncol(y)), cex=0.8, fill=seq_len(ncol(y)))",
			};

            // we need R's bin folder in PATH
            REngine.SetEnvironmentVariables();
            REngine engine = REngine.GetInstance();

            engine.Evaluate(string.Format("par(mfrow=c({0}, 1))", LogData.Keys.Count));

            foreach (string plot in LogData.Keys)
            {
                string tmpFile = Path.GetTempFileName();
                string tmpFile2 = tmpFile.Replace("\\", "/");
                SaveAsCsv(tmpFile, plot);

                Debug.WriteLine(string.Format("opening {0} with R", tmpFile));
                foreach (string command in RCommands)
                {
                    string commandExpanded = string.Format(command, plot, XLabels[plot], tmpFile2);
                    try
                    {
                        engine.Evaluate(commandExpanded);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(string.Format("Plotter caused R exception {0}", e.Message));
                    }
                }
            }

            // if we dispose the REngine here, we can not plot again,
            // until we have re-started the main application
            // engine.Dispose();
        }
        #endregion
#endif // R_DOT_NET

#if false
        // for R, we need R Tools for Visual Studio (RTVS) installed.
        // - Microsoft.R.Host.Client
        #region public byte[] PlotWithR(int width, int height, int dpi)
        /// <summary>
        /// Render with R.
        /// </summary>
        /// <param name="width">canvas width</param>
        /// <param name="height">canvas height</param>
        /// <param name="dpi">canvas resolution</param>
        /// <param name="rscript">list of R commands, default null</param>
        /// <returns>rendered bitmap</returns>
        private byte[] RenderWithR(int width = 640, int height = 480, int dpi = 96, List<string> rscript = null)
        {
            if (AllData == null || AllData.Keys.Count == 0)
                throw new Exception("Logger: no data to render");

            int rows = AllData.Keys
                .Select(item => AllData[item].Count)
                .Min();
            if (rows <= 1)
            {
                Clear();
                throw new Exception("Logger: no data to render");
            }

            if (rscript == null)
                rscript = new List<string>()
            {
                "data<-read.csv(\"{2}\")",
                // https://stackoverflow.com/questions/18178451/is-there-a-way-to-check-if-a-column-is-a-date-in-r
                //"sapply(data, function(x) !all(is.na(as.Date(as.character(x),format=\"%m/%d/%Y\"))))",
                "x<-data[,1]",
                "y<-data[,-1]",
				"matplot(x, y, type=\"l\", lty=1)",
                "title(main=\"{0}\",xlab=\"{1}\",ylab=\"\")",
				//"legend(\"bottom\",legend=colnames(y), col=seq_len(ncol(y)), cex=0.8, fill=seq_len(ncol(y)))",
			};

            // Init R session
            IRHostSession _rSession = RHostSession.Create("Test");
            Task sessionStartTask = _rSession.StartHostAsync(new RHostSessionCallback());
            sessionStartTask.Wait();

            // prepare R commands
            string rcommand = string.Format("par(mfrow=c({0}, 1))", AllData.Keys.Count) + Environment.NewLine;
            foreach (string plotTitle in AllData.Keys)
            {
#if true
                string _convertToString(object o)
                {
                    // see https://stackoverflow.com/questions/298976/is-there-a-better-alternative-than-this-to-switch-on-type/299001#299001

                    if (o.GetType() == typeof(DateTime))
                    {
                        DateTime d = (DateTime)o;
                        return (d.Year + d.DayOfYear / 365.25).ToString();
                        //return string.Format("as.Date(\"{0}, format=\"%m/%d/%Y\")", d);
                        //return string.Format("{0:MM/dd/yyyy})", d);
                    }

                    return o.ToString();
                }

                string tmpFile = Path.GetTempFileName();
                string tmpFile2 = tmpFile.Replace("\\", "/");
                SaveAsCsv(tmpFile, plotTitle, _convertToString);
#else
                // push C# data directly into R dataframe
#endif

                foreach (string command in rscript)
                    rcommand += string.Format(command, plotTitle, AllLabels[plotTitle].First(), tmpFile2) + Environment.NewLine;
                    //rcommand += string.Format(command, plotTitle, AllLabels[plotTitle], tmpFile2) + Environment.NewLine;
            }

            // execute text command
            var textResult = _rSession.ExecuteAndOutputAsync(rcommand);
            textResult.Wait();
            Output.WriteLine(rcommand);
            Output.WriteLine(textResult.Result.Output);

            // get plot output
            var plotResult = _rSession.PlotAsync(rcommand, width, height, dpi);
            plotResult.Wait();

            return plotResult.Result;
        }
        #endregion
#endif // R_HOST_CLIENT

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

                string _convertToString(object o)
                {
                    // see https://stackoverflow.com/questions/298976/is-there-a-better-alternative-than-this-to-switch-on-type/299001#299001

                    if (o.GetType() == typeof(DateTime))
                    {
                        DateTime d = (DateTime)o;
                        //return (d.Year + d.DayOfYear / 365.25).ToString();
                        return string.Format("{0:MM/dd/yyyy}", d);
                    }

                    return o.ToString();
                }

                SaveAsCsv(tmpFile, plotTitle, _convertToString);

                // see https://stackoverflow.com/questions/5510343/escape-command-line-arguments-in-c-sharp/6040946#6040946
                string _encodeParameterArgument(string original)
                {
                    if (string.IsNullOrEmpty(original))
                        return original;
                    string value = Regex.Replace(original, @"(\\*)" + "\"", @"$1\$0");
                    value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
                    return value;
                }

                // Note that this version does the same but handles new lines in the arugments
                string _enncodeParameterArgumentMultiLine(string original)
                {
                    if (string.IsNullOrEmpty(original))
                        return original;
                    string value = Regex.Replace(original, @"(\\*)" + "\"", @"$1\$0");
                    value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"", RegexOptions.Singleline);

                    return value;
                }
                
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
				Output.WriteLine("Logger: OpenWithRscript bypassed w/ ENABLE_R switch");
        }
#endif
        #endregion

        //----- initialization & cleanup
        #region public Logger()
        /// <summary>
        /// Create and initialize logger object.
        /// </summary>
        public Logger() { }
        #endregion
        #region public void Clear()
        /// <summary>
        /// Clear all current data.
        /// </summary>
        public void Clear()
        {
            AllLabels.Clear();
            AllData.Clear();
        }
        #endregion

        //----- logging values
        #region public void SelectPlot(string plotTitle, string xLabel)
        /// <summary>
        /// Select current plot.
        /// </summary>
        /// <param name="plotTitle">title of plot</param>
        /// <param name="xLabel">label on x-axis</param>
        public void SelectPlot(string plotTitle, string xLabel)
        {
            if (!AllLabels.ContainsKey(plotTitle))
                AllLabels[plotTitle] = new List<string>();

            if (!AllData.ContainsKey(plotTitle))
                AllData[plotTitle] = new List<Dictionary<string, object>>();

            CurrentPlot = plotTitle;
            CurrentData = AllData[plotTitle];
            CurrentLabels = AllLabels[plotTitle];

            if (CurrentLabels.Count == 0)
                CurrentLabels.Add(xLabel);

            if (CurrentLabels.First() != xLabel)
                throw new Exception("Logger: x-label mismatch");
        }
        #endregion
        #region public void SetX(object xValue)
        /// <summary>
        /// Set value along x-asis.
        /// </summary>
        /// <param name="xValue">x-axis value</param>
        public void SetX(object xValue)
        {
            if (CurrentPlot == null)
                SelectPlot("Untitled Plot", "x");

            // create row for xValue (multiple rows w/ identical xValues are possible)
            CurrentData.Add(new Dictionary<string, object>());

            // save xValue
            CurrentData.Last()[CurrentLabels.First()] = xValue;
        }
        #endregion
        #region public void Log(string yLabel, object yValue)
        /// <summary>
        /// Log new value to current plot, at current x-axis value.
        /// </summary>
        /// <param name="yLabel">y-axis label</param>
        /// <param name="yValue">y-axis value</param>
        public void Log(string yLabel, object yValue)
        {
            if (!CurrentLabels.Contains(yLabel))
                CurrentLabels.Add(yLabel);

            CurrentData.Last()[yLabel] = yValue;
        }
        #endregion

        //----- output
        #region public int SaveAsCsv(string filePath, string plotTitle = null)
        /// <summary>
        /// Save log as CSV file.
        /// </summary>
        /// <param name="filePath">path to destination file</param>
        /// <param name="plotTitle">plot to save, null for current plot</param>
        /// <param name="toString">function to convert object to string, default null</param>
        public int SaveAsCsv(string filePath, string plotTitle = null, Func<object, string> toString = null)
        {
            if (plotTitle == null)
                plotTitle = CurrentPlot;

            if (toString == null)
                toString = (o) => o.ToString();

            using (StreamWriter file = new StreamWriter(filePath))
            {
                Output.WriteLine("{0}: saving {1} data points to {2}", plotTitle, AllData[plotTitle].Count, filePath);

                //--- header row
                var currentData = AllData[plotTitle];
                var currentLabels = AllLabels[plotTitle];

                file.WriteLine(string.Join(",", currentLabels.Select(l => AddQuotesAsRequired(l))));

                //--- data rows
                foreach (var row in currentData)
                {
                    var items = currentLabels
                        .Select(l => AddQuotesAsRequired(toString(row[l])));

                    file.WriteLine(string.Join(",", items));
                }

                // empty row seperates tables
                file.WriteLine("");
            }

            return AllData[plotTitle].Count;
        }
        #endregion
        #region public void OpenWith(string pathToTemplateFile)
        /// <summary>
        /// Open plot with Excel or R, depending on the template file.
        /// The template file can be an absolute path, a relative path, 
        /// or a simple file name. Relative pathes as well as simple
        /// file names will both start from the Templates folder inside
        /// TuringTrader's home location.
        /// </summary>
        /// <param name="pathToTemplateFile">path to template file</param>
        public void OpenWith(string pathToTemplateFile)
        {
            string fullPath = Path.IsPathRooted(pathToTemplateFile)
                ? pathToTemplateFile
                : Path.Combine(GlobalSettings.TemplatePath, pathToTemplateFile);

            string extension = Path.GetExtension(fullPath).ToLower();
            if (extension.Equals(""))
            {
                extension = GlobalSettings.DefaultTemplateExtension;
                fullPath += extension;
            }

            if (!File.Exists(fullPath))
                throw new Exception(string.Format("Logger: template {0} not found", fullPath));

            if (extension.Equals(".xlsm"))
            {
                OpenWithExcel(fullPath);
            }
            else if (extension.Equals(".r"))
            {
                OpenWithRscript(fullPath);
            }
            else if (extension.Equals(".rmd"))
            {
                string launcherScript = Path.ChangeExtension(Path.GetTempFileName(), ".r");
                string renderOutput = Path.ChangeExtension(Path.GetTempFileName(), ".htm");

                using (var sw = new StreamWriter(launcherScript))
                {
#if false
                    sw.WriteLine("rmarkdown::render(\"{0}\", output_file=\"{1}\")",
                        fullPath.Replace("\\", "/"),
                        renderOutput.Replace("\\", "/"));
                    sw.WriteLine("browseURL(\"{0}\")",
                        renderOutput.Replace("\\", "/"));
#else
                    sw.WriteLine("library(rmarkdown)");
                    sw.WriteLine("render(\"{0}\", output_file=\"{1}\")",
                        fullPath.Replace("\\", "/"),
                        renderOutput.Replace("\\", "/"));
                    sw.WriteLine("browseURL(\"{0}\")",
                        renderOutput.Replace("\\", "/"));
#endif
                    sw.Flush();
                    OpenWithRscript(launcherScript);
                }
            }
        }
#endregion
    }
}

//==============================================================================
// end of file