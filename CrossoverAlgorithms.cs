using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

namespace KeyboardApp
{
    public class CrossoverAlgorithms
    {
        private static Random random = new Random();
        private const double PROB_DISTANT_SWAP = 0.3;  // 30% chance for distant swap
        private const double PROB_ADAPTIVE_BOOST = 0.2; // 20% chance to increase randomness
        private const double PROBABILITY_INCREMENT = 0.1; // Increment when stagnation occurs


        public static List<string[][]> ApplyCrossover(List<string[][]> parents, string crossoverType)
        {
            List<string[][]> offspring = new List<string[][]>();
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine($"{crossoverType} Crossover Results");
            logContent.AppendLine("===================================\n");

            for (int i = 0; i < parents.Count; i += 2)
            {
                if (i + 1 >= parents.Count) break;

                string[][] parent1 = parents[i];
                string[][] parent2 = parents[i + 1];

                string[][] child;

                switch (crossoverType)
                {
                    case "AEX":
                        child = ApplyAEX(parent1, parent2);
                        break;
                    case "PMX":
                        child = ApplyPMX(parent1, parent2);
                        break;
                    default:
                        throw new ArgumentException("Invalid crossover method");
                }

                offspring.Add(child);

                logContent.AppendLine($"Parent 1 (Layout {i + 1}):");
                LogLayout(parent1, logContent);

                logContent.AppendLine($"Parent 2 (Layout {i + 2}):");
                LogLayout(parent2, logContent);

                logContent.AppendLine($"Child (from layout {i + 1}):");
                LogLayout(child, logContent);
            }

            File.AppendAllText("KeyboardCrossover.log", logContent.ToString());
            return offspring;
        }
        private static string[][] ApplyAEX(string[][] parent1, string[][] parent2)
        {
            StringBuilder logContent = new StringBuilder();
            string[] flatParent1 = parent1.SelectMany(row => row).ToArray();
            string[] flatParent2 = parent2.SelectMany(row => row).ToArray();
            int length = flatParent1.Length;
            string[] child = new string[length];
            HashSet<string> usedElements = new HashSet<string>();
            List<string> availableElements = flatParent1.ToList();

            string currentElement = flatParent1[0];
            child[0] = currentElement;
            usedElements.Add(currentElement);
            availableElements.Remove(currentElement);

            int counter = 1;
            int currentParentIndex = 1;

            while (counter < length)
            {
                string[] selectedParent = (currentParentIndex == 0) ? flatParent1 : flatParent2;
                currentParentIndex = 1 - currentParentIndex;

                int index = Array.IndexOf(selectedParent, currentElement);
                string nextElement = null;

                if (index != -1 && index < length - 1)
                {
                    nextElement = selectedParent[index + 1];
                }

                if (nextElement == null || usedElements.Contains(nextElement))
                {
                    double probability = PROB_DISTANT_SWAP;
                    if (random.NextDouble() < 0.2) probability += PROB_ADAPTIVE_BOOST; // Adaptive boost

                    if (random.NextDouble() < probability)
                    {
                        int randomIndex = random.Next(0, availableElements.Count);
                        nextElement = availableElements[randomIndex];
                        logContent.AppendLine($"[RANDOMIZED] Selected {nextElement} instead of adjacency-based swap.");
                    }
                    else
                    {
                        nextElement = availableElements.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                    }
                }

                child[counter] = nextElement;
                usedElements.Add(nextElement);
                availableElements.Remove(nextElement);
                currentElement = nextElement;
                counter++;

                logContent.AppendLine($"Step {counter}: Placed {nextElement} at position {counter - 1}");
            }

            return UnflattenKeyboardLayout(child, parent1.Length, parent1[0].Length);
        }
        private static string[][] UnflattenKeyboardLayout(string[] flatLayout, int rows, int cols)
        {
            string[][] keyboardLayout = new string[rows][];
            for (int i = 0; i < rows; i++)
            {
                keyboardLayout[i] = flatLayout.Skip(i * cols).Take(cols).ToArray();
            }
            return keyboardLayout;
        }
        private static string[][] ApplyPMX(string[][] parent1, string[][] parent2)
        {
            int rows = parent1.Length;
            int cols = parent1[0].Length;

            // Spłaszczamy tablice rodziców do jednowymiarowych tablic stringów
            string[] flatParent1 = parent1.SelectMany(row => row).ToArray();
            string[] flatParent2 = parent2.SelectMany(row => row).ToArray();

            int parentLength = flatParent1.Length;
            string[] offspring = new string[parentLength];

            Random random = new Random();

            // Wybór dwóch punktów cięcia
            int num1, num2;
            do
            {
                num1 = random.Next(parentLength);
                num2 = random.Next(parentLength);
            }
            while (num1 == num2 || Math.Abs(num1 - num2) == parentLength);

            int start = Math.Min(num1, num2);
            int stop = Math.Max(num1, num2);

            // Tworzymy mapę odwzorowań
            int cutSize = stop - start;
            string[] parent1CutPart = new string[cutSize];
            string[] parent2CutPart = new string[cutSize];

            // Kopiujemy segment z Parent1 do potomka
            for (int i = 0; i < parentLength; i++)
            {
                offspring[i] = flatParent2[i]; // Domyślnie kopiujemy Parent2
            }

            int k = 0;
            for (int i = start; i < stop; i++)
            {
                offspring[i] = flatParent1[i]; // Wstawiamy segment z Parent1

                parent1CutPart[k] = flatParent1[i];
                parent2CutPart[k] = flatParent2[i];
                k++;
            }

            // Naprawa konfliktów
            bool flag = true;
            while (flag)
            {
                flag = offspring.Distinct().Count() != offspring.Length;

                for (int i = 0; i < parentLength - cutSize; i++)
                {
                    int geneIndex = (stop + i) % parentLength;

                    for (int j = 0; j < cutSize; j++)
                    {
                        if (parent1CutPart[j] == offspring[geneIndex])
                        {
                            offspring[geneIndex] = parent2CutPart[j];
                        }
                    }
                }
            }

            // Zamiana z powrotem na tablicę 2D
            string[][] offspring2D = new string[rows][];
            for (int i = 0; i < rows; i++)
            {
                offspring2D[i] = offspring.Skip(i * cols).Take(cols).ToArray();
            }

            return offspring2D;
        }


        private static void LogLayout(string[][] layout, StringBuilder logContent)
        {
            foreach (var row in layout)
            {
                logContent.AppendLine(string.Join(" ", row));
            }
            logContent.AppendLine();
        }
    }
}
