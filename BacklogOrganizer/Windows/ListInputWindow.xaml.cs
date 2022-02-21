using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

using BacklogOrganizer.UserControls;


namespace BacklogOrganizer.Windows
{
    public partial class ListInputWindow
    {
        private readonly List<string> myItems;
        private readonly Action<List<string>> myOnWindowClosed;
        private TextInputControl myLastFocusedItem;

        public ListInputWindow(List<string> items, Action<List<string>> onWindowClosed)
        {
            InitializeComponent();
            myItems = items.Where(y => !string.IsNullOrEmpty(y)).Distinct().ToList();
            myOnWindowClosed = onWindowClosed;
            TextInputControl.OnTextBoxFocus += TextInputControl_OnTextBoxFocus;
        }

        private void ListInputWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in myItems)
            {
                AddControl(item, false);
            }
        }

        private void ListInputWindow_OnClosing(object sender, CancelEventArgs e)
        {
            var controls = TextBoxInputsListBox.Items.OfType<TextInputControl>().ToList();
            var values = controls.Select(x => x.InputTextBox.Text.Trim()).Where(y => !string.IsNullOrEmpty(y)).Distinct().ToList();
            myOnWindowClosed.Invoke(values);
            TextInputControl.OnTextBoxFocus -= TextInputControl_OnTextBoxFocus;
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddControl(string.Empty, true);
        }

        private void AddControl(string text, bool setFocus)
        {
            var control = new TextInputControl {HorizontalAlignment = HorizontalAlignment.Stretch, InputTextBox = {Text = text}};
            TextBoxInputsListBox.Items.Add(control);
            if (setFocus)
            {
                control.InputTextBox.Focus();
            }
        }

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (myLastFocusedItem == null) return;

            var controls = TextBoxInputsListBox.Items.OfType<TextInputControl>().ToList();
            if (!controls.Contains(myLastFocusedItem)) return;

            TextBoxInputsListBox.Items.Remove(myLastFocusedItem);
            myLastFocusedItem = null;
        }

        private void TextInputControl_OnTextBoxFocus(object sender, EventArgs e)
        {
            myLastFocusedItem = sender as TextInputControl;
        }
    }
}
