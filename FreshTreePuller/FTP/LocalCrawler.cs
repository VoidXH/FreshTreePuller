using System;
using System.Collections.Generic;
using System.IO;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// Local filesystem crawling info.
    /// </summary>
    public class LocalCrawler : Crawler {
        /// <summary>
        /// Get the contents of a folder in this filesystem.
        /// </summary>
        public override List<TreeEntry> ListFolder(TreeEntry folder = null) {
            List<TreeEntry> result = new List<TreeEntry>();
            if (folder != null) {
                string path = folder.URI;
                if (!path.EndsWith('\\')) {
                    path += '\\';
                }
                string[] folders;
                try {
                    folders = Directory.GetDirectories(path);
                } catch (UnauthorizedAccessException e) {
                    return new List<TreeEntry>(new TreeEntry[1] { new TreeEntry() { Name = e.Message } });
                }
                for (int i = 0; i < folders.Length; ++i) {
                    result.Add(new TreeEntry() {
                        IsDirectory = true,
                        Name = Path.GetFileName(folders[i]),
                        ParentDirectory = path
                    });
                }
            } else {
                string[] drives = Directory.GetLogicalDrives();
                for (int i = 0; i < drives.Length; ++i) {
                    result.Add(new TreeEntry() {
                        IsDirectory = true,
                        Name = drives[i],
                        ParentDirectory = string.Empty
                    });
                }
            }
            return result;
        }
    }
}