using System.IO;
using System.Windows;

namespace KeyboardApp
{
    public partial class SummaryWindow : Window
    {
        private double[] generationGraphEffort;
        private string summaryLogFilePath; 
        public SummaryWindow(double[] generationEffort, string logFilePath)
        {
            InitializeComponent();
            PopulateSummary();
            generationGraphEffort = generationEffort;
            summaryLogFilePath = logFilePath;
        }
        private void OpenLogFile(object sender, RoutedEventArgs e)
        {
            string logFilePath = summaryLogFilePath;
            if (File.Exists(logFilePath))
            {
                System.Diagnostics.Process.Start("notepad.exe", logFilePath);
            }
            else
            {
                MessageBox.Show("Log file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ViewEffortGraph(object sender, RoutedEventArgs e)
        {
            if (generationGraphEffort != null && generationGraphEffort.Length > 0)
            {
                // Open the effort graph window with the effort data
                EffortGraphWindow effortGraphWindow = new EffortGraphWindow(generationGraphEffort);
                effortGraphWindow.Show();
            }
            else
            {
                MessageBox.Show("No effort data available to display the graph.", "Effort Graph", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void PopulateSummary()
        {
            // Wypełnij dane na podstawie statycznych właściwości z SettingsWindow

            CorpusTextBlock.Text = SettingsWindow.SelectedCorpusFilePath;
            DistanceMetricTextBlock.Text = SettingsWindow.IsDistanceMetricEnabled ? "True" : "False";
            HandBalanceMetricTextBlock.Text = SettingsWindow.IsHandBalanceMetricEnabled ? "True" : "False";
            RowSwitchMetricTextBlock.Text = SettingsWindow.IsRowSwitchMetricEnabled ? "True" : "False";
            TypingStylePreferenceTextBlock.Text = SettingsWindow.SelectedOptimizationPattern;
            GenerationCountTextBlock.Text = SettingsWindow.GenerationCount.ToString();
            PopulationSizeTextBlock.Text = SettingsWindow.PopulationSize.ToString();
            ElitismCountTextBlock.Text = SettingsWindow.ElitismCount.ToString();
            MutationRateTextBlock.Text = SettingsWindow.MutationRate.ToString();
            SelectionMethodTextBlock.Text = SettingsWindow.SelectedAlgorithm;
            ParentsSelectedTextBlock.Text = SettingsWindow.NumParentsSelected.ToString();
            MutationTextBlock.Text = SettingsWindow.SelectedMutationMethod;
            CrossoverTextBlock.Text = SettingsWindow.SelectedCrossoverMethod;
            ButtonLockingTextBlock.Text = SettingsWindow.IsButtonLockEnabled ? "True" : "False";
        }
    }
}
