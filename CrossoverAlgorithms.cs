using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace KeyboardApp
{
    public class CrossoverAlgorithms
    {
        private static Random random = new Random();
        private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardCrossover.log");

        public static List<string[][]> ReverseSequenceCrossover(List<string[][]> parents)
        {
            List<string[][]> offspring = new List<string[][]>();
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("Reverse Sequence Crossover Results");
            logContent.AppendLine("===================================\n");

            for (int i = 0; i < parents.Count; i += 2)
            {
                if (i + 1 >= parents.Count) break; // Ensure we have pairs

                string[][] parent1 = parents[i];
                string[][] parent2 = parents[i + 1];

                string[][] child1;
                string[][] child2;

                do
                {
                    child1 = ApplyReverseSequence(parent1);
                } while (AreLayoutsEqual(child1, parent1));

                do
                {
                    child2 = ApplyReverseSequence(parent2);
                } while (AreLayoutsEqual(child2, parent2));

                offspring.Add(child1);
                offspring.Add(child2);

                // Log Parent 1
                logContent.AppendLine($"Parent 1 (Layout {i + 1}):");
                LogLayout(parent1, logContent);

                // Log Parent 2
                logContent.AppendLine($"Parent 2 (Layout {i + 2}):");
                LogLayout(parent2, logContent);

                // Log Child 1
                logContent.AppendLine($"Child 1 (from Parent 1, Layout {i + 1}):");
                LogLayout(child1, logContent);

                // Log Child 2
                logContent.AppendLine($"Child 2 (from Parent 2, Layout {i + 2}):");
                LogLayout(child2, logContent);
            }

            File.AppendAllText(logFilePath, logContent.ToString());
            return offspring;
        }

        private static string[][] ApplyReverseSequence(string[][] parent)
        {
            List<string> flattened = parent.SelectMany(row => row).ToList();

            int start, end;
            do
            {
                start = random.Next(flattened.Count - 1);
                end = random.Next(start, flattened.Count);
            } while (end - start < 2); // Ensure meaningful change

            flattened.Reverse(start, end - start + 1);

            return new string[][]
            {
                flattened.Take(10).ToArray(),
                flattened.Skip(10).Take(10).ToArray(),
                flattened.Skip(20).Take(10).ToArray()
            };
        }

        private static bool AreLayoutsEqual(string[][] layout1, string[][] layout2)
        {
            for (int i = 0; i < layout1.Length; i++)
            {
                for (int j = 0; j < layout1[i].Length; j++)
                {
                    if (layout1[i][j] != layout2[i][j])
                    {
                        return false;
                    }
                }
            }
            return true;
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
