using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KeyboardApp
{
    public class SelectionAlgorithms
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardSelection.log");

        public static List<string[][]> SelectParents(string selectionMethod, int numParents, List<string[][]> population)
        {
            int elitismCount = SettingsWindow.ElitismCount;
            List<string[][]> selectedParents = new List<string[][]>();

            if (elitismCount > 0)
            {
                selectedParents = population
                    .OrderBy(layout =>
                    {
                        StringBuilder debugInfo = new StringBuilder();
                        return EvaluationAlgorithm.EvaluateKeyboardEffort(layout,
                            EvaluationAlgorithm.AnalyzeCorpusFrequency(SettingsWindow.CorpusContent),
                            EvaluationAlgorithm.AnalyzeCorpusBigrams(SettingsWindow.CorpusContent), debugInfo);
                    })
                    .Take(elitismCount)
                    .ToList();
            }

            List<string[][]> remainingParents = selectionMethod switch
            {
                "Tournament" => TournamentSelection(numParents - selectedParents.Count, population),
                "Roulette" => RouletteWheelSelection(numParents - selectedParents.Count, population),
                "Ranked" => RankSelection(numParents - selectedParents.Count, population),
                "Stochastic Universal Sampling" => StochasticUniversalSampling(numParents - selectedParents.Count, population),
                _ => throw new ArgumentException("Invalid selection method.")
            };

            selectedParents.AddRange(remainingParents);
            return selectedParents;
        }


        private static List<string[][]> TournamentSelection(int tournamentSize, List<string[][]> population)
        {
            if (population.Count == 0)
            {
                throw new InvalidOperationException("No population available for selection.");
            }

            if (tournamentSize <= 0)
            {
                throw new ArgumentException("Tournament size must be greater than zero.");
            }

            if (tournamentSize > population.Count)
            {
                tournamentSize = population.Count; // Prevent issues with small populations
            }

            Random random = new Random();
            List<string[][]> selectedParents = new List<string[][]>();

            // Debug Logging
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TournamentSelection.log");
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine($"Tournament Selection Debug Log - {DateTime.Now}");
            logContent.AppendLine($"Tournament Size: {tournamentSize}");
            logContent.AppendLine($"Population Count: {population.Count}");
            logContent.AppendLine($"Total Tournaments: {population.Count / tournamentSize}");
            logContent.AppendLine("===================================");

            for (int i = 0; i < population.Count / tournamentSize; i++)
            {
                var tournamentGroup = population.OrderBy(_ => random.Next()).Take(tournamentSize).ToList();

                logContent.AppendLine($"\nTournament {i + 1}: {tournamentGroup.Count} Participants");

                // Evaluate fitness values
                Dictionary<string[][], double> fitnessValues = new Dictionary<string[][], double>();

                foreach (var layout in tournamentGroup)
                {
                    StringBuilder debugInfo = new StringBuilder();
                    double effort = EvaluationAlgorithm.EvaluateKeyboardEffort(layout,
                        EvaluationAlgorithm.AnalyzeCorpusFrequency(SettingsWindow.CorpusContent),
                        EvaluationAlgorithm.AnalyzeCorpusBigrams(SettingsWindow.CorpusContent), debugInfo);

                    if (effort == 0)
                    {
                        logContent.AppendLine($"Warning: Layout evaluated with effort = 0 (possible cause of division by zero)");
                        effort = double.MaxValue; // Assign high effort to prevent divide by zero
                    }

                    fitnessValues[layout] = effort;
                }

                // Select the best layout
                var bestLayout = fitnessValues.OrderBy(kv => kv.Value).First().Key;
                selectedParents.Add(bestLayout);

                logContent.AppendLine("Best Layout:");
                LogLayout(bestLayout, logContent);
            }

            File.WriteAllText(logFilePath, logContent.ToString());
            return selectedParents;
        }


        private static List<string[][]> RouletteWheelSelection(int numParents, List<string[][]> population)
        {
            if (population.Count == 0)
                throw new InvalidOperationException("No population available for selection.");

            Random random = new Random();
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("\nRoulette Wheel Selection Results");
            logContent.AppendLine("================================");

            Dictionary<string[][], double> fitnessValues = new Dictionary<string[][], double>();

            foreach (var layout in population)
            {
                StringBuilder debugInfo = new StringBuilder();
                double effort = EvaluationAlgorithm.EvaluateKeyboardEffort(layout,
                    EvaluationAlgorithm.AnalyzeCorpusFrequency(SettingsWindow.CorpusContent),
                    EvaluationAlgorithm.AnalyzeCorpusBigrams(SettingsWindow.CorpusContent), debugInfo);

                fitnessValues[layout] = 1.0 / effort;
            }

            double totalFitness = fitnessValues.Values.Sum();
            List<string[][]> selectedParents = new List<string[][]>();

            for (int i = 0; i < numParents; i++)
            {
                double spin = random.NextDouble() * totalFitness;
                double cumulative = 0;

                foreach (var entry in fitnessValues.OrderByDescending(kv => kv.Value))
                {
                    cumulative += entry.Value;
                    if (cumulative >= spin)
                    {
                        selectedParents.Add(entry.Key);
                        logContent.AppendLine($"\nSelected Layout {i + 1}:");
                        LogLayout(entry.Key, logContent);
                        break;
                    }
                }
            }

            File.AppendAllText(logFilePath, logContent.ToString());
            return selectedParents;
        }

        private static List<string[][]> RankSelection(int numParents, List<string[][]> population)
        {
            if (population.Count == 0)
                throw new InvalidOperationException("No population available for selection.");

            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("\nRank Selection Results");
            logContent.AppendLine("=======================");

            var rankedLayouts = population.OrderBy(layout =>
            {
                StringBuilder debugInfo = new StringBuilder();
                return EvaluationAlgorithm.EvaluateKeyboardEffort(layout,
                    EvaluationAlgorithm.AnalyzeCorpusFrequency(SettingsWindow.CorpusContent),
                    EvaluationAlgorithm.AnalyzeCorpusBigrams(SettingsWindow.CorpusContent), debugInfo);
            }).ToList();

            double totalRank = rankedLayouts.Count * (rankedLayouts.Count + 1) / 2.0;
            Dictionary<string[][], double> rankProbabilities = new Dictionary<string[][], double>();

            for (int i = 0; i < rankedLayouts.Count; i++)
                rankProbabilities[rankedLayouts[i]] = (double)(rankedLayouts.Count - i) / totalRank;

            List<string[][]> selectedParents = SelectByProbability(numParents, rankProbabilities);

            for (int i = 0; i < selectedParents.Count; i++)
            {
                logContent.AppendLine($"\nSelected Layout {i + 1}:");
                LogLayout(selectedParents[i], logContent);
            }

            File.AppendAllText(logFilePath, logContent.ToString());
            return selectedParents;
        }

        private static List<string[][]> StochasticUniversalSampling(int numParents, List<string[][]> population)
        {
            if (population.Count == 0)
                throw new InvalidOperationException("No population available for selection.");

            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("\nStochastic Universal Sampling Results");
            logContent.AppendLine("=====================================");

            Dictionary<string[][], double> fitnessValues = new Dictionary<string[][], double>();

            foreach (var layout in population)
            {
                StringBuilder debugInfo = new StringBuilder();
                double effort = EvaluationAlgorithm.EvaluateKeyboardEffort(layout,
                    EvaluationAlgorithm.AnalyzeCorpusFrequency(SettingsWindow.CorpusContent),
                    EvaluationAlgorithm.AnalyzeCorpusBigrams(SettingsWindow.CorpusContent), debugInfo);

                fitnessValues[layout] = 1.0 / effort;
            }

            double totalFitness = fitnessValues.Values.Sum();
            double stepSize = totalFitness / numParents;
            Random random = new Random();
            double startPoint = random.NextDouble() * stepSize;
            List<double> pointers = Enumerable.Range(0, numParents).Select(i => startPoint + i * stepSize).ToList();

            List<string[][]> selectedParents = SelectByPointers(fitnessValues, pointers);

            for (int i = 0; i < selectedParents.Count; i++)
            {
                logContent.AppendLine($"\nSelected Layout {i + 1}:");
                LogLayout(selectedParents[i], logContent);
            }

            File.AppendAllText(logFilePath, logContent.ToString());
            return selectedParents;
        }

        private static List<string[][]> SelectByProbability(int numParents, Dictionary<string[][], double> probabilities)
        {
            List<string[][]> selected = new List<string[][]>();
            Random random = new Random();

            while (selected.Count < numParents)
            {
                double roll = random.NextDouble();
                double cumulative = 0;

                foreach (var kv in probabilities)
                {
                    cumulative += kv.Value;
                    if (roll < cumulative)
                    {
                        selected.Add(kv.Key);
                        break;
                    }
                }
            }

            return selected;
        }

        private static List<string[][]> SelectByPointers(Dictionary<string[][], double> fitnessValues, List<double> pointers)
        {
            List<string[][]> selected = new List<string[][]>();
            double cumulative = 0;
            var sortedFitness = fitnessValues.OrderByDescending(kv => kv.Value).ToList();
            int index = 0;

            foreach (double pointer in pointers)
            {
                while (cumulative < pointer && index < sortedFitness.Count)
                {
                    cumulative += sortedFitness[index].Value;
                    index++;
                }

                if (index > 0)
                    selected.Add(sortedFitness[index - 1].Key);
            }

            return selected;
        }

        private static void LogLayout(string[][] layout, StringBuilder logContent)
        {
            foreach (var row in layout)
                logContent.AppendLine(string.Join(" ", row));
            logContent.AppendLine();
        }
    }
}
