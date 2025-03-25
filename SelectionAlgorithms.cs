using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace KeyboardApp
{
    public class SelectionAlgorithms
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardSelection.log");

        public static List<string[][]> SelectParents(string selectionMethod, int numParents, List<string[][]> population)
        {
            List<string[][]> selectedParents = new List<string[][]>();
            List<string[][]> remainingParents = selectionMethod switch
            {
                "Tournament" => TournamentSelection(numParents, population),
                "Roulette" => RouletteWheelSelection(numParents, population),
                "Ranked" => RankSelection(numParents, population),
                "Stochastic Universal Sampling" => StochasticUniversalSampling(numParents, population),
                _ => throw new ArgumentException("Invalid selection method.")
            };


            selectedParents.AddRange(remainingParents);
            return selectedParents;
        }

        private static List<string[][]> TournamentSelection(int numParents, List<string[][]> population)
        {
            if (population.Count == 0)
                throw new InvalidOperationException("No population available for selection.");
            if (numParents <= 0)
                throw new ArgumentException("Number of parents must be greater than zero.");

            Random random = new Random();
            List<string[][]> selectedParents = new List<string[][]>();

            // Zwiększamy liczbę turniejów do większej części populacji
            int numTournaments = Math.Max(population.Count / 2, numParents);
            int tournamentSize = Math.Max(4, numParents / 3);

            var availablePopulation = new List<string[][]>(population); // Kopia populacji

            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine($"Tournament Selection Debug Log - {DateTime.Now}");
            logContent.AppendLine($"Tournament Size: {tournamentSize}");
            logContent.AppendLine($"Population Count: {population.Count}");
            logContent.AppendLine($"Total Tournaments: {numTournaments}");
            logContent.AppendLine("===================================");

            while (selectedParents.Count < numParents)
            {
                if (availablePopulation.Count < tournamentSize) // Odbudowa puli populacji
                    availablePopulation = new List<string[][]>(population);

                // Losowanie uczestników
                var shuffledPopulation = availablePopulation.OrderBy(_ => Guid.NewGuid()).ToList();
                var tournamentGroup = shuffledPopulation.Take(tournamentSize).ToList();

                logContent.AppendLine($"\nTournament {selectedParents.Count + 1}: {tournamentGroup.Count} Participants");

                // Ocena fitness i wybór najlepszego z możliwym 20% losowym wyborem drugiego
                var fitnessValues = tournamentGroup.ToDictionary(
                    layout => layout,
                    layout => EvaluationAlgorithm.EvaluateKeyboardEffort(layout, new StringBuilder())
                );

                logContent.AppendLine("Tournament Participants Fitness:");
                foreach (var kv in fitnessValues)
                {
                    logContent.AppendLine($"Fitness: {kv.Value:F4}");
                }

                var sortedGroup = fitnessValues.OrderBy(kv => kv.Value).ToList();
                var bestLayout = sortedGroup.First().Key;

                // 80% wybieramy najlepszy, 20% drugi najlepszy
                if (random.NextDouble() < 0.2 && sortedGroup.Count > 1)
                {
                    bestLayout = sortedGroup[1].Key;
                }

                selectedParents.Add(bestLayout);
                availablePopulation.Remove(bestLayout); // Usuwamy wybranego osobnika

                logContent.AppendLine("Best Layout Selected:");
                LogLayout(bestLayout, logContent);
            }

            File.AppendAllText(logFilePath, logContent.ToString());
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

            var fitnessValues = population.ToDictionary(
                layout => layout,
                layout => 1.0 / EvaluationAlgorithm.EvaluateKeyboardEffort(layout, new StringBuilder())
            );

            double totalFitness = fitnessValues.Values.Sum();
            List<string[][]> selectedParents = new List<string[][]>();

            for (int i = 0; i <= numParents; i++)
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

            // Posortowanie układów według fitnessu
            var rankedLayouts = population
                .OrderBy(layout => EvaluationAlgorithm.EvaluateKeyboardEffort(layout, new StringBuilder()))
                .ToList();

            double totalRank = rankedLayouts.Count * (rankedLayouts.Count + 1) / 2.0;
            var rankProbabilities = rankedLayouts
                .Select((layout, index) => new { Layout = layout, Probability = (double)(rankedLayouts.Count - index) / totalRank })
                .ToDictionary(x => x.Layout, x => x.Probability);

            List<string[][]> selectedParents = new List<string[][]>();

            Random random = new Random();
            while (selectedParents.Count < numParents)
            {
                // Wybór układu na podstawie prawdopodobieństw
                double roll = random.NextDouble();
                double cumulative = 0;

                foreach (var kv in rankProbabilities.OrderByDescending(kv => kv.Value))
                {
                    cumulative += kv.Value;
                    if (roll < cumulative)
                    {
                        selectedParents.Add(kv.Key);
                        rankProbabilities.Remove(kv.Key);  // Usuwamy wybrany układ z listy

                        // Po usunięciu z listy, należy także usunąć go z rankedLayouts
                        rankedLayouts.Remove(kv.Key);

                        logContent.AppendLine($"\nSelected Layout:");
                        LogLayout(kv.Key, logContent);
                        break;
                    }
                }
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

            var fitnessValues = population.ToDictionary(
                layout => layout,
                layout => 1.0 / EvaluationAlgorithm.EvaluateKeyboardEffort(layout, new StringBuilder())
            );

            double totalFitness = fitnessValues.Values.Sum();
            double stepSize = totalFitness / numParents;
            Random random = new Random();
            double startPoint = random.NextDouble() * stepSize;
            List<double> pointers = Enumerable.Range(0, numParents).Select(i => startPoint + i * stepSize).ToList();

            List<string[][]> selectedParents = SelectByPointers(fitnessValues, pointers);
            foreach (var parent in selectedParents)
            {
                logContent.AppendLine($"\nSelected Layout:");
                LogLayout(parent, logContent);
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
