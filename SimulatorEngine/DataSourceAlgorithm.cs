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
                var algoName = Info[DataSourceValue.dataSource]
                    .Split(' ')
                    .Last();

                var cacheKey = new CacheId(null, "", 0,
                    algoName.GetHashCode(),
                    startTime.GetHashCode(),
                    endTime.GetHashCode());

                List<Bar> retrievalFunction()
                {
                    try
                    {
                        DateTime t1 = DateTime.Now;
                        Output.WriteLine(string.Format("DataSourceAlgorithm: generating data for {0}...", Info[DataSourceValue.nickName]));

                        var algo = (SubclassableAlgorithm)AlgorithmLoader.InstantiateAlgorithm(algoName);

                        algo.SubclassedStartTime = startTime;
                        algo.SubclassedEndTime = endTime;
                        algo.ParentDataSource = this;

                        algo.SubclassedData = new List<Bar>(); ;

                        algo.Run();

                        DateTime t2 = DateTime.Now;
                        Output.WriteLine(string.Format("DataSourceAlgorithm: finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                        return algo.SubclassedData;
                    }

                    catch
                    {
                        throw new Exception("DataSourceAlgorithm: failed to run sub-classed algorithm " + algoName);
                    }
                }

                List<Bar> data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction);

                if (data.Count == 0)
                    throw new Exception(string.Format("DataSourceNorgate: no data for {0}", Info[DataSourceValue.nickName]));

                Data = data;
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file