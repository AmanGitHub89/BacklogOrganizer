using System;
using System.IO;

using BacklogOrganizer.Utilities;

using Newtonsoft.Json;


namespace BacklogOrganizer.Configuration
{
    internal class AppConfiguration
    {
        private int myRefreshInterval = 5;
        public int RefreshInterval
        {
            get => myRefreshInterval;
            set
            {
                myRefreshInterval = value;
                Save();
            }
        }

        private bool myPlayReminderSound = true;
        public bool PlayReminderSound
        {
            get => myPlayReminderSound;
            set
            {
                myPlayReminderSound = value;
                Save();
            }
        }

        private string myReminderSound = "beeps.mp3";
        public string ReminderSound
        {
            get => myReminderSound;
            set
            {
                myReminderSound = value;
                Save();
            }
        }

        private int myReminderSnoozeDuration = 5;
        public int ReminderSnoozeDuration
        {
            get => myReminderSnoozeDuration;
            set
            {
                myReminderSnoozeDuration = value;
                Save();
            }
        }

        private int myWorkItemNotUpdatedDuration = 24;
        public int WorkItemNotUpdatedDuration
        {
            get => myWorkItemNotUpdatedDuration;
            set
            {
                myWorkItemNotUpdatedDuration = value;
                Save();
            }
        }

        private bool myShowIterationTab = true;
        public bool ShowIterationTab
        {
            get => myShowIterationTab;
            set
            {
                myShowIterationTab = value;
                Save();
            }
        }

        private bool myShowTeamMembersTab;
        public bool ShowTeamMembersTab
        {
            get => myShowTeamMembersTab;
            set
            {
                myShowTeamMembersTab = value;
                Save();
            }
        }

        private static string myAppConfigurationFilePath;

        public AppConfiguration()
        {

        }

        public AppConfiguration(int configurationVersion) : this()
        {
            try
            {
                if (string.IsNullOrEmpty(myAppConfigurationFilePath))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    myAppConfigurationFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                        "Configuration", $"AppConfiguration_v{configurationVersion}.txt");
                }

                if (!File.Exists(myAppConfigurationFilePath)) return;

                var jsonString = File.ReadAllText(myAppConfigurationFilePath);
                if (string.IsNullOrEmpty(jsonString)) return;

                var appConfiguration = JsonConvert.DeserializeObject<AppConfiguration>(jsonString);
                if (appConfiguration == null) return;

                RefreshInterval = appConfiguration.RefreshInterval == 0 ? 5 : appConfiguration.RefreshInterval;
                PlayReminderSound = appConfiguration.PlayReminderSound;
                ReminderSound = appConfiguration.ReminderSound;

                var notUpdatedDuration = appConfiguration.WorkItemNotUpdatedDuration;
                WorkItemNotUpdatedDuration = notUpdatedDuration < 24 || notUpdatedDuration > 48 ? 24 : notUpdatedDuration;

                var duration = appConfiguration.ReminderSnoozeDuration;
                ReminderSnoozeDuration = duration < 5 || duration > 30 ? 5 : duration;

                ShowIterationTab = appConfiguration.ShowIterationTab;
                ShowTeamMembersTab = appConfiguration.ShowTeamMembersTab;
            }
            catch (Exception ex)
            {
                Logger.Fatal("Could not load app configuration", ex);
            }
        }


        private void Save()
        {
            try
            {
                var serializedValue = JsonConvert.SerializeObject(this);
                File.WriteAllText(myAppConfigurationFilePath, serializedValue);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Could not save configuration", ex);
            }
        }
    }
}
