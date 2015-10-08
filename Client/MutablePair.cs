using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class MutablePair<TFirst, TSecond>
    {
        public TFirst First { get; set; }
        public TSecond Second { get; set; }

        public MutablePair()
        {
        }

        public MutablePair(TFirst first, TSecond second)
        {
            First = first;
            Second = second;
        }
    }
}
