using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Falconne.SolutionTools
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        static void AddProjectToSolution(string sourceSolutionName, 
            string sourceProjectName, string targetSolutionName)
        {
            var sourceSolution = Onion.SolutionParser.Parser.SolutionParser.Parse(sourceSolutionName);
            var sourceProject = sourceSolution.Projects.Where(p => p.Name.Equals(sourceSolutionName));
        }
    }
}
