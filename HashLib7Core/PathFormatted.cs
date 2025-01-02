using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib7
{
    /// <summary>
    /// Represents a path broken into database format
    /// </summary>
    internal class PathFormatted
    {
        /// <summary>
        /// The database-ready form of the first 255 chars
        /// </summary>
        public readonly string part1;
        /// <summary>
        /// The database-ready form of the second 255 chars (if required)
        /// </summary>
        public readonly string part2;
        public PathFormatted(string filename)
        {
            if (filename.Length > 510)
                throw new Exception(String.Format("File path is too long: {0}", filename));
            if (filename.Length > 255)
            {
                part1 = filename.Substring(0, 255);
                part2 = filename.Substring(255, filename.Length - 255);
                part2 = part2.Replace("'", "''");
            }
            else
            {
                part1 = filename;
                part2 = "";
            }
            part1 = part1.Replace("'", "''");
        }

    }
}
