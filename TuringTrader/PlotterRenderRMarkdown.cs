//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PlotterRenderRMarkdown
// Description: Plotter renderer for R Markdown templates
// History:     2019vi20, FUB, created
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

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader
{
    static class PlotterRenderRMarkdown
    {
        public static void Register()
        {
            Plotter.Renderer += Renderer;
        }

        #region public static void Renderer(Plotter plotter, string pathToRmdTemplate)
        public static void Renderer(Plotter plotter, string pathToRmdTemplate)
        {
            if (Path.GetExtension(pathToRmdTemplate).ToLower() != ".rmd")
                return;

            string launcherScript = Path.ChangeExtension(Path.GetTempFileName(), ".r");
            string renderOutput = Path.ChangeExtension(Path.GetTempFileName(), ".htm");

            using (var sw = new StreamWriter(launcherScript))
            {
#if false
                    sw.WriteLine("rmarkdown::render(\"{0}\", output_file=\"{1}\")",
                        fullPath.Replace("\\", "/"),
                        renderOutput.Replace("\\", "/"));
                    sw.WriteLine("browseURL(\"{0}\")",
                        renderOutput.Replace("\\", "/"));
#else
                sw.WriteLine("library(rmarkdown)");
                sw.WriteLine("render(\"{0}\", output_file=\"{1}\")",
                    pathToRmdTemplate.Replace("\\", "/"),
                    renderOutput.Replace("\\", "/"));
                sw.WriteLine("browseURL(\"{0}\")",
                    renderOutput.Replace("\\", "/"));
#endif
                sw.Flush();

                // open launcher script with R renderer
                PlotterRenderR.Renderer(plotter, launcherScript);
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file