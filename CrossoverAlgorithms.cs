using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace KeyboardApp
{
    public class CrossoverAlgorithms
    {
        private static Random random = new Random();

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
            string[][] parents = new string[2][];
            string[] flatParent1 = parent1.SelectMany(row => row).ToArray();
            string[] flatParent2 = parent2.SelectMany(row => row).ToArray();
            parents[0] = flatParent1; parents[1] = flatParent2;
            int length = flatParent1.Length;
            string[] child = new string[length];
            HashSet<string> usedElements = new HashSet<string>(); // Przechowuje już wybrane znaki
            List<string> availableElements = flatParent1.ToList(); // Elementy, które jeszcze można dodać

            // 2. Start od pierwszego elementu `parent1`
            string currentElement = flatParent1[0];
            child[0] = currentElement;
            usedElements.Add(currentElement);
            availableElements.Remove(currentElement);

            int counter = 1;
            int currentParentIndex = 1; // Pierwszy skok do parent2

            while (counter < length)
            {
                string[] selectedParent = parents[currentParentIndex]; // Pobranie obecnego rodzica
                currentParentIndex = 1 - currentParentIndex; // Zamiana rodzica na przeciwną wersję

                int index = Array.IndexOf(selectedParent, currentElement);
                string nextElement = null;

                // Jeśli index istnieje i nie jest na końcu, bierzemy kolejny element
                if (index != -1 && index < length - 1)
                {
                    nextElement = selectedParent[index + 1];
                }

                // Jeśli element już jest w dziecku lub nie istnieje, losujemy inny dostępny element
                if (nextElement == null || usedElements.Contains(nextElement))
                {
                    nextElement = availableElements.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                }

                // Dodanie do dziecka
                child[counter] = nextElement;
                usedElements.Add(nextElement);
                availableElements.Remove(nextElement);
                currentElement = nextElement;
                counter++;
            }

            // 3. Konwersja `string[]` z powrotem do `string[][]`
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
