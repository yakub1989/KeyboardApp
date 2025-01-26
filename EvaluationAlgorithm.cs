using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Diacritics.Extensions;

namespace KeyboardApp
{
    public class EvaluationAlgorithm
    {
        public static double bigramPenaltyTotal = 0;
        // Define effort matrix for the keyboard
        private static readonly double[,] EffortMatrix = new double[,]
        {
            { 4.0, 2.0, 2.0, 3.0, 4.0, 5.0, 3.0, 2.0, 2.0, 4.0 }, // Top row
            { 1.5, 1.0, 1.0, 1.0, 3.0, 3.0, 1.0, 1.0, 1.0, 1.5 }, // Home row
            { 4.0, 4.0, 3.0, 2.0, 5.0, 3.0, 2.0, 3.0, 4.0, 4.0 }  // Bottom row
        };

        public static Dictionary<char, int> AnalyzeCorpusFrequency(string corpusContent)
        {
            string normalizedContent = corpusContent.RemoveDiacritics();

            var frequency = new Dictionary<char, int>();
            foreach (char c in normalizedContent)
            {
                if (char.IsWhiteSpace(c)) continue;

                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c))
                {
                    if (frequency.ContainsKey(c))
                    {
                        frequency[c]++;
                    }
                    else
                    {
                        frequency[c] = 1;
                    }
                }
            }
            return frequency
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static Dictionary<string, int> AnalyzeCorpusBigrams(string corpusContent)
        {
            var bigramFrequency = new Dictionary<string, int>();
            string normalizedText = corpusContent.RemoveDiacritics().ToUpper(); // Convert to uppercase

            for (int i = 0; i < normalizedText.Length - 1; i++)
            {
                char firstChar = normalizedText[i];
                char secondChar = normalizedText[i + 1];

                if (!char.IsWhiteSpace(firstChar) && !char.IsWhiteSpace(secondChar))
                {
                    string bigram = $"{firstChar}{secondChar}";

                    if (bigramFrequency.ContainsKey(bigram))
                    {
                        bigramFrequency[bigram]++;
                    }
                    else
                    {
                        bigramFrequency[bigram] = 1;
                    }
                }
            }
            return bigramFrequency
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static double GetEffortValue(int row, int column)
        {
            if (row >= 0 && row < EffortMatrix.GetLength(0) && column >= 0 && column < EffortMatrix.GetLength(1))
            {
                return EffortMatrix[row, column];
            }
            throw new ArgumentOutOfRangeException("Invalid row or column for effort matrix.");
        }

        public static double EvaluateKeyboardEffort(string[][] keyboardLayout, Dictionary<char, int> frequencyData, Dictionary<string, int> bigramData, StringBuilder debugBigramInfo)
        {
            double totalEffort = 0.0;
            var bigramDiagnostics = new List<(string bigram, int frequency, double totalPenalty)>();

            for (int row = 0; row < keyboardLayout.Length; row++)
            {
                for (int column = 0; column < keyboardLayout[row].Length; column++)
                {
                    char key = keyboardLayout[row][column][0];
                    if (frequencyData.TryGetValue(key, out int frequency))
                    {
                        double effort = GetEffortValue(row, column) * frequency;
                        totalEffort += effort;
                    }
                }
            }

            if (SettingsWindow.IsDistanceMetricEnabled)
            {
                foreach (var bigram in bigramData)
                {
                    string keyPair = bigram.Key;
                    int bigramFrequency = bigram.Value;

                    char firstChar = keyPair[0];
                    char secondChar = keyPair[1];

                    double bigramPenalty = EvaluateBigramPenalty(keyboardLayout, firstChar, secondChar);
                    double totalPenalty = bigramPenalty * bigramFrequency;

                    bigramDiagnostics.Add((keyPair, bigramFrequency, totalPenalty));

                    debugBigramInfo.AppendLine($"Bigram: '{firstChar}{secondChar}', Frequency: {bigramFrequency}, Penalty: {bigramPenalty:F2}, Total Contribution: {totalPenalty:F2}");
                    totalEffort += totalPenalty;
                }

                var topBigramDiagnostics = bigramDiagnostics
                    .OrderByDescending(b => b.totalPenalty)
                    .Take(10);

                debugBigramInfo.AppendLine("\nTop 10 Bigrams by Penalty:");
                foreach (var (bigram, frequency, totalPenalty) in topBigramDiagnostics)
                {
                    debugBigramInfo.AppendLine($"Bigram: '{bigram}', Frequency: {frequency}, Total Penalty: {totalPenalty:F2}");
                }
            }

            return totalEffort;
        }

        public static double EvaluateBigramPenalty(string[][] keyboardLayout, char firstChar, char secondChar)
        {
            // Define finger zones for the keyboard
            int[,] fingerZones = new int[,]
            {
        { 0, 1, 2, 3, 3, 4, 4, 5, 6, 7 }, // Top row
        { 0, 1, 2, 3, 3, 4, 4, 5, 6, 7 }, // Home row
        { 0, 1, 2, 3, 3, 4, 4, 5, 6, 7 }  // Bottom row
            };

            // Find the positions of both characters in the keyboard layout
            (int row, int col)? pos1 = FindKeyPosition(keyboardLayout, firstChar);
            (int row, int col)? pos2 = FindKeyPosition(keyboardLayout, secondChar);

            // If either character is not found, return no penalty (0.0 penalty value)
            if (pos1 == null || pos2 == null)
            {
                return 0.0; // No penalty if characters are missing
            }

            // Extract positions
            var position1 = pos1.Value;
            var position2 = pos2.Value;

            // Check if both keys belong to the same finger zone
            int finger1 = fingerZones[position1.row, position1.col];
            int finger2 = fingerZones[position2.row, position2.col];

            if (finger1 == finger2)
            {
                // Calculate the Euclidean distance between the two keys
                double distance = CalculateDistance(position1, position2);

                // Scale the distance to a penalty range (1.0 - 3.0)
                double penaltyMultiplier = 1.0 + (distance / 10.0);
                penaltyMultiplier = Math.Min(penaltyMultiplier, 3.0); // Cap the penalty at 3.0

                return penaltyMultiplier;
            }

            // If they do not belong to the same finger zone, return no penalty (0.0 penalty value)
            return 0.0;
        }



        private static (int row, int col)? FindKeyPosition(string[][] keyboardLayout, char key)
        {
            key = char.ToUpper(key); // Convert key to uppercase
            for (int row = 0; row < keyboardLayout.Length; row++)
            {
                for (int col = 0; col < keyboardLayout[row].Length; col++)
                {
                    if (keyboardLayout[row][col][0] == key)
                    {
                        return (row, col);
                    }
                }
            }
            return null; // Key not found
        }


        private static double CalculateDistance((int row, int col) pos1, (int row, int col) pos2)
        {
            double distance = Math.Sqrt(Math.Pow(pos1.row - pos2.row, 2) + Math.Pow(pos1.col - pos2.col, 2));

            Console.WriteLine($"Distance calculated: {distance:F2} for positions {pos1} -> {pos2}");
            return distance;
        }


    }
}
