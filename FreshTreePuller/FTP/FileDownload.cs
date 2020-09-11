using System;
using System.IO;
using System.Net;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// FTP file download handler.
    /// </summary>
    public class FileDownload {
        /// <summary>
        /// Called after every block write. Ratio of transferred and total data.
        /// </summary>
        public Action<double, DateTime> reporter;

        readonly string uri;
        readonly string saveAs;
        readonly NetworkCredential credentials;
        readonly long size;

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
            size = request.GetResponse().ContentLength;
        }

        /// <summary>
        /// Download the set file.
        /// </summary>
        public void Download() {
            WebRequest request = WebRequest.Create(uri);
            request.Credentials = credentials;
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            using Stream ftp = request.GetResponse().GetResponseStream(), file = File.Create(saveAs);
            byte[] buffer = new byte[10240];
            int read;
            DateTime started = DateTime.Now;
            while ((read = ftp.Read(buffer, 0, buffer.Length)) > 0) {
                file.Write(buffer, 0, read);
                reporter?.Invoke(file.Position / (double)size, started);
            }
            reporter?.Invoke(1, started);
        }
    }
}