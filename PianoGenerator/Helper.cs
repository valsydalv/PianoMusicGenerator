using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PianoGenerator
{
    public static class Helper
    {
        public static bool AlmostEqual(this double x, double y)
        {
            double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
            return Math.Abs(x - y) <= epsilon;
        }
    }
}
