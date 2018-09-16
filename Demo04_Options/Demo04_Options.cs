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

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    class Demo04_Options : Algorithm
    {
        #region internal data
        private Logger _plotter = new Logger();
        private readonly string _dataPath = Directory.GetCurrentDirectory() + @"\..\..\..\Data";
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleChart.xlsm";
        private readonly string _underlyingNickname = "^XSP.Index";
        private readonly string _optionsNickname = "^XSP.Options";
        private readonly double _regTMarginToUse = 0.8;
        private readonly double _initialCash = 100000.00;
        private double? _initialUnderlyingPrice = null;
        private Instrument _underlyingInstrument;
        #endregion

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2007");
            EndTime = DateTime.Parse("08/01/2018");

            // set account value
            Cash = _initialCash;

            // add instruments
            // the underlying must be added explicitly,
            // as the simulation engine requires it
            DataPath = _dataPath;
            DataSources.Add(DataSource.New(_underlyingNickname));
            DataSources.Add(DataSource.New(_optionsNickname));

            //---------- simulation

            // loop through all bars
            foreach (DateTime simTime in SimTime)
            {
                // find the underlying instrument
                // we could also find the underlying from the option chain
                if (_underlyingInstrument == null)
                    _underlyingInstrument = FindInstruments(_underlyingNickname).First();

                // retrieve the underlying spot price
                double underlyingPrice = _underlyingInstrument.Close[0];
                if (_initialUnderlyingPrice == null)
                    _initialUnderlyingPrice = underlyingPrice;

                // calculate volatility
                ITimeSeries<double> volatilitySeries = _underlyingInstrument.Close.Volatility(10);
                double averageVolatility = volatilitySeries.EMA(21)[0];
                //double volatility = volatilitySeries.Highest(1)[0];
                double volatility = Math.Max(averageVolatility, volatilitySeries.Highest(5)[0]);

                // retrieve the option chain
                // we can filter the chain to narrow down our search
                List<Instrument> optionChain = OptionChain(_optionsNickname)
                        .Where(o => o.OptionIsPut
                            && (o.OptionExpiry - simTime).Days > 21
                            && (o.OptionExpiry - simTime).Days < 28
                            && (o.OptionExpiry.Date.DayOfWeek == DayOfWeek.Friday
                            || o.OptionExpiry.Date.DayOfWeek == DayOfWeek.Saturday))
                        .ToList();

                // if we are currently flat, attempt to open a position
                if (Positions.Count == 0)
                {
                    // determine strike price: far away from spot price
                    double strikePrice = _underlyingInstrument.Close[0]
                        / Math.Exp(3.0 * Math.Sqrt(28.0 / 365.25) * volatility);

                    // find contract closest to our desired strike
                    Instrument shortPut = optionChain
                        .OrderBy(o => Math.Abs(o.OptionStrike - strikePrice))
                        .FirstOrDefault();

                    // enter short put position
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

                // monitor and maintain existing positions
                else // if (Postions.Count != 0)
                {
                    // find our currently open position
                    // we might need fancier code, in case we have more than
                    // one position open
                    Instrument shortPut = Positions.Keys.First();

                    // re-evaluate the likely trading range
                    double expectedLowestPrice = _underlyingInstrument.Close[0]
                        / Math.Exp(1.5 * Math.Sqrt((shortPut.OptionExpiry - simTime).Days / 365.25) * volatility);

                    // exit, when the risk of ending in the money is too high
                    // and, the contract is actively traded
                    if (expectedLowestPrice < shortPut.OptionStrike
                    &&  shortPut.BidVolume[0] > 0
                    &&  shortPut.Ask[0] < 2 * shortPut.Bid[0])
                    {
                        shortPut.Trade(-Positions[shortPut], OrderExecution.closeThisBar);
                    }
                }

                // plot the underlying against our strategy results, plus volatility
                _plotter.SelectPlot("nav vs time", "time"); // this will go to Sheet1
                _plotter.SetX(simTime);
                _plotter.Log(_underlyingInstrument.Symbol, underlyingPrice / (double)_initialUnderlyingPrice);
                _plotter.Log("volatility", volatilitySeries[0]);
                _plotter.Log("net asset value", NetAssetValue / _initialCash);
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

        #region miscellaneous stuff
        public Demo04_Options()
        {

        }

        public void CreateChart()
        {
            _plotter.OpenWithExcel(_excelPath);
        }

        static void Main(string[] args)
        {
            var algo = new Demo04_Options();
            algo.Run();
            algo.CreateChart();
        }
        #endregion
    }
}


//==============================================================================
// end of file