//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo07_Subclassing
// Description: demonstrate subclassable algorithms
// History:     2019v21, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace Demos
{
    #region Demo07_Subclassing_Child
    // note how this class is not declared public
    // because of this, the class will not show up in TuringTrader's
    // algorithm selector, but it can still be instantiated
    class Demo07_Subclassing_Child : SubclassableAlgorithm
    {
        private static readonly string SPX = "$SPX";
        private Plotter _plotter = new Plotter();

        public override string Name => "Demo 07 child algorithm";

        public override void Run()
        {
            // SubclassedStartTime and SubclassedEndTime are passed in
            // from the parent data-source, in case we are sub-classed
            // if running stand-alone, we are free to set any values
            StartTime = SubclassedStartTime ?? DateTime.Parse("01/01/2008");
            EndTime = SubclassedEndTime ?? DateTime.Now.Date - TimeSpan.FromDays(5);

            AddDataSource(SPX);
            Instrument spx = null;

            foreach (var s in SimTimes)
            {
                spx = spx ?? FindInstrument(SPX);

                if (IsSubclassed)
                {
                    // this is where the sub-classed algorithm
                    // adds a new bar to the parent data-source
                    AddSubclassedBar(spx.Close[0] / 10.0);
                }
                else
                {
                    // this is only relevant, if we are running
                    // stand-alone
                    _plotter.SelectChart(Name, "date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(spx.Name, spx.Close[0]);
                }
            }
        }

        public override void Report()
        {
            // this is only called when the algorithm
            // is run stand-alone, never when sub-classed
            _plotter.OpenWith("SimpleChart");
        }
    }
    #endregion

    #region Demo07_Subclassing
    public class Demo07_Subclassing : Algorithm
    {
        private static readonly string DATA = "algo:Demo07_Subclassing_Child";
        private Plotter _plotter = new Plotter();
        public override void Run()
        {
            StartTime = DateTime.Parse("01/01/2008");
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            // our sub-classed data source is used exactly
            // the same way as any other data source
            AddDataSource(DATA);
            Instrument data = null;

            foreach (var s in SimTimes)
            {
                data = data ?? FindInstrument(DATA);

                _plotter.SelectChart(Name, "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot(data.Name, data.Close[0]);
            }
        }

        public override void Report()
        {
            _plotter.OpenWith("SimpleChart");
        }
    }
    #endregion
}

//==============================================================================
// end of file