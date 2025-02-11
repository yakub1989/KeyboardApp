using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace KeyboardApp
{
    public class MutationAlgorithms
    {
        private static Random random = new Random();
        private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardMutation.log");

        public static List<string[][]> ApplyMutation(List<string[][]> population, double mutationRate, string mutationType)
        {
            List<string[][]> mutatedOffspring = new List<string[][]>();
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine($"{mutationType} Mutation Results");
            logContent.AppendLine("===================================\n");

            foreach (var individual in population)
            {
                double mutationChance = random.NextDouble() * 100;

                if (mutationChance <= mutationRate)
                {
                    string[][] mutatedIndividual;
                    do
                    {
                        mutatedIndividual = mutationType switch
                        {
                            "Reverse Sequence" => ApplyReverseSequence(individual),
                            "Swap" => ApplySwap(individual),
                            "Scramble" => ApplyScramble(individual),
                            "Displacement" => ApplyDisplacement(individual),
                            _ => individual
                        };
                    } while (AreLayoutsEqual(mutatedIndividual, individual));

                    mutatedOffspring.Add(mutatedIndividual);

                    logContent.AppendLine("Original Layout:");
                    LogLayout(individual, logContent);

                    logContent.AppendLine("Mutated Layout:");
                    LogLayout(mutatedIndividual, logContent);
                }
                else
                {
                    mutatedOffspring.Add(individual);
                }
            }

            File.AppendAllText(logFilePath, logContent.ToString());
            return mutatedOffspring;
        }

        private static string[][] ApplyReverseSequence(string[][] parent)
        {
            List<string> flattened = parent.SelectMany(row => row).ToList();

            int start, end;
            do
            {
                start = random.Next(flattened.Count - 1);
                end = random.Next(start, flattened.Count);
            } while (end - start < 2);

            flattened.Reverse(start, end - start + 1);

            return new string[][]
            {
                flattened.Take(10).ToArray(),
                flattened.Skip(10).Take(10).ToArray(),
                flattened.Skip(20).Take(10).ToArray()
            };
        }

        private static string[][] ApplySwap(string[][] parent)
        {
            List<string> flattened = parent.SelectMany(row => row).ToList();
            int swaps = random.Next(2, 5);

            for (int i = 0; i < swaps; i++)
            {
                int index1, index2;
                do
                {
                    index1 = random.Next(flattened.Count);
                    index2 = random.Next(flattened.Count);
                } while (index1 == index2 || Math.Abs(index1 - index2) < 3);

                (flattened[index1], flattened[index2]) = (flattened[index2], flattened[index1]);
            }

            return new string[][]
            {
                flattened.Take(10).ToArray(),
                flattened.Skip(10).Take(10).ToArray(),
                flattened.Skip(20).Take(10).ToArray()
            };
        }
        private static string[][] ApplyScramble(string[][] parent)
        {
            List<string> flattened = parent.SelectMany(row => row).ToList();

            int start, end;
            do
            {
                start = random.Next(flattened.Count - 1);
                end = random.Next(start, flattened.Count);
            } while (end - start < 2);

            List<string> segment = flattened.GetRange(start, end - start + 1);

            segment = segment.OrderBy(x => random.Next()).ToList();

            flattened.RemoveRange(start, end - start + 1);
            flattened.InsertRange(start, segment);

            return new string[][]
            {
                flattened.Take(10).ToArray(),
                flattened.Skip(10).Take(10).ToArray(),
                flattened.Skip(20).Take(10).ToArray()
            };
        }
        private static string[][] ApplyDisplacement(string[][] parent)
        {
            List<string> flattened = parent.SelectMany(row => row).ToList();

            int start = random.Next(flattened.Count - 3);
            int length = random.Next(3, 6);
            int end = Math.Min(start + length, flattened.Count);

            List<string> segment = flattened.GetRange(start, end - start);
            flattened.RemoveRange(start, end - start);

            int insertPosition = random.Next(flattened.Count + 1);
            flattened.InsertRange(insertPosition, segment);

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
