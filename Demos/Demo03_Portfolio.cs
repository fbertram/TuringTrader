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

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Demos
{
    public class Demo03_Portfolio: Algorithm
    {
        #region internal data
        private Plotter _plotter = new Plotter();
        private readonly string _template = "SimpleChart";
        private readonly double _initialCash = 100000.00;
        private readonly List<string> _tradingInstruments = new List<string>()
        {
            "FB.Stock",
            "AAPL.Stock",
            "AMZN.Stock",
            "NFLX.Stock",
            "GOOGL.Stock"
        };
        #endregion

        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            StartTime = DateTime.Parse("01/01/2007");
            EndTime = DateTime.Parse("08/01/2018");

            // set account value
            Deposit(_initialCash);

            // add instruments
            foreach (string nickname in _tradingInstruments)
                AddDataSource(nickname);

            // clear plotters
            _plotter.Clear();

            //---------- simulation

            // loop through all bars
            foreach (DateTime simTime in SimTimes)
            {

                // this list of instruments is dynamic: the simulator engine
                // adds a new instrument whenever needed. we need to determine 
                // which of these instruments have received new bars.
                // also, we want to ignore our benchmark instrument.
                var activeInstruments = Instruments
                        .Where(i => i.Time[0] == simTime
                            && _tradingInstruments.Contains(i.Nickname));

                // this algorithm allocates an equal share of the net asset value
                // to all active instruments, and rebalances daily
                double targetEquity = NetAssetValue[0] / Math.Max(1, activeInstruments.Count());

                foreach (Instrument instr in activeInstruments)
                {
                    // determine # of shares
                    int targetShares = (int)Math.Floor(targetEquity / instr.Close[0]);
                    int currentShares = instr.Position;

                    // place trades
                    if (targetShares != currentShares)
                        instr.Trade(targetShares - currentShares);
                }

                // plot net asset value on Sheet1
                _plotter.SelectPlot("Performance vs Time", "date");
                _plotter.SetX(simTime);
                _plotter.Plot("Net Asset Value", NetAssetValue[0] / _initialCash);
            }

            //---------- post-processing

            // create a list of trades on Sheet2
            _plotter.SelectPlot("trades", "time");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Plot("action", entry.Action);
                _plotter.Plot("instr", entry.Symbol);
                _plotter.Plot("qty", entry.OrderTicket.Quantity);
                _plotter.Plot("price", entry.FillPrice);
            }
        }

        public override void Report()
        {
            _plotter.OpenWith(_template);
        }
    }
}

//==============================================================================
// end of file