using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace FreshTreePuller.FTP {
    /// <summary>
    /// A general file entry.
    /// Linux listing based on https://stackoverflow.com/questions/1013486/parsing-ftpwebrequest-listdirectorydetails-line
    /// Windows listing based on
    /// https://stackoverflow.com/questions/7060983/c-sharp-class-to-parse-webrequestmethods-ftp-listdirectorydetails-ftp-response
    /// </summary>
    public class TreeEntry {
        /// <summary>
        /// Parser for one line of Unix servers' list results.
        /// </summary>
        readonly static Regex unixParser =
            new Regex(@"^([\w-]+)\s+(\d+)\s+(.+)\s+(.+)\s+(\d+)\s+(\w+\s+\d+\s+\d+|\w+\s+\d+\s+\d+:\d+)\s+(.+)$"),
            windowsParser = new Regex(@"^(\d+-\d+-\d+\s+\d+:\d+(?:AM|PM))\s+(<DIR>|\d+)\s+(.+)$");
        /// <summary>
        /// Number format used by Unix servers.
        /// </summary>
        readonly static IFormatProvider culture = CultureInfo.GetCultureInfo("en-us");
        /// <summary>
        /// Possible date-time formats in Unix servers' list results.
        /// </summary>
        readonly static string[] dateTimeFormats = new[] { "MMM dd HH:mm", "MMM dd H:mm", "MMM d HH:mm", "MMM d H:mm" };
        /// <summary>
        /// Possible date formats in Unix servers' list results.
        /// </summary>
        readonly static string[] dateFormats = new[] { "MMM dd yyyy", "MMM d yyyy" };

        /// <summary>
        /// This entry is for a folder.
        /// </summary>
        public bool IsDirectory { get; set; }
        /// <summary>
        /// FTP permissions in string format.
        /// </summary>
        public string Permission { get; set; }
        /// <summary>
        /// FTP filecode in string format.
        /// </summary>
        public string Filecode { get; set; }
        /// <summary>
        /// File owner.
        /// </summary>
        public string Owner { get; set; }
        /// <summary>
        /// File group.
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// Filename.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Parent folder path with a closing slash.
        /// </summary>
        public string ParentDirectory { get; set; }
        /// <summary>
        /// Last modification time.
        /// </summary>
        public DateTime LastModified { get; set; }
        /// <summary>
        /// Full URI for this entry.
        /// </summary>
        public string URI => ParentDirectory + Name;
        /// <summary>
        /// Full URI for this entry in request-compatible format.
        /// </summary>
        public string RequestURI => string.Format(IsDirectory ? "{0}{1}/" : "{0}{1}", ParentDirectory, Uri.EscapeDataString(Name));

        /// <summary>
        /// Parse the partial timestamp from Unix servers' list lines.
        /// </summary>
        void ParseUnixTime(string timeString) {
            if (!string.IsNullOrWhiteSpace(timeString)) {
                if (timeString.Contains(':'))
                    LastModified = DateTime.ParseExact(timeString, dateTimeFormats, culture, DateTimeStyles.None);
                else
                    LastModified = DateTime.ParseExact(timeString, dateFormats, culture, DateTimeStyles.None);
            }
        }

        /// <summary>
        /// Create a <see cref="TreeEntry"/> from a server's list line.
        /// </summary>
        /// <param name="listLine">Source string to parse</param>
        /// <param name="parentDirectory">Parent directory URI in request-compatible format</param>
        /// <param name="credentials">FTP server credentials in case additional file info is needed</param>
        public static TreeEntry CreateFromListLine(string listLine, string parentDirectory, NetworkCredential credentials) {
            Match match = unixParser.Match(listLine);
            string timeString = null;
            TreeEntry entry;
            if (match.Success) {
                entry = new TreeEntry() {
                    IsDirectory = match.Groups[1].Value[0] == 'd',
                    Permission = match.Groups[1].Value.Substring(1),
                    Filecode = match.Groups[2].Value,
                    Owner = match.Groups[3].Value,
                    Group = match.Groups[4].Value,
                    Size = long.Parse(match.Groups[5].Value, culture),
                    Name = match.Groups[7].Value,
                    ParentDirectory = parentDirectory
                };
                timeString = Regex.Replace(match.Groups[6].Value, @"\s+", " ");
            } else {
                match = windowsParser.Match(listLine);
                bool isDir = match.Groups[2].Value != "<DIR>";
                entry = new TreeEntry() {
                    IsDirectory = isDir,
                    Name = match.Groups[3].Value,
                    LastModified = DateTime.ParseExact(match.Groups[1].Value, "MM-dd-yy  hh:mmtt", culture, DateTimeStyles.None)
                };
                if (isDir)
                    entry.Size = long.Parse(match.Groups[2].Value);
            }
            if (!entry.IsDirectory) {
                try {
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(entry.RequestURI);
                    request.Credentials = credentials;
                    request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                    using FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                    entry.LastModified = response.LastModified;
                } catch {
                    entry.ParseUnixTime(timeString);
                }
            } else
                entry.ParseUnixTime(timeString);
            return entry;
        }

        /// <summary>
        /// Display the name of this entry.
        /// </summary>
        public override string ToString() => Name;
    }
}