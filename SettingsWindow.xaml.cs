using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        public static bool IsButtonLockEnabled { get; private set; }
        public static string SelectedOptimizationPattern { get; private set; }
        public static string SelectedAlgorithm { get; private set; }
        public static string SelectedMutationMethod { get; private set; }
        public static string SelectedCrossoverMethod { get; private set; }
        public static int GenerationCount { get; private set; }
        public static int PopulationSize { get; private set; }
        public static int ElitismCount { get; private set; }
        public static double MutationRate { get; private set; }
        public static int NumParentsSelected { get; set; }
        

        public static string CorpusContent
        {
            get
            {
                if (!string.IsNullOrEmpty(SelectedCorpusFilePath) && File.Exists(SelectedCorpusFilePath))
                {
                    return File.ReadAllText(SelectedCorpusFilePath);
                }
                return string.Empty;
            }
        }

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCorpusList();
            LoadSettingsFromConfig();
        }

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
                cmbCorpusList.SelectedIndex = 0;
            }
        }

        private void LoadSettingsFromConfig()
        {
            SelectedCorpusFilePath = ConfigurationManager.AppSettings["SelectedCorpusFilePath"];
            IsDistanceMetricEnabled = bool.Parse(ConfigurationManager.AppSettings["IsDistanceMetricEnabled"] ?? "false");
            IsHandBalanceMetricEnabled = bool.Parse(ConfigurationManager.AppSettings["IsHandBalanceMetricEnabled"] ?? "false");
            IsRowSwitchMetricEnabled = bool.Parse(ConfigurationManager.AppSettings["IsRowSwitchMetricEnabled"] ?? "false");
            SelectedOptimizationPattern = ConfigurationManager.AppSettings["SelectedOptimizationPattern"] ?? "No Preference";
            SelectedAlgorithm = ConfigurationManager.AppSettings["SelectedAlgorithm"] ?? "Roulette";
            SelectedMutationMethod = ConfigurationManager.AppSettings["SelectedMutationMethod"] ?? "Reverse Sequence Mutation";
            SelectedCrossoverMethod = ConfigurationManager.AppSettings["SelectedCrossoverMethod"] ?? "Placeholder";
            IsButtonLockEnabled = bool.Parse(ConfigurationManager.AppSettings["IsButtonLockEnabled"] ?? "false");

            GenerationCount = int.Parse(ConfigurationManager.AppSettings["GenerationCount"] ?? "100");
            PopulationSize = int.Parse(ConfigurationManager.AppSettings["PopulationSize"] ?? "50");
            ElitismCount = int.Parse(ConfigurationManager.AppSettings["ElitismCount"] ?? "5");
            MutationRate = double.TryParse(ConfigurationManager.AppSettings["MutationRate"], NumberStyles.Float, CultureInfo.InvariantCulture, out double mutation) ? mutation : 0.02;
            NumParentsSelected = int.TryParse(ConfigurationManager.AppSettings["NumParentsSelected"], out int tSize) ? tSize : 3;

            chkButtonLock.IsChecked = IsButtonLockEnabled;
            cmbCorpusList.SelectedItem = Path.GetFileName(SelectedCorpusFilePath);
            chkDistanceMetric.IsChecked = IsDistanceMetricEnabled;
            chkHandBalanceMetric.IsChecked = IsHandBalanceMetricEnabled;
            chkRowSwitchMetric.IsChecked = IsRowSwitchMetricEnabled;

            txtGenerationCount.Text = GenerationCount.ToString();
            txtPopulationSize.Text = PopulationSize.ToString();
            txtElitismCount.Text = ElitismCount.ToString();
            txtMutationRate.Text = MutationRate.ToString(CultureInfo.InvariantCulture);
            txtNumParentsSelected.Text = NumParentsSelected.ToString();

            SetComboBoxSelection(cmbOptimizationPattern, SelectedOptimizationPattern);
            SetComboBoxSelection(cmbAlgorithm, SelectedAlgorithm);
            SetComboBoxSelection(cmbMutation, SelectedMutationMethod);
            SetComboBoxSelection(cmbCrossoverMethod, SelectedCrossoverMethod);
        }

        private void SetComboBoxSelection(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            comboBox.SelectedIndex = 0;
        }

        private void RefreshCorpusList(object sender, RoutedEventArgs e)
        {
            LoadCorpusList();
            MessageBox.Show("Corpus list refreshed.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            if (cmbCorpusList.SelectedItem == null)
            {
                MessageBox.Show("Please select a corpus before saving.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string selectedCorpus = cmbCorpusList.SelectedItem.ToString();
            SelectedCorpusFilePath = Path.Combine(corpusDirectory, selectedCorpus);

            IsDistanceMetricEnabled = chkDistanceMetric.IsChecked == true;
            IsHandBalanceMetricEnabled = chkHandBalanceMetric.IsChecked == true;
            IsRowSwitchMetricEnabled = chkRowSwitchMetric.IsChecked == true;
            IsButtonLockEnabled = chkButtonLock.IsChecked == true;

            SelectedOptimizationPattern = ((ComboBoxItem)cmbOptimizationPattern.SelectedItem)?.Content?.ToString();
            SelectedAlgorithm = ((ComboBoxItem)cmbAlgorithm.SelectedItem)?.Content?.ToString();
            SelectedMutationMethod = ((ComboBoxItem)cmbMutation.SelectedItem)?.Content?.ToString();
            SelectedCrossoverMethod = ((ComboBoxItem)cmbCrossoverMethod.SelectedItem)?.Content?.ToString();

            GenerationCount = int.TryParse(txtGenerationCount.Text, out int genCount) ? genCount : 100;
            PopulationSize = int.TryParse(txtPopulationSize.Text, out int popSize) ? popSize : 50;
            ElitismCount = int.TryParse(txtElitismCount.Text, out int elitism) ? elitism : 5;
            MutationRate = double.TryParse(txtMutationRate.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double mutation) ? mutation : 0.02;
            NumParentsSelected = int.TryParse(txtNumParentsSelected.Text, out int tSize) ? tSize : 3;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            UpdateConfigValue(config, "SelectedCorpusFilePath", SelectedCorpusFilePath);
            UpdateConfigValue(config, "IsDistanceMetricEnabled", IsDistanceMetricEnabled.ToString());
            UpdateConfigValue(config, "IsHandBalanceMetricEnabled", IsHandBalanceMetricEnabled.ToString());
            UpdateConfigValue(config, "IsRowSwitchMetricEnabled", IsRowSwitchMetricEnabled.ToString());
            UpdateConfigValue(config, "SelectedOptimizationPattern", SelectedOptimizationPattern);
            UpdateConfigValue(config, "SelectedAlgorithm", SelectedAlgorithm);
            UpdateConfigValue(config, "SelectedMutationMethod", SelectedMutationMethod);
            UpdateConfigValue(config, "SelectedCrossoverMethod", SelectedCrossoverMethod);
            UpdateConfigValue(config, "GenerationCount", GenerationCount.ToString());
            UpdateConfigValue(config, "PopulationSize", PopulationSize.ToString());
            UpdateConfigValue(config, "ElitismCount", ElitismCount.ToString());
            UpdateConfigValue(config, "MutationRate", MutationRate.ToString(CultureInfo.InvariantCulture));
            UpdateConfigValue(config, "NumParentsSelected", NumParentsSelected.ToString());
            UpdateConfigValue(config, "IsButtonLockEnabled", IsButtonLockEnabled.ToString());

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            this.DialogResult = true;
        }
        private void UpdateConfigValue(Configuration config, string key, string value)
        {
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }
        }
        private void CancelSettings(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]*$");
        }

        private void OnPasteNumeric(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string clipboardText = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(clipboardText, "^[0-9]*$") || int.Parse(clipboardText) > 100)
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void MutationRateTextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtMutationRate.Text, out int value))
            {
                if (value < 0) value = 0;
                if (value > 100) value = 100;
                txtMutationRate.Text = value.ToString();
            }
        }
    }
}
