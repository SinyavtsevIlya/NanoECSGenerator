using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoEcs.Generator.Extensions
{
    public static class ConsoleExtensions
    {
        public static void WriteToColsole(this string line, ConsoleColor color)
        {
            var beforeColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = beforeColor;
        }
    }
}
