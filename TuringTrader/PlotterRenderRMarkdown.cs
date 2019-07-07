//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PlotterRenderRMarkdown
// Description: Plotter renderer for R Markdown templates
// History:     2019vi20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TuringTrader.Simulator;

#if false
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
                        fullPath.Replace("\\", "/"),
                        renderOutput.Replace("\\", "/"));
                    sw.WriteLine("browseURL(\"{0}\")",
                        renderOutput.Replace("\\", "/"));
#endif
                    sw.Flush();
                    OpenWithRscript(launcherScript);

#endif

namespace TuringTrader
{
    static class PlotterRenderRMarkdown
    {
        public static void Register()
        {
            Plotter.Renderer += Renderer;
        }

        public static void Renderer(Plotter plotter, string template)
        {
            if (Path.GetExtension(template).ToLower() != ".rmd")
                return;

            Output.WriteLine("Rendering with R Markdown is currently unavailable. Stay tuned");
        }
    }
}

//==============================================================================
// end of file