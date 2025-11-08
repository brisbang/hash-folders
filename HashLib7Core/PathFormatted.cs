using System;
using Microsoft.Graph.Identity.CustomAuthenticationExtensions.ValidateAuthenticationConfiguration;

namespace HashLib7
{
    /// <summary>
    /// Represents a path broken into database format
    /// </summary>
    public class PathFormatted
    {
        // Use read-only properties so WPF data binding can find them (bindings work with properties, not fields)
        public string Path { get; }
        public string Name { get; }
        public string FullName { get; }

        public PathFormatted(string filename)
        {
            FullName = filename;
            int slashPos = filename.LastIndexOf('\\');
            if (slashPos == 0)
                throw new ArgumentException(String.Format("Path lacks a drive letter: '{0}'", filename));
            if (slashPos < 0)
                throw new ArgumentException(String.Format("Path lacks a backslash: '{0}'", filename));
            Path = filename[..(slashPos)];
            Name = filename[(slashPos + 1)..];
            Validate();
        }

        private void Validate()
        {
            if (Path.Length > 512)
                throw new ArgumentException(String.Format("File path is too long: '{0}'", Path));
            if (Name.Length > 255)
                throw new ArgumentException(String.Format("File name is too long: '{0}'", Name));
        }

        public PathFormatted(string path, string name)
        {
            if (path.Length > 512)
                throw new ArgumentException(String.Format("File path is too long: '{0}'", path));
            if (name.Length > 255)
                throw new ArgumentException(String.Format("File name is too long: '{0}'", name));
            this.Path = path;
            this.Name = name;
            FullName = String.Format("{0}\\{1}", path, name);
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
