using ConfigGenerator.CommandLineOptions;
using Fclp;
using System;
using System.Diagnostics;
using System.Linq;

namespace ConfigGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new FluentCommandLineParser<BaseOptions>();
            
            Setup(parser);

            var result = parser.Parse(args);
            if (!result.HasErrors && !result.HelpCalled)
            {
                var arguments = parser.Object;
                var generator =  new ConfigGen.ConfigGenerator(new Uri(arguments.Input,UriKind.RelativeOrAbsolute));
                try
                {
                    if (arguments.MultipleFiles)
                    {
                        generator.GenerateConfig(new Uri(arguments.Output, UriKind.RelativeOrAbsolute), ConfigGen.OutPutType.FilePerClass);
                    }
                    else
                    {
                        generator.GenerateConfig(new Uri(arguments.Output, UriKind.RelativeOrAbsolute), ConfigGen.OutPutType.SingleFile);
                    }
                } catch(Exception e)
                {
                    Console.WriteLine(string.Format("An error occurred during execution: {0}", e.Message));
                }
                
                Console.WriteLine("Done ...");
            } else if(!result.HelpCalled)
            {
                foreach (var item in result.Errors)
                {
                    Console.WriteLine("Missing argument {0}",item.Option.LongName);
                }
                parser.HelpOption.ShowHelp(parser.Options);
            }
        }

        static void Setup(FluentCommandLineParser<BaseOptions> p)
        {
            p.Setup(a => a.Input)
                .As('i', "input")
                .WithDescription("XML file to use as input.")
                .Required();

            p.Setup(a => a.Output)
                .As('o', "output")
                .WithDescription("File or directory to output generated classes to.")
                .Required();

            p.Setup(a => a.MultipleFiles)
                .As('m', "MultipleFiles")
                .SetDefault(false)
                .WithDescription("Flag indicating that each class should be in it's own file.");

            p.SetupHelp("?", "help", "h")
                .Callback(text => {
                    Console.WriteLine();
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    string version = fvi.FileVersion;
                    Console.WriteLine("Configuration Generator \nversion {0}", version);
                    Console.WriteLine("Developed by: Daniel Thomassen");
                    Console.WriteLine();
                    Console.WriteLine(text);
                    });
        }
    }
}
