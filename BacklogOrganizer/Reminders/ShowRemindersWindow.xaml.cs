using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.Reminders
{
    public partial class ShowRemindersWindow
    {
        private readonly Action myOnClosed;
        private readonly List<Reminder> myReminders;
        private MediaPlayer myMediaPlayer;
        private int myReminderCardHeight = 130;

        public ShowRemindersWindow(List<Reminder> reminders, Action onClosed)
        {
            myReminders = reminders;
            myOnClosed = onClosed;
            InitializeComponent();
            ResizeMode = ResizeMode.NoResize;
        }

        private void ShowRemindersWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            ShowAllReminders(true);
        }

        public void ShowReminders(List<Reminder> reminders)
        {
            myReminders.AddRange(reminders);
            ShowAllReminders(true);
        }

        private void ShowAllReminders(bool playSound)
        {
            if (myReminders.Count == 0)
            {
                Close();
            }
            ReminderCardsPanel.Children.Clear();
            foreach (var reminder in myReminders.OrderByDescending(x => x.ReminderDateTime))
            {
                var reminderCard = new ReminderCard(reminder, () =>
                {
                    if (!myReminders.Remove(reminder))
                    {
                        Logger.Error($"Reminder not found in list. WorkItem ID = {reminder.WorkItemId}, Description = {reminder.ReminderDescription}, DateTime = {reminder.ReminderDateTime}", null);
                    }
                    ShowAllReminders(false);
                }) {Height = 130};
                ReminderCardsPanel.Children.Add(reminderCard);
            }
            SetBounds();
            if (playSound)
            {
                PlaySound();
            }
        }

        private void SetBounds()
        {
            var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Width = 400;
            Height = 60 + ReminderCardsPanel.Children.Count * myReminderCardHeight;
            Top = screenBounds.Height - Height - 50;
            Left = screenBounds.Width - Width - 20;
        }

        private void ShowRemindersWindow_OnClosed(object sender, EventArgs e)
        {
            myOnClosed();
        }

        private void ShowRemindersWindow_OnClosing(object sender, CancelEventArgs e)
        {
            myMediaPlayer?.Stop();
        }

        private void PlaySound()
        {
            if (!BacklogOrganizerConfiguration.AppConfiguration.PlayReminderSound) return;

            var reminderSound = BacklogOrganizerConfiguration.AppConfiguration.ReminderSound;
            // ReSharper disable once AssignNullToNotNullAttribute
            var path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), $"Reminders\\Sounds\\{reminderSound}");
            if (!File.Exists(path)) return;

            myMediaPlayer?.Stop();
            myMediaPlayer = new MediaPlayer();
            myMediaPlayer.Open(new Uri(path));
            myMediaPlayer.Play();
        }
    }
}
