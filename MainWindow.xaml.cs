using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace KeyboardApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ProgressWindow progressWindow;
        private Thread progressThread;
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
            if (bestLayout == null || bestLayout.Length == 0)
            {
                MessageBox.Show("No valid layout to display.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Definiujemy indeksy odpowiadające przyciskom w UI
            int[][] buttonIndexes = new int[][]
            {
        new int[] { 28, 29, 30, 31, 32, 33, 34, 35, 36, 37 }, // Górny rząd
        new int[] { 42, 43, 44, 45, 46, 47, 48, 49, 50, 51 }, // Środkowy rząd
        new int[] { 55, 56, 57, 58, 59, 60, 61, 62, 63, 64 }  // Dolny rząd
            };

            if (bestLayout.Length != buttonIndexes.Length)
            {
                MessageBox.Show("Mismatch between best layout and button index mapping.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            for (int row = 0; row < bestLayout.Length; row++)
            {
                if (bestLayout[row].Length != buttonIndexes[row].Length)
                {
                    MessageBox.Show($"Row {row + 1} has incorrect number of keys.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                for (int col = 0; col < bestLayout[row].Length; col++)
                {
                    int buttonIndex = buttonIndexes[row][col];

                    string buttonName = $"btn_{buttonIndex:D2}";
                    var button = FindName(buttonName) as Button;

                    if (button != null)
                    {
                        button.Content = bestLayout[row][col];
                    }
                    else
                    {
                        MessageBox.Show($"Button {buttonName} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            MessageBox.Show("Optimization process complete.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

            // Wykonanie analizy korpusu jednokrotnie
            EvaluationAlgorithm.ClearCache();
            string corpusContent = File.ReadAllText(corpusFilePath);
            EvaluationAlgorithm.PrecomputeCorpusAnalysis(corpusContent);

            // Ocena układu klawiatury na podstawie wcześniej obliczonych wartości
            StringBuilder debugBigramInfo = new StringBuilder();
            double totalEffort = EvaluationAlgorithm.EvaluateKeyboardEffort(buttonContents, debugBigramInfo);

            aggregatedData.AppendLine();
            aggregatedData.AppendLine($"Total Effort (including bigram penalties if enabled): {totalEffort:F2}");

            // Logowanie wyników
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardEvaluation.log");
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
            ShowProgressWindow();
            UpdateProgress("Initializing optimization...");

            int populationSize = SettingsWindow.PopulationSize;
            int generationCount = SettingsWindow.GenerationCount;
            double mutationRate = SettingsWindow.MutationRate;
            int numParentsSelected = SettingsWindow.NumParentsSelected;
            string selectedAlgorithm = SettingsWindow.SelectedAlgorithm;
            string selectedMutation = SettingsWindow.SelectedMutationMethod;
            string selectedCrossover = SettingsWindow.SelectedCrossoverMethod;
            int elitismCount = SettingsWindow.ElitismCount;
            string corpusFilePath = SettingsWindow.SelectedCorpusFilePath;
            EvaluationAlgorithm.ClearCache();

            // 🔹 Sprawdzenie, czy plik korpusu istnieje
            if (!File.Exists(corpusFilePath))
            {
                MessageBox.Show("Corpus file not found. Please select a valid corpus in settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string corpusContent = File.ReadAllText(corpusFilePath);
            EvaluationAlgorithm.PrecomputeCorpusAnalysis(corpusContent);

            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("=======================================");
            logContent.AppendLine("          OPTIMIZATION PROCESS         ");
            logContent.AppendLine("=======================================\n");

            // 🔹 Logowanie analizy korpusu (znaki i ich częstotliwości)
            logContent.AppendLine("Corpus Character Frequencies:");
            logContent.AppendLine("---------------------------------------");

            foreach (var entry in EvaluationAlgorithm.publicCorpusFrequencyData.OrderByDescending(x => x.Value))
            {
                logContent.AppendLine($"Character: '{entry.Key}'  |  Frequency: {entry.Value}");
            }

            File.WriteAllText("CorpusAnalysis.log", logContent.ToString()); // Logowanie do osobnego pliku

            UpdateProgress("Corpus analysis completed.");

            GenerationAlgorithms.GenerateInitialPopulation(populationSize);
            UpdateProgress("Generation 1: Population initialized");

            for (int generation = 0; generation < generationCount; generation++)
            {
                logContent.AppendLine($"\nGENERATION {generation + 1}");
                logContent.AppendLine("---------------------------------------");

                //UpdateProgress($"Generation {generation + 1}: Evaluating layouts...");

                var populationEffort = GenerationAlgorithms.KeyboardPopulation
                    .Select(layout => (layout, effort: EvaluationAlgorithm.EvaluateKeyboardEffort(layout, new StringBuilder())))
                    .OrderBy(x => x.effort)
                    .ToList();

                for (int i = 0; i < populationEffort.Count; i++)
                {
                    logContent.AppendLine($"Layout {i + 1}: Effort = {populationEffort[i].effort:F4}");
                    LogLayout(populationEffort[i].layout, logContent);
                }

                logContent.AppendLine("\nBest Layout of this Generation:");
                LogLayout(populationEffort.First().layout, logContent);

                UpdateProgress($"Generation {generation + 1}   Best effort {populationEffort[0].effort}");
                List<string[][]> selectedParents = SelectionAlgorithms.SelectParents(selectedAlgorithm, numParentsSelected, GenerationAlgorithms.KeyboardPopulation);

                //UpdateProgress($"Generation {generation + 1}: Performing crossover...");
                List<string[][]> offspring = CrossoverAlgorithms.ApplyCrossover(selectedParents, selectedCrossover);

                //UpdateProgress($"Generation {generation + 1}: Applying mutation...");
                offspring = MutationAlgorithms.ApplyMutation(offspring, mutationRate, selectedMutation);

                //UpdateProgress($"Generation {generation + 1}: Creating new population...");
                List<string[][]> nextGeneration = new List<string[][]>();

                if (elitismCount > 0)
                {
                    var elites = populationEffort.Take(elitismCount).Select(x => x.layout).ToList();
                    nextGeneration.AddRange(elites);
                }
                nextGeneration.AddRange(offspring);

                while (nextGeneration.Count < populationSize)
                {
                    nextGeneration.Add(GenerationAlgorithms.GenerateRandomKeyboardLayout(new Random()));
                }

                GenerationAlgorithms.KeyboardPopulation = nextGeneration;
            }

            UpdateProgress("Finalizing best layout...");
            var finalBestLayout = GenerationAlgorithms.KeyboardPopulation
                .OrderBy(layout => EvaluationAlgorithm.EvaluateKeyboardEffort(layout, new StringBuilder()))
                .First();

            logContent.AppendLine("\n=======================================");
            logContent.AppendLine("FINAL BEST LAYOUT:");
            logContent.AppendLine("=======================================");
            LogLayout(finalBestLayout, logContent);

            CloseProgressWindow();

            File.WriteAllText("OptimizationLog.log", logContent.ToString());

            DisplayBestLayout(finalBestLayout);
        }




        private void UpdateProgress(string status)
        {
            if (progressWindow != null)
            {
                progressWindow.Dispatcher.Invoke(() => progressWindow.UpdateStatus(status));
            }
        }


        private void CloseProgressWindow()
        {
            if (progressWindow != null)
            {
                progressWindow.Dispatcher.Invoke(() =>
                {
                    if (progressWindow.IsVisible)
                    {
                        progressWindow.Close();
                    }
                });

                if (!progressWindow.Dispatcher.HasShutdownStarted)
                {
                    progressWindow.Dispatcher.InvokeShutdown();
                }

                progressWindow = null; 
            }

            if (progressThread != null && progressThread.IsAlive)
            {
                progressThread.Join();
                progressThread = null;
            }
        }

        private void ShowProgressWindow()
        {
            if (progressThread != null && progressThread.IsAlive)
            {
                return;
            }

            progressThread = new Thread(() =>
            {
                progressWindow = new ProgressWindow();
                progressWindow.Loaded += (s, e) => progressWindow.UpdateStatus("Initializing...");
                progressWindow.Show();

                System.Windows.Threading.Dispatcher.Run();
            });

            progressThread.SetApartmentState(ApartmentState.STA);
            progressThread.IsBackground = true;
            progressThread.Start();
        }
    }
}