using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Onion.SolutionParser.Parser;
using Onion.SolutionParser.Parser.Model;

namespace Falconne.SolutionTools
{
    using OnionProject = Onion.SolutionParser.Parser.Model.Project;

    public class Solution
    {
        public Solution(string path)
        {
            _path = path;
            try
            {
                var onionSolution = SolutionParser.Parse(path);

                Global = onionSolution.Global.ToList();
                var root = Path.GetDirectoryName(_path);

                Projects = onionSolution.Projects.Select(op => new Project(op, root)).ToList();

            }
            catch (Exception)
            {
                Console.Error.WriteLine($"Error parsing {path}. Check the syntax of the file.");
                throw;
            }
        }

        public Project GetProject(string name)
        {
            return Projects.FirstOrDefault(p => p.Onion.Name.ToLower().Equals(name.ToLower()));
        }

        public IEnumerable<Project> GetFlattenedRelatedProjectList(Project root)
        {
            var rootInSolution = Projects.FirstOrDefault(op => op == root);
            if (rootInSolution == null)
                throw new Exception($"{root.Path} not found in {_path}");

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
            foreach (var pair in configs)
            {
                AddBuildConfig(pair);
            }
        }

        public void AddBuildConfig(KeyValuePair<string, string> pair)
        {
            var bcSection = GetBuildConfigSection();
            if (!bcSection.Any(bcs => bcs.Key.Equals(pair.Key)))
                bcSection.Add(pair);
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

            var content = File.ReadAllLines(_path);
            foreach (var line in content.TakeWhile(l => !l.Contains("Project(")))
            {
                result.AppendLine(line);
            }

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

            File.WriteAllText(_path, result.ToString());
        }

        private string MakeProjectEntry(Project project)
        {
            var relPath = project.GetRelativePathInSolution(_path);
            var result = new StringBuilder();
            result.AppendLine(
                $"Project(\"{{{project.TypeGuid.ToString().ToUpper()}}}\") = \"{project.Name}\", \"{relPath}\", \"{{{project.GetGuidString()}}}\"");

            result.Append(MakeProjectSection(project.GetProjectSectionIfExists()));

            result.AppendLine("EndProject");

            return result.ToString();
        }

        private static string MakeProjectSection(ProjectSection section)
        {
            if (section == null)
                return "";

            var result = new StringBuilder();
            result.AppendLine(
                $"\tProjectSection({section.Name}) = {GetAsCamelCase(section.Type.ToString())}");

            foreach (var entry in section.Entries)
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

        private readonly string _path;
    }
}
