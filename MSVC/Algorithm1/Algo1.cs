//==============================================================================
// Project:      Trading Simulator
// File:         MyAlgo2.cs
// Description:  Sample Algorithm
// History:      2018/09/08, FUB, created
// Copyright (c) 2011-2018, Bertram Solutions LLC
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
    class Algorithm1: AlgoBase
    {
        public Algorithm1()
        {

        }

        override public void Run()
        {
            // set simulation time frame
            StartDate = DateTime.Parse("01/01/2009");
            EndDate = DateTime.Parse("08/01/2017");

            // add instruments
            DataPath = Directory.GetCurrentDirectory() + @"\..\..\..\..\Data";
            //AddInstrument("AAPL");
            //AddInstrument("TSLA");
            AddInstrument("^XSP");

            // loop through all bars
            foreach (BarCollection bars in Bars)
            {
                Debug.WriteLine("{0:MM/dd/yyyy}", SimDate);
                foreach (string symbol in bars.Symbols)
                {
                    //Debug.WriteLine("{0:MM/dd/yyyy} {1}: {2}, {3}, {4}, {5}, {6}, {7}",
                    //    SimDate, symbol, bars[symbol].TimeStamp, 
                    //    bars[symbol].Open, bars[symbol].High, bars[symbol].Low, bars[symbol].Close, 
                    //    bars[symbol].Volume);
                }
            }

            FitnessValue = 0.0;
        }

        public override object Report(ReportType reportType)
        {
            return base.Report(reportType);
        }

        static void Main(string[] args)
        {
            var algo = new Algorithm1();
            algo.Run();
            double fitness = (double)algo.Report(ReportType.FitnessValue);
        }
    }
}


//==============================================================================
// end of file