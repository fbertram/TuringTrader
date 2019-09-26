//==============================================================================
// Project:     TuringTrader
// Name:        AboutBox
// Description: about box code-behind
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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

#region Libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
#endregion

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        private string _gitVersion => GitInfo.Version
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace(" ", string.Empty);

        public AboutBox()
        {
            InitializeComponent();

            string version = 
            Version.Text = _gitVersion;
        }

        private void AboutBox_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender,
                                               System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            MainWindow.OpenWithShell(e.Uri.AbsoluteUri);
        }

        private void Hyperlink_RequestNavigateGit(object sender,
                                               System.Windows.Navigation.RequestNavigateEventArgs e)
        {
#if true
            // https://github.com/
            int hashIndex = _gitVersion.LastIndexOf("-");

            string commitUrl = (hashIndex < 0)
                ? "https://github.com/fbertram/TuringTrader/releases/tag/" + _gitVersion + "/"
                // https://github.com/fbertram/TuringTrader/tree/1ac5737
                : "https://github.com/fbertram/TuringTrader/tree/" + _gitVersion.Substring(hashIndex + 2) + "/";
            //  // https://github.com/fbertram/TuringTrader/commit/1ac5737/
            //  //: "https://github.com/fbertram/TuringTrader/commit/" + _gitVersion.Substring(hashIndex + 2) + "/";
#else
            // https://bitbucket.org/
            // https://bitbucket.org/fbertram/fub_tradingsimulator/commits/all
            // https://bitbucket.org/fbertram/fub_tradingsimulator/commits/f1f13fb

            string gitCommit = _gitVersion.Substring(_gitVersion.LastIndexOf("-") + 2);
            string commitUrl = e.Uri.AbsoluteUri + gitCommit;
#endif

            MainWindow.OpenWithShell(commitUrl);
        }
    }
}

//==============================================================================
// end of file