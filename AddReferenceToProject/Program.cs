using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace Falconne.AddReferenceToProject
{
    internal class Options
    {
        [ParserState]
        public IParserState LastParserState { get; set; }

        [Option('r', "refproject", Required = true, HelpText = "Project to add as a reference.")]
        public string RefProjectPath { get; set; }

        [Option('t', "targetproject", Required = true, HelpText = "Projecte to add reference into.")]
        public string TargetProjectPath { get; set; }

        [HelpOption(HelpText = "Dispaly this help screen.")]
        public string GetUsage()
        {
            var help = new HelpText("Add one project into another, as a project refence.");
            if (LastParserState.Errors.Any())
            {
                var errors = help.RenderParsingErrorsText(this, 2);

                if (!string.IsNullOrEmpty(errors))
                {
                    help.AddPreOptionsLine(string.Concat(Environment.NewLine, "ERROR(S):"));
                    help.AddPreOptionsLine(errors);
                }
            }

            help.AddOptions(this);
            return help;
        }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArgumentsStrict(args, options))
            {
                return 1;
            }

            if (!File.Exists(options.RefProjectPath))
            {
                Console.Error.WriteLine("File not found: " + options.RefProjectPath);
                return 1;
            }

            if (!File.Exists(options.TargetProjectPath))
            {
                Console.Error.WriteLine("File not found: " + options.TargetProjectPath);
                return 1;
            }

            // Find the GUID of the source project
            var refContent = File.ReadAllLines(options.RefProjectPath);
            var guidLine = refContent.FirstOrDefault(line => line.Contains("<ProjectGuid"));
            if (string.IsNullOrEmpty(guidLine))
            {
                Console.Error.WriteLine("Invalid project file: " + options.RefProjectPath);
                return 1;
            }
            var guidRaw = Regex.Match(guidLine, @">(.+)<").Groups[1].Value;
            var refGUID = new Guid(guidRaw);

            // Create relative path to source project from target
            var targetDir = Path.GetDirectoryName(options.TargetProjectPath);

            return 0;
        }
    }
}
