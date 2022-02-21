using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.SingleInstance;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.Reminders
{
    internal class RemindersService
    {
        internal static RemindersService Instance { get; private set; }

        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<Timer> myActiveReminderTimers = new List<Timer>();
        private List<Reminder> myActiveReminders = new List<Reminder>();
        private static Dispatcher myDispatcherObject;
        private ShowRemindersWindow myShowRemindersWindow;


        private RemindersService()
        {
            LoadReminders();
        }

        public static void StartService(Dispatcher dispatcherObject)
        {
            if (Instance != null) throw new Exception("Service already initialized.");
            myDispatcherObject = dispatcherObject;
            Instance = new RemindersService();
        }

        private void LoadReminders()
        {
            try
            {
                var reminders = BacklogOrganizerConfiguration.SavedReminders;
                if (reminders == null || reminders.Count == 0) return;


                var missedReminders = reminders.Where(x => x.ReminderDateTime <= DateTime.Now).ToList();
                var activeReminders = reminders.Where(x => !missedReminders.Any(y => y.Equals(x))).ToList();

                myActiveReminders = activeReminders;
                BacklogOrganizerConfiguration.SavedReminders = myActiveReminders;

                foreach (var activeReminder in myActiveReminders)
                {
                    ScheduleReminderInternal(activeReminder);
                }

                if (missedReminders.Count > 0)
                {
                    BacklogOrganizerApplication.Instance.ActivateMainWindow(null);
                    ShowMissedReminders(missedReminders);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public void ScheduleReminder(Reminder reminder)
        {
            if (myActiveReminders.Contains(reminder)) return;
            ScheduleReminderInternal(reminder);
            myActiveReminders.Add(reminder);
            BacklogOrganizerConfiguration.SavedReminders = myActiveReminders;
        }

        public void ScheduleReminderInternal(Reminder reminder)
        {
            if (reminder.ReminderDateTime < DateTime.Now)
                throw new Exception("Cannot set a reminder in the past.");

            var timeToRun = reminder.ReminderDateTime - DateTime.Now;
            if (timeToRun <= TimeSpan.Zero)
            {
                timeToRun = TimeSpan.Zero;
            }
            var timer = new Timer(x =>
            {
                ShowReminder(reminder);
            }, null, timeToRun, TimeSpan.FromMilliseconds(-1));
            myActiveReminderTimers.Add(timer);
        }

        private void ShowReminder(Reminder reminder)
        {
            myActiveReminders.Remove(reminder);
            BacklogOrganizerConfiguration.SavedReminders = myActiveReminders;
            RunOnUIThread(() =>
            {
                if (myShowRemindersWindow == null)
                {
                    myShowRemindersWindow = new ShowRemindersWindow(new List<Reminder> { reminder }, () => { myShowRemindersWindow = null; });
                    myShowRemindersWindow.Show();
                    myShowRemindersWindow.Activate();
                }
                else
                {
                    myShowRemindersWindow.ShowReminders(new List<Reminder> { reminder });
                    myShowRemindersWindow.Activate();
                }
            });
        }

        private void ShowMissedReminders(List<Reminder> missedReminders)
        {
            foreach (var missedReminder in missedReminders)
            {
                missedReminder.IsMissedReminder = true;
            }
            myShowRemindersWindow = new ShowRemindersWindow(missedReminders, () => { myShowRemindersWindow = null; });
            myShowRemindersWindow.Show();
        }

        private void RunOnUIThread(Action action)
        {
            myDispatcherObject.Invoke(() => {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Logger.Error("RunOnUIThread - Error", ex);
                }
            });
        }
    }
}
