using FUB_TradingSim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TuringTrader
{
    class AlgorithmLoader
    {
        public static IEnumerable<Type> GetAllAlgorithms()
        {
            Assembly turingTrader = Assembly.GetExecutingAssembly();
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(turingTrader.Location));
            FileInfo[] Files = dirInfo.GetFiles("*.dll");

            foreach (FileInfo file in Files)
                Assembly.LoadFrom(file.FullName);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (Type type in assembly.GetTypes())
                    if (!type.IsAbstract && type.IsSubclassOf(typeof(Algorithm)))
                        yield return type;

            yield break;
        }

        public static Algorithm InstantiateAlgorithm(string algorithmName)
        {
            foreach (Type algorithmType in GetAllAlgorithms())
                if (algorithmType.Name == algorithmName)
                    return (Algorithm)Activator.CreateInstance(algorithmType);

            return null;
        }
    }
}
