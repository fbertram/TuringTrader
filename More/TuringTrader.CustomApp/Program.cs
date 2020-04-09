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

using System;
using TuringTrader.Simulator;
using TuringTrader;
using System.IO;

namespace TuringTrader.CustomApp
{
    class Program
    {
        private static string ALGO = "Demo01_Indicators";

        /// <summary>
        /// Renderer for TuringTrader's Plotter object
        /// </summary>
        /// <param name="plotter">plotter object</param>
        /// <param name="pathToCSharpTemplate">template to use</param>
        private static void Renderer(Plotter plotter, string pathToTemplate)
        {
            foreach(var chart in plotter.AllData.Keys)
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

        /// <summary>
        /// Event-handler for TuringTrader's Output object
        /// </summary>
        /// <param name="message">message to be printed</param>
        private static void Writer(string message)
        {
            Console.Write("Output: {0}", message);
        }
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

            // optional: disable loading of algorithms from external DLLs
            // this way, only algorithms compiled into the entry-assembly
            // are available
            GlobalSettings.LoadAlgoDlls = false;

            // scan available algorithms
            var list = AlgorithmLoader.GetAllAlgorithms(false);

            // instantiate algorithm
            var algo = AlgorithmLoader.InstantiateAlgorithm(ALGO);

            // run backtest
            algo.Run();

            // create report
            // note that the report template needs to exist, even though
            // the renderer for our example most likely won't use it
            algo.Report();
        }
    }
}

//==============================================================================
// end of file
