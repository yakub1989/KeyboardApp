using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KeyboardApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void OnKeyClick(object sender, RoutedEventArgs e)
        {
            // Cast sender to Button
            if (sender is Button button)
            {
                var editWindow = new EditKeyWindow();
                if (editWindow.ShowDialog() == true)
                {
                    // Pobieramy wartość klawisza i ustawiamy na przycisku
                    button.Content = editWindow.PressedKey;
                }
            }
        }
        private void ResetLayout(object sender, RoutedEventArgs e)
        {
            // Map of default QWERTY layout
            string[] defaultLayout = new string[]
            {
                "Esc", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", // Row 1
                "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Backspace",     // Row 2
                "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\",         // Row 3
                "Caps", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter",         // Row 4
                "Shift", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift",             // Row 5
                "Ctrl", "Alt", "Space", "Alt", "Ctrl"                                           // Row 6
            };

            // Iterate over buttons by name (btn_00 to btn_70)
            for (int i = 0; i <= 70; i++)
            {
                string buttonName = $"btn_{i:D2}";
                var button = FindName(buttonName) as Button;
                if (button != null && i < defaultLayout.Length)
                {
                    button.Content = defaultLayout[i];
                }
            }
        }
        private string[][] GetSpecificButtonContents()
        {
            // Define the ranges of button IDs to include
            int[][] ranges = new int[][]
            {
                new int[] { 28, 37 }, // btn_28 to btn_37
                new int[] { 42, 52 }, // btn_42 to btn_52
                new int[] { 55, 61 }  // btn_55 to btn_61
            };

            // List to store rows of content
            string[][] buttonContents = new string[ranges.Length][];

            // Iterate through the ranges and collect content
            for (int row = 0; row < ranges.Length; row++)
            {
                int start = ranges[row][0];
                int end = ranges[row][1];

                // Collect content for the current row
                var rowContents = new List<string>();
                for (int i = start; i <= end; i++)
                {
                    string buttonName = $"btn_{i:D2}";
                    var button = FindName(buttonName) as Button;
                    if (button != null)
                    {
                        rowContents.Add(button.Content.ToString());
                    }
                }

                // Assign collected row contents to the buttonContents array
                buttonContents[row] = rowContents.ToArray();
            }
            return buttonContents;
        }
        private void EvaluateLayout(object sender, RoutedEventArgs e)
        {
            var buttonContents = GetSpecificButtonContents();

            // Aggregate data into a string
            StringBuilder aggregatedData = new StringBuilder();
            for (int i = 0; i < buttonContents.Length; i++)
            {
                aggregatedData.AppendLine($"Row {i + 1}: {string.Join(", ", buttonContents[i])}");
            }

            // Display the aggregated data in a MessageBox
            MessageBox.Show(aggregatedData.ToString(), "Keyboard Layout Evaluation", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}