//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdaterIQFeed
// Description: IQFeed/ DTN data updater
// History:     2018x02, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

// the login credentials are taken from HKEY_CURRENT_USER\Software\DTN\IQFeed\Startup,
// which is where the IQFeed launcher will store them.
// if this doesn't work, credentials may be placed in the environment as follows:
// Run rundll32 sysdm.cpl,EditEnvironmentVariables to open the Environment Variables
// In your User variables, create 4 new ones: 
// IQCONNECT_LOGIN
// IQCONNECT_PASSWORD
// IQCONNECT_PRODUCT_ID
// IQCONNECT_PRODUCT_VERSION(not mandatory, will fallback to 1.0.0.0)

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IQFeed.CSharpApiClient;
using IQFeed.CSharpApiClient.Lookup;
using IQFeed.CSharpApiClient.Lookup.Historical.Messages;
using Microsoft.Win32;
#endregion

namespace FUB_TradingSim
{
    class DataUpdaterIQFeed : DataUpdater
    {
        #region internal helpers
        private string LoginName
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DTN\IQFeed\Startup"))
                {
                    if (key != null)
                        return (string)key.GetValue("Login");
                    else
                        return null;
                }
            }
        }
        private string Password
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DTN\IQFeed\Startup"))
                {
                    if (key != null)
                        return (string)key.GetValue("Password");
                    else
                        return null;
                }
            }
        }
        #endregion

        #region public DataUpdaterIQFeed(Dictionary<DataSourceValue, string> info) : base(info)
        public DataUpdaterIQFeed(Dictionary<DataSourceValue, string> info) : base(info)
        {
        }
        #endregion

        #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
        override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
        {
            IQFeedLauncher.Start(this.LoginName, this.Password, "ONDEMAND_SERVER", "1.0");
            var lookupClient = LookupClientFactory.CreateNew();
            lookupClient.Connect();

            string symbol = Info[DataSourceValue.symbolIqfeed];

            IEnumerable<IDailyWeeklyMonthlyMessage> dailyMessages =
                lookupClient.Historical.ReqHistoryDailyTimeframeAsync(
                    symbol, startTime, endTime).Result;

            foreach (IDailyWeeklyMonthlyMessage msg in dailyMessages)
            {
                DateTime barTime = msg.Timestamp.Date + DateTime.Parse("16:00").TimeOfDay;

                Bar newBar = new Bar(
                    Info[DataSourceValue.ticker], barTime,
                    msg.Open, msg.High, msg.Low, msg.Close, msg.PeriodVolume, true,
                    0.0, 0.0, 0, 0, false,
                    default(DateTime), 0.0, false);

                if (newBar.Time >= startTime
                && newBar.Time <= endTime)
                    yield return newBar;
            }

            yield break;
        }
        #endregion

        #region public override string Name
        public override string Name
        {
            get
            {
                return "IQFeed";
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file