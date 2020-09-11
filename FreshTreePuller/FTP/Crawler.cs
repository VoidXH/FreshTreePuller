using System.Collections.Generic;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// Filesystem crawling info.
    /// </summary>
    public abstract class Crawler {
        /// <summary>
        /// Get the contents of a folder in this filesystem.
        /// </summary>
        public abstract List<TreeEntry> ListFolder(TreeEntry folder = null);
    }
}