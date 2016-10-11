using System;
using System.Diagnostics;

namespace ConfigGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
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
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Console.WriteLine("Configuration Generator \nversion {0}", version);
            Console.WriteLine("usage: ConfigGenerator <templatefile> <destinationfile>");
            Console.WriteLine("More information at: https://github.com/DanielThomassen/CS-ConfigurationSectionGenerator");
            Console.WriteLine();
        }
    }
}
