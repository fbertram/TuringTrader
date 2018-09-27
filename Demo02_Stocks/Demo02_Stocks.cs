//==============================================================================
// Project:     Trading Simulator
// Name:        Demo02_Stocks
// Description: demonstrate simple stock trading
// History:     2018ix15, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    class Demo02_Stocks : Algorithm
    {
        #region internal data
        private Logger _plotter = new Logger();
        private readonly string _dataPath = Directory.GetCurrentDirectory() + @"\..\..\..\Data";
        private readonly string _excelPath = Directory.GetCurrentDirectory() + @"\..\..\..\Excel\SimpleChart.xlsm";
        private readonly double _initialCash = 100000.00;
        private double? _initialPrice = null;
        private readonly string _instrumentNick = "AAPL.Stock";
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
            DataPath = _dataPath;
            DataSources.Add(DataSource.New(_instrumentNick));

            //---------- simulation

            foreach (DateTime simTime in SimTimes)
            {
                // find our instrument. if we have only one instrument, 
                // we can also just use Instrument[0]
                Instrument instrument = FindInstrument(_instrumentNick);

                // calculate moving averages
                ITimeSeries<double> slow = instrument.Close.EMA(63);
                ITimeSeries<double> fast = instrument.Close.EMA(21);

                // determine current and target position size,
                // based on a simple moving average crossover strategy
                int currentPosition = Positions.ContainsKey(instrument)
                    ? Positions[instrument]
                    : 0;
                int targetPosition = fast[0] > slow[0]
                    ? (int)Math.Floor(NetAssetValue[0] / instrument.Close[0]) // go long
                    : 0;                                                   // short... disabled for this demo

                // place trades
                if (targetPosition != currentPosition)
                    instrument.Trade(targetPosition - currentPosition, OrderType.openNextBar);

                // plot net asset value versus benchmark
                if (_initialPrice == null) _initialPrice = instrument.Close[0];

                _plotter.SelectPlot("nav & benchmark vs time", "date");
                _plotter.SetX(simTime);
                _plotter.Log(instrument.Symbol, instrument.Close[0] / (double)_initialPrice);
                _plotter.Log("MA Crossover", NetAssetValue[0] / _initialCash);
            }
        }

        #region miscellanous stuff
        public Demo02_Stocks()
        { }

        public void CreateChart()
        {
            _plotter.OpenWithExcel(_excelPath);
        }

        static void Main(string[] args)
        {
            var algo = new Demo02_Stocks();
            algo.Run();
            algo.CreateChart();
        }
        #endregion
    }
}

//==============================================================================
// end of file