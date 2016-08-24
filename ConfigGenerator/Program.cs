using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigGen;
using System.IO;

namespace ConfigGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if(string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
            {
                Usage();
                return;
            }
            Uri file = new Uri(args[0], UriKind.RelativeOrAbsolute);
            var generator = new ConfigGen.ConfigGenerator(file);
            generator.GenerateConfig(new Uri(args[1],UriKind.RelativeOrAbsolute));
        }

        static void Usage()
        {
            Console.WriteLine(@"ConfigGenerator.exe <templatefile> <destinationfile>");
        }
    }
}
