using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ConvertReferences
{
    class Project
    {
        private static readonly Regex projectRegex = new Regex(@"^Project\(""{........-....-....-....-............}""\) = ""([A-Za-z0-9]+)"", ""(.*?)"", ""({........-....-....-....-............})""", RegexOptions.Compiled);


        public static IEnumerable<Project> FromSolutionFile(string solution)
        {
            var solutionDir = Path.GetDirectoryName(solution);

            using (var reader = new StreamReader(solution))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var match = projectRegex.Match(line);

                    if (match.Success)
                    {
                        var ext = Path.GetExtension(match.Groups[2].Value);

                        if (ext == ".csproj" || ext == ".vbproj")
                        {
                            yield return new Project(PathUtil.GetAbsolutePath(solutionDir, match.Groups[2].Value));
                        }
                    }
                }
            }
        }


        private string ExtensionForOutputType(string outputType)
        {
            switch (outputType)
            {
                case "WinExe":
                    return ".exe";
                case "Library":
                    return ".dll";
                default:
                    throw new ArgumentException($"unknown output type '{outputType}'");
            }
        }


        public string Name { get; }
        public string ProjectFolder { get; }
        public string ProjectPath { get; }
        public string OutputPath { get; set; }
        public Guid Guid { get; }
        public XElement ProjectFile { get; }


        public Project(string path)
        {
            path = Path.GetFullPath(path);
            ProjectFile = XElement.Load(path);
            var ns = $"{{{ProjectFile.GetDefaultNamespace().NamespaceName}}}";
            var props = ProjectFile.Descendants($"{ns}PropertyGroup");
            var outputType = props.Elements($"{ns}OutputType").Single().Value;

            ProjectPath = path;
            Name = props.Elements($"{ns}AssemblyName").Single().Value;
            Guid = Guid.Parse(props.Elements($"{ns}ProjectGuid").Single().Value);

            ProjectFolder = Path.GetDirectoryName(ProjectPath);
            var outputPath = Path.GetFullPath(Path.Combine(ProjectFolder, props.Elements($"{ns}OutputPath").First().Value));
            OutputPath = Path.Combine(outputPath, Name + ExtensionForOutputType(outputType));
        }


        public string GetRelativePath(string path)
        {
            return Path.GetRelativePath(ProjectFolder, path);
        }

        public string GetAbsolutePath(string path)
        {
            return PathUtil.GetAbsolutePath(ProjectFolder, path);
        }
    }
}
