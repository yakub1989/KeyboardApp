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

        private static Dictionary<char, int> corpusFrequencyData;
        private static Dictionary<string, int> corpusBigramData;

        private static readonly double[,] EffortMatrix = new double[,]
        {
            { 4.0, 2.0, 2.0, 3.0, 4.0, 5.0, 3.0, 2.0, 2.0, 4.0 },
            { 1.5, 1.0, 1.0, 1.0, 3.0, 3.0, 1.0, 1.0, 1.0, 1.5 },
            { 4.0, 4.0, 3.0, 2.0, 5.0, 3.0, 2.0, 3.0, 4.0, 4.0 }
        };

        public static void PrecomputeCorpusAnalysis(string corpusContent)
        {
            corpusFrequencyData = AnalyzeCorpusFrequency(corpusContent);
            corpusBigramData = AnalyzeCorpusBigrams(corpusContent);
        }

        private static Dictionary<char, int> AnalyzeCorpusFrequency(string corpusContent)
        {
            string normalizedContent = corpusContent.RemoveDiacritics();
            var frequency = new Dictionary<char, int>();

            foreach (char c in normalizedContent)
            {
                if (char.IsWhiteSpace(c)) continue;
                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c))
                {
                    if (frequency.ContainsKey(c))
                        frequency[c]++;
                    else
                        frequency[c] = 1;
                }
            }

            return frequency
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        public static void ClearCache()
        {
            corpusFrequencyData = new Dictionary<char, int>();
            corpusBigramData = new Dictionary<string, int>();
        }

        private static Dictionary<string, int> AnalyzeCorpusBigrams(string corpusContent)
        {
            var bigramFrequency = new Dictionary<string, int>();
            string normalizedText = corpusContent.RemoveDiacritics().ToUpper();

            for (int i = 0; i < normalizedText.Length - 1; i++)
            {
                char firstChar = normalizedText[i];
                char secondChar = normalizedText[i + 1];

                if (!char.IsWhiteSpace(firstChar) && !char.IsWhiteSpace(secondChar))
                {
                    string bigram = $"{firstChar}{secondChar}";

                    if (bigramFrequency.ContainsKey(bigram))
                        bigramFrequency[bigram]++;
                    else
                        bigramFrequency[bigram] = 1;
                }
            }

            return bigramFrequency
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static double EvaluateKeyboardEffort(string[][] keyboardLayout, StringBuilder debugBigramInfo)
        {
            if (corpusFrequencyData == null || corpusBigramData == null)
                throw new InvalidOperationException("Corpus data has not been precomputed. Call PrecomputeCorpusAnalysis first.");

            double totalEffort = 0.0;
            var bigramDiagnostics = new List<(string bigram, int frequency, double totalPenalty, double totalReward, double rowSwitchPenalty)>();

            for (int row = 0; row < keyboardLayout.Length; row++)
            {
                for (int column = 0; column < keyboardLayout[row].Length; column++)
                {
                    char key = keyboardLayout[row][column][0];
                    if (corpusFrequencyData.TryGetValue(key, out int frequency))
                    {
                        double effort = GetEffortValue(row, column) * frequency;
                        totalEffort += effort;
                    }
                }
            }

            foreach (var bigram in corpusBigramData)
            {
                string keyPair = bigram.Key;
                int bigramFrequency = bigram.Value;

                char firstChar = keyPair[0];
                char secondChar = keyPair[1];

                double bigramPenalty = SettingsWindow.IsDistanceMetricEnabled
                    ? EvaluateBigramPenalty(keyboardLayout, firstChar, secondChar)
                    : 0.0;
                double totalPenalty = bigramPenalty * bigramFrequency;

                double alternationReward = SettingsWindow.SelectedOptimizationPattern == "Prefer Alternations"
                    ? EvaluateBigramAlternationReward(keyboardLayout, firstChar, secondChar)
                    : 0.0;
                double totalReward = alternationReward * bigramFrequency;

                double rowSwitchPenalty = SettingsWindow.IsRowSwitchMetricEnabled
                ? EvaluateRowSwitchPenalty(keyboardLayout, firstChar, secondChar) * bigramFrequency
                : 0.0;



                bigramDiagnostics.Add((keyPair, bigramFrequency, totalPenalty, totalReward, rowSwitchPenalty));

                totalEffort += totalPenalty;
                totalEffort += rowSwitchPenalty;
                totalEffort -= totalReward;
            }

            if (SettingsWindow.IsHandBalanceMetricEnabled)
            {
                double handBalancePenalty = CalculateHandBalancePenalty(keyboardLayout, corpusFrequencyData);
                totalEffort *= (1 + handBalancePenalty);
                debugBigramInfo.AppendLine($"\nHand Balance Penalty Multiplier: {handBalancePenalty:F2}");
            }

            var topPenaltyDiagnostics = bigramDiagnostics
                .OrderByDescending(b => b.totalPenalty)
                .Take(10);

            debugBigramInfo.AppendLine("\nTop 10 Bigrams by Penalty:");
            foreach (var (bigram, frequency, totalPenalty, _, _) in topPenaltyDiagnostics)
            {
                debugBigramInfo.AppendLine($"Bigram: '{bigram}', Frequency: {frequency}, Total Penalty: {totalPenalty:F2}");
            }

            var topRewardDiagnostics = bigramDiagnostics
            .OrderBy(b => b.totalReward)
            .Take(10);

            debugBigramInfo.AppendLine("\nTop 10 Bigrams by Reward (Ascending Order):");
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


        public static double EvaluateBigramAlternationReward(string[][] keyboardLayout, char firstChar, char secondChar)
        {
            int[,] handZones = new int[,]
            {
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 },
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 },
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }
            };

            var pos1 = FindKeyPosition(keyboardLayout, firstChar);
            var pos2 = FindKeyPosition(keyboardLayout, secondChar);

            if (pos1 == null || pos2 == null)
                return 0.0;

            int hand1 = handZones[pos1.Value.row, pos1.Value.col];
            int hand2 = handZones[pos2.Value.row, pos2.Value.col];

            if (hand1 != hand2)
            {
                double effort1 = GetEffortValue(pos1.Value.row, pos1.Value.col);
                double effort2 = GetEffortValue(pos2.Value.row, pos2.Value.col);
                return -((effort1 + effort2) / 25.0);
            }
            return 0.0;
        }

        public static double EvaluateRowSwitchPenalty(string[][] keyboardLayout, char firstChar, char secondChar)
        {
            int[,] rowAssignments = new int[,]
            {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }
            };

            var pos1 = FindKeyPosition(keyboardLayout, firstChar);
            var pos2 = FindKeyPosition(keyboardLayout, secondChar);

            if (pos1 == null || pos2 == null)
                return 0.0;

            int row1 = rowAssignments[pos1.Value.row, pos1.Value.col];
            int row2 = rowAssignments[pos2.Value.row, pos2.Value.col];

            return row1 != row2 ? (1.0 / 20.0) : 0.0;
        }



        public static double CalculateHandBalancePenalty(string[][] keyboardLayout, Dictionary<char, int> frequencyData)
        {
            int[,] handZones = new int[,]
            {
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 },
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 },
        { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 }
            };

            double leftHandFrequency = 0;
            double rightHandFrequency = 0;

            for (int row = 0; row < keyboardLayout.Length; row++)
            {
                for (int col = 0; col < keyboardLayout[row].Length; col++)
                {
                    char key = keyboardLayout[row][col][0];
                    if (frequencyData.TryGetValue(key, out int frequency))
                    {
                        if (handZones[row, col] == 0)
                            leftHandFrequency += frequency;
                        else
                            rightHandFrequency += frequency;
                    }
                }
            }

            double totalFrequency = leftHandFrequency + rightHandFrequency;
            if (totalFrequency == 0) return 0;

            double imbalance = Math.Abs(leftHandFrequency - rightHandFrequency) / totalFrequency;
            return imbalance * 0.5;
        }


        private static double GetEffortValue(int row, int column)
        {
            if (row >= 0 && row < EffortMatrix.GetLength(0) && column >= 0 && column < EffortMatrix.GetLength(1))
                return EffortMatrix[row, column];

            throw new ArgumentOutOfRangeException("Invalid row or column for effort matrix.");
        }

        public static double EvaluateBigramPenalty(string[][] keyboardLayout, char firstChar, char secondChar)
        {
            int[,] fingerZones = new int[,]
            {
                { 0, 1, 2, 3, 3, 4, 4, 5, 6, 7 },
                { 0, 1, 2, 3, 3, 4, 4, 5, 6, 7 },
                { 0, 1, 2, 3, 3, 4, 4, 5, 6, 7 }
            };

            (int row, int col)? pos1 = FindKeyPosition(keyboardLayout, firstChar);
            (int row, int col)? pos2 = FindKeyPosition(keyboardLayout, secondChar);

            if (pos1 == null || pos2 == null)
                return 0.0;

            var position1 = pos1.Value;
            var position2 = pos2.Value;

            int finger1 = fingerZones[position1.row, position1.col];
            int finger2 = fingerZones[position2.row, position2.col];

            if (finger1 == finger2)
            {
                double distance = CalculateDistance(position1, position2);
                double penaltyMultiplier = 1.0 + (distance / 10.0);
                return Math.Min(penaltyMultiplier, 3.0);
            }

            return 0.0;
        }

        private static (int row, int col)? FindKeyPosition(string[][] keyboardLayout, char key)
        {
            key = char.ToUpper(key);
            for (int row = 0; row < keyboardLayout.Length; row++)
            {
                for (int col = 0; col < keyboardLayout[row].Length; col++)
                {
                    if (keyboardLayout[row][col][0] == key)
                        return (row, col);
                }
            }
            return null;
        }

        private static double CalculateDistance((int row, int col) pos1, (int row, int col) pos2)
        {
            return Math.Sqrt(Math.Pow(pos1.row - pos2.row, 2) + Math.Pow(pos1.col - pos2.col, 2));
        }
    }
}
