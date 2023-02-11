//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        AlgorithmLoader
// Description: support for dynamic loading of algorithms
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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

// LOAD_ALGOS_V2_DLL: with this switch turned on, the algorithm loader
// looks for Algos.v2.dll in the same folder as the TuringTrader binary.
// This is used for convenient testing of the v2 sim core.
//#define LOAD_ALGOS_V2_DLL

#region Libraries
using System;
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
        /// Path to DLL, in case algorithm is contained in DLL
        /// </summary>
        public string DllPath;
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
            catch (Exception e)
            {
                // can't load types: ignore
                // NOTE: no error message here, as we might look at many DLLs
                // that intentionally don't contain algorithms.
                //Output.WriteLine("AlgorithmLoader: failed to load {0}, {1}", assembly.FullName, e.Message);
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
                //&& type.IsSubclassOf(typeof(Algorithm))) // NOTE: used until 04/23/2021
                && type.GetInterface(nameof(IAlgorithm)) != null) // NOTE: new 04/23/2021
                {
                    yield return new AlgorithmInfo
                    {
                        Name = type.Name,
                        IsPublic = type.IsPublic,
                        DllType = type,
                        DllPath = assembly.Location,
                        DisplayPath = new List<string>() { title },
                    };
                }
            }

            yield break;
        }

        private static IEnumerable<AlgorithmInfo> _enumDllAlgorithms(string dllPath)
        {
#if true
            // enumerate algorithms from entry assembly
            var entry = Assembly.GetEntryAssembly();
            foreach (var type in _enumAssyAlgorithms(entry))
                yield return type;
#endif

            if (GlobalSettings.LoadAlgoDlls)
            {
#if LOAD_ALGOS_V2_DLL
                // FIXME: this is only temporary while we are 
                // kicking of development of the v2 simulator core
                var v2Simulator = Path.Combine(
                    Path.GetDirectoryName(entry.Location),
                    "Algos.v2.dll");

                if (File.Exists(v2Simulator))
                {
                    var assembly = Assembly.LoadFrom(v2Simulator);
                    foreach (var type in _enumAssyAlgorithms(assembly))
                        yield return type;
                }
#endif

                DirectoryInfo dirInfo = new DirectoryInfo(dllPath);

                if (!dirInfo.Exists)
                    yield break;

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
#if false
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

            return allAlgorithms
                    .Where(t => t.IsPublic == true || publicOnly == false)
                    .ToList();
        }
        #endregion
        #region public static Algorithm InstantiateAlgorithm(string algorithmName)
        /// <summary>
        /// Instantiate TuringTrader algorithm
        /// </summary>
        /// <param name="algorithmName">class name</param>
        /// <returns>algorithm instance</returns>
        public static IAlgorithm InstantiateAlgorithm(string algorithmName)
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
        public static IAlgorithm InstantiateAlgorithm(AlgorithmInfo algorithmInfo)
        {
            if (algorithmInfo.DllType != null)
            {
                // instantiate from DLL
                return (IAlgorithm)Activator.CreateInstance(algorithmInfo.DllType);
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

        #region public static void PrintInfo()
        /// <summary>
        /// Print info about all algorithms found
        /// </summary>
        public static void PrintInfo()
        {
            Output.WriteLine("===== AlgorithmLoader Info =====");
            int i = 0;
            foreach (var info in _staticAlgorithms)
            {
                if (info.DllType != null)
                {
                    Output.WriteLine("{0}: {1} ({2})", i++, info.Name, info.DllPath);
                }
                else
                {
                    Output.WriteLine("{0}: {1} ({2})", i++, info.Name, info.SourcePath);
                }
            }
            Output.WriteLine("===== end =====");
        }
        #endregion
    }
}

//==============================================================================
// end of file