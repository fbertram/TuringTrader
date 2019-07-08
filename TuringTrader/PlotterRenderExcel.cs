//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PlotterRenderExcel
// Description: Plotter renderer for Excel templates
// History:     2019vi20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
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
using System.Linq;
using System.Text;
using System.Threading;
using TuringTrader.Simulator;
//using Excel = Microsoft.Office.Interop.Excel;
using Excel = NetOffice.ExcelApi;

#if false
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

            if (rows < 1)
            {
                Clear();
                return;
            }

            var excel = new Excel.Application();
            var wbooks = excel.Workbooks;
            var wbook = wbooks.Add(pathToExcelTemplate); // create new w/ file as template
            Thread.Sleep(500); // FIXME: prevents Excel from crashing

            List<string> plots = AllData.Keys.ToList();
            for (int i = 0; i < plots.Count; i++)
            {
                string plot = plots[i];
                string tmpFile = Path.GetTempFileName();

                SaveAsCsv(
                    tmpFile,
                    plot,
                    o =>
                    {
                        if (o.GetType() == typeof(DateTime))
                            return string.Format("{0:MM/dd/yyyy}", (DateTime)o);

                        return o.ToString();
                    });

                excel.Run("UPDATE_LOGGER", tmpFile, plots.Count, i, plot);
                Thread.Sleep(500); // FIXME: prevents Excel from crashing
            }

#if true
            // return control to the user
            excel.Visible = true;
            excel.UserControl = true;

#endif
#if false
            // BUGBUG: it seems that Excel is occasionally leaving 
            // a zombie process behind, after the workbook is closed.

            void releaseObject(object obj)
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                    obj = null;
                }
                catch (Exception ex)
                {
                    obj = null;
                }
                finally
                {
                    GC.Collect();
                }
            }

            //wbook.Close(false); // can't do this: want to keep document open
            //excel.Quit();       // can't do this: will ask to save & close
            releaseObject(excel); // doesn't seem to do anything
            releaseObject(wbook); // doesn't seem to do anything
#endif
        }
#else // ENABLE_EXCEL
			public void OpenWithExcel(string pathToExcelFile = @"C:\ProgramData\TS Support\MultiCharts .NET64\__FUB_Research.xlsm")
			{
				Output.WriteLine("Plotter: OpenWithExcel bypassed w/ ENABLE_EXCEL switch");
			}
#endif // ENABLE_EXCEL
#endregion
#endif

namespace TuringTrader
{
    static class PlotterRenderExcel
    {
        public static void Register()
        {
            Plotter.Renderer += Renderer;
        }

        public static void Renderer(Plotter plotter, string template)
        {
            if (Path.GetExtension(template).ToLower() != ".xlsm")
                return;


            if (plotter.AllData == null || plotter.AllData.Keys.Count == 0)
                return;

            int rows = plotter.AllData.Keys
                .Select(item => plotter.AllData[item].Count)
                .Max();

            if (rows < 1)
                return;

            var excel = new Excel.Application();
            var wbooks = excel.Workbooks;
            var wbook = wbooks.Add(template); // create new w/ file as template
            Thread.Sleep(500); // FIXME: prevents Excel from crashing

            List<string> plots = plotter.AllData.Keys.ToList();
            for (int i = 0; i < plots.Count; i++)
            {
                string plot = plots[i];
                string tmpFile = Path.GetTempFileName();

                plotter.SaveAsCsv(
                    tmpFile,
                    plot,
                    o =>
                    {
                        if (o.GetType() == typeof(DateTime))
                            return string.Format("{0:MM/dd/yyyy}", (DateTime)o);

                        return o.ToString();
                    });

                excel.Run("UPDATE_LOGGER", tmpFile, plots.Count, i, plot);
                Thread.Sleep(500); // FIXME: prevents Excel from crashing
            }

#if true
            // return control to the user
            excel.Visible = true;
            excel.UserControl = true;

#endif
#if false
            // BUGBUG: it seems that Excel is occasionally leaving 
            // a zombie process behind, after the workbook is closed.

            void releaseObject(object obj)
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                    obj = null;
                }
                catch (Exception ex)
                {
                    obj = null;
                }
                finally
                {
                    GC.Collect();
                }
            }

            //wbook.Close(false); // can't do this: want to keep document open
            //excel.Quit();       // can't do this: will ask to save & close
            releaseObject(excel); // doesn't seem to do anything
            releaseObject(wbook); // doesn't seem to do anything
#endif
        }
    }
}

//==============================================================================
// end of file