using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        private void LockKey(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Background == Brushes.Yellow)
                {
                    button.ClearValue(BackgroundProperty);
                } else
                {
                    button.Background = Brushes.Yellow;
                }
            }
        }
        private void ExportLayoutToPKL(object sender, RoutedEventArgs e)
        {
            int[][] ranges = new int[][]
           {
                new int[] { 28, 37 },
                new int[] { 42, 51 },
                new int[] { 55, 64 }
           };
            string[][] buttonMatrix = new string[ranges.Length][];
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
                    else
                    {
                        rowContents.Add("0");
                    }
                }
                buttonMatrix[row] = rowContents.ToArray();
            }
            GenerateLayoutFile(buttonMatrix);
        }
        public void GenerateLayoutFile(string[][] keyboardLayout)
        {
            // Uruchomienie dialogu "Zapisz jako"
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*",  // Ustawienie filtru dla plików .ini
                DefaultExt = "ini"  // Domyślne rozszerzenie pliku
            };

            // Jeśli użytkownik wybrał plik
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                // Tworzenie pliku
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Nagłówek
                    writer.WriteLine(";");
                    writer.WriteLine("; Keyboard Layout definition for");
                    writer.WriteLine("; Portable Keyboard Layout ");
                    writer.WriteLine("; http://pkl.sourceforge.net");
                    writer.WriteLine(";");
                    writer.WriteLine();
                    writer.WriteLine("[informations]");
                    writer.WriteLine("layoutname           = QWERTY-PL");
                    writer.WriteLine("layoutcode           = QWERTY-PL");
                    writer.WriteLine("localeid             = 00000415");  // Polski język
                    writer.WriteLine("copyright            = Jakub Jonderko");
                    writer.WriteLine("company              = ");
                    writer.WriteLine("homepage             = ");
                    writer.WriteLine("version              = 1.0");
                    writer.WriteLine();
                    writer.WriteLine("generated_at         = " + DateTime.Now.ToString("ddd MMM dd HH:mm:ss yyyy"));
                    writer.WriteLine("generated_from       = KeyboardLayoutOptimizer");
                    writer.WriteLine("modified_after_generate = yes");
                    writer.WriteLine();
                    writer.WriteLine("[global]");
                    writer.WriteLine("shiftstates = 0:1:2:6:7");
                    writer.WriteLine("img_width = 296");
                    writer.WriteLine("img_height = 102");
                    writer.WriteLine();
                    writer.WriteLine("[fingers]");
                    writer.WriteLine("row1 = 1123445567888");
                    writer.WriteLine("row2 = 1123445567888");
                    writer.WriteLine("row3 = 1123445567888");
                    writer.WriteLine("row4 = 11234455678");
                    writer.WriteLine();

                    // Sekcja layout - generowanie przypisań klawiszy
                    writer.WriteLine("[layout]");

                    // Sztywne przypisanie dla SC002 do SC00d
                    writer.WriteLine("SC002 = 1	0	1	1	--	!	¡	¹	; 1");
                    writer.WriteLine("SC003 = 2	0	2	2	--	@	º	²	; 2");
                    writer.WriteLine("SC004 = 3	0	3	3	--	#	ª	³	; 3");
                    writer.WriteLine("SC005 = 4	0	4	4	--	$	¢	£	; 4");
                    writer.WriteLine("SC006 = 5	0	5	5	--	%	€	¥	; 5");
                    writer.WriteLine("SC007 = 6	4	6	6	--	^	ħ	Ħ	; 6");
                    writer.WriteLine("SC008 = 7	4	7	7	--	&	ð	Ð	; 7");
                    writer.WriteLine("SC009 = 8	4	8	8	--	*	þ	Þ	; 8");
                    writer.WriteLine("SC00a = 9	0	9	9	--	(	‘	“	; 9");
                    writer.WriteLine("SC00b = 0	0	0	0	--	)	’	”	; 0");
                    writer.WriteLine("SC00c = OEM_MINUS	0	-	_	--	–	—	; -");
                    writer.WriteLine("SC00d = OEM_PLUS	0	=	+	--	×	÷	; =");
                    writer.WriteLine("CapsLock = OEM_1	0	={backspace}	*{CapsLock}	={backspace}	={backspace}	={backspace}	; Caps Lock");
                    writer.WriteLine("SC039 = SPACE	0	={space}	={space}	--	={space}	 	; QWERTY Space");
                    writer.WriteLine("SC01a = OEM_4	0	[	{	--	«	‹	; QWERTY [{{");
                    writer.WriteLine("SC01b = OEM_6	0	]	}	--	»	›	; QWERTY ]}}");
                    writer.WriteLine("SC028 = OEM_7	4	'	\"	--	õ	Õ	; QWERTY '\"");
                    writer.WriteLine();

                    // Indeksy przypisań
                    int scanCode = 0x010;

                    // Iterowanie przez przekazany układ klawiatury (keyboardLayout)
                    for (int row = 0; row < keyboardLayout.Length; row++)
                    {
                        for (int col = 0; col < keyboardLayout[row].Length; col++)
                        {
                            // Pomijamy SC02A (pierwszy klawisz w trzecim wierszu, pierwszy kolumna)
                            if (row == 2 && col == 0)
                                continue;

                            string key = keyboardLayout[row][col];
                            int finger = 5;  // Ustalamy "na sztywno" numer palca dla każdego klawisza (można to zmienić)

                            // Generowanie przypisań w formacie: SC010 = Q	5	q	Q	--	q	Q	; Q
                            writer.WriteLine($"SC{scanCode:X3} = {key.ToUpper()}	{finger}	{key.ToLower()}	{key.ToUpper()}	--	{key.ToLower()}	{key.ToUpper()}	; {key}");
                            scanCode++;  // Przechodzimy do kolejnego kodu skanera
                        }
                    }

                    MessageBox.Show("Plik został zapisany do: " + filePath);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano pliku do zapisania.");
            }
        }
        private void ShowHelp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PROGRAM HELP\n\n" +
                "You can design your own keyboard layout manually by swapping and assigning" +
                " keys to specific positions on the main screen. Then, using the \"Export to PKL\" key" +
                ", you can turn it into an .ini file for the PKL program. \n" +
                "By right clicking a button, you can lock it in place - this will ensure, " +
                "that during the layout generation process, that key will remain static " +
                "and not be changed.\n\n" +
                "By clicking \"Evaluate layout\", you can manually evaluate your current " +
                "layout on the main screen. \n\n" +
                "The generation options allow a wide variety of algorithms and metrics to" +
                " further personalize the process and promote chosen style of a keyboard.\n" +
                "\nJakub Jonderko, 2025");
        }
        private string[][] GetButtonMatrix()
        {
            int[][] ranges = new int[][]
            {
        new int[] { 28, 37 },
        new int[] { 42, 51 },
        new int[] { 55, 64 }
            };
            string[][] buttonMatrix = new string[ranges.Length][];

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
                        if (button.Background == Brushes.Yellow)
                        {
                            rowContents.Add(button.Content.ToString());
                        }
                        else
                        {
                            rowContents.Add("0");
                        }
                    }
                    else
                    {
                        rowContents.Add("0");
                    }
                }
                buttonMatrix[row] = rowContents.ToArray();
            }

            return buttonMatrix;
        }
        public void DisplayButtonMatrix(string[][] buttonMatrix)
        {
            StringBuilder matrixText = new StringBuilder();

            for (int row = 0; row < buttonMatrix.Length; row++)
            {
                for (int col = 0; col < buttonMatrix[row].Length; col++)
                {
                    matrixText.Append(buttonMatrix[row][col] + " ");
                }
                matrixText.AppendLine();
            }
            MessageBox.Show(matrixText.ToString(), "Button Matrix");
        }

        private void OnKeyClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var editWindow = new EditKeyWindow();
                if (editWindow.ShowDialog() == true)
                {
                    string newKey = editWindow.PressedKey.ToUpper();
                    string oldKey = button.Content.ToString().ToUpper();
                    if (oldKey != newKey)
                    {
                        SwapKeysInLayout(oldKey, newKey);
                    }
                    button.Content = newKey;
                }
            }
        }
        private void SwapKeysInLayout(string oldKey, string newKey)
        {
            int[][] ranges = new int[][]
            {
                new int[] { 28, 37 },
                new int[] { 42, 51 },
                new int[] { 55, 64 }
            };
            string[][] buttonMatrix = new string[ranges.Length][];
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
                    else
                    {
                        rowContents.Add("0");
                    }
                }
                buttonMatrix[row] = rowContents.ToArray();
            }
            (int oldRow, int oldCol) = (-1, -1);
            (int newRow, int newCol) = (-1, -1);
            oldKey = oldKey.ToUpper();
            newKey = newKey.ToUpper();
            for (int row = 0; row < buttonMatrix.Length; row++)
            {
                for (int col = 0; col < buttonMatrix[row].Length; col++)
                {
                    string currentKey = buttonMatrix[row][col].ToUpper();
                    if (currentKey == oldKey)
                    {
                        oldRow = row;
                        oldCol = col;
                    }
                    else if (currentKey == newKey)
                    {
                        newRow = row;
                        newCol = col;
                    }
                }
            }
            if (oldRow != -1 && newRow != -1)
            {
                string temp = buttonMatrix[oldRow][oldCol];
                buttonMatrix[oldRow][oldCol] = buttonMatrix[newRow][newCol];
                buttonMatrix[newRow][newCol] = temp;
                string buttonName1 = $"btn_{ranges[oldRow][0] + oldCol:D2}";
                string buttonName2 = $"btn_{ranges[newRow][0] + newCol:D2}";
                var button1 = FindName(buttonName1) as Button;
                var button2 = FindName(buttonName2) as Button;
                if (button1 != null)
                {button1.Content = buttonMatrix[oldRow][oldCol];}
                if (button2 != null)
                {button2.Content = buttonMatrix[newRow][newCol];}
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
                    button.ClearValue(BackgroundProperty);
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
                new int[] { 28, 29, 30, 31, 32, 33, 34, 35, 36, 37 },
                new int[] { 42, 43, 44, 45, 46, 47, 48, 49, 50, 51 },
                new int[] { 55, 56, 57, 58, 59, 60, 61, 62, 63, 64 } 
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

            //MessageBox.Show("Optimization process complete.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

            EvaluationAlgorithm.ClearCache();
            string corpusContent = File.ReadAllText(corpusFilePath);
            EvaluationAlgorithm.PrecomputeCorpusAnalysis(corpusContent);

            StringBuilder debugBigramInfo = new StringBuilder();
            double totalEffort = EvaluationAlgorithm.EvaluateKeyboardEffort(buttonContents, debugBigramInfo);

            aggregatedData.AppendLine();
            aggregatedData.AppendLine($"Total Effort (including bigram penalties if enabled): {totalEffort:F2}");

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
            double[] generationEffort = new double[generationCount];
            double mutationRate = SettingsWindow.MutationRate;
            int numParentsSelected = SettingsWindow.NumParentsSelected;
            string selectedAlgorithm = SettingsWindow.SelectedAlgorithm;
            string selectedMutation = SettingsWindow.SelectedMutationMethod;
            string selectedCrossover = SettingsWindow.SelectedCrossoverMethod;
            int elitismCount = SettingsWindow.ElitismCount;
            string corpusFilePath = SettingsWindow.SelectedCorpusFilePath;
            bool buttonLock = SettingsWindow.IsButtonLockEnabled;
            EvaluationAlgorithm.ClearCache();
            string[][] LockedButtons = GetButtonMatrix();
            
            //DisplayButtonMatrix(LockedButtons);
            
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

            logContent.AppendLine("Corpus Character Occurences:");
            logContent.AppendLine("---------------------------------------");

            double totalCharacters = EvaluationAlgorithm.publicCorpusFrequencyData.Values.Sum();
            foreach (var entry in EvaluationAlgorithm.publicCorpusFrequencyData.OrderByDescending(x => x.Value))
            {
                double percentage = (entry.Value / totalCharacters) * 100;
                logContent.AppendLine($"Character: '{entry.Key}'  |  Occurences: {entry.Value} | Percentage: {percentage:F2}%");
            }

            File.WriteAllText("CorpusAnalysis.log", logContent.ToString());

            UpdateProgress("Corpus analysis completed.");

            GenerationAlgorithms.GenerateInitialPopulation(populationSize);
            UpdateProgress("Generation 1: Population initialized");
            
            for (int generation = 0; generation < generationCount; generation++)
            {
                logContent.AppendLine($"\nGENERATION {generation + 1}");
                logContent.AppendLine("---------------------------------------");
                if (buttonLock)
                {
                    for (int i = 0; i < GenerationAlgorithms.KeyboardPopulation.Count; i++)
                    {
                        //DisplayButtonMatrix(GenerationAlgorithms.KeyboardPopulation[i]);
                        GenerationAlgorithms.KeyboardPopulation[i] = GenerationAlgorithms.AdjustLayoutToLockedKeys(GenerationAlgorithms.KeyboardPopulation[i], LockedButtons);
                        //DisplayButtonMatrix(GenerationAlgorithms.KeyboardPopulation[i]);
                    }
                }
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
                

                UpdateProgress($"Generation {generation + 1}   Best effort {populationEffort[0].effort:F4}");
                generationEffort[generation] = populationEffort[0].effort;
                List<string[][]> nextGeneration = new List<string[][]>();
                if (elitismCount > 0)
                {
                    var elites = populationEffort.Take(elitismCount).Select(x => x.layout).ToList();
                    nextGeneration.AddRange(elites);
                }
                List<string[][]> selectedParents = SelectionAlgorithms.SelectParents(selectedAlgorithm, numParentsSelected, GenerationAlgorithms.KeyboardPopulation);
                List<string[][]> offspring = CrossoverAlgorithms.ApplyCrossover(selectedParents, selectedCrossover);
                nextGeneration.AddRange(offspring);
                offspring.AddRange(MutationAlgorithms.ApplyMutation(nextGeneration, mutationRate, selectedMutation));

                while (nextGeneration.Count < populationSize)
                {
                    nextGeneration.Add(GenerationAlgorithms.GenerateRandomKeyboardLayout(new Random()));
                }

                GenerationAlgorithms.KeyboardPopulation = nextGeneration;
            }
            if (buttonLock)
            {
                for (int i = 0; i < GenerationAlgorithms.KeyboardPopulation.Count; i++)
                {
                    //DisplayButtonMatrix(GenerationAlgorithms.KeyboardPopulation[i]);
                    GenerationAlgorithms.KeyboardPopulation[i] = GenerationAlgorithms.AdjustLayoutToLockedKeys(GenerationAlgorithms.KeyboardPopulation[i], LockedButtons);
                    //DisplayButtonMatrix(GenerationAlgorithms.KeyboardPopulation[i]);
                }
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
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OptimizationLog.log");
            var result = MessageBox.Show("Optimization process complete! View summary?", "Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                var summaryWindow = new SummaryWindow(generationEffort, logFilePath);
                summaryWindow.Show();
            }
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