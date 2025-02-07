using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KeyboardApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var settingsWindow = new SettingsWindow();
            settingsWindow.Close();
        }

        private void OnKeyClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var editWindow = new EditKeyWindow();
                if (editWindow.ShowDialog() == true)
                {
                    button.Content = editWindow.PressedKey;
                }
            }
        }

        private bool CheckForDuplicates()
        {
            var buttonContents = GetSpecificButtonContents();

            var allKeys = new List<string>();
            foreach (var row in buttonContents)
            {
                allKeys.AddRange(row);
            }

            var seen = new HashSet<string>();
            bool isDuplicate = false;

            foreach (var key in allKeys)
            {
                if (seen.Contains(key))
                {
                    isDuplicate = true;
                    MessageBox.Show($"Duplicate key found: {key}", "Duplicate Key Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return isDuplicate;
                }
                else
                {
                    seen.Add(key);
                }
            }

            return isDuplicate;
        }
        private void ResetLayout(object sender, RoutedEventArgs e)
        {
            string[] defaultLayout = new string[]
            {
                "Esc", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", // Row 1
                "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Backspace",     // Row 2
                "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\",         // Row 3
                "Caps", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter",         // Row 4
                "Shift", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift",             // Row 5
                "Ctrl", "Alt", "Space", "Alt", "Ctrl"                                           // Row 6
            };

            for (int i = 0; i <= 70; i++)
            {
                string buttonName = $"btn_{i:D2}";
                var button = FindName(buttonName) as Button;
                if (button != null && i < defaultLayout.Length)
                {
                    button.Content = defaultLayout[i];
                }
            }
        }
        private string[][] GetSpecificButtonContents()
        {
            int[][] ranges = new int[][]
            {
                new int[] { 28, 37 },
                new int[] { 42, 51 },
                new int[] { 55, 64 } 
            };

            string[][] buttonContents = new string[ranges.Length][];

            for (int row = 0; row < ranges.Length; row++)
            {
                int start = ranges[row][0];
                int end = ranges[row][1];

                var rowContents = new List<string>();
                for (int i = start; i <= end; i++)
                {
                    string buttonName = $"btn_{i:D2}";
                    var button = FindName(buttonName) as Button;
                    if (button != null)
                    {
                        rowContents.Add(button.Content.ToString());
                    }
                }

                buttonContents[row] = rowContents.ToArray();
            }
            return buttonContents;
        }
        private void DisplayBestLayout(string[][] bestLayout)
        {
            int[][] buttonIndexes = new int[][]
            {
        new int[] { 28, 37 }, // Górny rząd
        new int[] { 42, 51 }, // Środkowy rząd
        new int[] { 55, 64 }  // Dolny rząd
            };

            for (int row = 0; row < buttonIndexes.Length; row++)
            {
                for (int col = 0; col < bestLayout[row].Length; col++)
                {
                    int buttonIndex = buttonIndexes[row][col];
                    string buttonName = $"btn_{buttonIndex:D2}";
                    var button = FindName(buttonName) as Button;

                    if (button != null)
                    {
                        button.Content = bestLayout[row][col];
                    }
                }
            }

            MessageBox.Show("Best layout has been displayed on the main screen.", "Best Layout", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void LogLayout(string[][] layout, StringBuilder logContent)
        {
            foreach (var row in layout)
            {
                logContent.AppendLine(string.Join(" ", row));
            }
            logContent.AppendLine();
        }

        private void EvaluateLayout(object sender, RoutedEventArgs e)
        {
            var isDuplicate = CheckForDuplicates();
            if (isDuplicate) return;

            var buttonContents = GetSpecificButtonContents();

            StringBuilder aggregatedData = new StringBuilder();
            for (int i = 0; i < buttonContents.Length; i++)
            {
                aggregatedData.AppendLine($"Row {i + 1}: {string.Join(" ", buttonContents[i])}");
            }

            string corpusFilePath = SettingsWindow.SelectedCorpusFilePath;
            if (!File.Exists(corpusFilePath))
            {
                MessageBox.Show("Corpus file not found. Please select a valid corpus in settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string corpusContent = File.ReadAllText(corpusFilePath);
            var frequencyData = EvaluationAlgorithm.AnalyzeCorpusFrequency(corpusContent);
            var bigramData = EvaluationAlgorithm.AnalyzeCorpusBigrams(corpusContent);

            StringBuilder debugBigramInfo = new StringBuilder();
            double totalEffort = EvaluationAlgorithm.EvaluateKeyboardEffort(buttonContents, frequencyData, bigramData, debugBigramInfo);

            aggregatedData.AppendLine();
            aggregatedData.AppendLine($"Total Effort (including bigram penalties if enabled): {totalEffort:F2}");

            string logFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardEvaluation.log");
            using (StreamWriter writer = new StreamWriter(logFilePath, false))
            {
                writer.WriteLine("Keyboard Layout Evaluation Log");
                writer.WriteLine("==============================");
                writer.WriteLine(aggregatedData.ToString());
                writer.WriteLine();
                writer.WriteLine(debugBigramInfo.ToString());
            }

            MessageBox.Show($"Evaluation results saved to log file:\n{logFilePath}", "Evaluation Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
            }
        }
        private void GenerateLayout(object sender, RoutedEventArgs e)
        {
            int populationSize = SettingsWindow.PopulationSize;
            int generationCount = SettingsWindow.GenerationCount;
            double mutationRate = SettingsWindow.MutationRate;
            int numParentsSelected = SettingsWindow.NumParentsSelected;
            string selectedAlgorithm = SettingsWindow.SelectedAlgorithm;
            string selectedMutation = SettingsWindow.SelectedMutationMethod;
            string selectedCrossover = SettingsWindow.SelectedCrossoverMethod;
            int elitismCount = SettingsWindow.ElitismCount;

            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("Evolution Process Log");
            logContent.AppendLine("==============================");

            GenerationAlgorithms.GenerateInitialPopulation(populationSize);

            for (int generation = 0; generation < generationCount; generation++)
            {
                logContent.AppendLine($"\nGeneration {generation + 1}");
                logContent.AppendLine("----------------------------");

                List<string[][]> selectedParents = SelectionAlgorithms.SelectParents(selectedAlgorithm, numParentsSelected, GenerationAlgorithms.KeyboardPopulation);

                List<string[][]> offspring = CrossoverAlgorithms.ApplyCrossover(selectedParents, selectedCrossover);

                offspring = MutationAlgorithms.ApplyMutation(offspring, mutationRate, selectedMutation);

                List<string[][]> nextGeneration = new List<string[][]>();

                if (elitismCount > 0)
                {
                    var elites = GenerationAlgorithms.KeyboardPopulation
                        .OrderBy(layout =>
                        {
                            StringBuilder debugInfo = new StringBuilder();
                            return EvaluationAlgorithm.EvaluateKeyboardEffort(layout,
                                EvaluationAlgorithm.AnalyzeCorpusFrequency(SettingsWindow.CorpusContent),
                                EvaluationAlgorithm.AnalyzeCorpusBigrams(SettingsWindow.CorpusContent), debugInfo);
                        })
                        .Take(elitismCount)
                        .ToList();

                    nextGeneration.AddRange(elites);
                }
                nextGeneration.AddRange(offspring);

                GenerationAlgorithms.KeyboardPopulation = nextGeneration;

                var bestLayout = GenerationAlgorithms.KeyboardPopulation
                    .OrderBy(layout =>
                    {
                        StringBuilder debugInfo = new StringBuilder();
                        return EvaluationAlgorithm.EvaluateKeyboardEffort(layout,
                            EvaluationAlgorithm.AnalyzeCorpusFrequency(SettingsWindow.CorpusContent),
                            EvaluationAlgorithm.AnalyzeCorpusBigrams(SettingsWindow.CorpusContent), debugInfo);
                    })
                    .First();

                logContent.AppendLine("Best Layout:");
                LogLayout(bestLayout, logContent);
            }
            var finalBestLayout = GenerationAlgorithms.KeyboardPopulation
                .OrderBy(layout =>
                {
                    StringBuilder debugInfo = new StringBuilder();
                    return EvaluationAlgorithm.EvaluateKeyboardEffort(layout,
                        EvaluationAlgorithm.AnalyzeCorpusFrequency(SettingsWindow.CorpusContent),
                        EvaluationAlgorithm.AnalyzeCorpusBigrams(SettingsWindow.CorpusContent), debugInfo);
                })
                .First();

            DisplayBestLayout(finalBestLayout);

            System.IO.File.WriteAllText("EvolutionProcess.log", logContent.ToString());

            MessageBox.Show("Evolution completed! The best layout is displayed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }



    }
}