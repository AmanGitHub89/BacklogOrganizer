using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

using BacklogOrganizer.Reminders;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;

using Newtonsoft.Json;


namespace BacklogOrganizer.Configuration
{
    internal class BacklogOrganizerConfiguration
    {
        public static AppConfiguration AppConfiguration { get; }
        public static TfsConfiguration TfsConfiguration { get; }

        static BacklogOrganizerConfiguration()
        {
            const int configurationVersion = 2;
            AppConfiguration = new AppConfiguration(configurationVersion);
            TfsConfiguration = new TfsConfiguration(configurationVersion);
        }


        // ReSharper disable once AssignNullToNotNullAttribute
        private static readonly string myProjectStructureFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), 
            "Configuration", "ProjectStructure.txt");
        // ReSharper disable once AssignNullToNotNullAttribute
        private static readonly string mySavedTfsTeamsDataFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), 
            "Configuration", "SavedTfsTeamsData_v2.txt");
        // ReSharper disable once AssignNullToNotNullAttribute
        private static readonly string myRemindersDataFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "Reminders", "BacklogOrganizerReminders.txt");


        private static string myOnHoldWorkItemIds;
        public static string OnHoldWorkItemIds
        {
            get => myOnHoldWorkItemIds ?? (myOnHoldWorkItemIds = GetValue(nameof(OnHoldWorkItemIds)));
            set
            {
                myOnHoldWorkItemIds = value;
                UpdateValue(nameof(OnHoldWorkItemIds), value);
            }
        }

        private static string myLastSelectedProjectCatalogName;
        public static string LastSelectedProjectCatalogName
        {
            get => myLastSelectedProjectCatalogName ?? (myLastSelectedProjectCatalogName = GetValue(nameof(LastSelectedProjectCatalogName)));
            set
            {
                myLastSelectedProjectCatalogName = value;
                UpdateValue(nameof(LastSelectedProjectCatalogName), value);
            }
        }

        private static string myLastSelectedProjectTypeName;
        public static string LastSelectedProjectTypeName
        {
            get => myLastSelectedProjectTypeName ?? (myLastSelectedProjectTypeName = GetValue(nameof(LastSelectedProjectTypeName)));
            set
            {
                myLastSelectedProjectTypeName = value;
                UpdateValue(nameof(LastSelectedProjectTypeName), value);
            }
        }

        private static string myLastSelectedTeamTabTfsTeam;
        public static string LastSelectedTeamTabTfsTeam
        {
            get => myLastSelectedTeamTabTfsTeam ?? (myLastSelectedTeamTabTfsTeam = GetValue(nameof(LastSelectedTeamTabTfsTeam)));
            set
            {
                myLastSelectedTeamTabTfsTeam = value;
                UpdateValue(nameof(LastSelectedTeamTabTfsTeam), value);
            }
        }

        private static string myTaskStates;
        public static string TaskStates
        {
            get => myTaskStates ?? (myTaskStates = GetValue(nameof(TaskStates)));
            set
            {
                myTaskStates = value;
                UpdateValue(nameof(TaskStates), value);
            }
        }


        public static List<Reminder> mySavedReminders;
        public static List<Reminder> SavedReminders
        {
            get
            {
                try
                {
                    if (mySavedReminders != null) return mySavedReminders;

                    if (!File.Exists(myRemindersDataFilePath)) return new List<Reminder>();

                    var jsonString = File.ReadAllText(myRemindersDataFilePath);
                    if (string.IsNullOrEmpty(jsonString)) jsonString = "[]";

                    var savedReminders = JsonConvert.DeserializeObject<List<Reminder>>(jsonString);
                    if (savedReminders != null && savedReminders.Count > 0)
                    {
                        mySavedReminders = savedReminders;
                    }
                    return mySavedReminders ?? new List<Reminder>();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return new List<Reminder>();
                }
            }
            set
            {
                try
                {
                    mySavedReminders = value;
                    var serializedValue = JsonConvert.SerializeObject(value);
                    File.WriteAllText(myRemindersDataFilePath, serializedValue);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }


        public static ProjectStructure myProjectStructure;
        public static ProjectStructure ProjectStructure
        {
            get
            {
                try
                {
                    if (myProjectStructure != null) return myProjectStructure;

                    if (!File.Exists(myProjectStructureFilePath)) return null;

                    var jsonString = File.ReadAllText(myProjectStructureFilePath);
                    if (string.IsNullOrEmpty(jsonString)) return null;

                    var settings = new JsonSerializerSettings { ContractResolver = new ProjectStructureJsonContractResolver() };
                    var projectStructure = JsonConvert.DeserializeObject<ProjectStructure>(jsonString, settings);
                    myProjectStructure = projectStructure;
                    return myProjectStructure;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return null;
                }
            }
            set
            {
                try
                {
                    myProjectStructure = value;
                    var serializedValue = JsonConvert.SerializeObject(value);
                    File.WriteAllText(myProjectStructureFilePath, serializedValue);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        public static SavedTfsTeamsData mySavedTfsTeamsData;
        public static SavedTfsTeamsData SavedTfsTeamsData
        {
            get
            {
                try
                {
                    if (mySavedTfsTeamsData != null) return mySavedTfsTeamsData;

                    if (!File.Exists(mySavedTfsTeamsDataFilePath)) return new SavedTfsTeamsData();

                    var jsonString = File.ReadAllText(mySavedTfsTeamsDataFilePath);
                    if (string.IsNullOrEmpty(jsonString)) jsonString = "{}";

                    var savedTfsTeamsData = JsonConvert.DeserializeObject<SavedTfsTeamsData>(jsonString);
                    //if (savedTfsTeamsData.UpdatedDateTime >= DateTime.Now.AddHours(-72))
                    //{
                    //    mySavedTfsTeamsData = savedTfsTeamsData;
                    //}
                    if (savedTfsTeamsData != null && savedTfsTeamsData.SavedTfsTeamsList.Count > 0)
                    {
                        mySavedTfsTeamsData = savedTfsTeamsData;
                    }
                    return mySavedTfsTeamsData ?? new SavedTfsTeamsData();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return new SavedTfsTeamsData();
                }
            }
            set
            {
                try
                {
                    mySavedTfsTeamsData = value;
                    var serializedValue = JsonConvert.SerializeObject(value);
                    File.WriteAllText(mySavedTfsTeamsDataFilePath, serializedValue);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }


        public static string myTfsUserName;
        public static string TfsUserName
        {
            get => myTfsUserName ?? (myTfsUserName = GetValue(nameof(TfsUserName)));
            set
            {
                myTfsUserName = value;
                UpdateValue(nameof(TfsUserName), value);
            }
        }


        private static string GetValue(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }

        private static void UpdateValue(string name, object value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (!config.AppSettings.Settings.AllKeys.Contains(name))
            {
                config.AppSettings.Settings.Add(name, value.ToString());
            }
            else
            {
                config.AppSettings.Settings[name].Value = value.ToString();
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
