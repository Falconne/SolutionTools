﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace Falconne.AddProjectChainToSln
{
    using SolutionTools;

    internal class Options
    {
        [ParserState]
        public IParserState LastParserState { get; set; }

        [Option('s', "sourcesln", Required = false, HelpText = 
            @"Optional path to source soution containing root project.
If provided, the required project and all its dependent projects will be imported.
Otherwise, dependencies will not be imported.")]
        public string SourceSolutionPath { get; set; }

        [Option('t', "targetsln", Required = true, HelpText =
            "Path to target soution to insert projects into.")]
        public string TargetSolutionPath { get; set; }

        [Option('p', "project", Required = true, HelpText =
            @"Root project name in source solution. If source solution is not provided, provide
full path to project here.")]
        public string ProjectName { get; set; }

        [Option("sourceconfig", Required = true, HelpText =
            @"Configuration|Platform to read from source solution, if sourcesln provided.
Otherwise, Configuration|Platform of project to use.")]
        public string SourceConfigPlatform { get; set; }

        [Option("targetconfig", Required = true, HelpText =
            "Configuration|Platform to use in target solution.")]
        public string TargetConfigPlatform { get; set; }

        [HelpOption(HelpText = "Dispaly this help screen.")]
        public string GetUsage()
        {
            var help = new HelpText(
                "Copy a given project and (if possible) all its dependent projects from one solution to another.");
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

    internal class Program
    {
        private static int Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArgumentsStrict(args, options))
            {
                return 1;
            }

            try
            {
                var result = false;
                if (!string.IsNullOrEmpty(options.SourceSolutionPath))
                {
                    result = AddProjectChainToSolution(options.SourceSolutionPath, options.TargetSolutionPath,
                        options.ProjectName, options.SourceConfigPlatform, options.TargetConfigPlatform);
                }
                else
                {
                    result = AddProjectToSolution(options.TargetSolutionPath,
                        options.ProjectName, options.SourceConfigPlatform, options.TargetConfigPlatform);

                }

                return result ? 0 : 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static bool AddProjectChainToSolution(string sourceSolutionPath, string targetSolutionPath,
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

            return true;
        }

        private static bool AddProjectToSolution(string targetSolutionPath,
            string sourceProjectPath, string sourceConfigPlatform, string targetConfigPlatform)
        {
            if (!File.Exists(targetSolutionPath))
            {
                Console.Error.WriteLine($"File not found: {targetSolutionPath}");
                return false;
            }

            if (!File.Exists(sourceProjectPath))
            {
                Console.Error.WriteLine($"File not found: {sourceProjectPath}");
                return false;
            }

            var targetSolution = new Solution(targetSolutionPath);
            var project = new Project(sourceProjectPath);
            targetSolution.AddProject(project);
            var configPrefix = $"{{{project.ProjectGuid.ToString().ToUpper()}}}.{targetConfigPlatform}";
            targetSolution.AddBuildConfig(new KeyValuePair<string, string>(
                $"{configPrefix}.ActiveCfg", sourceConfigPlatform));

            targetSolution.AddBuildConfig(new KeyValuePair<string, string>(
                $"{configPrefix}.Build.0", sourceConfigPlatform));

            targetSolution.Save();

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
