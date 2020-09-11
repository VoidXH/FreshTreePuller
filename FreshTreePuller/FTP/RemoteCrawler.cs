using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// Lightweight FTP navigator.
    /// Based on https://stackoverflow.com/questions/18830900/c-sharp-populate-treeview-with-ftp-server-directories
    /// </summary>
    public class RemoteCrawler : Crawler {
        /// <summary>
        /// Root path to crawl.
        /// </summary>
        public string URI { get; protected set; }
        /// <summary>
        /// FTP user that has access to <see cref="URI"/>.
        /// </summary>
        public readonly NetworkCredential credentials;

        /// <summary>
        /// Lightweight FTP navigator.
        /// </summary>
        /// <param name="uri">Root path to crawl</param>
        /// <param name="credentials">FTP server login data</param>
        public RemoteCrawler(string uri, NetworkCredential credentials) {
            URI = uri.EndsWith('/') ? uri : uri + '/';
            this.credentials = credentials;
        }

        /// <summary>
        /// Get the contents of a folder under <see cref="URI"/>.
        /// </summary>
        public override List<TreeEntry> ListFolder(TreeEntry folder) {
            string path;
            if (folder == null)
                path = URI.StartsWith("ftp://") ? URI : "ftp://" + URI;
            else
                path = folder.RequestURI;
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(path);
            request.Credentials = credentials;
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            WebResponse response;
            try {
                response = request.GetResponse();
            } catch (WebException e) {
                return new List<TreeEntry>(new TreeEntry[1] { new TreeEntry() { Name = e.Message } });
            }
            using StreamReader reader = new StreamReader(response.GetResponseStream());
            StringBuilder result = new StringBuilder();
            string line = reader.ReadLine();
            while (line != null) {
                result.Append(line).Append('\n');
                line = reader.ReadLine();
            }
            response.Close();
            if (!string.IsNullOrEmpty(result.ToString()))
                result.Remove(result.ToString().LastIndexOf("\n"), 1);
            string[] list = result.ToString().Split('\n');
            List<TreeEntry> output = new List<TreeEntry>();
            foreach (string listLine in list) {
                if (!string.IsNullOrEmpty(listLine)) {
                    TreeEntry entry = TreeEntry.CreateFromListLine(listLine, path, credentials);
                    if (!entry.Name.Equals(".") && !entry.Name.Equals(".."))
                        output.Add(entry);
                }
            }
            return output;
        }
    }
}