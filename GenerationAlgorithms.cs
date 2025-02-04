using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KeyboardApp
{
    public class GenerationAlgorithms
    {
        private static readonly char[] AvailableCharacters = "QWERTYUIOPASDFGHJKL;ZXCVBNM,./".ToCharArray();
        public static List<string[][]> KeyboardPopulation { get; private set; } = new List<string[][]>();

        public static List<string[][]> GenerateInitialPopulation(int populationSize)
        {
            KeyboardPopulation.Clear();
            Random random = new Random();

            for (int i = 0; i < populationSize; i++)
            {
                string[][] keyboardLayout = GenerateRandomKeyboardLayout(random);
                KeyboardPopulation.Add(keyboardLayout);
            }

            LogGeneratedPopulation();
            EvaluateAndLogEffort();

            return KeyboardPopulation;
        }

        private static string[][] GenerateRandomKeyboardLayout(Random random)
        {
            char[] shuffledCharacters = AvailableCharacters.OrderBy(_ => random.Next()).ToArray();
            return new string[][]
            {
                shuffledCharacters.Take(10).Select(c => c.ToString()).ToArray(),
                shuffledCharacters.Skip(10).Take(10).Select(c => c.ToString()).ToArray(),
                shuffledCharacters.Skip(20).Take(10).Select(c => c.ToString()).ToArray()
            };
        }

        private static void LogGeneratedPopulation()
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardGeneration.log");
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("Generated Keyboard Layouts");
            logContent.AppendLine("===========================");

            for (int i = 0; i < KeyboardPopulation.Count; i++)
            {
                logContent.AppendLine($"\nLayout {i + 1}:");
                foreach (var row in KeyboardPopulation[i])
                {
                    logContent.AppendLine(string.Join(" ", row));
                }
            }

            File.WriteAllText(logFilePath, logContent.ToString());
        }

        private static void EvaluateAndLogEffort()
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardGeneration.log");
            StringBuilder logContent = new StringBuilder();

            logContent.AppendLine("\nEvaluation Results");
            logContent.AppendLine("==================");

            string corpusFilePath = SettingsWindow.SelectedCorpusFilePath;
            if (!File.Exists(corpusFilePath))
            {
                logContent.AppendLine("Error: Corpus file not found. Evaluation skipped.");
                File.AppendAllText(logFilePath, logContent.ToString());
                return;
            }

            string corpusContent = File.ReadAllText(corpusFilePath);
            var frequencyData = EvaluationAlgorithm.AnalyzeCorpusFrequency(corpusContent);
            var bigramData = EvaluationAlgorithm.AnalyzeCorpusBigrams(corpusContent);

            for (int i = 0; i < KeyboardPopulation.Count; i++)
            {
                StringBuilder debugBigramInfo = new StringBuilder();
                double totalEffort = EvaluationAlgorithm.EvaluateKeyboardEffort(KeyboardPopulation[i], frequencyData, bigramData, debugBigramInfo);

                logContent.AppendLine($"\nLayout {i + 1}: Total Effort = {totalEffort:F2}");
            }

            File.AppendAllText(logFilePath, logContent.ToString());
        }
        public static List<string[][]> TournamentSelection(int tournamentSize)
        {
            if (KeyboardPopulation.Count == 0)
            {
                throw new InvalidOperationException("No population available for selection.");
            }

            Random random = new Random();
            List<string[][]> availableLayouts = new List<string[][]>(KeyboardPopulation);
            List<string[][]> selectedParents = new List<string[][]>();

            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardGeneration.log");
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("\nTournament Selection Results");
            logContent.AppendLine("============================");

            int tournamentCount = KeyboardPopulation.Count / tournamentSize;
            if (KeyboardPopulation.Count % tournamentSize != 0)
            {
                tournamentCount += 1; // Zaokrąglenie w górę, jeśli są dodatkowe układy
            }

            // Pobieramy dane korpusu **tylko raz** przed selekcją
            string corpusFilePath = SettingsWindow.SelectedCorpusFilePath;
            if (!File.Exists(corpusFilePath))
            {
                throw new FileNotFoundException("Corpus file not found.");
            }
            string corpusContent = File.ReadAllText(corpusFilePath);
            var frequencyData = EvaluationAlgorithm.AnalyzeCorpusFrequency(corpusContent);
            var bigramData = EvaluationAlgorithm.AnalyzeCorpusBigrams(corpusContent);

            for (int i = 0; i < tournamentCount; i++)
            {
                List<string[][]> tournamentGroup = new List<string[][]>();

                while (tournamentGroup.Count < tournamentSize)
                {
                    if (availableLayouts.Count > 0)
                    {
                        // Pobieramy losowy układ i usuwamy go z dostępnych
                        var selectedLayout = availableLayouts.OrderBy(x => random.Next()).First();
                        availableLayouts.Remove(selectedLayout);
                        tournamentGroup.Add(selectedLayout);
                    }
                    else
                    {
                        // Jeśli zabrakło układów, generujemy nowy i oceniamy go natychmiast
                        string[][] newLayout = GenerateRandomKeyboardLayout(random);
                        KeyboardPopulation.Add(newLayout);
                        tournamentGroup.Add(newLayout);
                    }
                }

                // Przechowujemy wyniki turnieju
                Dictionary<string[][], double> fitnessValues = new Dictionary<string[][], double>();

                logContent.AppendLine($"\nTournament {i + 1}:");

                foreach (var layout in tournamentGroup)
                {
                    StringBuilder debugBigramInfo = new StringBuilder();
                    double effort = EvaluationAlgorithm.EvaluateKeyboardEffort(layout, frequencyData, bigramData, debugBigramInfo);
                    fitnessValues[layout] = effort;

                    // Logujemy wszystkich uczestników turnieju
                    logContent.AppendLine("\nTournament Participant:");
                    foreach (var row in layout)
                    {
                        logContent.AppendLine(string.Join(" ", row));
                    }
                    logContent.AppendLine($"Total Effort: {effort:F2}");
                }

                // Wybieramy układ z **najmniejszym** wysiłkiem
                var bestLayout = fitnessValues.OrderBy(kv => kv.Value).First().Key;
                selectedParents.Add(bestLayout);

                // Logujemy zwycięzcę turnieju
                logContent.AppendLine("\nSelected Layout:");
                foreach (var row in bestLayout)
                {
                    logContent.AppendLine(string.Join(" ", row));
                }
                logContent.AppendLine($"Total Effort: {fitnessValues[bestLayout]:F2}");
            }

            // Zapis wyników do pliku
            File.AppendAllText(logFilePath, logContent.ToString());

            return selectedParents; // Zwrot wybranych rodziców do dalszego użycia
        }

        public static List<string[][]> RouletteWheelSelection(int numParents)
        {
            if (KeyboardPopulation.Count == 0)
            {
                throw new InvalidOperationException("No population available for selection.");
            }

            Random random = new Random();
            List<string[][]> selectedParents = new List<string[][]>();

            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardGeneration.log");
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("\nRoulette Wheel Selection Results");
            logContent.AppendLine("================================");

            // Pobieramy dane korpusu **tylko raz** przed selekcją
            string corpusFilePath = SettingsWindow.SelectedCorpusFilePath;
            if (!File.Exists(corpusFilePath))
            {
                throw new FileNotFoundException("Corpus file not found.");
            }
            string corpusContent = File.ReadAllText(corpusFilePath);
            var frequencyData = EvaluationAlgorithm.AnalyzeCorpusFrequency(corpusContent);
            var bigramData = EvaluationAlgorithm.AnalyzeCorpusBigrams(corpusContent);

            // Obliczamy sumaryczny wysiłek (odwrócona wartość fitness)
            Dictionary<string[][], double> effortValues = new Dictionary<string[][], double>();
            double totalEffort = 0.0;

            foreach (var layout in KeyboardPopulation)
            {
                StringBuilder debugBigramInfo = new StringBuilder();
                double effort = EvaluationAlgorithm.EvaluateKeyboardEffort(layout, frequencyData, bigramData, debugBigramInfo);
                effortValues[layout] = effort;
                totalEffort += 1 / effort; // Odwracamy wysiłek, aby większy wysiłek oznaczał mniejsze prawdopodobieństwo
            }

            // Normalizacja prawdopodobieństw selekcji
            Dictionary<string[][], double> selectionProbabilities = new Dictionary<string[][], double>();
            double cumulativeProbability = 0.0;

            foreach (var layout in effortValues.OrderBy(kv => kv.Value)) // Sortujemy według wysiłku
            {
                cumulativeProbability += (1 / layout.Value) / totalEffort;
                selectionProbabilities[layout.Key] = cumulativeProbability;
            }

            // Wybór rodziców
            for (int i = 0; i < numParents; i++)
            {
                double spin = random.NextDouble();
                string[][] selectedLayout = selectionProbabilities.First(kv => kv.Value >= spin).Key;
                selectedParents.Add(selectedLayout);

                // Logowanie wybranego układu
                logContent.AppendLine($"\nSelected Layout {i + 1}:");
                foreach (var row in selectedLayout)
                {
                    logContent.AppendLine(string.Join(" ", row));
                }
                logContent.AppendLine($"Total Effort: {effortValues[selectedLayout]:F2}");
            }

            // Zapis wyników do pliku
            File.AppendAllText(logFilePath, logContent.ToString());

            return selectedParents; // Zwrot wybranych rodziców do dalszego użycia
        }
        public static List<string[][]> RankSelection(int numParents)
        {
            if (KeyboardPopulation.Count == 0)
            {
                throw new InvalidOperationException("No population available for selection.");
            }

            // **Pobieramy dane korpusu przed selekcją**
            string corpusFilePath = SettingsWindow.SelectedCorpusFilePath;
            if (!File.Exists(corpusFilePath))
            {
                throw new FileNotFoundException("Corpus file not found.");
            }
            string corpusContent = File.ReadAllText(corpusFilePath);
            var frequencyData = EvaluationAlgorithm.AnalyzeCorpusFrequency(corpusContent);
            var bigramData = EvaluationAlgorithm.AnalyzeCorpusBigrams(corpusContent);

            // **Oblicz wysiłek dla każdego układu**
            Dictionary<string[][], double> fitnessValues = new Dictionary<string[][], double>();
            foreach (var layout in KeyboardPopulation)
            {
                StringBuilder debugBigramInfo = new StringBuilder();
                double effort = EvaluationAlgorithm.EvaluateKeyboardEffort(layout, frequencyData, bigramData, debugBigramInfo);
                fitnessValues[layout] = effort;
            }

            // **Sortowanie układów od najlepszego do najgorszego**
            var rankedLayouts = fitnessValues.OrderBy(kv => kv.Value).ToList();

            // **Wycinamy dolne 20% najgorszych układów**
            int cutoffIndex = (int)(rankedLayouts.Count * 0.8);
            rankedLayouts = rankedLayouts.Take(cutoffIndex).ToList();

            // **Nadajemy rangi od 1 do N (gdzie 1 = NAJLEPSZY, N = NAJGORSZY)**
            int populationSize = rankedLayouts.Count;
            Dictionary<string[][], double> rankProbabilities = new Dictionary<string[][], double>();

            // **Obliczamy prawdopodobieństwo wyboru bazując na wykładniczej skali**
            double totalRankSum = 0;
            for (int i = 0; i < populationSize; i++)
            {
                rankProbabilities[rankedLayouts[i].Key] = Math.Pow((populationSize - i), 4); // **Większa różnica w wagach**
                totalRankSum += rankProbabilities[rankedLayouts[i].Key];
            }

            // **Normalizacja do przedziału [0,1]**
            foreach (var key in rankProbabilities.Keys.ToList())
            {
                rankProbabilities[key] /= totalRankSum;
            }

            // **Losowanie proporcjonalne do rangi (BEZ POWTÓRZEŃ!)**
            Random random = new Random();
            List<string[][]> selectedParents = new List<string[][]>();
            HashSet<string[][]> usedLayouts = new HashSet<string[][]>(); // Zapamiętane układy

            while (selectedParents.Count < numParents)
            {
                double roll = random.NextDouble(); // Losujemy wartość między 0 a 1
                double cumulativeSum = 0.0;

                foreach (var layout in rankedLayouts)
                {
                    cumulativeSum += rankProbabilities[layout.Key];

                    if (roll < cumulativeSum && !usedLayouts.Contains(layout.Key))
                    {
                        selectedParents.Add(layout.Key);
                        usedLayouts.Add(layout.Key);
                        break;
                    }
                }
            }

            // **Logowanie wyników**
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardGeneration.log");
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("\nRank Selection Results");
            logContent.AppendLine("=======================");

            for (int i = 0; i < selectedParents.Count; i++)
            {
                logContent.AppendLine($"\nSelected Layout {i + 1}:");
                foreach (var row in selectedParents[i])
                {
                    logContent.AppendLine(string.Join(" ", row));
                }
                logContent.AppendLine($"Total Effort: {fitnessValues[selectedParents[i]]:F2}");
            }

            File.AppendAllText(logFilePath, logContent.ToString());

            return selectedParents;
        }
        public static List<string[][]> StochasticUniversalSampling(int numParents)
        {
            if (KeyboardPopulation.Count == 0)
            {
                throw new InvalidOperationException("No population available for selection.");
            }

            // **Pobranie danych korpusu przed selekcją**
            string corpusFilePath = SettingsWindow.SelectedCorpusFilePath;
            if (!File.Exists(corpusFilePath))
            {
                throw new FileNotFoundException("Corpus file not found.");
            }
            string corpusContent = File.ReadAllText(corpusFilePath);
            var frequencyData = EvaluationAlgorithm.AnalyzeCorpusFrequency(corpusContent);
            var bigramData = EvaluationAlgorithm.AnalyzeCorpusBigrams(corpusContent);

            // **Obliczenie wysiłku dla każdego układu**
            Dictionary<string[][], double> fitnessValues = new Dictionary<string[][], double>();
            double totalFitness = 0.0;

            foreach (var layout in KeyboardPopulation)
            {
                StringBuilder debugBigramInfo = new StringBuilder();
                double effort = EvaluationAlgorithm.EvaluateKeyboardEffort(layout, frequencyData, bigramData, debugBigramInfo);
                double fitness = 1.0 / effort;  // Fitness to odwrotność wysiłku
                fitnessValues[layout] = fitness;
                totalFitness += fitness;
            }

            // **Obliczenie równych odstępów na kole ruletki**
            double stepSize = totalFitness / numParents;
            Random random = new Random();
            double startPoint = random.NextDouble() * stepSize;
            List<double> pointers = Enumerable.Range(0, numParents)
                                              .Select(i => startPoint + i * stepSize)
                                              .ToList();

            // **Selekcja układów**
            List<string[][]> selectedParents = new List<string[][]>();
            double cumulativeFitness = 0.0;
            var sortedFitness = fitnessValues.OrderByDescending(kv => kv.Value).ToList();
            int currentIndex = 0;

            foreach (double pointer in pointers)
            {
                while (cumulativeFitness < pointer && currentIndex < sortedFitness.Count)
                {
                    cumulativeFitness += sortedFitness[currentIndex].Value;
                    currentIndex++;
                }

                if (currentIndex > 0)
                {
                    selectedParents.Add(sortedFitness[currentIndex - 1].Key);
                }
            }

            // **Logowanie wyników**
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardGeneration.log");
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("\nStochastic Universal Sampling Results");
            logContent.AppendLine("=====================================");

            for (int i = 0; i < selectedParents.Count; i++)
            {
                logContent.AppendLine($"\nSelected Layout {i + 1}:");
                foreach (var row in selectedParents[i])
                {
                    logContent.AppendLine(string.Join(" ", row));
                }
                logContent.AppendLine($"Total Effort: {1.0 / fitnessValues[selectedParents[i]]:F2}");
            }

            File.AppendAllText(logFilePath, logContent.ToString());

            return selectedParents;
        }

    }
}
