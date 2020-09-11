using System.Collections.Generic;
using System.Windows.Controls;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// Crawler-based versatile <see cref="TreeView"/>.
    /// </summary>
    public class FilesystemTreeView : TreeView {
        /// <summary>
        /// Progress reporter and job handler.
        /// </summary>
        public TaskEngine taskEngine;

        /// <summary>
        /// The currently selected <see cref="TreeEntry"/> in this view.
        /// </summary>
        public TreeEntry SelectedEntry => Dispatcher.Invoke(() => (TreeEntry)((FilesystemItem)SelectedItem)?.Header);

        /// <summary>
        /// Set the crawler which contains the target filesystem information and root files.
        /// </summary>
        public void SetCrawler(Crawler crawler) {
            Items.Clear();
            List<TreeEntry> result = crawler.ListFolder();
            foreach (TreeEntry entry in result)
                Items.Add(new FilesystemItem(crawler, entry));
        }

        /// <summary>
        /// Expand the entire filesystem in the background.
        /// </summary>
        public void ExpandAll() {
            for (int i = 0, c = Items.Count; i < c; ++i) {
                double progress = i / (double)c;
                taskEngine.UpdateProgressBar(progress);
                taskEngine.UpdateStatusLazy(string.Format("Expanding folder {0} of {1} ({2})...", i + 1, c, progress.ToString("0.00%")));
                ((FilesystemItem)Items[i]).Dispatcher.Invoke(() => ((FilesystemItem)Items[i]).ExpandSubtree());
            }
            taskEngine.UpdateProgressBar(1);
            taskEngine.UpdateStatus("All folders expanded.");
        }
    }
}