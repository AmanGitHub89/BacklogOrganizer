using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.Reminders
{
    public partial class SetReminderWindow
    {
        private readonly TfsWorkItem myTfsWorkItem;

        public SetReminderWindow()
        {
            InitializeComponent();
        }
        public SetReminderWindow(TfsWorkItem tfsWorkItem) : this()
        {
            myTfsWorkItem = tfsWorkItem;
        }


        private void SetReminderWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            ReminderTimePicker.TimeInterval = TimeSpan.FromMinutes(30);
            ReminderDatePicker.SelectedDate = DateTime.Now;

            var timeNow = DateTime.Now;
            ReminderTimePicker.DefaultValue = timeNow;
            ReminderTimePicker.Value = timeNow;
            ReminderTimePicker.Text = timeNow.ToString("hh:mm tt", CultureInfo.InvariantCulture);

            var timeFromNowComboBoxData = new List<ComboBoxKeyValue>();
            for (var i = 1; i <= 12; i++)
            {
                var hourValue = 30 * i;
                var hourDescription = hourValue / 60d;
                timeFromNowComboBoxData.Add(new ComboBoxKeyValue(hourValue.ToString(), $"{hourDescription:0.0} Hours"));
            }

            TimeFromNowComboBox.ItemsSource = timeFromNowComboBoxData;
            TimeFromNowComboBox.DataContext = timeFromNowComboBoxData;
            TimeFromNowComboBox.SelectedIndex = 0;

            TimeFromNowRadioButton.IsChecked = true;

            if (myTfsWorkItem != null)
            {
                RemindAfterTimeLabel.Content = $"Remind me for {myTfsWorkItem.Type} {myTfsWorkItem.Id}";
                RemindAtTimeLabel.Content = $"Remind me for {myTfsWorkItem.Type} {myTfsWorkItem.Id} on";
            }
            else
            {
                RemindAfterTimeLabel.Content = "Remind me";
                RemindAtTimeLabel.Content = "Remind me on";
            }
        }

        private void TimeInputFormat_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (TimeFromNowRadioButton.IsChecked.GetValueOrDefault())
            {
                TimeFromNowPanel.Visibility = Visibility.Visible;
                CustomTimePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                TimeFromNowPanel.Visibility = Visibility.Collapsed;
                CustomTimePanel.Visibility = Visibility.Visible;
            }
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var reminderDateTime = DateTime.Now.AddMinutes(30);
            if (TimeFromNowRadioButton.IsChecked.GetValueOrDefault())
            {
                reminderDateTime = DateTime.Now.AddMinutes(int.Parse(((ComboBoxKeyValue)TimeFromNowComboBox.SelectedItem).Key));
            }
            else if (CustomTimeRadioButton.IsChecked.GetValueOrDefault())
            {
                var date = ReminderDatePicker.SelectedDate ?? DateTime.Now;
                var time = ReminderTimePicker.Value ?? DateTime.Now;
                reminderDateTime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0);
            }

            if (reminderDateTime < DateTime.Now)
            {
                MessageBoxHelper.ShowErrorMessage("Invalid date and time!", $"Reminder cannot be set in the past.{Environment.NewLine}Please select valid date time.");
                return;
            }

            var description = DescriptionTextBox.Text.Trim();
            if (string.IsNullOrEmpty(description)) description = "<no description>";
            var reminder = myTfsWorkItem == null ? new Reminder(reminderDateTime, description) : new Reminder(reminderDateTime, description, myTfsWorkItem);
            try
            {
                RemindersService.Instance.ScheduleReminder(reminder);
                Close();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage("Could not set reminder.", ex.Message);
            }
        }
    }
}
