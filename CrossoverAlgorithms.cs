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

                string[][] child1;
                string[][] child2;

                switch (crossoverType)
                {
                    case "AEX":
                        (child1, child2) = ApplyAEX(parent1, parent2);
                        break;
                    case "PMX":
                        (child1, child2) = ApplyPMX(parent1, parent2);
                        break;
                    default:
                        throw new ArgumentException("Invalid crossover method");
                }

                offspring.Add(child1);
                offspring.Add(child2);

                logContent.AppendLine($"Parent 1 (Layout {i + 1}):");
                LogLayout(parent1, logContent);

                logContent.AppendLine($"Parent 2 (Layout {i + 2}):");
                LogLayout(parent2, logContent);

                logContent.AppendLine($"Child 1 (from Parent 1, Layout {i + 1}):");
                LogLayout(child1, logContent);

                logContent.AppendLine($"Child 2 (from Parent 2, Layout {i + 2}):");
                LogLayout(child2, logContent);
            }

            File.AppendAllText("KeyboardCrossover.log", logContent.ToString());
            return offspring;
        }


        private static (string[][], string[][]) ApplyAEX(string[][] parent1, string[][] parent2)
        {
            var flattenedParent1 = parent1.SelectMany(row => row).ToList();
            var flattenedParent2 = parent2.SelectMany(row => row).ToList();

            Dictionary<string, HashSet<string>> adjacencyList = new Dictionary<string, HashSet<string>>();

            void AddEdge(string key, string neighbor)
            {
                if (!adjacencyList.ContainsKey(key)) adjacencyList[key] = new HashSet<string>();
                adjacencyList[key].Add(neighbor);
            }

            for (int i = 0; i < flattenedParent1.Count; i++)
            {
                string current1 = flattenedParent1[i];
                string next1 = flattenedParent1[(i + 1) % flattenedParent1.Count];
                string prev1 = flattenedParent1[(i - 1 + flattenedParent1.Count) % flattenedParent1.Count];

                string current2 = flattenedParent2[i];
                string next2 = flattenedParent2[(i + 1) % flattenedParent2.Count];
                string prev2 = flattenedParent2[(i - 1 + flattenedParent2.Count) % flattenedParent2.Count];

                AddEdge(current1, next1);
                AddEdge(current1, prev1);
                AddEdge(current2, next2);
                AddEdge(current2, prev2);
            }
            List<string> GenerateChild()
            {
                HashSet<string> used = new HashSet<string>();
                List<string> child = new List<string>();
                string current = flattenedParent1[random.Next(flattenedParent1.Count)];

                while (child.Count < flattenedParent1.Count)
                {
                    child.Add(current);
                    used.Add(current);

                    if (adjacencyList.ContainsKey(current))
                    {
                        var nextChoices = adjacencyList[current].Where(n => !used.Contains(n)).ToList();
                        if (nextChoices.Count > 0)
                        {
                            current = nextChoices[random.Next(nextChoices.Count)];
                        }
                        else
                        {
                            current = flattenedParent1.FirstOrDefault(k => !used.Contains(k)) ?? flattenedParent1[0];
                        }
                    }
                }
                return child;
            }

            string[][] child1Matrix = ConvertListToMatrix(GenerateChild());
            string[][] child2Matrix = ConvertListToMatrix(GenerateChild());

            return (child1Matrix, child2Matrix);
        }

        private static (string[][], string[][]) ApplyPMX(string[][] parent1, string[][] parent2)
        {
            var flattenedParent1 = parent1.SelectMany(row => row).ToList();
            var flattenedParent2 = parent2.SelectMany(row => row).ToList();
            int length = flattenedParent1.Count;

            List<string> child1 = new List<string>(new string[length]);
            List<string> child2 = new List<string>(new string[length]);

            Random random = new Random();
            int start = random.Next(length / 2);
            int end = random.Next(start, length);

            Dictionary<string, string> mapping1 = new Dictionary<string, string>();
            Dictionary<string, string> mapping2 = new Dictionary<string, string>();

            for (int i = start; i < end; i++)
            {
                child1[i] = flattenedParent2[i];
                child2[i] = flattenedParent1[i];

                mapping1[flattenedParent2[i]] = flattenedParent1[i];
                mapping2[flattenedParent1[i]] = flattenedParent2[i];
            }

            FillRemainingPMX(child1, flattenedParent1, mapping1, start, end);
            FillRemainingPMX(child2, flattenedParent2, mapping2, start, end);

            return (ConvertListToMatrix(child1), ConvertListToMatrix(child2));
        }

        private static void FillRemainingPMX(List<string> child, List<string> parent, Dictionary<string, string> mapping, int start, int end)
        {
            for (int i = 0; i < child.Count; i++)
            {
                if (i >= start && i < end) continue;

                string gene = parent[i];

                while (mapping.ContainsKey(gene))
                {
                    gene = mapping[gene];
                }

                child[i] = gene;
            }
        }

        private static string[][] ConvertListToMatrix(List<string> list)
        {
            return new string[][]
            {
                list.Take(10).ToArray(),
                list.Skip(10).Take(10).ToArray(),
                list.Skip(20).Take(10).ToArray()
            };
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
