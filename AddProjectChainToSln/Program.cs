using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace Falconne.AddProjectChainToSln
{
    using SolutionTools;

    class Options
    {
        [ParserState]
        public IParserState LastParserState { get; set; }

        [Option('s', "sourcesln", Required = true, HelpText = "Path to source soution containing root project.")]
        public string SourceSolutionPath { get; set; }

        [Option('t', "targetsln", Required = true, HelpText = "Path to target soution to insert projects into.")]
        public string TargetSolutionPath { get; set; }

        [Option('p', "project", Required = true, HelpText = "Root project name in source solution.")]
        public string ProjectName { get; set; }

        [Option("sourceconfig", Required = true, HelpText = "Configuration|Platform to read from source solution.")]
        public string SourceConfigPlatform { get; set; }

        [Option("targetconfig", Required = true, HelpText = "Configuration|Platform to use in target solution.")]
        public string TargetConfigPlatform { get; set; }

        [HelpOption(HelpText = "Dispaly this help screen.")]
        public string GetUsage()
        {
            var help = new HelpText("Inject a root project and dependencies into target solution.");
            if (LastParserState.Errors.Any())
            {
                var errors = help.RenderParsingErrorsText(this, 2); // indent with two spaces

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

    class Program
    {
        static int Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArgumentsStrict(args, options))
            {
                return 1;
            }

            try
            {
                return AddProjectToSolution(options.SourceSolutionPath, options.TargetSolutionPath,
                    options.ProjectName, options.SourceConfigPlatform, options.TargetConfigPlatform)
                    ? 0
                    : 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static bool AddProjectToSolution(string sourceSolutionPath, string targetSolutionPath,
            string sourceProjectName, string sourceConfigPlatform, string targetConfigPlatform)
        {
            if (!File.Exists(sourceSolutionPath))
            {
                Console.Error.WriteLine($"File not found: {sourceSolutionPath}");
                return false;
            }

            if (!File.Exists(targetSolutionPath))
            {
                Console.Error.WriteLine($"File not found: {targetSolutionPath}");
                return false;
            }

            var sourceSolution = new Solution(sourceSolutionPath);
            var sourceProject = sourceSolution.GetProject(sourceProjectName);
            if (sourceProject == null)
            {
                Console.Error.WriteLine("Project not found in source solution");
                return false;
            }

            IEnumerable<Project> allRequiredProjects;
            try
            {
                allRequiredProjects = sourceSolution.GetFlattenedRelatedProjectList(sourceProject);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return false;
            }

            var targetSolution = new Solution(targetSolutionPath);
            foreach (var project in allRequiredProjects)
            {
                targetSolution.AddProject(project);

                var sourceBuildConfigs = sourceSolution.GetBuildConfigsFor(project, sourceConfigPlatform);
                var newTargetBuildConfigs = sourceBuildConfigs.Select(
                    sc => new KeyValuePair<string, string>(
                        SwitchConfigPlatform(sc.Key, targetConfigPlatform), sc.Value));

                targetSolution.AddBuildConfigs(newTargetBuildConfigs);
            }

            targetSolution.Save();

            Console.WriteLine("Done.");

            return true;
        }

        private static string SwitchConfigPlatform(string originalKey, string newConfigPlatform)
        {
            var parts = originalKey.Split('.');
            parts[1] = newConfigPlatform;
            var newKey = string.Join(".", parts);

            return newKey;
        }
    }
}
