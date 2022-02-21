using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BacklogOrganizer.Utilities;

using Newtonsoft.Json;


namespace BacklogOrganizer.Configuration
{
    internal class TfsConfiguration
    {
        private string myTfsServerPath = "https://apollo.healthcare.siemens.com/tfs";
        public string TfsServerPath
        {
            get => myTfsServerPath;
            set
            {
                myTfsServerPath = value;
                Save();
            }
        }

        private List<string> myIncludedProjectCollections = new List<string>();
        public List<string> IncludedProjectCollections
        {
            get => myIncludedProjectCollections;
            set
            {
                myIncludedProjectCollections = value;
                Save();
            }
        }

        private List<string> myIncludedProjectTypes = new List<string>();
        public List<string> IncludedProjectTypes
        {
            get => myIncludedProjectTypes;
            set
            {
                myIncludedProjectTypes = value;
                Save();
            }
        }

        private string myTfsTaskAreaPath = @"MI\Jaguar";
        public string TfsTaskAreaPath
        {
            get => myTfsTaskAreaPath;
            set
            {
                myTfsTaskAreaPath = value;
                Save();
            }
        }

        private List<string> myExcludedWorkItemTypes = new List<string> { "Code Review Request", "Code Review Participant", "Code Review Response" };
        public List<string> ExcludedWorkItemTypes
        {
            get => myExcludedWorkItemTypes;
            set
            {
                myExcludedWorkItemTypes = value;
                Save();
            }
        }

        private List<string> myExcludedWorkItemStates = new List<string> { "Terminated", "Done", "Removed", "Closed", "Implemented", "Deferred", "Resolved" };
        public List<string> ExcludedWorkItemStates
        {
            get => myExcludedWorkItemStates;
            set
            {
                myExcludedWorkItemStates = value;
                Save();
            }
        }

        private List<string> myExcludedWorkItemAreaPaths = new List<string> { @"MI\ZZ_Obsolete", @"MI\Trash" };
        public List<string> ExcludedWorkItemAreaPaths
        {
            get => myExcludedWorkItemAreaPaths;
            set
            {
                myExcludedWorkItemAreaPaths = value;
                Save();
            }
        }
        
        private static string myTfsConfigurationFilePath;
        public TfsConfiguration()
        {
        }
        public TfsConfiguration(int configurationVersion) : this()
        {
            try
            {
                if (string.IsNullOrEmpty(myTfsConfigurationFilePath))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    myTfsConfigurationFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                        "Configuration", $"TfsConfiguration_v{configurationVersion}.txt");
                }

                if (!File.Exists(myTfsConfigurationFilePath)) return;

                var jsonString = File.ReadAllText(myTfsConfigurationFilePath);
                if (string.IsNullOrEmpty(jsonString)) return;

                var tfsConfiguration = JsonConvert.DeserializeObject<TfsConfiguration>(jsonString, new JsonSerializerSettings(){});
                if (tfsConfiguration == null) return;

                TfsServerPath = tfsConfiguration.TfsServerPath;
                IncludedProjectCollections = tfsConfiguration.IncludedProjectCollections.Distinct().ToList();
                IncludedProjectTypes = tfsConfiguration.IncludedProjectTypes.Distinct().ToList();
                TfsTaskAreaPath = tfsConfiguration.TfsTaskAreaPath;
                ExcludedWorkItemTypes = tfsConfiguration.ExcludedWorkItemTypes.Distinct().ToList();
                ExcludedWorkItemStates = tfsConfiguration.ExcludedWorkItemStates.Distinct().ToList();
                ExcludedWorkItemAreaPaths = tfsConfiguration.ExcludedWorkItemAreaPaths.Distinct().ToList();
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
                File.WriteAllText(myTfsConfigurationFilePath, serializedValue);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Could not save tfs configuration", ex);
            }
        }
    }
}
