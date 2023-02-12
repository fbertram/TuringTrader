//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Comparison
// Description: Comparison of various strategies
// History:     2019xi29, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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

using System.Collections.Generic;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Simulator;

namespace TuringTrader.BooksAndPubs
{
    public class Comparison : Algorithm
    {
        private readonly List<string> ALGORITHMS = new List<string>
        {
            "algorithm:Antonacci_ParityPortfolioWithAbsoluteMomentum",
            "algorithm:Keller_CAA_N8_TV5",
            "algorithm:Livingston_MuscularPortfolios_MamaBear",
            Indices.PORTF_60_40,
        };

        private Plotter _plotter = null;

        public Comparison()
        {
            _plotter = new Plotter(this);
        }

        public override void Run()
        {
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            var algorithms = AddDataSources(ALGORITHMS);

            foreach (var simTime in SimTimes)
            {
                if (!HasInstruments(algorithms))
                    continue;

                _plotter.SelectChart("Strategy Comparison", "Date");
                _plotter.SetX(SimTime[0]);

                foreach (var a in algorithms)
                    _plotter.Plot(a.Instrument.Name, a.Instrument.Close[0]);
            }
        }

        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
    }
}

//==============================================================================
// end of file