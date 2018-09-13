//==============================================================================
// Project:     Trading Simulator
// Name:        Algorithm1
// Description: Sample Algorithm #1
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#define CREATE_EXCEL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    class Algorithm1: Algorithm
    {
        public Logger Plotter = new Logger();
        private readonly string _dataPath = Directory.GetCurrentDirectory() + @"\..\..\..\Data";
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleChart.xlsm";
        private readonly double _initialCash = 100000.00;
        private readonly string _benchmarkInstrument = "^GSPC.Index";
        private readonly List<string> _tradingInstruments = new List<string>()
        {
            "FB.Stock",
            "AAPL.Stock",
            "AMZN.Stock",
            "NFLX.Stock",
            "GOOGL.Stock"
        };
        private Dictionary<string, double> _initialValues = new Dictionary<string, double>();

        public Algorithm1()
        { }

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2007");
            EndTime = DateTime.Parse("08/01/2018");

            // set account value
            Cash = _initialCash;

            // add instruments
            DataPath = _dataPath;
            DataSources.Add(DataSource.New(_benchmarkInstrument));
            foreach (string nickname in _tradingInstruments)
                DataSources.Add(DataSource.New(nickname));

            //---------- simulation

            // loop through all bars
            foreach (DateTime simTime in SimTime)
            {
                // keep initial values
                foreach (Instrument instr in Instruments.Values)
                    if (!_initialValues.ContainsKey(instr.Nickname))
                        _initialValues[instr.Nickname] = instr.Open[0];

                // find active instruments
                var activeInstruments = Instruments.Values
                        .Where(i => i.LastTime == simTime
                            && _tradingInstruments.Contains(i.Nickname));
                double targetEquity = NetAssetValue / Math.Max(1, activeInstruments.Count());

                foreach (Instrument instr in activeInstruments)
                {
                    // determine # of shares
                    int targetShares = (int)Math.Floor(targetEquity / instr.Close[0]);
                    int currentShares = Positions.ContainsKey(instr)
                        ? Positions[instr]
                        : 0;

                    // place trades
                    if (targetShares != currentShares)
                        instr.Trade(targetShares - currentShares);
                }

                // create plot output
                Plotter.SetX(simTime);
                Plotter.Log(_benchmarkInstrument,
                            FindInstruments(_benchmarkInstrument).FirstOrDefault().Close[0] 
                            / _initialValues[_benchmarkInstrument]);
                Plotter.Log("Net Asset Value", NetAssetValue / _initialCash);

                foreach (string nickname in _tradingInstruments)
                {
                    Instrument instr = FindInstruments(nickname).FirstOrDefault();
                    double y = instr != null
                        ? instr.Close[0] / _initialValues[nickname]
                        : 1.0;

                    Plotter.Log(nickname, y);
                }
            }

            //---------- post-processing

            Plotter.SelectPlot("trades", "time");
            foreach (LogEntry entry in Log)
            {
                Plotter.SetX(entry.BarOfExecution.Time);
                Plotter.Log("qty", entry.OrderTicket.Quantity);
                Plotter.Log("instr", entry.OrderTicket.Instrument.Symbol);
                Plotter.Log("price", entry.FillPrice);
            }
        }

        public void CreateChart()
        {
#if CREATE_EXCEL
            Plotter.OpenWithExcel(_excelPath);
#endif
        }

        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            var algo = new Algorithm1();
            algo.Run();
            algo.CreateChart();

            DateTime finishTime = DateTime.Now;
            Debug.WriteLine("Total algorithm run time = {0:F1} seconds", (finishTime - startTime).TotalSeconds);
        }
    }
}


//==============================================================================
// end of file