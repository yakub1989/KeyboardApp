using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
