//==============================================================================
// Project:     Trading Simulator
// Name:        Demo03_Portfolio
// Description: portfolio trading demo
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    class Demo03_Portfolio: Algorithm
    {
        private Logger _plotter = new Logger();
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

        public Demo03_Portfolio()
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

                // this list of instruments is dynamic: the simulator engine
                // adds a new instrument whenever needed. we need to determine 
                // which of these instruments have received new bars
                // also, we want to ignore our benchmark instrument.
                var activeInstruments = Instruments.Values
                        .Where(i => i.LastTime == simTime
                            && _tradingInstruments.Contains(i.Nickname));

                // this algorithm allocates an equal share of the net asset value
                // to all active instruments... and rebalances daily
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

                // plot net asset value on Sheet1
                _plotter.SetX(simTime);
                _plotter.Log("Net Asset Value", NetAssetValue / _initialCash);
            }

            //---------- post-processing

            // create a list of trades on Sheet2
            _plotter.SelectPlot("trades", "time");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Log("qty", entry.OrderTicket.Quantity);
                _plotter.Log("instr", entry.OrderTicket.Instrument.Symbol);
                _plotter.Log("price", entry.FillPrice);
            }
        }

        public void CreateChart()
        {
            _plotter.OpenWithExcel(_excelPath);
        }

        public static void Main(string[] args)
        {
            var algo = new Demo03_Portfolio();
            algo.Run();
            algo.CreateChart();
        }
    }
}


//==============================================================================
// end of file