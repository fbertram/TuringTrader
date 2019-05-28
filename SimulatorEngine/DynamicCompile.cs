//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DynamicCompile
// Description: support for dynamic compilation
// History:     2019v28, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Helper class to dynamically compile C# source code
    /// </summary>
    class DynamicCompile
    {
        /// <summary>
        /// Compile C# source code
        /// </summary>
        /// <param name="sourcePath">path to source</param>
        /// <returns>compiled assembly</returns>
        public static Assembly CompileSource(string sourcePath)
        {
            Output.WriteLine("DynamicCompile: compiling {0}", sourcePath);

            if (!File.Exists(sourcePath))
                return null;

            string source = "";
            using (var sr = new StreamReader(sourcePath))
                source = sr.ReadToEnd();

            // code provider
            var options = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };
            CSharpCodeProvider provider = new CSharpCodeProvider(options);

            // compiler parameters
            CompilerParameters cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Runtime.dll");
            cp.ReferencedAssemblies.Add("System.Collections.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add("System.Data.dll");
            cp.ReferencedAssemblies.Add("OxyPlot.dll");
            cp.ReferencedAssemblies.Add("OxyPlot.Wpf.dll");
            cp.GenerateInMemory = true;
            cp.TreatWarningsAsErrors = false;
            //cp.CompilerOptions = "/optimize /langversion:5"; // 7, 7.1, 7.2, 7.3, Latest
            //cp.WarningLevel = 3;
            //cp.GenerateExecutable = false;
            //cp.IncludeDebugInformation = true;

            CompilerResults cr = provider.CompileAssemblyFromSource(cp, source);

            if (cr.Errors.HasErrors)
            {
                string errorMessages = "";
                cr.Errors.Cast<CompilerError>()
                    .ToList()
                    .ForEach(error => errorMessages += "Line " + error.Line + ": " + error.ErrorText + "\r\n");

                Output.WriteLine(errorMessages);
                return null;
            }

            return cr.CompiledAssembly;
        }
    }
}

//==============================================================================
// end of file