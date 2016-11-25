using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Onion.SolutionParser.Parser.Model;

namespace Falconne.SolutionTools
{
    public class Project
    {
        protected bool Equals(Project other)
        {
            return ProjectGuid.Equals(other.ProjectGuid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Project) obj);
        }

        public override int GetHashCode()
        {
            return ProjectGuid.GetHashCode();
        }

        public static bool operator ==(Project left, Project right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Project left, Project right)
        {
            return !Equals(left, right);
        }

        public Project(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new Exception($"File not found: {path}");

            Path = path;
            var doc = XDocument.Load(Path);
            ProjectGuid = new Guid(doc.Descendants().First(d => d.Name.LocalName == "ProjectGuid").Value);
            Name = System.IO.Path.GetFileNameWithoutExtension(Path);
            IsFolder = false;

            var extension = System.IO.Path.GetExtension(Path).ToLower();
            if (extension.Equals(".csproj"))
            {
                TypeGuid = new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
            }
            else if (extension.Equals(".vcxproj"))
            {
                TypeGuid = new Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942");
            }
            else
            {
                throw new Exception("Project type not supported for direct import.");
            }
        }

        public Project(Onion.SolutionParser.Parser.Model.Project onionProject, string solutionRoot)
        {
            Onion = onionProject;
            ProjectGuid = onionProject.Guid;
            IsFolder = Onion.TypeGuid.Equals(new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8"));
            Path = IsFolder ? onionProject.Path : System.IO.Path.Combine(solutionRoot, onionProject.Path);
            TypeGuid = Onion.TypeGuid;
            Name = Onion.Name;
        }

        public IEnumerable<Project> GetDependencies()
        {
            var projectDirectory = Pri.LongPath.Path.GetDirectoryName(Path);
            var doc = XDocument.Load(Path);
            var projectReferences = doc.Descendants().Where(d => d.Name.LocalName == "ProjectReference");
            foreach (var projectReference in projectReferences)
            {
                var refPath = projectReference.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(refPath))
                    continue;
                var refRealPath = Pri.LongPath.Path.Combine(projectDirectory, refPath);
                if (!File.Exists(refRealPath))
                    throw new Exception("Refence not found: " + refRealPath);

                yield return new Project(refRealPath);
            }
        }

        public string GetRelativePathInSolution(string solutionRoot)
        {
            if (IsFolder)
                return Path;

            var fullPath = new Uri(Path, UriKind.Absolute);
            var relRoot = new Uri(solutionRoot, UriKind.Absolute);
            var relPath = relRoot.MakeRelativeUri(fullPath).ToString();

            return relPath.Replace("/", "\\");
        }

        public string GetGuidString()
        {
            return ProjectGuid.ToString().ToUpper();
        }

        public ProjectSection GetProjectSectionIfExists()
        {
            return _onion == null ? null : Onion.ProjectSection;
        }

        public Guid ProjectGuid { get; }

        public string Path { get; }

        public Onion.SolutionParser.Parser.Model.Project Onion
        {
            private set { _onion = value; }
            get
            {
                if (_onion == null)
                    throw new Exception("Invalid access of uninitalised Onion project");

                return _onion;
            }
        }

        public bool IsFolder { get; }

        public Guid TypeGuid { get; private set; }

        public string Name { get; private set; }

        private Onion.SolutionParser.Parser.Model.Project _onion;
    }
}
