using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// <see cref="FilesystemTreeView"/> for FTP support.
    /// </summary>
    public class RemoteTreeView : FilesystemTreeView {
        private static readonly TimeSpan day = TimeSpan.FromDays(1), hour = TimeSpan.FromHours(1);

        /// <summary>
        /// Cancel the queue.
        /// </summary>
        bool cancel;
        /// <summary>
        /// The active download.
        /// </summary>
        FileDownload downloader;
        /// <summary>
        /// Filesystem browser used to mark the target download location.
        /// </summary>
        FilesystemTreeView localFilesystem;
        /// <summary>
        /// FTP credentials for downloads.
        /// </summary>
        NetworkCredential credentials;
        /// <summary>
        /// Name of the currently downloading file.
        /// </summary>
        string downloading;

        /// <summary>
        /// Set the local filesystem browser used in pair with this view. The files will be downloaded there.
        /// </summary>
        public void SetupDownload(FilesystemTreeView localFilesystem, NetworkCredential credentials) {
            this.localFilesystem = localFilesystem;
            this.credentials = credentials;
        }

        /// <summary>
        /// Cancel the running download if one is in progress.
        /// </summary>
        public void CancelCurrent() {
            if (downloader != null) {
                downloader.Cancel();
            }
        }

        public void CancelAll() {
            CancelCurrent();
            cancel = true;
        }

        /// <summary>
        /// Progress reporting for the currently downloading file.
        /// </summary>
        void DownloadProgress(double progress, DateTime started) {
            taskEngine.UpdateProgressBar(progress);
            TimeSpan elapsed = DateTime.Now - started, estimatedDownloadTime = elapsed / progress, remaining = estimatedDownloadTime - elapsed;
            string remDisp;
            if (remaining < day) {
                if (remaining < hour) {
                    remDisp = remaining.ToString("mm':'ss");
                } else {
                    remDisp = remaining.ToString("h':'mm':'ss");
                }
            } else {
                remDisp = remaining.ToString("d':'hh':'mm':'ss");
            }
            taskEngine.UpdateStatusLazy(string.Format("Downloading ({0}, {1} remaining) {2}...", progress.ToString("0.00%"),
                remDisp, downloading));
        }

        /// <summary>
        /// Download an FTP entry to a local path.
        /// </summary>
        /// <param name="entry">Remote file entry</param>
        /// <param name="outputPath">Full output path and file name</param>
        void ManualDownload(TreeEntry entry, string outputPath) {
            if (File.Exists(outputPath) && new FileInfo(outputPath).Length == entry.Size) {
                return; // Skip already downloaded files
            }
            downloader = new FileDownload(entry.RequestURI, outputPath, credentials) {
                reporter = DownloadProgress
            };
            downloading = entry.Name;
            downloader.Download();
            taskEngine.UpdateStatus(string.Format("Downloaded {0}.", downloading));
        }

        /// <summary>
        /// Download a server or folder recursively.
        /// </summary>
        /// <param name="item">Remote filesystem entry to download, null means root</param>
        /// <param name="outputPath">Folder output on the local filesystem, should end with a backslash</param>
        /// <param name="after">Only download files after this date</param>
        void ManualDownloadRecursive(FilesystemItem item, string outputPath, DateTime after) {
            if (cancel) {
                return;
            }
            TreeEntry entry;
            ItemCollection items = null;
            if (item == null) {
                entry = new TreeEntry {
                    IsDirectory = true
                };
                items = Items;
            } else {
                entry = null;
                taskEngine.UpdateProgressBar(.5);
                Dispatcher.Invoke(() => {
                    entry = (TreeEntry)item.Header;
                    if (entry.IsDirectory) {
                        taskEngine.UpdateStatus(string.Format("Expanding {0}...", entry));
                    }
                    item.IsExpanded = true;
                    items = item.Items;
                });
            }
            if (entry.IsDirectory) {
                outputPath = string.Format("{0}{1}\\", outputPath, entry.Name);
                for (int i = 0, c = items.Count; i < c; ++i) {
                    ManualDownloadRecursive((FilesystemItem)items[i], outputPath, after);
                }
            } else {
                if (entry.LastModified < after) {
                    return;
                }
                new DirectoryInfo(outputPath).Create();
                ManualDownload(entry, outputPath + entry.Name);
            }
            Dispatcher.Invoke(() => {
                if (item != null && item.IsExpanded) {
                    item.IsExpanded = false;
                }
            });
        }

        /// <summary>
        /// Download the selected file when double clicked.
        /// </summary>
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e) {
            if (SelectedItem == null) {
                return;
            }
            TreeEntry entry = SelectedEntry, targetParent = localFilesystem.SelectedEntry;
            if (targetParent == null) {
                MessageBox.Show("Please select the output folder on the local filesystem browser, then retry this download.");
                return;
            }
            if (!entry.IsDirectory) {
                cancel = false;
                taskEngine.Run(() => ManualDownload(entry, string.Format("{0}\\{1}", targetParent.URI, downloading = entry.Name)));
            }
            base.OnMouseDoubleClick(e);
        }

        /// <summary>
        /// Download all files from the server last modified after the given date.
        /// </summary>
        public void DownloadAllAfter(DateTime after) {
            TreeEntry output = localFilesystem.SelectedEntry;
            if (output == null) {
                MessageBox.Show("Please select the output folder on the local filesystem browser, then retry this download.");
                return;
            }
            cancel = false;
            ManualDownloadRecursive(null, string.Format(output.URI + '\\'), after);
            if (!cancel) {
                taskEngine.UpdateProgressBar(1);
                taskEngine.UpdateStatus("Latest files downloaded.");
            } else {
                taskEngine.UpdateStatus("Process cancelled.");
            }
        }
    }
}