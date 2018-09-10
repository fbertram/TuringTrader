using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    interface ITimeSeries<T>
    {
        T this[int daysBack] { get; }
    }
}
