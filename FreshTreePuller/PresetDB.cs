using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace FreshTreePuller {
    /// <summary>
    /// Saved FTP login information collection.
    /// </summary>
    public class PresetDB : ICollection {
        /// <summary>
        /// Stored FTP login information.
        /// </summary>
        public struct Preset : IComparable<Preset> {
            /// <summary>
            /// FTP host URI.
            /// </summary>
            public string server;
            /// <summary>
            /// Login username.
            /// </summary>
            public string username;
            /// <summary>
            /// Password for <see cref="username"/>.
            /// </summary>
            public string password;

            /// <summary>
            /// The last time the files were successfully downloaded from this server.
            /// </summary>
            public DateTime lastDownload;

            /// <summary>
            /// Stored FTP login information.
            /// </summary>
            public Preset(string server, string username, string password) {
                this.server = server;
                this.username = username;
                this.password = password;
                lastDownload = DateTime.MinValue;
            }

            /// <summary>
            /// Preset comparator.
            /// </summary>
            public int CompareTo(Preset other) {
                int result = string.Compare(server, other.server);
                if (result != 0) {
                    return result;
                } else {
                    return string.Compare(username, other.username);
                }
            }

            /// <summary>
            /// Display name of the preset.
            /// </summary>
            public override string ToString() => server + '/' + username;
        }

        /// <summary>
        /// List of all presets in this database.
        /// </summary>
        public List<Preset> Presets { get; private set; } = new List<Preset>();

        /// <summary>
        /// Get the preset at the given index in this database.
        /// </summary>
        public Preset this[int index] {
            get => Presets[index];
            set => Presets[index] = value;
        }

        /// <summary>
        /// Number of contained presets.
        /// </summary>
        public int Count => Presets.Count;

        /// <summary>
        /// This collection is based on <see cref="List{T}"/>, and it's not thread-safe.
        /// </summary>
        public bool IsSynchronized => false;

        /// <summary>
        /// Object used for synchronization.
        /// </summary>
        public object SyncRoot => this;

        /// <summary>
        /// Copy the presets to another array.
        /// </summary>
        public void CopyTo(Array array, int index) => Presets.CopyTo((Preset[])array, index);

        /// <summary>
        /// Get an <see cref="IEnumerator"/> for the contained presets.
        /// </summary>
        public IEnumerator GetEnumerator() => Presets.GetEnumerator();

        /// <summary>
        /// Add an existing preset to the collection.
        /// </summary>
        public void Add(Preset preset) {
            Remove(preset.server, preset.username);
            Presets.Add(preset);
            Presets.Sort();
        }

        /// <summary>
        /// Add a new preset to the collection.
        /// </summary>
        public void Add(string server, string username, string password) => Add(new Preset(server, username, password));

        /// <summary>
        /// Remove a preset from the collection.
        /// </summary>
        public void Remove(string server, string username) {
            for (int i = 0, count = Presets.Count; i < count; ++i) {
                if (string.Equals(Presets[i].server, server) && string.Equals(Presets[i].username, username)) {
                    Presets.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Export the collection to an XML file.
        /// </summary>
        public void Serialize(string fileName) {
            XmlSerializer serializer = new(typeof(PresetDB));
            TextWriter writer = new StreamWriter(fileName);
            serializer.Serialize(writer, this);
        }

        /// <summary>
        /// Read the collection from an XML file.
        /// </summary>
        public void Deserialize(string fileName) {
            if (!File.Exists(fileName)) {
                return;
            }
            XmlSerializer serializer = new(typeof(PresetDB));
            FileStream reader = new(fileName, FileMode.Open);
            Presets = ((PresetDB)serializer.Deserialize(reader)).Presets;
        }
    }
}