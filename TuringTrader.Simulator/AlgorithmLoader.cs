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
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                // can't load types: ignore
                yield break;
            }

#if true
            // .net core 3
            object[] descrAttributes = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            string title = descrAttributes.Count() > 0
                ? (descrAttributes[0] as AssemblyDescriptionAttribute).Description
                : "n/a";
#else
            // .net framework
            object[] titleAttributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            string title = titleAttributes.Count() > 0
                ? (titleAttributes[0] as AssemblyTitleAttribute).Title
                : "n/a";
#endif

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

        private static IEnumerable<AlgorithmInfo> _enumDllAlgorithms(string dllPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dllPath);
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
#if true
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (var algorithm in _enumDllAlgorithms(exePath))
                yield return algorithm;
#endif

#if true
            foreach (var algorithm in _enumDllAlgorithms(GlobalSettings.AlgorithmPath))
                yield return algorithm;
#endif

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
                _currentSourceAssembly = DynamicCompile.CompileSource(algorithmInfo.SourcePath);

                if (_currentSourceAssembly == null)
                    return null;

                var publicAlgorithms = _enumAssyAlgorithms(_currentSourceAssembly)
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

                var algo = InstantiateAlgorithm(publicAlgorithms.First());

                if (algo != null)
                    Output.WriteLine("AlgorithmLoader: success!");

                return algo;
            }
        }
#endregion
    }
}

//==============================================================================
// end of file