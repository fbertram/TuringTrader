//==============================================================================
// Project:     TuringTrader
// Name:        Settings
// Description: settings dialog code-behind
// History:     2019v13, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using Avalon.Windows.Dialogs;
using System.Windows;
using TuringTrader.Simulator;

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private class DataModel
        {
            public string HomePath
            {
                get
                {
                    string homePath = GlobalSettings.HomePath ?? "";
                    if (homePath.Length > 40)
                    {
                        string left = homePath.Substring(0, 15);
                        string right = homePath.Substring(homePath.Length - 25, 25);
                        homePath = left + "..." + right;
                    }
                    return homePath;
                }
            }
            public string TiingoApiKey
            {
                get
                {
                    return GlobalSettings.TiingoApiKey;
                }
                set
                {
                    GlobalSettings.TiingoApiKey = value;
                }
            }

            public string DefaultDataSource
            {
                get
                {
                    return GlobalSettings.DefaultDataFeed;
                }
                set
                {
                    GlobalSettings.DefaultDataFeed = value;
                }
            }

            public string DefaultTemplateExtension
            {
                get
                {
                    return GlobalSettings.DefaultTemplateExtension;
                }
                set
                {
                    GlobalSettings.DefaultTemplateExtension = value;
                }
            }
        }

        public Settings()
        {
            InitializeComponent();
            DataContext = new DataModel();
        }

        private void HomePathButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog()
            {
                Title = "Select TuringTrader Home Location"
            };

            bool? result = folderDialog.ShowDialog();
            if (result == true)
            {
                GlobalSettings.HomePath = folderDialog.SelectedPath;
            }

            LabelHomePath.Content = (DataContext as DataModel).HomePath;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

//==============================================================================
// end of file