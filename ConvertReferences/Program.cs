using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ConvertReferences
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("usage: ConvertReferences [project|static] <solution file>");
                return;
            }

            var projects = Project.FromSolutionFile(args[1]);

            switch (args[0])
            {
                case "project":
                    ToProjectRefs(projects);
                    break;

                case "static":
                    ToStaticRefs(projects);
                    break;
            }
        }


        static void ToProjectRefs(IEnumerable<Project> projects)
        {
            var projectsByOutputPath = projects.ToDictionary(x => x.OutputPath);

            foreach (var project in projectsByOutputPath.Values)
            {
                var newFile = (XElement)VisitConvertToProject(project.ProjectFile, project, projectsByOutputPath);
                newFile.Save(project.ProjectPath);
            }
        }


        static void ToStaticRefs(IEnumerable<Project> projects)
        {
            var projectsByProjectFile = projects.ToDictionary(x => x.ProjectPath);

            foreach (var project in projectsByProjectFile.Values)
            {
                var newFile = (XElement)VisitConvertToStatic(project.ProjectFile, project, projectsByProjectFile);
                newFile.Save(project.ProjectPath);
            }
        }


        static XNode VisitConvertToProject(XNode node, Project currentProject, IDictionary<string, Project> projectsByOutputPath)
        {
            var ns = $"{{{currentProject.ProjectFile.GetDefaultNamespace().NamespaceName}}}";

            if (node is XElement element)
            {
                if (element.Name == $"{ns}Reference")
                {
                    var refPath = element.Elements($"{ns}HintPath").SingleOrDefault()?.Value;

                    if (refPath != null)
                    {
                        refPath = currentProject.GetAbsolutePath(refPath);

                        if (projectsByOutputPath.TryGetValue(refPath, out var refProject))
                        {
                            return new XElement(
                                $"{ns}ProjectReference",
                                new XAttribute($"Include", currentProject.GetRelativePath(refProject.ProjectPath)),
                                new XElement($"{ns}Name", refProject.Name),
                                new XElement($"{ns}Project", refProject.Guid.ToString("B"))
                            );
                        }
                    }
                }

                return new XElement(
                    element.Name,
                    Enumerable.Union<object>(
                        element.Attributes(),
                        element.Nodes().Select(x => VisitConvertToProject(x, currentProject, projectsByOutputPath))
                    )
                );
            }
            return node;
        }


        static XNode VisitConvertToStatic(XNode node, Project currentProject, IDictionary<string, Project> projectsByProjectFile)
        {
            var ns = $"{{{currentProject.ProjectFile.GetDefaultNamespace().NamespaceName}}}";

            if (node is XElement element)
            {
                if (element.Name == $"{ns}ProjectReference")
                {
                    var projectFile = element.Attributes($"Include").Single().Value;
                    projectFile = currentProject.GetAbsolutePath(projectFile);

                    if (projectsByProjectFile.TryGetValue(projectFile, out var refProject))
                    {
                        return new XElement(
                            $"{ns}Reference",
                            new XAttribute($"Include", refProject.Name),
                            new XElement($"{ns}HintPath", currentProject.GetRelativePath(refProject.OutputPath))
                        );
                    }
                }

                return new XElement(
                    element.Name,
                    Enumerable.Union<object>(
                        element.Attributes(),
                        element.Nodes().Select(x => VisitConvertToStatic(x, currentProject, projectsByProjectFile))
                    )
                );
            }
            return node;
        }
    }
}
