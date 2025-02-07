using System.Windows;
using System.Windows.Input;

namespace KeyboardApp
{
    public partial class EditKeyWindow : Window
    {
        public string PressedKey { get; private set; }

        public EditKeyWindow()
        {
            InitializeComponent();
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (IsValidKey(e.Key))
            {
                PressedKey = KeyToString(e.Key);
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Nieobsługiwany przycisk. Spróbuj ponownie.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private bool IsValidKey(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
                return true;

            if (key >= Key.D0 && key <= Key.D9)
                return true;

            switch (key)
            {
                case Key.Space:
                case Key.Oem1: // ;
                case Key.Oem2: // /
                case Key.Oem3: // `
                case Key.Oem4: // [
                case Key.Oem5: // \
                case Key.Oem6: // ]
                case Key.Oem7: // '
                case Key.OemMinus:
                case Key.OemPlus:
                case Key.OemComma:
                case Key.OemPeriod:
                    return true;
                default:
                    return false;
            }
        }

        private string KeyToString(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
                return key.ToString().ToUpper();

            if (key >= Key.D0 && key <= Key.D9)
                return ((char)('0' + (key - Key.D0))).ToString();

            switch (key)
            {
                case Key.Space: return " ";
                case Key.Oem1: return ";";
                case Key.Oem2: return "/";
                case Key.Oem3: return "`";
                case Key.Oem4: return "[";
                case Key.Oem5: return "\\";
                case Key.Oem6: return "]";
                case Key.Oem7: return "'";
                case Key.OemMinus: return "-";
                case Key.OemPlus: return "=";
                case Key.OemComma: return ",";
                case Key.OemPeriod: return ".";
                default: return null;
            }
        }
    }
}
