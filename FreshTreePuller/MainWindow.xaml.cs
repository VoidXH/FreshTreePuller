using FreshTreePuller.FTP;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Windows;

using Preset = FreshTreePuller.PresetDB.Preset;

namespace FreshTreePuller {
    /// <summary>
    /// Interaction logic for FreshTreePuller.
    /// </summary>
    public partial class MainWindow : Window {
        readonly PresetDB presetData = new();
        readonly TaskEngine taskEngine = new();

        /// <summary>
        /// Update the display list of presets.
        /// </summary>
        void ResetPresets() {
            presets.Items.Clear();
            foreach (Preset preset in presetData)
                presets.Items.Add(preset);
            if (presets.Items.Count != 0)
                presets.SelectedIndex = 0;
        }

        /// <summary>
        /// Initialize the main window and load user settings.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            NameValueCollection settings = ConfigurationManager.AppSettings;
            server.Text = settings["server"] ?? string.Empty;
            user.Text = settings["user"] ?? string.Empty;
            password.Password = settings["password"] ?? string.Empty;
            if (settings["getAfter"] != null)
                getAfter.SelectedDate = new DateTime(long.Parse(settings["getAfter"]));
            hours.Value = int.Parse(settings["hours"] ?? "12");
            minutes.Value = int.Parse(settings["minutes"] ?? "0");
            presetData.Deserialize("FTP Presets.xml");
            ResetPresets();
            taskEngine.SetProgressReporting(progressBar, progressLabel);
            local.SetCrawler(new LocalCrawler());
            remote.taskEngine = taskEngine;
        }

        /// <summary>
        /// Connect to the selected FTP server.
        /// </summary>
        void Connect(object _, RoutedEventArgs e) {
            NetworkCredential credentials = new NetworkCredential(user.Text, password.Password);
            remote.SetCrawler(new RemoteCrawler(server.Text, credentials));
            remote.SetupDownload(local, credentials);
        }

        /// <summary>
        /// Set a setting or create its entry if it doesn't exist.
        /// </summary>
        static void SetSetting(KeyValueConfigurationCollection settings, string key, string value) {
            if (settings[key] == null)
                settings.Add(key, value);
            else
                settings[key].Value = value;
        }

        /// <summary>
        /// Enter the data of the selected preset to the connection fields.
        /// </summary>
        void LoadPreset(object _, RoutedEventArgs e) {
            if (presets.SelectedItem == null)
                return;
            Preset preset = (Preset)presets.SelectedItem;
            server.Text = preset.server;
            user.Text = preset.username;
            password.Password = preset.password;
        }

        /// <summary>
        /// Save the connection fields' data to a preset.
        /// </summary>
        void SavePreset(object _, RoutedEventArgs e) {
            Preset preset = new Preset(server.Text, user.Text, password.Password);
            presetData.Add(preset);
            ResetPresets();
            presets.SelectedItem = preset;
        }

        /// <summary>
        /// Delete a saved preset.
        /// </summary>
        void DeletePreset(object _, RoutedEventArgs e) {
            Preset preset = (Preset)presets.SelectedItem;
            presetData.Remove(preset.server, preset.username);
            ResetPresets();
        }

        /// <summary>
        /// Download all files from the server last modified after <see cref="getAfter"/>.
        /// </summary>
        void DownloadAllAfter(object _, RoutedEventArgs e) {
            if (getAfter.SelectedDate.HasValue) {
                DateTime after = getAfter.SelectedDate.Value.AddMinutes(hours.Value * 60 + minutes.Value);
                taskEngine.Run(() => {
                    remote.DownloadAllAfter(after);
                    Dispatcher.Invoke(() => {
                        getAfter.SelectedDate = DateTime.Today;
                        DateTime now = DateTime.Now;
                        hours.Value = now.Hour;
                        minutes.Value = now.Minute;
                    });
                });
            }
        }

        /// <summary>
        /// Cancel the running download if one is in progress.
        /// </summary>
        void NextFile(object _, RoutedEventArgs e) {
            if (!taskEngine.IsOperationRunning)
                MessageBox.Show("This button skips a download. There is no download in progress.");
            remote.CancelCurrent();
        }

        /// <summary>
        /// Cancel current and queued downloads.
        /// </summary>
        void CancelOperation(object _, RoutedEventArgs e) {
            if (!taskEngine.IsOperationRunning)
                MessageBox.Show("This button cancels all queued downloads. There is no download in progress.");
            remote.CancelAll();
        }

        /// <summary>
        /// Save all data before exiting.
        /// </summary>
        void Window_Closed(object _, EventArgs e) {
            Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = configFile.AppSettings.Settings;
            SetSetting(settings, "server", server.Text);
            SetSetting(settings, "user", user.Text);
            SetSetting(settings, "password", password.Password);
            if (getAfter.SelectedDate.HasValue)
                SetSetting(settings, "getAfter", getAfter.SelectedDate.Value.Ticks.ToString());
            SetSetting(settings, "hours", hours.Value.ToString());
            SetSetting(settings, "minutes", minutes.Value.ToString());
            configFile.Save(ConfigurationSaveMode.Modified);
            presetData.Serialize("FTP Presets.xml");
        }
    }
}