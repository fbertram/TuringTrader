//==============================================================================
// Project:     Trading Simulator
// Name:        AfterTaxSimulation
// Description: After-tax simulation.
// History:     2019ii04, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    public class AfterTaxSimulation
    {
        public static void Run(Algorithm algo)
        {
            _afterTaxSimulator afterTaxSim = new _afterTaxSimulator(algo);
            afterTaxSim.Run();
        }

        private class _afterTaxSimulator : SimulatorCore
        {
            private Algorithm _algo;

            public _afterTaxSimulator(Algorithm algo)
            {
                _algo = algo;
            }

            override public void Run()
            {
                //----- copy setup from our parent simulation
                WarmupStartTime = _algo.WarmupStartTime;
                StartTime = _algo.StartTime;
                EndTime = _algo.EndTime;

                CommissionPerShare = _algo.CommissionPerShare;

                foreach (DataSource dataSource in _algo.DataSources)
                    AddDataSource(dataSource);

                //----- get access to parent trade log
                List<LogEntry>.Enumerator logEnum = _algo.Log.GetEnumerator();
                if (!logEnum.MoveNext())
                    return;

                //----- run after-tax simulation
                foreach (DateTime simTime in SimTimes)
                {
                    if (logEnum.Current.BarOfExecution.Time == SimTime[0])
                    {
                        LogEntry transaction = logEnum.Current;

                        logEnum.MoveNext();
                    }
                }
            }
        }
    }
}

//==============================================================================
// end of file