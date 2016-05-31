using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using Onion.SolutionParser.Parser.Model;

namespace Falconne.SolutionTools
{
    using OnionProject = Onion.SolutionParser.Parser.Model.Project;

    public class Solution
    {
        public Solution(string path)
        {
            Path = path;
            var onionSolution = Onion.SolutionParser.Parser.SolutionParser.Parse(path);

            Global = onionSolution.Global.ToList();
            var root = System.IO.Path.GetDirectoryName(Path);
            if (onionSolution.Projects.Any(op => op.ProjectSection?.Type == ProjectSectionType.PreProject))
                throw new Exception("Unsupported project pre-section");

            Projects = onionSolution.Projects.Select(op => new Project(op, root)).ToList();
        }

        public Project GetProject(string name)
        {
            return Projects.FirstOrDefault(p => p.Onion.Name.ToLower().Equals(name.ToLower()));
        }

        public IEnumerable<Project> GetFlattenedRelatedProjectList(Project root)
        {
            var rootInSolution = Projects.FirstOrDefault(op => op.ProjectGuid == root.ProjectGuid);
            if (rootInSolution == null)
                throw new Exception($"{root.Path} not found in {Path}");

            yield return rootInSolution;
            foreach (var dependency in root.GetDependencies())
            {
                foreach (var depProject in GetFlattenedRelatedProjectList(dependency))
                {
                    yield return depProject;
                }
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetBuildConfigsFor(Project project,
            string configPlatform)
        {
            var bcSection = GetBuildConfigSection();

            var configPlatformSearch = $"{{{project.ProjectGuid}}}.{configPlatform.ToLower()}.";
            var relevantConfigs = bcSection.Where(
                pair => pair.Key.ToLower().Contains(configPlatformSearch));

            return relevantConfigs;
        }

        public void AddProject(Project project)
        {
            if (Projects.Any(p => p.Equals(project)))
                return;

            Projects.Add(project);
        }

        public void AddBuildConfigs(IEnumerable<KeyValuePair<string, string>> configs)
        {
            var bcSection = GetBuildConfigSection();
            foreach (var pair in configs)
            {
                if (!bcSection.Any(bcs => bcs.Key.Equals(pair.Key)))
                    bcSection.Add(pair);
            }
        }

        public IDictionary<string, string> GetBuildConfigSection()
        {
            return Global.First(
                gs => gs.Name == "ProjectConfigurationPlatforms" &&
                    gs.Type == GlobalSectionType.PostSolution).Entries;
        }

        public void Save()
        {
            var result = new StringBuilder();
            result.Append(@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2013
VisualStudioVersion = 12.0.31101.0
MinimumVisualStudioVersion = 10.0.40219.1
");
            foreach (var project in Projects)
            {
                result.Append(MakeProjectEntry(project));
            }

            result.AppendLine("Global");
            foreach (var section in Global)
            {
                result.AppendLine($"\tGlobalSection({section.Name}) = {GetAsCamelCase(section.Type.ToString())}");
                foreach (var entry in section.Entries)
                {
                    result.AppendLine($"\t\t{entry.Key} = {entry.Value}");
                }
                result.AppendLine("\tEndGlobalSection");
            }
            result.AppendLine("EndGlobal");

            File.WriteAllText(Path, result.ToString());
        }

        private string MakeProjectEntry(Project project)
        {
            var onion = project.Onion;
            var relPath = project.GetRelativePathInSolution(Path);
            var result = new StringBuilder();
            result.AppendLine(
                $"Project(\"{{{project.GetTypeGuidString()}}}\") = \"{onion.Name}\", \"{relPath}\", \"{{{project.GetGuidString()}}}\"");

            if (onion.ProjectSection != null)
            {
                result.Append(MakeProjectSection(onion));
            }

            result.AppendLine("EndProject");

            return result.ToString();
        }

        private static string MakeProjectSection(OnionProject project)
        {
            var result = new StringBuilder();
            result.AppendLine(
                $"\tProjectSection({project.ProjectSection.Name}) = {GetAsCamelCase(project.ProjectSection.Type.ToString())}");

            foreach (var entry in project.ProjectSection.Entries)
            {
                result.AppendLine($"\t\t{entry.Key} = {entry.Value}");
            }

            result.AppendLine("\tEndProjectSection");

            return result.ToString();
        }

        private static string GetAsCamelCase(string typeName)
        {
            return char.ToLower(typeName[0]) + typeName.Substring(1);
        }

        public IList<GlobalSection> Global { get; set; }
        public IList<Project> Projects { get; set; }

        private string Path;
    }
}
