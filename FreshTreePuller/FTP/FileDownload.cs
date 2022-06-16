﻿using System;
using System.IO;
using System.Net;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// FTP file download handler.
    /// </summary>
    public class FileDownload {
        /// <summary>
        /// Timeout length of a query in milliseconds.
        /// </summary>
        const int timeout = 10000;

        /// <summary>
        /// Called after every block write. Ratio of transferred and total data.
        /// </summary>
        public Action<double, DateTime> reporter;

        readonly string uri;
        readonly string saveAs;
        readonly NetworkCredential credentials;
        readonly long size;

        bool cancel;

        /// <summary>
        /// FTP file download handler.
        /// </summary>
        /// <param name="uri">Source file URI</param>
        /// <param name="saveAs">Target local file path and name</param>
        /// <param name="credentials">FTP login data</param>
        public FileDownload(string uri, string saveAs, NetworkCredential credentials) {
            this.uri = uri;
            this.saveAs = saveAs;
            WebRequest request = WebRequest.Create(uri);
            request.Credentials = this.credentials = credentials;
            request.Method = WebRequestMethods.Ftp.GetFileSize;
            request.Timeout = timeout;
            size = request.GetResponse().ContentLength;
        }

        /// <summary>
        /// Cancel the download in progress.
        /// </summary>
        public void Cancel() => cancel = true;

        /// <summary>
        /// Download the set file.
        /// </summary>
        public void Download() {
            WebRequest request = WebRequest.Create(uri);
            request.Credentials = credentials;
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Timeout = timeout;
            using Stream ftp = request.GetResponse().GetResponseStream();
            FileStream file = File.Create(saveAs);
            byte[] buffer = new byte[10240];
            int read;
            DateTime started = DateTime.Now;
            while (true) {
                try {
                    read = ftp.Read(buffer, 0, buffer.Length);
                } catch {
                    break;
                }
                if (read <= 0)
                    break;
                if (cancel) {
                    file.Close();
                    File.Delete(saveAs);
                    return;
                }
                file.Write(buffer, 0, read);
                reporter?.Invoke(file.Position / (double)size, started);
            }
            reporter?.Invoke(1, started);
            file.Close();
        }
    }
}