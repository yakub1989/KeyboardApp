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
            var bigramDiagnostics = new List<(string bigram, int frequency, double totalPenalty, double totalReward, double rowSwitchPenalty)>();

            // Podstawowy wysiłek
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

            // Analiza bigramów z uwzględnieniem warunków boolowskich
            foreach (var bigram in bigramData)
            {
                string keyPair = bigram.Key;
                int bigramFrequency = bigram.Value;

                char firstChar = keyPair[0];
                char secondChar = keyPair[1];

                // Obliczanie kary za dystans
                double bigramPenalty = SettingsWindow.IsDistanceMetricEnabled
                    ? EvaluateBigramPenalty(keyboardLayout, firstChar, secondChar)
                    : 0.0;
                double totalPenalty = bigramPenalty * bigramFrequency;

                // Obliczanie nagrody za alternację
                double alternationReward = SettingsWindow.SelectedOptimizationPattern == "Prefer Alternations"
                    ? EvaluateBigramAlternationReward(keyboardLayout, firstChar, secondChar)
                    : 0.0;
                double totalReward = alternationReward * bigramFrequency;

                // Obliczanie kary za przełączanie rzędów
                double rowSwitchPenalty = SettingsWindow.IsRowSwitchMetricEnabled
                    ? EvaluateRowSwitchPenalty(keyboardLayout, firstChar, secondChar) * bigramFrequency
                    : 0.0;

                // Dodanie wyników do diagnostyki bigramów
                bigramDiagnostics.Add((keyPair, bigramFrequency, totalPenalty, totalReward, rowSwitchPenalty));

                // Uwzględnianie kar i nagród w całkowitym wysiłku
                totalEffort += totalPenalty;
                totalEffort += totalReward;
                totalEffort += rowSwitchPenalty;
            }

            // 🔹 **NOWOŚĆ: Obliczenie kary za niezbalansowanie rąk**
            if (SettingsWindow.IsHandBalanceMetricEnabled)
            {
                double handBalancePenalty = CalculateHandBalancePenalty(keyboardLayout, frequencyData);
                totalEffort *= (1 + handBalancePenalty); // Uwzględnienie kary jako mnożnik
                debugBigramInfo.AppendLine($"\nHand Balance Penalty Multiplier: {handBalancePenalty:F2}");
            }

            // Debug: Największe kary i nagrody
            var topPenaltyDiagnostics = bigramDiagnostics
                .OrderByDescending(b => b.totalPenalty)
                .Take(10);

            debugBigramInfo.AppendLine("\nTop 10 Bigrams by Penalty:");
            foreach (var (bigram, frequency, totalPenalty, _, _) in topPenaltyDiagnostics)
            {
                debugBigramInfo.AppendLine($"Bigram: '{bigram}', Frequency: {frequency}, Total Penalty: {totalPenalty:F2}");
            }

            var topRewardDiagnostics = bigramDiagnostics
                .OrderByDescending(b => b.totalReward)
                .Take(10);

            debugBigramInfo.AppendLine("\nTop 10 Bigrams by Reward:");
            foreach (var (bigram, frequency, _, totalReward, _) in topRewardDiagnostics)
            {
                debugBigramInfo.AppendLine($"Bigram: '{bigram}', Frequency: {frequency}, Total Reward: {totalReward:F2}");
            }

            var topRowSwitchDiagnostics = bigramDiagnostics
                .OrderByDescending(b => b.rowSwitchPenalty)
                .Take(10);

            debugBigramInfo.AppendLine("\nTop 10 Bigrams by Row Switch Penalty:");
            foreach (var (bigram, frequency, _, _, rowSwitchPenalty) in topRowSwitchDiagnostics)
            {
                debugBigramInfo.AppendLine($"Bigram: '{bigram}', Frequency: {frequency}, Row Switch Penalty: {rowSwitchPenalty:F2}");
            }

            return totalEffort;
        }


        public static double EvaluateRowSwitchPenalty(string[][] keyboardLayout, char firstChar, char secondChar)
        {
            // Przypisanie rzędów klawiszom (0: Top Row, 1: Home Row, 2: Bottom Row)
            int[,] rowAssignments = new int[,]
            {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Top Row
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, // Home Row
        { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }  // Bottom Row
            };

            // Znajdź pozycje znaków w układzie klawiatury
            (int row, int col)? pos1 = FindKeyPosition(keyboardLayout, firstChar);
            (int row, int col)? pos2 = FindKeyPosition(keyboardLayout, secondChar);

            // Jeśli któryś znak nie zostanie znaleziony, brak kary
            if (pos1 == null || pos2 == null)
            {
                return 0.0;
            }

            // Wyciągnij pozycje
            var position1 = pos1.Value;
            var position2 = pos2.Value;

            // Pobierz rzędy dla obu znaków
            int row1 = rowAssignments[position1.row, position1.col];
            int row2 = rowAssignments[position2.row, position2.col];

            // Oblicz różnicę rzędów
            int rowDifference = Math.Abs(row1 - row2);

            // Przydziel karę na podstawie różnicy rzędów
            switch (rowDifference)
            {
                case 1: // Przełączenie Home <-> Top lub Home <-> Bottom
                    return 1/20;
                case 2: // Przełączenie Top <-> Bottom
                    return 1.5/20;
                default: // Brak przełączenia rzędów
                    return 0.0;
            }
        }

        public static double EvaluateBigramAlternationReward(string[][] keyboardLayout, char firstChar, char secondChar)
        {
            // Define hand zones for the keyboard
            int[,] handZones = new int[,]
            {
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }, // Top row
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }, // Home row
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }  // Bottom row
            };

            // Find positions of both characters in the keyboard layout
            (int row, int col)? pos1 = FindKeyPosition(keyboardLayout, firstChar);
            (int row, int col)? pos2 = FindKeyPosition(keyboardLayout, secondChar);

            // If either character is not found, no reward
            if (pos1 == null || pos2 == null)
            {
                return 0.0;
            }

            // Extract positions
            var position1 = pos1.Value;
            var position2 = pos2.Value;

            // Check if the bigram alternates between hands
            int hand1 = handZones[position1.row, position1.col];
            int hand2 = handZones[position2.row, position2.col];

            if (hand1 != hand2) // Alternation detected
            {
                // Calculate base reward as the average effort for the two keys
                double effort1 = GetEffortValue(position1.row, position1.col);
                double effort2 = GetEffortValue(position2.row, position2.col);
                double baseReward = (effort1 + effort2) / 2.0;

                // Divide the reward by 25 to reduce its impact
                return -(baseReward / 25.0); // Negative reward decreases effort
            }

            // No alternation, no reward
            return 0.0;
        }

        public static double EvaluateBigramRollsReward(string[][] keyboardLayout, char firstChar, char secondChar)
        {
            // Find the positions of both characters in the keyboard layout
            (int row, int col)? pos1 = FindKeyPosition(keyboardLayout, firstChar);
            (int row, int col)? pos2 = FindKeyPosition(keyboardLayout, secondChar);

            // If either character is not found, no reward
            if (pos1 == null || pos2 == null)
            {
                return 0.0;
            }

            // Extract positions
            var position1 = pos1.Value;
            var position2 = pos2.Value;

            // Check if the bigram forms a roll (adjacent keys in the same row)
            if (position1.row == position2.row && Math.Abs(position1.col - position2.col) == 1)
            {
                // Calculate base reward as the average effort for the two keys
                double effort1 = GetEffortValue(position1.row, position1.col);
                double effort2 = GetEffortValue(position2.row, position2.col);
                double baseReward = (effort1 + effort2) / 2.0;

                // Divide the reward by 25 to keep it in a reasonable range
                return -(baseReward / 25.0); // Negative reward decreases effort
            }

            // No roll, no reward
            return 0.0;
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

        public static double CalculateHandBalancePenalty(string[][] keyboardLayout, Dictionary<char, int> frequencyData)
        {
            // Define hand zones for the keyboard
            int[,] handZones = new int[,]
            {
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }, // Top row
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }, // Home row
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }  // Bottom row
            };

            double leftHandFrequency = 0;
            double rightHandFrequency = 0;

            // Iterate through the keyboard layout and count occurrences for each hand
            for (int row = 0; row < keyboardLayout.Length; row++)
            {
                for (int col = 0; col < keyboardLayout[row].Length; col++)
                {
                    char key = keyboardLayout[row][col][0];
                    if (frequencyData.TryGetValue(key, out int frequency))
                    {
                        if (handZones[row, col] == 0)
                        {
                            leftHandFrequency += frequency;
                        }
                        else if (handZones[row, col] == 1)
                        {
                            rightHandFrequency += frequency;
                        }
                    }
                }
            }

            // Calculate the imbalance
            double totalFrequency = leftHandFrequency + rightHandFrequency;
            if (totalFrequency == 0) return 0; // Avoid division by zero

            double imbalance = Math.Abs(leftHandFrequency - rightHandFrequency) / totalFrequency;

            // Scale the imbalance to a maximum penalty of 100% (1.0 multiplier)
            double penaltyMultiplier = imbalance * 0.5;

            return penaltyMultiplier;
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
