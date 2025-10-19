using System;

namespace HashLib7
{
    /// <summary>
    /// Represents a path broken into database format
    /// </summary>
    public class PathFormatted
    {
        public readonly string path;
        public readonly string name;
        public string Name => this.name;
        public readonly string fullName;
        public PathFormatted(string filename)
        {
            int slashPos = filename.LastIndexOf('\\');
            if (slashPos == 0)
                throw new ArgumentException(String.Format("Path lacks a drive letter: '{0}'", filename));
            if (slashPos < 0)
                throw new ArgumentException(String.Format("Path lacks a backslash: '{0}'", filename));
            path = filename[..(slashPos)];
            name = filename[(slashPos + 1)..];
            if (path.Length > 512)
                throw new ArgumentException(String.Format("File path is too long: '{0}'", filename));
            if (name.Length > 255)
                throw new ArgumentException(String.Format("File name is too long: '{0}'", filename));
            fullName = filename;
        }

        public PathFormatted(string path, string name)
        {
            if (path.Length > 512)
                throw new ArgumentException(String.Format("File path is too long: '{0}'", path));
            if (name.Length > 255)
                throw new ArgumentException(String.Format("File name is too long: '{0}'", name));
            this.path = path;
            this.name = name;
            fullName = String.Format("{0}\\{1}", path, name);
        }

        public override string ToString()
        {
            return fullName;
        }
    }
}
