using System;
using System.Collections.Generic;
using System.Linq;

namespace KeyboardApp
{
    public class CrossoverAlgorithms
    {
        private static Random random = new Random();

        public static List<string[][]> ReverseSequenceCrossover(List<string[][]> parents)
        {
            List<string[][]> offspring = new List<string[][]>();

            for (int i = 0; i < parents.Count; i += 2)
            {
                if (i + 1 >= parents.Count) break; // Ensure we have pairs

                string[][] parent1 = parents[i];
                string[][] parent2 = parents[i + 1];

                // Generate offspring by reversing a random segment
                string[][] child1 = ApplyReverseSequence(parent1);
                string[][] child2 = ApplyReverseSequence(parent2);

                offspring.Add(child1);
                offspring.Add(child2);
            }

            return offspring;
        }

        private static string[][] ApplyReverseSequence(string[][] parent)
        {
            // Flatten the keyboard layout
            List<string> flattened = parent.SelectMany(row => row).ToList();

            // Select random range to reverse
            int start = random.Next(flattened.Count - 1);
            int end = random.Next(start, flattened.Count);

            // Reverse the selected range
            flattened.Reverse(start, end - start + 1);

            // Convert back to 3-row keyboard layout
            return new string[][]
            {
                flattened.Take(10).ToArray(),
                flattened.Skip(10).Take(10).ToArray(),
                flattened.Skip(20).Take(10).ToArray()
            };
        }
    }
}
