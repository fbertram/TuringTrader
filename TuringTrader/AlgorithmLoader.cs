//==============================================================================
// Project:     TuringTrader
// Name:        AlgorithmLoader
// Description: support for dynamic loading of algorithms
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region Libraries
using FUB_TradingSim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader
{
    class AlgorithmLoader
    {
        #region public static IEnumerable<Type> GetAllAlgorithms()
        public static IEnumerable<Type> GetAllAlgorithms()
        {
            Assembly turingTrader = Assembly.GetExecutingAssembly();
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(turingTrader.Location));
            FileInfo[] Files = dirInfo.GetFiles("*.dll");

            foreach (FileInfo file in Files)
                Assembly.LoadFrom(file.FullName);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] Types;
                try
                {
                    // under certain circumstances, this might throw
                    Types = assembly.GetTypes();
                }
                catch (Exception)
                {
                    // just ignore
                    continue;
                }

                foreach (Type type in Types)
                    if (!type.IsAbstract && type.IsSubclassOf(typeof(Algorithm)))
                        yield return type;
            }

            yield break;
        }
        #endregion
        #region public static Algorithm InstantiateAlgorithm(string algorithmName)
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