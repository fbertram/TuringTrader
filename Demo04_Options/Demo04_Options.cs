//==============================================================================
// Project:     Trading Simulator
// Name:        Demo04_Options
// Description: demonstrate option trading
// History:     2018ix11, FUB, created
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
    class Demo04_Options : Algorithm
    {
        private Logger _plotter = new Logger();
        private readonly string _dataPath = Directory.GetCurrentDirectory() + @"\..\..\..\Data";
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleChart.xlsm";
        private readonly string _underlyingNickname = "^XSP.Index";
        private readonly string _optionsNickname = "^XSP.Options";
        private readonly double _regTMarginToUse = 0.8;
        private readonly double _initialCash = 100000.00;
        private double? _initialUnderlyingPrice = null;
        private Instrument _underlyingInstrument;

        public Demo04_Options()
        {

        }

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
            DataSources.Add(DataSource.New(_underlyingNickname));
            DataSources.Add(DataSource.New(_optionsNickname));

            //---------- simulation

            // loop through all bars
            foreach (DateTime simTime in SimTime)
            {
                // retrieve the option chain
                List<Instrument> optionChain = OptionChain(_optionsNickname);
                if (optionChain.Count == 0)
                    continue;

                // retrieve the underlying price
                if (_underlyingInstrument == null)
                    _underlyingInstrument = Instruments[optionChain.Select(o => o.OptionUnderlying).FirstOrDefault()];
                double underlyingPrice = _underlyingInstrument.Close[0];
                if (_initialUnderlyingPrice == null)
                    _initialUnderlyingPrice = underlyingPrice;

                if (Positions.Count == 0)
                {
                    // find option
                    Instrument shortPut = optionChain
                        .Where(o => o.OptionIsPut
                            && (o.OptionExpiry - simTime).Days > 21
                            && (o.OptionExpiry - simTime).Days < 28
                            && o.OptionStrike < 0.85 * underlyingPrice
                            && o.Bid[0] > 0.10)
                        .OrderByDescending(o => o.Bid[0])
                        .FirstOrDefault();

                    // trade option
                    if (shortPut != null)
                    {
                        // Interactive Brokers margin requirements for short naked puts:
                        // Put Price + Maximum((15 % * Underlying Price - Out of the Money Amount), 
                        //                     (10 % * Strike Price)) 
                        double margin = Math.Max(0.15 * underlyingPrice - Math.Max(0.0, underlyingPrice - shortPut.OptionStrike),
                                            0.10 * underlyingPrice);
                        int contracts = (int)Math.Floor(Math.Max(0.0, _regTMarginToUse * Cash / (100.0 * margin)));

                        shortPut.Trade(-contracts, OrderExecution.closeThisBar);
                    }
                }

                // calculate volatility
                ITimeSeries<double> vol = _underlyingInstrument.Close.Volatility(20);

                // create plot output
                _plotter.SelectPlot("nav vs time", "time"); // this will go to Sheet1
                _plotter.SetX(simTime);
                _plotter.Log(_underlyingNickname, underlyingPrice / (double)_initialUnderlyingPrice);
                _plotter.Log("vol", vol[0]);
                _plotter.Log("nav", NetAssetValue / _initialCash);
            }

            //---------- post-processing

            _plotter.SelectPlot("trades", "time"); // this will go to Sheet2
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
#if CREATE_EXCEL
            _plotter.OpenWithExcel(_excelPath);
#endif
        }

        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            var algo = new Demo04_Options();
            algo.Run();
            algo.CreateChart();

            DateTime finishTime = DateTime.Now;
            Debug.WriteLine("Total algorithm run time = {0:F1} seconds", (finishTime - startTime).TotalSeconds);
        }
    }
}


//==============================================================================
// end of file