//==============================================================================
// Project:     TuringTrader
// Name:        AboutBox
// Description: about box code-behind
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
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
            // https://bitbucket.org/fbertram/fub_tradingsimulator/commits/all
            // https://bitbucket.org/fbertram/fub_tradingsimulator/commits/f1f13fb

            string gitCommit = _gitVersion.Substring(_gitVersion.LastIndexOf("-") + 2);
            string commitUrl = e.Uri.AbsoluteUri + gitCommit;

            MainWindow.OpenWithShell(commitUrl);
        }
    }
}

//==============================================================================
// end of file