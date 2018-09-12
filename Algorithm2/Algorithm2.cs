//==============================================================================
// Project:     Trading Simulator
// Name:        Algorithm2
// Description: Sample Algorithm #2
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL v3.0
//==============================================================================

#define DEBUG_PLOT
//#define EXCEL_REPORT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    class Algorithm2 : Algorithm
    {
        private Logger _plotter = new Logger();
        private readonly string _dataPath = Directory.GetCurrentDirectory() + @"\..\..\..\Data";
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\ImportOnly.xlsm";

        public Algorithm2()
        {

        }

        override public void Run()
        {
            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2009");
            EndTime = DateTime.Parse("08/01/2017");

            // set account value
            Cash = 100000.00;

            // add instruments
            DataPath = _dataPath;
            DataSources.Add(DataSource.New("^XSP.Index"));
            DataSources.Add(DataSource.New("^XSP.Options"));

            // loop through all bars
            foreach (DateTime simTime in SimTime)
            {
                List<Instrument> optionChain = OptionChain("^XSP.Options");

                double underlyingPrice = optionChain
                    .Select(o => Instruments[o.OptionUnderlying].Close[0])
                    .FirstOrDefault();

                if (Positions.Count == 0)
                {
                    Instrument shortPut = optionChain
                        .Where(o => o.OptionIsPut
                            && (o.OptionExpiry - simTime).Days > 21
                            && (o.OptionExpiry - simTime).Days < 28
                            && o.OptionStrike < 0.9 * underlyingPrice
                            && o.Bid[0] > 0.10)
                        .OrderByDescending(o => o.Bid[0])
                        .FirstOrDefault();

                    if (shortPut != null)
                    {
                        shortPut.Trade(-1, OrderExecution.closeThisBar);
                    }
                }

#if DEBUG_PLOT || EXCEL_REPORT
                double date = simTime.Year + (simTime.Month - 1) / 12.0 + (simTime.Day - 1) / 372.0; // 12 * 31 = 372
                _plotter.SelectPlot("nav vs time", "time");
                _plotter.SetX(date);
                _plotter.Log("nav", NetAssetValue);
#endif
            }

            FitnessValue = 0.0;
        }

        public override object Report(ReportType reportType)
        {
#if true
#if DEBUG_PLOT
            _plotter.OpenWithR();
#endif
#if EXCEL_REPORT
            _plotter.OpenWithExcel(_excelPath);
#endif
            return 0.0;
#else
            return base.Report(reportType);
#endif
        }

        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            var algo = new Algorithm2();
            algo.Run();
            double fitness = (double)algo.Report(ReportType.FitnessValue);

            DateTime finishTime = DateTime.Now;
            Debug.WriteLine("Total algorithm run time = {0:F1} seconds", (finishTime - startTime).TotalSeconds);

            foreach (LogEntry entry in algo.Log)
            {
                Debug.WriteLine("{0:MM/dd/yyyy}: {1} x {2} @ {3:C2}", entry.BarOfExecution.Time, entry.OrderTicket.Quantity, entry.OrderTicket.Instrument.Symbol, entry.FillPrice);
            }

            Console.WriteLine("Press key to continue");
            Console.ReadKey();
            //System.Threading.Thread.Sleep(3000);
        }
    }
}


//==============================================================================
// end of file