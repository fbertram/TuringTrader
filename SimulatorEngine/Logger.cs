//==============================================================================
// Project:     Trading Simulator
// Name:        Plotter
// Description: logging class to connect w/ CSV Files, Excel, R
// History:     2017xii08,   FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

//#define ENABLE_R
// for R, we need RDotNet installed. comment the line above to disable R
// install with the following command: nuget install R.Net.Community
// tested successfully w/ MultiCharts 11 and RDotNet 1.7.0
// add assembly references to the following DLLs
// - DynamicInterop.dll
// - RDotNet.dll
// - RDotNet.NativeLibrary.dll

#define ENABLE_EXCEL
// for Excel, we need Microsoft office installed. comment the line above to disable Excel
// tested successfully w/ MultiCharts 11 and Excel 2016 via Office 365
// add global assembly references to the following DLLs
// - Microsoft.Office.Interop.Excel.dll

#region libraries
using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;

#if ENABLE_EXCEL
using Excel = Microsoft.Office.Interop.Excel;
#endif

#if ENABLE_R
using RDotNet;
using RDotNet.NativeLibrary;
#endif
#endregion


namespace FUB_TradingSim
{
    public class Logger
    {
        #region internal data
        //private CStudyControl Host;
        private string CurrentPlot;
        private Dictionary<string, string> XLabels;
        private Dictionary<string, List<Dictionary<string, string>>> LogData;
        #endregion
        #region internal helpers
        private void _Log(string yLabel, string yValue)
        {
            LogData[CurrentPlot].Last()[yLabel] = yValue;
        }
        #endregion

        //----- initialization & cleanup
        public Logger() { }
        #region public void Clear()
        public void Clear()
        {
            XLabels = null;
            LogData = null;
        }
        #endregion

        //----- logging values
        #region public void SelectPlot(string plotTitle, string xLabel)
        /// <summary>
        /// select current plot
        /// </summary>
        /// <param name="plotTitle">title of plot</param>
        /// <param name="xLabel">label on x-axis</param>
        public void SelectPlot(string plotTitle, string xLabel)
        {
            CurrentPlot = plotTitle;

            if (XLabels == null)
                XLabels = new Dictionary<string, string>();

            XLabels[plotTitle] = xLabel;
        }
        #endregion

        #region public void SetX(double xValue)
        /// <summary>
        /// set value along x-asis
        /// </summary>
        /// <param name="xLabel">x-axis label</param>
        /// <param name="xValue">x-axis value</param>
        public void SetX(double xValue)
        {
            string xValueStr = string.Format("{0}", xValue);
            SetX(xValueStr);
        }
        #endregion
        #region public void SetX(DateTime xValue)
        /// <summary>
        /// set x-axis, select plot
        /// </summary>
        /// <param name="xLabel">x-axis label</param>
        /// <param name="xValue">x-axis value</param>
        public void SetX(DateTime xValue)
        {
            string xValueStr = string.Format("{0:MM/dd/yyyy}", xValue);
            SetX(xValueStr);
        }
        #endregion
        #region public void SetX(string xValue)
        /// <summary>
        /// set x-axis, select plot
        /// </summary>
        /// <param name="xLabel">x-axis label</param>
        /// <param name="xValue">x-axis value</param>
        public void SetX(string xValue)
        {
            // create log data structure
            if (LogData == null)
                LogData = new Dictionary<string, List<Dictionary<string, string>>>();

            if (CurrentPlot == null)
                SelectPlot("Untitled Plot", "x");

            // create structure for current plot
            if (!LogData.ContainsKey(CurrentPlot))
                LogData[CurrentPlot] = new List<Dictionary<string, string>>();

            // create row for xValue (multiple rows w/ identical xValues are possible)
            LogData[CurrentPlot].Add(new Dictionary<string, string>());

            // save xValue
            LogData[CurrentPlot].Last()[XLabels[CurrentPlot]] = xValue;
        }
        #endregion

        #region public void Log(string yLabel, double yValue)
        /// <summary>
        /// add log to current x-axis/ plot
        /// </summary>
        /// <param name="yLabel">y-axis label</param>
        /// <param name="yValue">y-axis value</param>
        public void Log(string yLabel, double yValue)
        {
            string yValueStr = string.Format("{0}", yValue);
            _Log(yLabel, yValueStr);
        }
        #endregion
        #region public void Log(string yLabel, string yValue)
        public void Log(string yLabel, string yValue)
        {
            string yValueStr = string.Format("\"{0}\"", yValue);
            _Log(yLabel, yValueStr);
        }
        #endregion

        //----- output
        #region public int SaveAsCsv(string filePath, string plotTitle = null)
        /// <summary>
        /// save log as CSV file
        /// </summary>
        /// <param name="filePath">path to destination file</param>
        public int SaveAsCsv(string filePath, string plotTitle = null)
        {
            if (plotTitle == null)
                plotTitle = CurrentPlot;

            using (StreamWriter file = new StreamWriter(filePath))
            {
                Debug.WriteLine("{0}: saving {1} data points to {2}", plotTitle, LogData[plotTitle].Count, filePath);

                //--- header row
                file.Write("\"{0}\"", XLabels[plotTitle]);
                foreach (string label in LogData[plotTitle][0].Keys)
                    if (label != XLabels[plotTitle]) file.Write(",\"{0}\"", label);
                file.WriteLine("");

                //--- data rows
                foreach (var row in LogData[plotTitle])
                {
                    file.Write("{0}", row[XLabels[plotTitle]]);
                    foreach (string label in row.Keys)
                        if (label != XLabels[plotTitle]) file.Write(",{0}", row[label]);
                    file.WriteLine("");
                }

                // empty row seperates tables
                file.WriteLine("");
            }

            return LogData[plotTitle].Count;
        }
        #endregion
        #region public void OpenWithExcel(string pathToExcelTemplate)
#if ENABLE_EXCEL
        /// <summary>
        /// open log with existing Excel sheet, containing UPDATE_ALL macro
        /// </summary>
        /// <param name="pathToExcelTemplate">path to existing excel file</param>
        public void OpenWithExcel(string pathToExcelTemplate)
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

            //
            // https://stackoverflow.com/questions/38542748/visual-c-sharp-setting-multiple-cells-at-once-with-excel-interop
            // https://stackoverflow.com/questions/4811664/set-cell-value-using-excel-interop
            // also: Application.ScreenUpdating = false, calculation to manual
            // https://support.microsoft.com/en-us/help/302096/how-to-automate-excel-by-using-visual-c-to-fill-or-to-obtain-data-in-a
            // https://social.msdn.microsoft.com/Forums/lync/en-US/2e33b8e5-c9fd-42a1-8d67-3d61d2cedc1c/how-to-call-excel-macros-programmatically-in-c?forum=exceldev

            //var excel = new Excel.ApplicationClass();
            var excel = new Excel.Application();

            excel.Visible = true;
            var wbooks = excel.Workbooks;
            //var wbook = wbooks.Open(pathToExcelFile);
            var wbook = wbooks.Add(pathToExcelTemplate); // create new w/ file as template
            Thread.Sleep(500); // this is ugly but prevents Excel from crashing

            List<string> plots = LogData.Keys.ToList();
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
				Debug.WriteLine("Logger: OpenWithExcel bypassed w/ ENABLE_EXCEL switch");
			}
#endif // ENABLE_EXCEL
        #endregion
        #region public void OpenWithR(List<string> RCommands = null)
#if ENABLE_R
        /// <summary>
        /// open and plot log with R
        /// </summary>
        /// <param name="RCommands">R commands to load and plot</param>
        public void OpenWithR(List<string> RCommands = null)
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
						// BUGBUG: every attempt to create a legend crashes R.NET:
						//"legend(\"bottom\",legend=colnames(data), col=seq_len(ncol(data)), cex=0.8, fill=seq_len(ncol(data)))",
	                    //"legend(0, 0, legend=c(\"a\",\"b\"),col=c(\"red\",\"blue\"))",
						// BUGBUG: even simple text output crashes:
						//"text(0,0,c(\"anna\",\"berta\"))",
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

            Clear();

            // if we dispose the REngine here, we can not plot again,
            // until we have re-started the main-application
            engine.Dispose();
        }
#else // ENABLE_R
			public void OpenWithR(List<string> RCommands = null)
			{
				Debug.WriteLine("Logger: OpenWithR bypassed w/ ENABLE_R switch");
			}
#endif // ENABLE_R
        #endregion
    }
}

//==============================================================================
// end of file