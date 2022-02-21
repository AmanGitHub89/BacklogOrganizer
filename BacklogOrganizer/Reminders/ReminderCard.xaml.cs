using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Utilities;
using BacklogOrganizer.Windows;

using VerticalAlignment = System.Windows.VerticalAlignment;


namespace BacklogOrganizer.Reminders
{
    public partial class ReminderCard
    {
        private readonly Reminder myReminder;
        private readonly Action myOnClosed;

        public ReminderCard()
        {
            //Only for design mode. Do not call in code
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            InitializeComponent();
        }

        public ReminderCard(Reminder reminder, Action onClosed)
        {
            myReminder = reminder;
            myOnClosed = onClosed;
            InitializeComponent();
            MissedReminderPanel.Visibility = Visibility.Collapsed;
        }

        private void ReminderCard_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            if (myReminder.WorkItemId == 0)
            {
                TaskTypeAndIdPanel.Visibility = Visibility.Collapsed;
                TitleText.Text = "<no work item>";
                DescriptionSection.VerticalAlignment = VerticalAlignment.Center;
                EditButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                WorkItemType.Text = myReminder.Type;
                WorkItemIdLink.Inlines.Clear();
                WorkItemIdLink.Inlines.Add(myReminder.WorkItemId.ToString());
                TitleText.Text = myReminder.Title;
            }

            if (myReminder.IsMissedReminder)
            {
                MissedReminderPanel.Visibility = Visibility.Visible;
                MissedReminderDateTime.Content = myReminder.ReminderDateTime.ToString("MMM dd, hh:mm", CultureInfo.InvariantCulture);
            }

            DescriptionText.Text = myReminder.ReminderDescription;
            SnoozeButton.ToolTip = $"Snooze for {BacklogOrganizerConfiguration.AppConfiguration.ReminderSnoozeDuration} minutes";
        }

        private void SnoozeButton_OnClick(object sender, RoutedEventArgs e)
        {
            myReminder.ReminderDateTime = DateTime.Now.AddMinutes(BacklogOrganizerConfiguration.AppConfiguration.ReminderSnoozeDuration);
            RemindersService.Instance.ScheduleReminder(myReminder);
            myOnClosed();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            myOnClosed();
        }

        private void WorkItemIdLink_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(myReminder.Url);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while opening url {myReminder.Url}", ex);
            }
        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            var addUpdateWorkItemWindow = new EditWorkItemWindow(myReminder.WorkItemId, myReminder.CatalogNode, myReminder.ProjectNode);
            addUpdateWorkItemWindow.ShowDialog();
            myOnClosed();
        }
    }
}
