using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.TfsConnectors;
using BacklogOrganizer.TfsConnectors.WorkItemRepository;
using BacklogOrganizer.Types;
using BacklogOrganizer.UserControls.DataGrids;
using BacklogOrganizer.Utilities;
using BacklogOrganizer.Windows;


namespace BacklogOrganizer.UserControls.Tabs
{
    /// <summary>
    /// Interaction logic for TeamWorkItemsTabControl.xaml
    /// </summary>
    public partial class TeamWorkItemsTab
    {
        private SavedTfsTeams myProjectTeams = new SavedTfsTeams();
        private Dispatcher myDispatcherObject;
        private readonly IWorkItemsRepository myWorkItemsRepository;
        private bool myIsLoaded;

        public event EventHandler<int> TeamWorkItemDisplayedCountChanged;

        public TeamWorkItemsTab()
        {
            InitializeComponent();
            ProjectStructureRepository.Instance.ProjectStructureUpdated += ProjectStructureUpdated;
            myWorkItemsRepository = TeamWorkItemsRepository.Instance;
            myWorkItemsRepository.WorkItemsUpdatingStatusChanged += OnWorkItemsUpdatingStatusChanged;
        }

        private void TeamWorkItemsTabControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            myDispatcherObject = Application.Current.Dispatcher;

            if (myIsLoaded) return;
            myIsLoaded = true;
            Load();
        }

        private void ProjectStructureUpdated(object sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                ClearAllUiData();
                Load();
            });
        }

        public void OnClosing()
        {
            myIsLoaded = false;
            ClearAllUiData();
        }

        public void ClearAllUiData()
        {
            MyTfsTeamsComboBox.ItemsSource = null;
            MyTfsTeamsComboBox.DataContext = null;

            TeamMemberNamesComboBox.ItemsSource = null;
            TeamMemberNamesComboBox.DataContext = null;

            TeamEditButton.Visibility = Visibility.Collapsed;
            TeamMembersEditButton.Visibility = Visibility.Collapsed;

            ReloadTeams.Visibility = Visibility.Collapsed;

            GetTeamWorkItemsButton.IsEnabled = false;

            TeamWorkItemDisplayedCountChanged?.Invoke(this, 0);
        }
        public async void Load()
        {
            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            if (selectedCatalogNode == null) return;

            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return;

            SetUiForTeamWorkItemsLoadingState(true, false, null);
            myProjectTeams = await TfsTeamsRepository.Instance.GetAllTeams(selectedCatalogNode, selectedProjectTypeNode);
            SetUiForTeamWorkItemsLoadingState(false, false, null);

            var teamNames = myProjectTeams.TfsTeamsList.Select(x => x.TeamName).ToList();
            MyTfsTeamsComboBox.ItemsSource = myProjectTeams.TfsTeamsList;
            MyTfsTeamsComboBox.DataContext = myProjectTeams.TfsTeamsList;

            if (MyTfsTeamsComboBox.Items.Count > 0)
            {
                var lastSelectedTeamTabTfsTeam = BacklogOrganizerConfiguration.LastSelectedTeamTabTfsTeam;
                if (!string.IsNullOrEmpty(lastSelectedTeamTabTfsTeam))
                {
                    var index = teamNames.FindIndex(x => x.CaseInsensitiveEquals(lastSelectedTeamTabTfsTeam));
                    if (index >= 0)
                    {
                        MyTfsTeamsComboBox.SelectedIndex = index;
                        UpdateTeamsGridData();
                        return;
                    }
                }
                var userTeams = TfsTeamsRepository.Instance.GetMyTeams(selectedCatalogNode, selectedProjectTypeNode);
                var userFirstTeam = userTeams.FirstOrDefault();
                MyTfsTeamsComboBox.SelectedIndex = userFirstTeam != null ? MyTfsTeamsComboBox.Items.Cast<SavedTfsTeam>().Select(x => x.TeamName).ToList().IndexOf(userFirstTeam.TeamName) : 0;
                UpdateTeamsGridData();
            }
        }
        public void CollapseAll()
        {
            foreach (var tfsWorkItem in myWorkItemsRepository.TfsWorkItems)
            {
                tfsWorkItem.IsExpanded = false;
            }
            UpdateTeamsGridData();
        }


        private void MyTfsTeamsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(MyTfsTeamsComboBox.SelectedItem is SavedTfsTeam selectedTeam)) return;
            BacklogOrganizerConfiguration.LastSelectedTeamTabTfsTeam = selectedTeam.TeamName;

            var administrators = GetSelectedTeam().TeamAdministrators;
            MyTfsTeamsComboBox.ToolTip = administrators.Count > 0 ? $"Administrators:{Environment.NewLine}{string.Join(Environment.NewLine, administrators)}" : null;

            UpdateTeamMemberNamesComboBox();
            myWorkItemsRepository.TfsWorkItems.Clear();
            UpdateTeamsGridData();
            GetTeamWorkItemsButton.IsEnabled = true;
        }

        private void UpdateTeamMemberNamesComboBox()
        {
            var names = GetSelectedTeam().GetTeamMembersToDisplay();
            if (names.Count < 0) return;
            var teamMembers = new List<string> { "All" };
            teamMembers.AddRange(names);
            TeamMemberNamesComboBox.ItemsSource = teamMembers;
            TeamMemberNamesComboBox.DataContext = teamMembers;
            TeamMemberNamesComboBox.SelectedIndex = 0;
        }

        private void GetTeamWorkItems()
        {
            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            if (selectedCatalogNode == null) return;

            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return;

            if (!(MyTfsTeamsComboBox.SelectedItem is SavedTfsTeam selectedTeam)) return;
            var team = myProjectTeams.TfsTeamsList.First(x => x.TeamName.Equals(selectedTeam.TeamName));

            myWorkItemsRepository.WithUsers(team.GetTeamMembersToDisplay()).GetWorkItems(selectedCatalogNode, selectedProjectTypeNode, null);
        }

        private void SetUiForTeamWorkItemsLoadingState(bool isLoadingTeams, bool isLoadingWorkItems, RepositoryTaskCompletionResult result)
        {
            if (isLoadingTeams)
            {
                SetTeamWorkItemTabStatusLabel(true, "Getting list of Teams from TFS...");

                MyTfsTeamsComboBox.ItemsSource = null;
                MyTfsTeamsComboBox.DataContext = null;

                TeamMemberNamesComboBox.ItemsSource = null;
                TeamMemberNamesComboBox.DataContext = null;

                TeamEditButton.Visibility = Visibility.Collapsed;
                TeamMembersEditButton.Visibility = Visibility.Collapsed;

                GetTeamWorkItemsButton.IsEnabled = false;
                return;
            }
            else
            {
                if (myProjectTeams.TfsTeamsList.Count == 0)
                {
                    SetTeamWorkItemTabStatusLabel(true, "Error while loading TFS teams.");
                    ReloadTeams.Visibility = Visibility.Visible;
                    return;
                }
                SetTeamWorkItemTabStatusLabel(false, string.Empty);

                TeamEditButton.Visibility = Visibility.Visible;
                TeamMembersEditButton.Visibility = Visibility.Visible;
                GetTeamWorkItemsButton.IsEnabled = true;
            }

            if (isLoadingWorkItems)
            {
                SetTeamWorkItemTabStatusLabel(true, "Getting work items for Team Members from TFS...");
                MyTfsTeamsComboBox.IsEnabled = false;
                TeamMemberNamesComboBox.IsEnabled = false;
                TeamMembersEditButton.IsEnabled = false;
                GetTeamWorkItemsButton.IsEnabled = false;
            }
            else
            {
                if (result != null && myWorkItemsRepository.TfsWorkItems.Count == 0)
                {
                    SetTeamWorkItemTabStatusLabel(true, result.IsSuccess ? "No work items found." : result.DisplayMessage);
                }
                else
                {
                    SetTeamWorkItemTabStatusLabel(false, string.Empty);
                }
                MyTfsTeamsComboBox.IsEnabled = true;
                TeamMemberNamesComboBox.IsEnabled = true;
                TeamMembersEditButton.IsEnabled = true;
                GetTeamWorkItemsButton.IsEnabled = true;
            }
        }

        private List<string> GetProjectAllTeamMemberNames()
        {
            var allTeamForProject = TfsTeamsRepository.Instance.GetAllTeamsForSelectedProject();
            if (allTeamForProject == null) return new List<string>();

            var memberNames = new List<string>();
            foreach (var team in allTeamForProject.TfsTeamsList)
            {
                memberNames.AddRange(team.OriginalTeamMembers);
            }

            memberNames.AddRange(GetSelectedTeam().GetTeamMembersToDisplay());

            return memberNames.Distinct().ToList();
        }


        private void OnWorkItemsUpdatingStatusChanged(object sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                if (myWorkItemsRepository.IsUpdating)
                {
                    SetUiForTeamWorkItemsLoadingState(false, true, null);
                    myWorkItemsRepository.TfsWorkItems.Clear();
                }
                else
                {
                    var result = myWorkItemsRepository.LastTaskCompletionResult;
                    SetUiForTeamWorkItemsLoadingState(false, false, result);
                }

                UpdateTeamsGridData();
            });
        }

        private SavedTfsTeam GetSelectedTeam()
        {
            var teamsForSelectedProject = TfsTeamsRepository.Instance.GetAllTeamsForSelectedProject();
            if (!(MyTfsTeamsComboBox.SelectedItem is SavedTfsTeam selectedTeam)) return null;

            return teamsForSelectedProject?.TfsTeamsList.FirstOrDefault(x => x.TeamName.Equals(selectedTeam.TeamName));
        }

        private void UpdateTeamsGridData()
        {
            TeamWorkItemsGrid.ItemExpanded -= OnTeamsDataGridDataChanged;
            TeamWorkItemsGrid.ItemCollapsed -= OnTeamsDataGridDataChanged;

            var displayedWorkItems = GetDisplayedWorkItems(myWorkItemsRepository.TfsWorkItems);
            if (!(TeamMemberNamesComboBox.SelectedItem is string selectedName)) return;

            if (!selectedName.CaseInsensitiveEquals("All"))
            {
                displayedWorkItems = displayedWorkItems.Where(x => x.UserName.Equals(selectedName)).ToList();
            }
            TeamWorkItemsDataGrid.ItemsSource = displayedWorkItems;
            TeamWorkItemsDataGrid.DataContext = displayedWorkItems;
            TeamWorkItemDisplayedCountChanged?.Invoke(this, displayedWorkItems.Where(x => x.Level == 0).ToList().Count);

            TeamWorkItemsGrid.ItemExpanded += OnTeamsDataGridDataChanged;
            TeamWorkItemsGrid.ItemCollapsed += OnTeamsDataGridDataChanged;
        }
        private static List<TfsWorkItem> GetDisplayedWorkItems(List<TfsWorkItem> tfsWorkItems)
        {
            var displayedWorkItems = tfsWorkItems.ToList();
            foreach (var workItem in tfsWorkItems.Where(x => x.HasChildren && x.IsExpanded).ToList())
            {
                var index = displayedWorkItems.IndexOf(workItem);
                if (index < 0) continue;
                displayedWorkItems.InsertRange(index + 1, workItem.ChildWorkItems);
            }

            return displayedWorkItems;
        }

        private void SetTeamWorkItemTabStatusLabel(bool show, string text)
        {
            MyTeamMembersWorkItemsTabStatusSection.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            MyTeamMembersWorkItemsTabStatusLabel.Content = text;
        }

        private void OnTeamsDataGridDataChanged(object sender, EventArgs e)
        {
            UpdateTeamsGridData();
        }

        private void TeamMemberNamesComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTeamsGridData();
        }

        private void TeamMembersEditButton_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedTeam = GetSelectedTeam();
            var addRemoveItemsWindow = new AddRemoveItemsWindow(GetProjectAllTeamMemberNames(), selectedTeam.GetTeamMembersToDisplay(), list =>
            {
                selectedTeam.TeamMembers = list;

                var savedTfsTeamsData = BacklogOrganizerConfiguration.SavedTfsTeamsData;
                var savedTeams = savedTfsTeamsData.SavedTfsTeamsList.FirstOrDefault(x =>
                    x.CatalogName.CaseInsensitiveEquals(myProjectTeams.CatalogName) && x.ProjectName.CaseInsensitiveEquals(myProjectTeams.ProjectName));
                var savedTeam = savedTeams?.TfsTeamsList.FirstOrDefault(x => x.TeamName.Equals(selectedTeam.TeamName));
                if (savedTeam != null) savedTeam.TeamMembers = list;

                BacklogOrganizerConfiguration.SavedTfsTeamsData = savedTfsTeamsData;
                UpdateTeamMemberNamesComboBox();
            });
            addRemoveItemsWindow.ShowDialog();
        }

        private void GetTeamWorkItemsButton_OnClick(object sender, RoutedEventArgs e)
        {
            GetTeamWorkItems();
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

        private void TeamEditButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var teamsForSelectedProject = TfsTeamsRepository.Instance.GetAllTeamsForSelectedProject();
                if (!(MyTfsTeamsComboBox.SelectedItem is SavedTfsTeam selectedTeam)) return;

                var serverPath = BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath;
                var url = $"{serverPath}/{teamsForSelectedProject.CatalogName}/{teamsForSelectedProject.ProjectName}/_settings/teams?teamId={selectedTeam.TeamId}";
                Process.Start(url);
            }
            catch (Exception ex)
            {
                Logger.Error("Error while opening url.", ex);
            }
        }

        private void ReloadTeams_OnClick(object sender, RoutedEventArgs e)
        {
            ReloadTeams.Visibility = Visibility.Collapsed;
            Load();
        }
    }
}
