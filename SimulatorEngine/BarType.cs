using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    class BarType
    {
        public readonly string Symbol;
        public readonly DateTime TimeStamp;
        public readonly double Open;
        public readonly double High;
        public readonly double Low;
        public readonly double Close;
        public readonly double Volume;

        public BarType(
            string symbol,
            DateTime timeStamp,
            double open,
            double high,
            double low,
            double close,
            double volume)
        {
            Symbol = symbol;
            TimeStamp = timeStamp;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
    }
}
