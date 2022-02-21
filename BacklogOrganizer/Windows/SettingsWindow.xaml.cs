using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.TfsConnectors;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;

using Microsoft.TeamFoundation.Client;


namespace BacklogOrganizer.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private readonly Action<bool> mySettingsWindowClosed;
        private bool myProjectsCacheCleared;

        // ReSharper disable once AssignNullToNotNullAttribute
        private readonly string myReminderSoundsPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Reminders\\Sounds");
        private MediaPlayer myMediaPlayer;
        private List<FileInfo> myReminderSoundFiles = new List<FileInfo>();

        public SettingsWindow(Action<bool> settingsWindowClosed)
        {
            InitializeComponent();
            mySettingsWindowClosed = settingsWindowClosed;
        }

        private void SettingsWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            ReminderSoundComboBox.Visibility = Visibility.Collapsed;
            TfsServerConnectionErrorLabel.Visibility = Visibility.Collapsed;

            TfsUsernameTextBox.Text = BacklogOrganizerConfiguration.TfsUserName;
            TfsServerTextBox.Text = BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath;
            IncludedProjectCollectionsTextBox.Text = UtilityMethods.GetCommaSeparatedString(BacklogOrganizerConfiguration.TfsConfiguration.IncludedProjectCollections);
            IncludedProjectTypesTextBox.Text = UtilityMethods.GetCommaSeparatedString(BacklogOrganizerConfiguration.TfsConfiguration.IncludedProjectTypes);

            NewTasksAreaPathTextBox.Text = BacklogOrganizerConfiguration.TfsConfiguration.TfsTaskAreaPath;
            ExcludedWorkItemTypesTextBox.Text = UtilityMethods.GetCommaSeparatedString(BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemTypes);
            ExcludedWorkItemStatesTextBox.Text = UtilityMethods.GetCommaSeparatedString(BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemStates);
            ExcludedWorkItemAreaPathsTextBox.Text = UtilityMethods.GetCommaSeparatedString(BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemAreaPaths);

            ShowIterationWorkItemsTabCheckBox.IsChecked = BacklogOrganizerConfiguration.AppConfiguration.ShowIterationTab;
            ShowTeamMembersWorkItemsTabCheckBox.IsChecked = BacklogOrganizerConfiguration.AppConfiguration.ShowTeamMembersTab;

            WorkItemNotUpdatedDuration.Value = BacklogOrganizerConfiguration.AppConfiguration.WorkItemNotUpdatedDuration;

            PlayReminderSoundCheckBox.IsChecked = BacklogOrganizerConfiguration.AppConfiguration.PlayReminderSound;
            ReminderSnoozeDuration.Value = BacklogOrganizerConfiguration.AppConfiguration.ReminderSnoozeDuration;
            LoadReminderSounds();

            ReminderSoundComboBox.SelectionChanged += ReminderSoundComboBox_OnSelectionChanged;
        }

        private void LoadReminderSounds()
        {
            try
            {
                myReminderSoundFiles = new DirectoryInfo(myReminderSoundsPath).GetFiles().ToList();
                var names = myReminderSoundFiles.Select(x => new ComboBoxKeyValue(x.FullName, x.Name.Replace(x.Extension, string.Empty))).ToList();
                ReminderSoundComboBox.ItemsSource = names;
                ReminderSoundComboBox.DataContext = names;

                var index = myReminderSoundFiles.FindIndex(x => x.Name.Equals(BacklogOrganizerConfiguration.AppConfiguration.ReminderSound));
                if (index >= 0)
                {
                    ReminderSoundComboBox.SelectedIndex = index;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void ClearProjectsCacheButton_OnClick(object sender, RoutedEventArgs e)
        {
            BacklogOrganizerConfiguration.SavedTfsTeamsData = new SavedTfsTeamsData();

            ProjectStructureRepository.Instance.ClearData();
            ProjectStructureRepository.Instance.Load();

            TfsTeamsRepository.Instance.ClearData();

            MessageBoxHelper.ShowInfoMessage("Cache Cleared!", "Project structure cache cleared successfully.");
            myProjectsCacheCleared = true;
        }

        private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            myMediaPlayer?.Stop();
            if (!IsSettingsStateValid())
            {
                e.Cancel = true;
                return;
            }
            ReminderSoundComboBox.SelectionChanged -= ReminderSoundComboBox_OnSelectionChanged;

            BacklogOrganizerConfiguration.AppConfiguration.ReminderSnoozeDuration = ReminderSnoozeDuration.Value ?? 5;

            BacklogOrganizerConfiguration.AppConfiguration.WorkItemNotUpdatedDuration = WorkItemNotUpdatedDuration.Value ?? 24;

            BacklogOrganizerConfiguration.TfsConfiguration.TfsTaskAreaPath = NewTasksAreaPathTextBox.Text.Trim();
            BacklogOrganizerConfiguration.AppConfiguration.ShowIterationTab = ShowIterationWorkItemsTabCheckBox.IsChecked ?? true;
            BacklogOrganizerConfiguration.AppConfiguration.ShowTeamMembersTab = ShowTeamMembersWorkItemsTabCheckBox.IsChecked ?? false;
            mySettingsWindowClosed?.Invoke(myProjectsCacheCleared);
        }
        private bool IsSettingsStateValid()
        {
            var isValid = TryTfsConnection();
            return isValid;
        }
        private bool TryTfsConnection()
        {
            try
            {
                var serverPath = TfsServerTextBox.Text.Trim();
                var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(serverPath));
                if(tfsConfigurationServer == null) throw new Exception();
                BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath = serverPath;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                TfsServerConnectionErrorLabel.Visibility = Visibility.Visible;
                return false;
            }
        }

        private void IncludedCollectionsEditButton_OnClick(object sender, RoutedEventArgs e)
        {
            new ListInputWindow(BacklogOrganizerConfiguration.TfsConfiguration.IncludedProjectCollections,
                list => { BacklogOrganizerConfiguration.TfsConfiguration.IncludedProjectCollections = list; }).ShowDialog();
        }

        private void IncludedProjectsEditButton_OnClick(object sender, RoutedEventArgs e)
        {
            new ListInputWindow(BacklogOrganizerConfiguration.TfsConfiguration.IncludedProjectTypes,
                list => { BacklogOrganizerConfiguration.TfsConfiguration.IncludedProjectTypes = list; }).ShowDialog();
        }

        private void ExcludedWorkItemTypesEditButton_OnClick(object sender, RoutedEventArgs e)
        {
            new ListInputWindow(BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemTypes,
                list => { BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemTypes = list; }).ShowDialog();
        }

        private void ExcludedWorkItemStatesEditButton_OnClick(object sender, RoutedEventArgs e)
        {
            new ListInputWindow(BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemStates,
                list => { BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemStates = list; }).ShowDialog();
        }

        private void ExcludedWorkItemAreaPathsEditButton_OnClick(object sender, RoutedEventArgs e)
        {
            new ListInputWindow(BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemAreaPaths,
                list => { BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemAreaPaths = list; }).ShowDialog();
        }

        private void PlayReminderSoundCheckBox_OnCheckChanged(object sender, RoutedEventArgs e)
        {
            var playSound = PlayReminderSoundCheckBox.IsChecked.GetValueOrDefault();
            ReminderSoundComboBox.Visibility = playSound ? Visibility.Visible : Visibility.Collapsed;
            BacklogOrganizerConfiguration.AppConfiguration.PlayReminderSound = playSound;
        }

        private void ReminderSoundComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReminderSoundComboBox.SelectedItem is ComboBoxKeyValue comboBoxKeyValue)
            {
                var index = myReminderSoundFiles.FindIndex(x => x.FullName.Equals(comboBoxKeyValue.Key));
                if (index >= 0)
                {
                    BacklogOrganizerConfiguration.AppConfiguration.ReminderSound = myReminderSoundFiles[index].Name;
                    PlaySound(comboBoxKeyValue.Key);
                }
            }
        }

        private void PlaySound(string filePath)
        {
            if (!File.Exists(filePath)) return;

            myMediaPlayer?.Stop();
            myMediaPlayer = new MediaPlayer();
            myMediaPlayer.Open(new Uri(filePath));
            myMediaPlayer.Play();
        }
    }
}
