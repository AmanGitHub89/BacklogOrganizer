using System;
using System.Windows;


namespace BacklogOrganizer.UserControls
{
    public partial class TextInputControl
    {
        public static event EventHandler OnTextBoxFocus;

        public TextInputControl()
        {
            InitializeComponent();
            DoneButton.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
        }

        private void InputTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            OnTextBoxFocus?.Invoke(this, null);
        }
    }
}
