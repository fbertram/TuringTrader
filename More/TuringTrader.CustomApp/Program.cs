//==============================================================================
// Project:     TuringTrader, custom application
// Name:        Program.cs
// Description: Custom application based on TuringTrader
// History:     2020iv08, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2020, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

#region libraries
using System;
using TuringTrader.Simulator;
using TuringTrader;
using System.IO;
using System.Globalization;
#endregion

namespace TuringTrader.CustomApp
{
    #region globals
    static class Globals
    {
        public static string Render = "Render Chart";
        public static string Ignore = "Ignore Chart";
        public static string Asset = "MSFT";
    }
    #endregion
    #region algorithm
    class MyAlgo : Algorithm
    {
        private Plotter _plotter = new Plotter();
        public override void Run()
        {
            StartTime = DateTime.Parse("01/01/2020", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("01/31/2020", CultureInfo.InvariantCulture);

            var a = AddDataSource(Globals.Asset);

            foreach (var simTime in SimTimes)
            {
                // this chart will be rendered by our renderer.
                // we recognize it by its name
                _plotter.SelectChart(Globals.Render, "Date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot(Globals.Asset, a.Instrument.Close[0]);
            }

            // this chart will be ignored by our renderer
            _plotter.SelectChart(Globals.Ignore, "Stuff");
            _plotter.SetX("Row 1");
            _plotter.Plot("Column 1", "some data");
            _plotter.SetX("Row 2");
            _plotter.Plot("Column 1", "more data");
        }

        public override void Report()
        {
            // note that the report template must exist, even though
            // our custom renderer most likely won't use it
            _plotter.OpenWith("SimpleChart");
        }
    }
    #endregion
    #region application
    class Program
    {
        #region renderer
        /// <summary>
        /// Renderer for TuringTrader's Plotter object
        /// </summary>
        /// <param name="plotter">plotter object</param>
        /// <param name="pathToCSharpTemplate">template to use</param>
        private static void Renderer(Plotter plotter, string pathToTemplate)
        {
            foreach(var chart in plotter.AllData.Keys)
            {
                if (chart == Globals.Render)
                {
                    Console.WriteLine("=== Chart = {0}, Template = {1} ===", chart, pathToTemplate);

                    foreach (var row in plotter.AllData[chart])
                    {
                        foreach (var col in row.Keys)
                            Console.Write("{0} = {1}, ", col, row[col]);
                        Console.WriteLine();
                    }
                }
            }
        }
        #endregion
        #region log output
        /// <summary>
        /// Event-handler for TuringTrader's Output object
        /// </summary>
        /// <param name="message">message to be printed</param>
        private static void Writer(string message)
        {
            Console.Write("Output: {0}", message);
        }
        #endregion

        #region application code
        static void Main(string[] args)
        {
            // add an event handler to TuringTrader's Output object,
            // so that we can see messages printed by the simulator engine
            // or our algorithm
            Output.WriteEvent += Writer;

            // add a renderer to TuringTrader's Plotter object,
            // so that we can create reports
            Plotter.Renderer += Renderer;

            // set the home path, because every entry-assembly has its own
            // registry entry for it. In this example, we set it to the
            // default folder for the TuringTrader application
            GlobalSettings.HomePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TuringTrader");

            // set the default data feed, to have a proper fallback when
            // there are no data source descriptor files present
            GlobalSettings.DefaultDataFeed = "yahoo";

            // instantiate algorithm
            // more advanced applications might use AlgorithmLoader here
            var algo = new MyAlgo();

            // run backtest
            algo.Run();

            // create report
            algo.Report();
        }
        #endregion
    }
    #endregion
}

//==============================================================================
// end of file
