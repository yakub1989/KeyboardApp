using System;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace KeyboardApp
{
    public partial class SettingsWindow : Window
    {
        private readonly string corpusDirectory = ".\\Corpus";

        // Public static properties for settings
        public static string SelectedCorpusFilePath { get; private set; }
        public static bool IsDistanceMetricEnabled { get; private set; }
        public static bool IsHandBalanceMetricEnabled { get; private set; }
        public static bool IsRowSwitchMetricEnabled { get; private set; }
        public static bool IsThumbUsageMetricEnabled { get; private set; }
        public static string SelectedOptimizationPattern { get; private set; }
        public static string SelectedAlgorithm { get; private set; }
        public static string SelectedCrossoverMethod { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCorpusList();
            LoadSettingsFromConfig();
        }

        // Load corpus list from the directory
        private void LoadCorpusList()
        {
            cmbCorpusList.Items.Clear();

            if (!Directory.Exists(corpusDirectory))
            {
                Directory.CreateDirectory(corpusDirectory);
            }

            var files = Directory.GetFiles(corpusDirectory, "*.txt");
            foreach (var file in files)
            {
                cmbCorpusList.Items.Add(Path.GetFileName(file));
            }

            if (cmbCorpusList.Items.Count > 0)
            {
                cmbCorpusList.SelectedIndex = 0; // Select the first item by default
            }
        }

        private void LoadSettingsFromConfig()
        {
            // Load settings from appsettings.config
            SelectedCorpusFilePath = ConfigurationManager.AppSettings["SelectedCorpusFilePath"];
            IsDistanceMetricEnabled = bool.Parse(ConfigurationManager.AppSettings["IsDistanceMetricEnabled"] ?? "false");
            IsHandBalanceMetricEnabled = bool.Parse(ConfigurationManager.AppSettings["IsHandBalanceMetricEnabled"] ?? "false");
            IsRowSwitchMetricEnabled = bool.Parse(ConfigurationManager.AppSettings["IsRowSwitchMetricEnabled"] ?? "false");
            IsThumbUsageMetricEnabled = bool.Parse(ConfigurationManager.AppSettings["IsThumbUsageMetricEnabled"] ?? "false");
            SelectedOptimizationPattern = ConfigurationManager.AppSettings["SelectedOptimizationPattern"];
            SelectedAlgorithm = ConfigurationManager.AppSettings["SelectedAlgorithm"];
            SelectedCrossoverMethod = ConfigurationManager.AppSettings["SelectedCrossoverMethod"];

            // Set the values in the UI
            cmbCorpusList.SelectedItem = Path.GetFileName(SelectedCorpusFilePath);
            chkDistanceMetric.IsChecked = IsDistanceMetricEnabled;
            chkHandBalanceMetric.IsChecked = IsHandBalanceMetricEnabled;
            chkRowSwitchMetric.IsChecked = IsRowSwitchMetricEnabled;
            chkThumbUsageMetric.IsChecked = IsThumbUsageMetricEnabled;
            cmbOptimizationPattern.SelectedItem = SelectedOptimizationPattern;
            cmbAlgorithm.SelectedItem = SelectedAlgorithm;
            cmbCrossoverMethod.SelectedItem = SelectedCrossoverMethod;
        }

        // Refresh the corpus list
        private void RefreshCorpusList(object sender, RoutedEventArgs e)
        {
            LoadCorpusList();
            MessageBox.Show("Corpus list refreshed.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Save settings
        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            if (cmbCorpusList.SelectedItem == null)
            {
                MessageBox.Show("Please select a corpus before saving.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Save values from settings to static properties
            string selectedCorpus = cmbCorpusList.SelectedItem.ToString();
            SelectedCorpusFilePath = Path.Combine(corpusDirectory, selectedCorpus);

            IsDistanceMetricEnabled = chkDistanceMetric.IsChecked == true;
            IsFingerUsageMetricEnabled = chkFingerUsageMetric.IsChecked == true;
            IsHandBalanceMetricEnabled = chkHandBalanceMetric.IsChecked == true;
            IsRowSwitchMetricEnabled = chkRowSwitchMetric.IsChecked == true;
            IsThumbUsageMetricEnabled = chkThumbUsageMetric.IsChecked == true;

            SelectedOptimizationPattern = ((ComboBoxItem)cmbOptimizationPattern.SelectedItem)?.Content?.ToString();
            SelectedAlgorithm = ((ComboBoxItem)cmbAlgorithm.SelectedItem)?.Content?.ToString();
            SelectedCrossoverMethod = ((ComboBoxItem)cmbCrossoverMethod.SelectedItem)?.Content?.ToString();

            // Save settings to appsettings.config
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["SelectedCorpusFilePath"].Value = SelectedCorpusFilePath;
            config.AppSettings.Settings["IsDistanceMetricEnabled"].Value = IsDistanceMetricEnabled.ToString();
            config.AppSettings.Settings["IsFingerUsageMetricEnabled"].Value = IsFingerUsageMetricEnabled.ToString();
            config.AppSettings.Settings["IsHandBalanceMetricEnabled"].Value = IsHandBalanceMetricEnabled.ToString();
            config.AppSettings.Settings["IsRowSwitchMetricEnabled"].Value = IsRowSwitchMetricEnabled.ToString();
            config.AppSettings.Settings["IsThumbUsageMetricEnabled"].Value = IsThumbUsageMetricEnabled.ToString();
            config.AppSettings.Settings["SelectedOptimizationPattern"].Value = SelectedOptimizationPattern;
            config.AppSettings.Settings["SelectedAlgorithm"].Value = SelectedAlgorithm;
            config.AppSettings.Settings["SelectedCrossoverMethod"].Value = SelectedCrossoverMethod;

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            MessageBox.Show("Settings saved to configuration file.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }

        // Cancel settings
        private void CancelSettings(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
