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

#define USE_ROSLYN

#if USE_ROSLYN

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

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
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(sourcePath), 
                path: sourcePath, 
                encoding: System.Text.Encoding.UTF8);

            string assemblyName = Path.GetRandomFileName();

            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            MetadataReference[] references = new MetadataReference[]
            {
                //--- these can't be created any other way
                //    https://stackoverflow.com/questions/23907305/roslyn-has-no-reference-to-system-runtime
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Data.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),

                //--- these are referenced by a type we need
                //MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(TuringTrader.Simulator.Algorithm).GetTypeInfo().Assembly.Location),
                //MetadataReference.CreateFromFile(typeof(TuringTrader.Indicators.IndicatorsBasic).GetTypeInfo().Assembly.Location),

                //--- this is what we used with CodeDOM
                /*
                cp.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
                cp.ReferencedAssemblies.Add("System.dll");
                cp.ReferencedAssemblies.Add("System.Runtime.dll");
                cp.ReferencedAssemblies.Add("System.Collections.dll");
                cp.ReferencedAssemblies.Add("System.Core.dll");
                cp.ReferencedAssemblies.Add("System.Data.dll");
                cp.ReferencedAssemblies.Add("OxyPlot.dll");
                cp.ReferencedAssemblies.Add("OxyPlot.Wpf.dll");
                */
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dll = new MemoryStream())
            using (var pdb = new MemoryStream())
            {
                EmitResult result = compilation.Emit(dll, pdb);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Output.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    dll.Seek(0, SeekOrigin.Begin);
                    pdb.Seek(0, SeekOrigin.Begin);

                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(dll, pdb);
                    return assembly;
                }
            }


            return null;
        }
    }
}

#else

#region libraries
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            // code provider
            // TODO: figure out how to compile for C# 7
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
            cp.IncludeDebugInformation = true;

#if false
            string source = "";
            using (var sr = new StreamReader(sourcePath))
                source = sr.ReadToEnd();
            CompilerResults cr = provider.CompileAssemblyFromSource(cp, source);
#else
            CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourcePath);
#endif

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

#endif

//==============================================================================
// end of file