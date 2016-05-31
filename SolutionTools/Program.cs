using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Onion.SolutionParser.Parser.Model;

namespace Falconne.SolutionTools
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                return AddProjectToSolution(@"C:\git\cti\NTiTy\Zeacom\Zeacom.sln", "Release|x86",
                    "Zeacom.Contracts", @"C:\git\cti\NTiTy\Clients\TouchPoint\TouchPoint.sln",
                    "Release|Licensed")
                    ? 0
                    : 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static bool AddProjectToSolution(string sourceSolutionPath, string sourceConfigPlatform,
            string sourceProjectName, string targetSolutionPath, string targetConfigPlatform)
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

        private static string SwitchConfigPlatform(string originalKey, string newConfigPlatform)
        {
            var parts = originalKey.Split('.');
            parts[1] = newConfigPlatform;
            var newKey = string.Join(".", parts);

            return newKey;
        }
    }
}
