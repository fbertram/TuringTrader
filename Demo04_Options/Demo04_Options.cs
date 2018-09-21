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

//#define BACKTEST
// with BACKTEST defined, this will run a backtest
// otherwise, we will run an optimization

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
        private readonly string _excelChartTemplate = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleChart.xlsm";
        private readonly string _excelTableTemplate = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleTable.xlsm";
        private readonly string _underlyingNickname = "^XSP.Index";
        private readonly string _optionsNickname = "^XSP.Options";
        private readonly double _regTMarginToUse = 0.8;
        private readonly double _initialCash = 100000.00;
        private double? _initialUnderlyingPrice = null;
        private Instrument _underlyingInstrument;
        #endregion

        #region override public void Run()
        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            WarmupStartTime = DateTime.Parse("01/01/2007");
            StartTime = DateTime.Parse("01/01/2008");
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
            foreach (DateTime simTime in SimTimes)
            {
                // find the underlying instrument
                // we could also find the underlying from the option chain
                if (_underlyingInstrument == null)
                    _underlyingInstrument = FindInstrument(_underlyingNickname);

                // retrieve the underlying spot price
                double underlyingPrice = _underlyingInstrument.Close[0];
                if (_initialUnderlyingPrice == null)
                    _initialUnderlyingPrice = underlyingPrice;

                // calculate volatility
                ITimeSeries<double> volatilitySeries = _underlyingInstrument.Close.Volatility(10);
                double averageVolatility = volatilitySeries.EMA(21)[0];
                //double volatility = volatilitySeries.Highest(1)[0];
                double volatility = Math.Max(averageVolatility, volatilitySeries.Highest(5)[0]);

                // find all expiry dates on the 3rd Friday of the month
                List<DateTime> expiryDates = OptionChain(_optionsNickname)
                    .Where(o => o.OptionExpiry.DayOfWeek == DayOfWeek.Friday && o.OptionExpiry.Day >= 15 && o.OptionExpiry.Day <= 21
                            || o.OptionExpiry.DayOfWeek == DayOfWeek.Saturday && o.OptionExpiry.Day >= 16 && o.OptionExpiry.Day <= 22)
                    .Select(o => o.OptionExpiry)
                    .Distinct()
                    .ToList();

                // select expiry 3 to 4 weeks out
                DateTime expiryDate = expiryDates
                        .Where(d => (d - simTime).TotalDays >= 21
                            && (d - simTime).TotalDays <= 28)
                        .FirstOrDefault();

                // retrieve option chain for this expiry
                List<Instrument> optionChain = OptionChain(_optionsNickname)
                        .Where(o => o.OptionIsPut
                            && o.OptionExpiry == expiryDate)
                        .ToList();

                // if we are currently flat, attempt to open a position
                if (Positions.Count == 0)
                {
                    // determine strike price: far away from spot price
                    double strikePrice = _underlyingInstrument.Close[0]
                        / Math.Exp(ENTRY_STDEV/100.0 * Math.Sqrt((expiryDate - simTime).TotalDays / 365.25) * volatility);

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
                        / Math.Exp(EXIT_STDEV/100.0 * Math.Sqrt((shortPut.OptionExpiry - simTime).Days / 365.25) * volatility);

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
                _plotter.Log("net asset value", NetAssetValue[0] / _initialCash);
            }

            //---------- post-processing

            _plotter.SelectPlot("trades", "time"); // this will go to Sheet2
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Log("qty", entry.OrderTicket.Quantity);
                _plotter.Log("instr", entry.Symbol);
                _plotter.Log("price", entry.FillPrice);
            }

            FitnessValue = NetAssetValue[0];
        }
        #endregion
        #region public void OptimizeEntryExit()
        [OptimizerParam(200, 300, 25)]
        //[OptimizerParam(200, 200, 25)]
        public int ENTRY_STDEV = 200;

        [OptimizerParam(50, 150, 25)]
        //[OptimizerParam(75, 75, 25)]
        public int EXIT_STDEV = 75;

        public void OptimizeEntryExit()
        {
            OptimizerExhaustive optimizer = new OptimizerExhaustive(this);
            optimizer.Run();

            // display a result table in Excel
            optimizer.ResultsToExcel(_excelTableTemplate);

            // walk through the results
            OptimizerResult bestResult = optimizer.Results
                    .OrderByDescending(r => r.Fitness)
                    .First();

            // re-run any the best result for a detailed report
            Demo04_Options algo = (Demo04_Options)optimizer.ReRun(bestResult);
            algo.CreateChart();
        }
        #endregion

        #region miscellaneous stuff
        public void CreateChart()
        {
            _plotter.OpenWithExcel(_excelChartTemplate);
        }

        static void Main(string[] args)
        {
            var algo = new Demo04_Options();

#if BACKTEST
            algo.Run();
            algo.CreateChart();
#else
            algo.OptimizeEntryExit();
#endif
        }
        #endregion
    }
}


//==============================================================================
// end of file