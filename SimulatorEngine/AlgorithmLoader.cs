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
using TuringTrader.Simulator;
using System;
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
        public string DisplayPath;
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
        private static readonly List<AlgorithmInfo> _allAlgorithms = 
                        _initAllAlgorithms()
                            .OrderBy(a => a.Name)
                            .ToList();

        private static IEnumerable<AlgorithmInfo> _initAllAlgorithms_Dll()
        {
            Assembly turingTrader = Assembly.GetExecutingAssembly();
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(turingTrader.Location));
            FileInfo[] files = dirInfo.GetFiles("*.dll");

            // see https://msdn.microsoft.com/en-us/library/ms972968.aspx

            foreach (FileInfo file in files)
            {
                Type[] types;
                string title;

                try
                {
                    Assembly assembly = Assembly.LoadFrom(file.FullName);
                    types = assembly.GetTypes();
                    title = (assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute).Title;
                }
                catch
                {
                    continue;
                }

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
                            DisplayPath = title,
                        };
                    }
                }
            }

            yield break;
        }

        private static IEnumerable<AlgorithmInfo> _initAllAlgorithms_Source(string path, string displayPath)
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
                foreach (var a in _initAllAlgorithms_Source(dir.FullName, displayPath + ":" + dir.Name))
                    yield return a;
            }

            yield break;
        }

        private static IEnumerable<AlgorithmInfo> _initAllAlgorithms()
        {
            foreach (var algorithm in _initAllAlgorithms_Dll())
                yield return algorithm;

            foreach (var algorithm in _initAllAlgorithms_Source(GlobalSettings.AlgorithmPath, ""))
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
            return publicOnly
                ? _allAlgorithms
                    .Where(t => t.IsPublic == true)
                    .ToList()
                : _allAlgorithms;
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
            List<AlgorithmInfo> matchingAlgorithms = _allAlgorithms
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
                return (Algorithm)Activator.CreateInstance(algorithmInfo.DllType);
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file