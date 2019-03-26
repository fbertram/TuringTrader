//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceNorgate
// Description: Data source for Norgate Data.
//              Tested w/ Norgate Data API 4.1.5.27.
// History:     2019i06, FUB, created
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using NDU = NorgateData.DataAccess;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceNorgate : DataSource
        {
            #region internal data
            private List<Bar> _data;
            private IEnumerator<Bar> _barEnumerator;
            private static bool _handleUnresolvedAssemblies = true;
            private static DateTime _lastNDURun = default(DateTime);
            #endregion
            #region internal helpers
            private Bar CreateBar(NDU.RecOHLC norgate, double priceMultiplier)
            {
                DateTime barTime = norgate.Date.Date
                    + DateTime.Parse(Info[DataSourceValue.time]).TimeOfDay;

                return new Bar(
                                Info[DataSourceValue.ticker], barTime,
                                (double)norgate.Open * priceMultiplier, 
                                (double)norgate.High * priceMultiplier, 
                                (double)norgate.Low * priceMultiplier, 
                                (double)norgate.Close * priceMultiplier, 
                                (long)norgate.Volume, true,
                                0.0, 0.0, 0, 0, false,
                                default(DateTime), 0.0, false);
            }
            private void LoadData(List<Bar> data, DateTime startTime, DateTime endTime)
            {
                if (!isAPIAvaliable)
                    throw new Exception("DataSourceNorgate: Norgata Data Updater not installed");

                //--- Norgate setup
                NDU.Api.SetAdjustmentType = GlobalSettings.AdjustForDividends
                    ? NDU.AdjustmentType.TotalReturn
                    : NDU.AdjustmentType.CapitalSpecial;
                NDU.Api.SetPaddingType = NDU.PaddingType.AllMarketDays;

                //--- run NDU as required
            #if false
                // this should work, but seems broken as of 01/09/2019
                DateTime dbTimeStamp = NDU.Api.LastDatabaseUpdateTime;
            #else
                List<NDU.RecOHLC> q = new List<NDU.RecOHLC>();
                NDU.Api.GetData("$SPX", out q, DateTime.Now - TimeSpan.FromDays(5), DateTime.Now + TimeSpan.FromDays(5));
                DateTime dbTimeStamp = q
                    .Select(ohlc => ohlc.Date)
                    .OrderByDescending(d => d)
                    .First();
            #endif

                if (endTime > dbTimeStamp)
                    RunNDU();

                //--- get data from Norgate
                List<NDU.RecOHLC> norgateData = new List<NDU.RecOHLC>();
                NDU.OperationResult result = NDU.Api.GetData(Info[DataSourceValue.symbolNorgate], out norgateData, startTime, endTime);

                //--- copy to TuringTrader bars
                double priceMultiplier = Info.ContainsKey(DataSourceValue.dataUpdaterPriceMultiplier)
                    ? Convert.ToDouble(Info[DataSourceValue.dataUpdaterPriceMultiplier])
                    : 1.0;

                foreach (var ohlc in norgateData)
                    data.Add(CreateBar(ohlc, priceMultiplier));
            }

            private static void RunNDU()
            {
                if (DateTime.Now - _lastNDURun > TimeSpan.FromMinutes(5))
                {
                    _lastNDURun = DateTime.Now;

                    string nduBinPath;
                    getNDUBinPath(out nduBinPath);
                    string nduTrigger = Path.Combine(nduBinPath, "NDU.Trigger.exe");

                    Process ndu = Process.Start(nduTrigger, "UPDATE CLOSE WAIT");
                    ndu.WaitForExit();
                }
            }
            private static void HandleUnresovledAssemblies()
            {
                if (_handleUnresolvedAssemblies)
                {
                    _handleUnresolvedAssemblies = false;

                    AppDomain currentDomain = AppDomain.CurrentDomain;
                    currentDomain.AssemblyResolve += currentDomain_AssemblyResolve;
                }
            }
            private static Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            {
                string actualassemblyname = new AssemblyName(args.Name).Name;

                if (actualassemblyname == "norgate.data.dotnet" && isAPIAvaliable)
                {
                    Assembly assembly = Assembly.LoadFrom(actualAPILocation);

                    return assembly;
                }

                return null;  // this is bad, means the API could not be loaded and will probably throw a program wide exception 
            }

            private static bool isNDUInstalled
            {
                get { return checkForNDUInstallKey(); }
            }
            private static string actualAPILocation
            {
                get
                {
                    string result = "";
                    if (isNDUInstalled)
                    {
                        // get the registry information for NDU BIN path
                        string nduBinPath = "";
                        if (getNDUBinPath(out nduBinPath))
                        {
                            if (File.Exists(nduBinPath + "norgate.data.dotnet.dll"))
                            {
                                result = nduBinPath + "norgate.data.dotnet.dll";
                            }
                        }
                    }
                    return result;
                }
            }
            private static bool isAPIAvaliable
            {
                get
                {
                    if (File.Exists(actualAPILocation))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            private static bool getNDUBinPath(out string binPath)
            {
                bool result = false;
                binPath = "";
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NDU\\norgate");
                    if (key != null)
                    {
                        Object o = key.GetValue("basePath");
                        if (o != null)
                        {
                            string work = (string)o;
                            work += "bin\\";
                            if (Directory.Exists(work))
                            {
                                binPath = work;
                                result = true;
                            }
                        }
                        else
                        {
                            result = false;
                        }
                    }
                }
                catch
                {
                    result = false;
                }
                return result;
            }
            private static bool checkForNDUInstallKey()
            {
                bool result = false;
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NDU\\norgate");
                    if (key != null)
                    {
                        Object o = key.GetValue("nduInstalled");
                        if (o != null)
                        {
                            int isInstalled = (int)o;
                            if (isInstalled != 1)
                            {
                                // not installed
                                result = false;
                            }
                            else
                            {
                                // is installed (at least registry believes so)
                                result = true;
                            }
                        }
                    }
                }
                catch
                {
                    result = false;
                }
                return result;
            }
            #endregion

            //---------- API
            #region public DataSourceNorgate(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for Norgate Data.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceNorgate(Dictionary<DataSourceValue, string> info) : base(info)
            {
                // make sure Norgate api is properly loaded
                HandleUnresovledAssemblies();
            }
            #endregion
            #region override public IEnumerator<Bar> BarEnumerator
            /// <summary>
            /// Retrieve enumerator for this data source's bars.
            /// </summary>
            override public IEnumerator<Bar> BarEnumerator
            {
                get
                {
                    if (_barEnumerator == null)
                        _barEnumerator = _data.GetEnumerator();
                    return _barEnumerator;
                }
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
                var cacheKey = new CacheId(null, "", 0,
                    Info[DataSourceValue.nickName].GetHashCode(),
                    startTime.GetHashCode(),
                    endTime.GetHashCode());

                List<Bar> retrievalFunction()
                {
                    DateTime t1 = DateTime.Now;
                    Output.Write(string.Format("DataSourceNorgate: loading data for {0}...", Info[DataSourceValue.nickName]));

                    List<Bar> data = new List<Bar>();

                    LoadData(data, startTime, endTime);

                    DateTime t2 = DateTime.Now;
                    Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                    return data;
                };

                _data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction);

                if (_data.Count == 0)
                    throw new Exception(string.Format("DataSourceNorgate: no data for {0}", Info[DataSourceValue.nickName]));
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file