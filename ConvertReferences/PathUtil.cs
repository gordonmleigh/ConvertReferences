using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConvertReferences
{
    static class PathUtil
    {
        public static string GetAbsolutePath(string basePath, string relativePath)
        {
            // from https://stackoverflow.com/a/35218619/358336

            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            if (basePath == null)
                basePath = Path.GetFullPath(".");
            else
                basePath = GetAbsolutePath(null, basePath); // make sure basepath is also absolute

            string path;

            if (!Path.IsPathRooted(relativePath) || Path.GetPathRoot(relativePath) == Path.DirectorySeparatorChar.ToString())
            {
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    path = Path.Combine(Path.GetPathRoot(basePath), relativePath.TrimStart(Path.DirectorySeparatorChar));
                else
                    path = Path.Combine(basePath, relativePath);
            }
            else
            {
                path = relativePath;
            }

            return Path.GetFullPath(path);
        }
    }
}
