using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace KeyboardApp
{
    public class GenerationAlgorithms
    {
        private static readonly char[] AvailableCharacters = "QWERTYUIOPASDFGHJKL;ZXCVBNM,./".ToCharArray();
        public static List<string[][]> KeyboardPopulation { get; set; } = new List<string[][]>(); // Poprawione na publiczny setter

        public static void GenerateInitialPopulation(int populationSize)
        {
            KeyboardPopulation.Clear();
            Random random = new Random();

            for (int i = 0; i < populationSize; i++)
            {
                string[][] keyboardLayout = GenerateRandomKeyboardLayout(random);
                KeyboardPopulation.Add(keyboardLayout);
            }

            LogGeneratedPopulation();
        }

        public static string[][] GenerateRandomKeyboardLayout(Random random)
        {
            char[] shuffledCharacters = AvailableCharacters.OrderBy(_ => random.Next()).ToArray();
            return new string[][]
            {
                shuffledCharacters.Take(10).Select(c => c.ToString()).ToArray(),
                shuffledCharacters.Skip(10).Take(10).Select(c => c.ToString()).ToArray(),
                shuffledCharacters.Skip(20).Take(10).Select(c => c.ToString()).ToArray()
            };
        }
        public static string[][] AdjustLayoutToLockedKeys(string[][] layout, string[][] lockedButtons)
        {
            int rows = layout.Length;
            int cols = layout[0].Length;

            // Zbieramy lokalizacje zablokowanych klawiszy i pożądane znaki
            List<Tuple<int, int, string>> lockedPositions = new List<Tuple<int, int, string>>();

            for (int row = 0; row < lockedButtons.Length; row++)
            {
                for (int col = 0; col < lockedButtons[row].Length; col++)
                {
                    if (lockedButtons[row][col] != "0") // Sprawdzamy, czy to nie jest "0"
                    {
                        // Dodajemy pozycję i pożądany znak
                        lockedPositions.Add(new Tuple<int, int, string>(row, col, lockedButtons[row][col]));
                    }
                }
            }

            // Iterujemy po całym układzie
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    string currentButton = layout[row][col];

                    // Sprawdzamy, czy ta pozycja znajduje się w lockedPositions
                    foreach (var lockedPosition in lockedPositions)
                    {
                        if (lockedPosition.Item1 == row && lockedPosition.Item2 == col)
                        {
                            // Jeśli pozycja w layout zgadza się z tą z lockedPositions, sprawdzamy, czy są takie same znaki
                            if (currentButton != lockedPosition.Item3)
                            {
                                // Znaleźliśmy różnicę, teraz szukamy pożądanego znaku w układzie
                                for (int searchRow = 0; searchRow < rows; searchRow++)
                                {
                                    for (int searchCol = 0; searchCol < cols; searchCol++)
                                    {
                                        if (layout[searchRow][searchCol] == lockedPosition.Item3)
                                        {
                                            // Zamieniamy pozycje
                                            string temp = layout[row][col];
                                            layout[row][col] = layout[searchRow][searchCol];
                                            layout[searchRow][searchCol] = temp;

                                            // Przerywamy pętlę, ponieważ zamiana już się odbyła
                                            break;
                                        }
                                    }
                                }
                            }
                            break; // Jeśli pozycja już została sprawdzona, wychodzimy z tej pętli
                        }
                    }
                }
            }

            return layout;
        }
        private static void LogGeneratedPopulation()
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KeyboardGeneration.log");
            StringBuilder logContent = new StringBuilder();
            logContent.AppendLine("Generated Keyboard Layouts");
            logContent.AppendLine("===========================");

            for (int i = 0; i < KeyboardPopulation.Count; i++)
            {
                logContent.AppendLine($"\nLayout {i + 1}:");
                foreach (var row in KeyboardPopulation[i])
                {
                    logContent.AppendLine(string.Join(" ", row));
                }
            }

            File.WriteAllText(logFilePath, logContent.ToString());
        }
    }
}
