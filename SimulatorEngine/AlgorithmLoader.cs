//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        AlgorithmLoader
// Description: support for dynamic loading of algorithms
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region Libraries
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Container for algorithm info
    /// </summary>
    public class AlgorithmInfo
    {
        /// <summary>
        /// Name of algorithm. This is either the class name, in case the
        /// algorithm is contained in a DLL, or the file name w/o path, in
        /// case the algorithm is provided as source code.
        /// </summary>
        public string Name;
        /// <summary>
        /// Path, as displayed in Algorithm menu. This is either a relative path 
        /// to the source file containing the algorithm, or the DLL title property
        /// </summary>
        public List<string> DisplayPath;
        /// <summary>
        /// True, if algorithm is public. This field is only properly initialized
        /// for algorithm contained in DLLs. For algorithms contained in source
        /// files, it is always true.
        /// </summary>
        public bool IsPublic;
        /// <summary>
        /// Algorithm Type, in case algorithm is contained in DLL
        /// </summary>
        public Type DllType;
        /// <summary>
        /// Algorithm source path, in case algorithm is contained in C# source file
        /// </summary>
        public string SourcePath;
    }

    /// <summary>
    /// Helper class for dynamic algorithm instantiation.
    /// </summary>
    public class AlgorithmLoader
    {
        #region internal helpers
        private static readonly List<AlgorithmInfo> _staticAlgorithms =
                        _enumStaticAlgorithms()
                            .OrderBy(a => a.Name)
                            .ToList();
        private static Assembly _currentSourceAssembly = null;

        private static IEnumerable<AlgorithmInfo> _enumAssyAlgorithms(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();
            object[] titleAttributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            string title = titleAttributes.Count() > 0
                ? (titleAttributes[0] as AssemblyTitleAttribute).Title
                : "n/a";

            foreach (Type type in types)
            {
                if (!type.IsAbstract
                && type.IsSubclassOf(typeof(Algorithm)))
                {
                    yield return new AlgorithmInfo
                    {
                        Name = type.Name,
                        IsPublic = type.IsPublic,
                        DllType = type,
                        DisplayPath = new List<string>() { title },
                    };
                }
            }

            yield break;
        }

        private static IEnumerable<AlgorithmInfo> _enumDllAlgorithms()
        {
            Assembly turingTrader = Assembly.GetExecutingAssembly();
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(turingTrader.Location));
            FileInfo[] files = dirInfo.GetFiles("*.dll");

            // see https://msdn.microsoft.com/en-us/library/ms972968.aspx

            foreach (FileInfo file in files)
            {
                Assembly assembly = null;

                try
                {
                    assembly = Assembly.LoadFrom(file.FullName);
                }
                catch
                {
                    continue;
                }

                foreach (var type in _enumAssyAlgorithms(assembly))
                    yield return type;
            }

            yield break;
        }

        private static IEnumerable<AlgorithmInfo> _enumSourceAlgorithms(string path, List<string> displayPath)
        {
            if (!Directory.Exists(path))
                yield break;

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            FileInfo[] files = dirInfo.GetFiles("*.cs");

            foreach (FileInfo file in files)
            {
                yield return new AlgorithmInfo
                {
                    Name = file.Name,
                    DisplayPath = displayPath,
                    IsPublic = true,
                    SourcePath = file.FullName,
                };
            }

            DirectoryInfo[] dirs = dirInfo.GetDirectories();

            foreach (DirectoryInfo dir in dirs)
            {
                List<string> newDisplayPath = new List<string>(displayPath);
                newDisplayPath.Add(dir.Name);

                foreach (var a in _enumSourceAlgorithms(dir.FullName, newDisplayPath))
                    yield return a;
            }

            yield break;
        }

        private static IEnumerable<AlgorithmInfo> _enumStaticAlgorithms()
        {
            foreach (var algorithm in _enumDllAlgorithms())
                yield return algorithm;

            foreach (var algorithm in _enumSourceAlgorithms(GlobalSettings.AlgorithmPath, new List<string>()))
                yield return algorithm;

            yield break;
        }
        #endregion

        #region public static List<Type> GetAllAlgorithms()
        /// <summary>
        /// Return list of all known TuringTrader algorithms
        /// </summary>
        /// <param name="publicOnly">if true, only return public classes</param>
        /// <returns>list of algorithms</returns>
        public static List<AlgorithmInfo> GetAllAlgorithms(bool publicOnly = true)
        {
            var allAlgorithms = new List<AlgorithmInfo>(_staticAlgorithms);

            if (_currentSourceAssembly != null)
            {
                var sourceAlgorithms = _enumAssyAlgorithms(_currentSourceAssembly);

                allAlgorithms = allAlgorithms
                    .Concat(sourceAlgorithms)
                    .ToList();
            }

            return publicOnly
                ? allAlgorithms
                    .Where(t => t.IsPublic == true)
                    .ToList()
                : allAlgorithms;
        }
        #endregion
        #region public static Algorithm InstantiateAlgorithm(string algorithmName)
        /// <summary>
        /// Instantiate TuringTrader algorithm
        /// </summary>
        /// <param name="algorithmName">class name</param>
        /// <returns>algorithm instance</returns>
        public static Algorithm InstantiateAlgorithm(string algorithmName)
        {
            List<AlgorithmInfo> allAlgorithms = GetAllAlgorithms(false);
            List<AlgorithmInfo> matchingAlgorithms = allAlgorithms
                .Where(a => a.Name == algorithmName)
                .ToList();

            if (matchingAlgorithms.Count > 1)
                throw new Exception(string.Format("AlgorithmLoader: algorithm {0} is ambiguous", algorithmName));

            if (matchingAlgorithms.Count < 1)
                throw new Exception(string.Format("AlgorithmLoader: algorithm {0} not found", algorithmName));

            return InstantiateAlgorithm(matchingAlgorithms.First());
        }
        #endregion
        #region public static Algorithm InstantiateAlgorithm(AlgorithmInfo algorithmInfo)
        /// <summary>
        /// Instantiate TuringTrader algorithm
        /// </summary>
        /// <param name="algorithmInfo">algorithm info</param>
        /// <returns>algorithm instance</returns>
        public static Algorithm InstantiateAlgorithm(AlgorithmInfo algorithmInfo)
        {
            if (algorithmInfo.DllType != null)
            {
                // instantiate from DLL
                return (Algorithm)Activator.CreateInstance(algorithmInfo.DllType);
            }
            else
            {
                // compile and instantiate from memory
                Output.WriteLine("AlgorithmLoader: compiling {0}", algorithmInfo.SourcePath);

                using (var sr = new StreamReader(algorithmInfo.SourcePath))
                {
                    var options = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };
                    CSharpCodeProvider provider = new CSharpCodeProvider(options);

                    CompilerParameters cp = new CompilerParameters();
                    cp.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
                    cp.ReferencedAssemblies.Add("System.dll");
                    cp.ReferencedAssemblies.Add("System.Core.dll");
                    cp.ReferencedAssemblies.Add("System.Data.dll");
                    cp.GenerateInMemory = true;
                    cp.TreatWarningsAsErrors = false;
                    //cp.CompilerOptions = "/optimize /langversion:5"; // 7, 7.1, 7.2, 7.3, Latest
                    //cp.WarningLevel = 3;
                    //cp.GenerateExecutable = false;
                    //cp.IncludeDebugInformation = true;

                    string source = sr.ReadToEnd();

                    CompilerResults cr = provider.CompileAssemblyFromSource(cp, source);

                    if (cr.Errors.HasErrors)
                    {
                        string errorMessages = "";
                        cr.Errors.Cast<CompilerError>()
                            .ToList()
                            .ForEach(error => errorMessages += "Line " + error.Line + ": " + error.ErrorText + "\r\n");

                        Output.WriteLine(errorMessages);
                        return null;
                        //throw new Exception("AlgorithmLoader: failed to compile");
                    }

                    _currentSourceAssembly = cr.CompiledAssembly;
                    var publicAlgorithms = _enumAssyAlgorithms(cr.CompiledAssembly)
                        .Where(t => t.IsPublic)
                        .ToList();

                    if (publicAlgorithms.Count == 0)
                    {
                        Output.WriteLine("AlgorithmLoader: no algorithm found");
                        return null;
                    }

                    if (publicAlgorithms.Count > 1)
                    {
                        Output.WriteLine("AlgorithmLoader: multiple algorithms found");
                        return null;
                    }

                    var algo = InstantiateAlgorithm(publicAlgorithms[0]);

                    if (algo != null)
                        Output.WriteLine("AlgorithmLoader: success!");

                    return algo;
                }
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file