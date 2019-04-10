//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceAlgorithm
// Description: Data source, derived from an algorithm.
// History:     2019iii13, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
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

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceAlgorithm : DataSource
        {
            //---------- API
            #region public DataSourceAlgorithm(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for algorithm quotes.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceAlgorithm(Dictionary<DataSourceValue, string> info) : base(info)
            {
            }
            #endregion
            #region override public void LoadData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Load data into memory.
            /// </summary>
            /// <param name="startTime">start of load range</param>
            /// <param name="endTime">end of load range</param>
            override public void LoadData(DateTime startTime, DateTime endTime)
            {
                DateTime t1 = DateTime.Now;
                Output.WriteLine(string.Format("DataSourceAlgorithm: generating data for {0}...", Info[DataSourceValue.nickName]));

                var algoName = Info[DataSourceValue.dataSource]
                    .Split(' ')
                    .Last();

                try
                {
                    var algo = (SubclassableAlgorithm)AlgorithmLoader.InstantiateAlgorithm(algoName);

                    // instantiating a new algorithm here will overwrite
                    // the most-recent algorithm. need to reset here.
                    GlobalSettings.MostRecentAlgorithm = Simulator.Name;

                    algo.SubclassedStartTime = startTime;
                    algo.SubclassedEndTime = endTime;
                    algo.ParentDataSource = this;

                    List<Bar> data = new List<Bar>();
                    Data = data;
                    algo.SubclassedData = data;

                    algo.Run();
                }

                catch
                {
                    throw new Exception("DataSourceAlgorithm: failed to run sub-classed algorithm " + algoName);
                }

                DateTime t2 = DateTime.Now;
                Output.WriteLine(string.Format("DataSourceAlgorithm: finished after {0:F1} seconds", (t2 - t1).TotalSeconds));
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file