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
    /// Helper class for dynamic algorithm instantiation.
    /// </summary>
    public class AlgorithmLoader
    {
        #region public static IEnumerable<Type> GetAllAlgorithms()
        private static List<Type> _allAlgorithms = null;
        private static IEnumerable<Type> _getAllAlgorithms()
        {
            Assembly turingTrader = Assembly.GetExecutingAssembly();
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(turingTrader.Location));
            FileInfo[] files = dirInfo.GetFiles("*.dll");

            // see https://msdn.microsoft.com/en-us/library/ms972968.aspx

            foreach (FileInfo file in files)
            {
                Type[] types;
                try
                {
                    Assembly assembly = Assembly.LoadFrom(file.FullName);
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (!type.IsAbstract 
                    && type.IsPublic
                    && type.IsSubclassOf(typeof(Algorithm)))
                        yield return type;
                }
            }

            yield break;
        }

        /// <summary>
        /// Return list of all known TuringTrader algorithms
        /// </summary>
        /// <returns>list of algorithms</returns>
        public static List<Type> GetAllAlgorithms()
        {
            if (_allAlgorithms == null || _allAlgorithms.Count == 0)
                _allAlgorithms = _getAllAlgorithms()
                    .OrderBy(t => t.Name)
                    .ToList();

            return _allAlgorithms;
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
            foreach (Type algorithmType in GetAllAlgorithms())
                if (algorithmType.Name == algorithmName)
                    return (Algorithm)Activator.CreateInstance(algorithmType);

            return null;
        }
        #endregion
    }
}

//==============================================================================
// end of file