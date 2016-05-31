using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Project)obj);
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
            if (!File.Exists(path))
                throw new Exception($"File not found: {path}");

            Path = path;
            var doc = XDocument.Load(Path);
            ProjectGuid = new Guid(doc.Descendants().First(d => d.Name.LocalName == "ProjectGuid").Value);
        }

        public Project(Onion.SolutionParser.Parser.Model.Project onionProject, string solutionRoot)
        {
            Onion = onionProject;
            ProjectGuid = onionProject.Guid;
            Path = IsFolder() ? onionProject.Path : System.IO.Path.Combine(solutionRoot, onionProject.Path);
        }

        public IEnumerable<Project> GetDependencies()
        {
            var projectDirectory = System.IO.Path.GetDirectoryName(Path);
            var doc = XDocument.Load(Path);
            var projectReferences = doc.Descendants().Where(d => d.Name.LocalName == "ProjectReference");
            foreach (var projectReference in projectReferences)
            {
                var refPath = projectReference.Attribute("Include").Value;
                var refRealPath = System.IO.Path.Combine(projectDirectory, refPath);
                if (!File.Exists(refRealPath))
                    continue;

                yield return new Project(refRealPath);
            }
        }

        public string GetRelativePathInSolution(string solutionRoot)
        {
            if (IsFolder())
                return Path;

            var fullPath = new Uri(Path, UriKind.Absolute);
            var relRoot = new Uri(solutionRoot, UriKind.Absolute);
            var relPath = relRoot.MakeRelativeUri(fullPath).ToString();

            return relPath.Replace("/", "\\");
        }

        public bool IsFolder()
        {
            return Onion.TypeGuid.Equals(new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8"));
        }

        public string GetGuidString()
        {
            return ProjectGuid.ToString().ToUpper();
        }

        public string GetTypeGuidString()
        {
            return Onion.TypeGuid.ToString().ToUpper();
        }

        public Guid ProjectGuid { get; private set; }
        public string Path { get; private set; }
        public Onion.SolutionParser.Parser.Model.Project Onion;
    }
}
