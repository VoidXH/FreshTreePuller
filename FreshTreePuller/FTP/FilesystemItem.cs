using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// Visual representation of a <see cref="TreeEntry"/>.
    /// </summary>
    public class FilesystemItem : TreeViewItem {
        /// <summary>
        /// Crawler of the container filesystem.
        /// </summary>
        readonly Crawler crawler;

        /// <summary>
        /// Visual representation of a <see cref="TreeEntry"/>.
        /// </summary>
        public FilesystemItem(Crawler crawler, TreeEntry entry) : base() {
            this.crawler = crawler;
            Header = entry;
            if (entry.IsDirectory) {
                Items.Add(new FilesystemItem(null, new TreeEntry() { Name = "Loading..." }));
            }
        }

        /// <summary>
        /// Callback when the user expands an entry, creates entries for all contained files of folders.
        /// </summary>
        protected override void OnExpanded(RoutedEventArgs e) {
            TreeEntry treeEntry = (TreeEntry)((TreeViewItem)e.OriginalSource).Header;
            if (!treeEntry.IsDirectory) {
                return;
            }
            Items.Clear();
            List<TreeEntry> result = crawler.ListFolder(treeEntry);
            foreach (TreeEntry entry in result) {
                Items.Add(new FilesystemItem(crawler, entry));
            }
            base.OnExpanded(e);
        }
    }
}