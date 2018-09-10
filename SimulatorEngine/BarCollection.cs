using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    class BarCollection : Dictionary<string, BarType>
    {
        public IEnumerable<string> Symbols
        {
            get
            {
                return Keys;
            }
        }
    }
}
