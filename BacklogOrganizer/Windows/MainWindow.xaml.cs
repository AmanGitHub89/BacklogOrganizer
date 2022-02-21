using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Reminders;
using BacklogOrganizer.TfsConnectors;
using BacklogOrganizer.TfsConnectors.WorkItemRepository;
using BacklogOrganizer.Types;
using BacklogOrganizer.UserControls.DataGrids;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Dispatcher myDispatcherObject;
        private bool myEventsRegistered;
        private readonly IWorkItemsRepository myWorkItemsRepository;
        private bool myIsFirstLoad = true;

        public MainWindow()
        {
            InitializeComponent();
            NewVersionAvailableInfoPanel.Visibility = Visibility.Collapsed;
            RefreshButton.Visibility = Visibility.Collapsed;
            myWorkItemsRepository = MyWorkItemsRepository.Instance;
            Title = $"Backlog Organizer {LatestVersionChecker.GetCurrentVersion()}";
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            SetBounds();
        }
        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            myDispatcherObject = Application.Current.Dispatcher;
            RegisterEvents();

            if (myIsFirstLoad)
            {
                if (ProjectStructureRepository.Instance.TfsProjectStructure != null)
                {
                    Task.Run(() =>
                    {
                        ProjectStructureUpdated(this, null);
                    });
                }
                LatestVersionChecker.CheckLatestVersion(isLatestVersion =>
                {
                    if (!isLatestVersion)
                    {
                        RunOnUIThread(() =>
                        {
                            NewVersionAvailableInfoPanel.Visibility = Visibility.Visible;
                        });
                    }
                });
                myIsFirstLoad = false;
            }
            else
            {
                UpdateGridData(false);
            }
        }
        private void SetBounds()
        {
            var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            double scalingFactor;
            try
            {
                var presentationSource = PresentationSource.FromVisual(this);
                scalingFactor = presentationSource.CompositionTarget.TransformToDevice.M11;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                scalingFactor = 1;
            }
            Width = 900;
            Height = screenBounds.Height * 0.8 / scalingFactor;
            Top = screenBounds.Height / scalingFactor - Height - 50;
            Left = screenBounds.Width / scalingFactor - Width - 20;
        }

        public void ShowFirstTab()
        {
            TabControl1.SelectedIndex = 0;
        }


        private void RegisterEvents()
        {
            if (myEventsRegistered) return;
            myEventsRegistered = true;
            ProjectStructureRepository.Instance.ProjectStructureUpdated += ProjectStructureUpdated;
            myWorkItemsRepository.WorkItemsUpdatingStatusChanged += OnWorkItemsUpdatingStatusChanged;

            WorkItemGridActions.WorkItemActiveStateChanged += OnDataGridDataChanged;
            AddTaskWindow.TaskAdded += OnDataGridDataChanged;
            TeamWorkItemsTabControl.TeamWorkItemDisplayedCountChanged += OnTeamWorkItemDisplayedCountChanged;
            IterationWorkItemsTabControl.IterationWorkItemsDisplayedCountChanged += OnIterationWorkItemsDisplayedCountChanged;
        }

        private void UnRegisterEvents()
        {
            myEventsRegistered = false;
            ProjectStructureRepository.Instance.ProjectStructureUpdated -= ProjectStructureUpdated;
            myWorkItemsRepository.WorkItemsUpdatingStatusChanged -= OnWorkItemsUpdatingStatusChanged;

            WorkItemGridActions.WorkItemActiveStateChanged -= OnDataGridDataChanged;
            AddTaskWindow.TaskAdded -= OnDataGridDataChanged;

            TeamWorkItemsTabControl.OnClosing();
            IterationWorkItemsTabControl.OnClosing();

            TeamWorkItemsTabControl.TeamWorkItemDisplayedCountChanged -= OnTeamWorkItemDisplayedCountChanged;
            IterationWorkItemsTabControl.IterationWorkItemsDisplayedCountChanged -= OnIterationWorkItemsDisplayedCountChanged;
        }

        //Select Project Catalog and Project and then Get Work Items
        private void ProjectStructureUpdated(object sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                ClearAllUiData();
                SetErrorInfoPanelData();

                if (ProjectStructureRepository.Instance.TfsProjectStructure == null) return;

                AddTaskButton.IsEnabled = true;
                IterationWorkItemsTabItem.Visibility = BacklogOrganizerConfiguration.AppConfiguration.ShowIterationTab ? Visibility.Visible : Visibility.Collapsed;
                MyTeamMembersWorkItemsTabItem.Visibility = BacklogOrganizerConfiguration.AppConfiguration.ShowTeamMembersTab ? Visibility.Visible : Visibility.Collapsed;

                ProjectCatalogComboBox.ItemsSource =
                    ProjectStructureRepository.Instance.TfsProjectStructure.ProjectCatalogNodeList.Select(x => new ComboBoxKeyValue(x.Id.ToString(), x.Name));
                if (ProjectCatalogComboBox.Items.Count == 0) return;

                SelectProjectCatalogInComboBox();
            });
        }

        private void SelectProjectCatalogInComboBox()
        {
            var itemToSelect = ProjectStructureRepository.Instance.TfsProjectStructure.ProjectCatalogNodeList.FirstOrDefault(x =>
                x.Name.Equals(BacklogOrganizerConfiguration.LastSelectedProjectCatalogName));
            if (itemToSelect != null)
            {
                var match = ProjectCatalogComboBox.Items.Cast<ComboBoxKeyValue>().FirstOrDefault(x => x.ToString().Equals(itemToSelect.Name));
                if (match != null)
                {
                    var index = ProjectCatalogComboBox.Items.IndexOf(match);
                    if (index >= 0)
                    {
                        ProjectCatalogComboBox.SelectedIndex = index;
                        return;
                    }
                }
            }
            ProjectCatalogComboBox.SelectedIndex = 0;
        }
        private void SelectProjectTypeInComboBox()
        {
            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            var itemToSelect = selectedCatalogNode?.ProjectInfoNodes.FirstOrDefault(x => x.Name.Equals(BacklogOrganizerConfiguration.LastSelectedProjectTypeName));
            if (itemToSelect != null)
            {
                var match = ProjectTypeComboBox.Items.Cast<ComboBoxKeyValue>().FirstOrDefault(x => x.ToString().Equals(itemToSelect.Name));
                if (match != null)
                {
                    var index = ProjectTypeComboBox.Items.IndexOf(match);
                    if (index >= 0)
                    {
                        ProjectTypeComboBox.SelectedIndex = index;
                        return;
                    }
                }
            }
            ProjectTypeComboBox.SelectedIndex = 0;
        }
        private void ProjectCatalogComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCatalogNode = GetSelectedProjectCatalogNode();
            if (selectedCatalogNode == null) return;

            BacklogOrganizerConfiguration.LastSelectedProjectCatalogName = selectedCatalogNode.Name;

            ProjectTypeComboBox.ItemsSource =
                selectedCatalogNode.ProjectInfoNodes.Select(x => new ComboBoxKeyValue(x.Id.ToString(), x.Name));
            if (ProjectTypeComboBox.Items.Count == 0) return;

            SelectProjectTypeInComboBox();
        }
        private void ProjectTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProjectTypeNode = GetSelectedProjectTypeNode();
            if (selectedProjectTypeNode == null) return;
            BacklogOrganizerConfiguration.LastSelectedProjectTypeName = selectedProjectTypeNode.Name;

            var selectedCatalogNode = GetSelectedProjectCatalogNode();
            if (selectedCatalogNode == null) return;
            ProjectStructureRepository.Instance.UpdateProjectIterationAndAreaPathAsync(selectedCatalogNode, selectedProjectTypeNode);

            GetWorkItemsForSelectedProject(true);
        }
        private void GetWorkItemsForSelectedProject(bool useCached)
        {
            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            if (selectedCatalogNode == null) return;
            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return;

            SetErrorInfoPanelData();

            if (useCached && myWorkItemsRepository.TfsWorkItems.Count > 0)
            {
                UpdateGridData(true);
                return;
            }

            myWorkItemsRepository.GetWorkItems(selectedCatalogNode, selectedProjectTypeNode, null);
        }
        private void UpdateControlsForWorkItemsLoadingState(bool enable)
        {
            if (enable)
            {
                ProjectCatalogComboBox.IsEnabled = true;
                ProjectTypeComboBox.IsEnabled = true;
                RefreshButton.Visibility = Visibility.Visible;
            }
            else
            {
                RefreshButton.Visibility = Visibility.Collapsed;
                ProjectCatalogComboBox.IsEnabled = false;
                ProjectTypeComboBox.IsEnabled = false;
            }
            SetErrorInfoPanelData();
        }
        private void ClearAllUiData()
        {
            ProjectCatalogComboBox.ItemsSource = null;
            ProjectCatalogComboBox.DataContext = null;

            ProjectTypeComboBox.ItemsSource = null;
            ProjectTypeComboBox.DataContext = null;

            ClearMyWorkItemsUiData();

            TeamWorkItemsTabControl.ClearAllUiData();

            IterationWorkItemsTabItem.Visibility = Visibility.Collapsed;
            MyTeamMembersWorkItemsTabItem.Visibility = Visibility.Collapsed;
        }
        private void ClearMyWorkItemsUiData()
        {
            OnHoldWorkItemsGrid.ItemsSource = null;
            OnHoldWorkItemsGrid.DataContext = null;

            ActiveWorkItemsGrid.ItemsSource = null;
            ActiveWorkItemsGrid.DataContext = null;

            ActiveTabItem.Header = "Active (0)";
            OnHoldTabItem.Header = "On Hold (0)";
        }
        private void SetErrorInfoPanelData()
        {
            ErrorInfoPanel.Visibility = Visibility.Collapsed;
            if (ProjectStructureRepository.Instance.TfsProjectStructure == null)
            {
                ErrorInfoPanel.Visibility = Visibility.Visible;
                if (ProjectStructureRepository.Instance.IsUpdating)
                {
                    ErrorInfoPanelLabel.Content = "Loading TFS projects...";
                    ErrorInfoPanelButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ErrorInfoPanelLabel.Content = "Error while loading TFS projects.";
                    var lastError = ProjectStructureRepository.Instance.GetLastError();
                    if (lastError != null) ErrorInfoPanelLabel.Content = lastError.Message;
                    ErrorInfoPanelButton.Visibility = Visibility.Visible;
                }
            }
            else if (myWorkItemsRepository.IsUpdating)
            {
                ErrorInfoPanel.Visibility = Visibility.Visible;
                ErrorInfoPanelLabel.Content = "Loading your work items...";
                ErrorInfoPanelButton.Visibility = Visibility.Collapsed;
            }
            else if (myWorkItemsRepository.TfsWorkItems.Count == 0)
            {
                ErrorInfoPanel.Visibility = Visibility.Visible;
                var result = myWorkItemsRepository.LastTaskCompletionResult;
                if (result == null) return;
                if (result.IsSuccess)
                {
                    ErrorInfoPanelLabel.Content = "No work items found.";
                }
                else
                {
                    ErrorInfoPanelLabel.Content = result.DisplayMessage;
                    ErrorInfoPanelButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void UpdateGridData(bool collapseAll)
        {
            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            if (selectedCatalogNode == null) return;
            var selectedCatalogNodeId = selectedCatalogNode.Id;

            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return;
            var selectedProjectNodeId = selectedProjectTypeNode.Id;

            var workItems = myWorkItemsRepository.TfsWorkItems.
                Where(x => x.ProjectCatalogNode.Id.Equals(selectedCatalogNodeId) && 
                           x.ProjectInfoNode.Id.Equals(selectedProjectNodeId))
                .OrderBy(x => x.Type).ThenBy(x => x.State).ToList();

            WorkItemsGrid.ItemExpanded -= OnDataGridDataChanged;
            WorkItemsGrid.ItemCollapsed -= OnDataGridDataChanged;

            //if (ActiveWorkItemsGrid.ItemsSource is List<TfsWorkItem> activeWorkItemsInGrid && OnHoldWorkItemsGrid.ItemsSource is List<TfsWorkItem> onHoldWorkItemsInGrid)
            //{
            //    //Work items list in repository might have changed while UI was closed. Data in grid may not be in sync with that in repository.
            //    var allItems = activeWorkItemsInGrid.Union(onHoldWorkItemsInGrid).ToList();
            //    foreach (var workItem in workItems.Where(workItem => allItems.Any(x => x.Equals(workItem) && x.IsExpanded)))
            //    {
            //        workItem.IsExpanded = true;
            //    }
            //}


            if (collapseAll)
            {
                foreach (var expandedItem in workItems.Where(x => x.Level == 0 && x.IsExpanded).ToList())
                {
                    expandedItem.IsExpanded = false;
                }
            }

            var activeWorkItems = workItems.Where(x => !x.IsOnHold).ToList();
            var displayedActiveWorkItems = collapseAll ? activeWorkItems : GetDisplayedWorkItems(activeWorkItems);
            ActiveWorkItemsGrid.ItemsSource = displayedActiveWorkItems;
            ActiveWorkItemsGrid.DataContext = displayedActiveWorkItems;
            ActiveTabItem.Header = $"Active ({displayedActiveWorkItems.Where(x => x.Level == 0).ToList().Count})";

            var onHoldWorkItems = workItems.Where(x => x.IsOnHold).ToList();
            var displayedOnHoldWorkItems = collapseAll ? onHoldWorkItems : GetDisplayedWorkItems(onHoldWorkItems);
            OnHoldWorkItemsGrid.ItemsSource = displayedOnHoldWorkItems;
            OnHoldWorkItemsGrid.DataContext = displayedOnHoldWorkItems;
            OnHoldTabItem.Header = $"On Hold ({displayedOnHoldWorkItems.Where(x => x.Level == 0).ToList().Count})";

            WorkItemsGrid.ItemExpanded += OnDataGridDataChanged;
            WorkItemsGrid.ItemCollapsed += OnDataGridDataChanged;
        }
        private void OnWorkItemsUpdatingStatusChanged(object sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                if (myWorkItemsRepository.IsUpdating)
                {
                    UpdateControlsForWorkItemsLoadingState(false);
                }
                else
                {
                    UpdateGridData(false);
                    UpdateControlsForWorkItemsLoadingState(true);
                }
            });
        }
        /// <summary>
        /// Adds child work items for the expanded work items to be shown on data grid
        /// </summary>
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


        private ProjectCatalogNode GetSelectedProjectCatalogNode()
        {
            var tfsProjectStructure = ProjectStructureRepository.Instance.TfsProjectStructure;
            if (tfsProjectStructure == null) return null;

            if (!(ProjectCatalogComboBox.SelectedItem is ComboBoxKeyValue projectCatalogSelectedItem)) return null;

            var selectedCatalogNodeId = Guid.Parse(projectCatalogSelectedItem.Key);
            return tfsProjectStructure.ProjectCatalogNodeList.First(x => x.Id.Equals(selectedCatalogNodeId));
        }
        private ProjectInfoNode GetSelectedProjectTypeNode()
        {
            var selectedCatalogNode = GetSelectedProjectCatalogNode();
            if (selectedCatalogNode == null) return null;

            if (!(ProjectTypeComboBox.SelectedItem is ComboBoxKeyValue projectTypeSelectedItem)) return null;
            var selectedProjectNodeId = Guid.Parse(projectTypeSelectedItem.Key);

            return selectedCatalogNode.ProjectInfoNodes.First(x => x.Id.Equals(selectedProjectNodeId));
        }

        private void OnDataGridDataChanged(object sender, EventArgs e)
        {
            UpdateGridData(false);
        }


        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            new SettingsWindow(projectsCacheCleared =>
            {
                if (projectsCacheCleared)
                {
                    //BacklogOrganizerApplication.Instance.ReInitMainWindow();
                }
                IterationWorkItemsTabItem.Visibility = BacklogOrganizerConfiguration.AppConfiguration.ShowIterationTab ? Visibility.Visible : Visibility.Collapsed;
                MyTeamMembersWorkItemsTabItem.Visibility = BacklogOrganizerConfiguration.AppConfiguration.ShowTeamMembersTab ? Visibility.Visible : Visibility.Collapsed;
                TabControl1.SelectedIndex = 0;
                UpdateGridData(false);
            }).ShowDialog();
        }
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            UnRegisterEvents();
        }
        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            GetWorkItemsForSelectedProject(false);
        }
        private void AddTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            if (selectedCatalogNode == null) return;

            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return;

            new AddTaskWindow(selectedCatalogNode, selectedProjectTypeNode, null, true).ShowDialog();
        }
        private void ErrorInfoPanelButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ProjectStructureRepository.Instance.TfsProjectStructure == null)
            {
                ProjectStructureRepository.Instance.Load();
                SetErrorInfoPanelData();
            }
            else
            {
                GetWorkItemsForSelectedProject(true);
            }
        }
        private void CollapseAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateGridData(true);
            IterationWorkItemsTabControl.CollapseAll();
            TeamWorkItemsTabControl.CollapseAll();
        }
        private void SetReminderButton_OnClick(object sender, RoutedEventArgs e)
        {
            new SetReminderWindow().ShowDialog();
        }
        private void AboutBacklogOrganizerButton_OnClick(object sender, RoutedEventArgs e)
        {
            new AboutBacklogOrganizer().ShowDialog();
        }
        private void OnTeamWorkItemDisplayedCountChanged(object sender, int count)
        {
            MyTeamMembersWorkItemsTabItem.Header = $"Team Work Items ({count})";
        }
        private void OnIterationWorkItemsDisplayedCountChanged(object sender, int count)
        {
            IterationWorkItemsTabItem.Header = $"Iteration Work Items ({count})";
        }
    }
}
