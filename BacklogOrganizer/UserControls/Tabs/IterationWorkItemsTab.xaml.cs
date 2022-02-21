using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

using BacklogOrganizer.TfsConnectors;
using BacklogOrganizer.TfsConnectors.WorkItemRepository;
using BacklogOrganizer.Types;
using BacklogOrganizer.UserControls.DataGrids;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.UserControls.Tabs
{
    /// <summary>
    /// Interaction logic for IterationWorkItemsTab.xaml
    /// </summary>
    public partial class IterationWorkItemsTab
    {
        private Dispatcher myDispatcherObject;
        public event EventHandler<int> IterationWorkItemsDisplayedCountChanged;
        private string mySelectedIterationPath;
        private readonly IWorkItemsRepository myWorkItemsRepository;
        private bool myIsLoaded;

        public IterationWorkItemsTab()
        {
            InitializeComponent();
            ProjectStructureRepository.Instance.ProjectStructureUpdated += OnProjectStructureUpdated;
            myWorkItemsRepository = IterationWorkItemsRepository.Instance;
            myWorkItemsRepository.WorkItemsUpdatingStatusChanged += OnWorkItemsUpdatingStatusChanged;
        }

        private void IterationWorkItemsTab_OnLoaded(object sender, RoutedEventArgs e)
        {
            myDispatcherObject = Application.Current.Dispatcher;
            if (myIsLoaded) return;
            myIsLoaded = true;
            Load();
        }

        private void OnProjectStructureUpdated(object sender, EventArgs e)
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
            CurrentIterationLabel.Content = "";
            SelectedIterationLabel.Content = "";

            PreviousIterationButton.IsEnabled = false;
            NextIterationButton.IsEnabled = false;

            GetIterationWorkItemsButton.IsEnabled = false;

            ReloadIterations.Visibility = Visibility.Collapsed;

            IterationWorkItemsGrid.ItemsSource = null;
            IterationWorkItemsGrid.DataContext = null;

            IterationWorkItemsDisplayedCountChanged?.Invoke(this, 0);
        }
        public void Load()
        {
            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            if (selectedCatalogNode == null) return;

            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return;

            if (selectedProjectTypeNode.IterationList.Count == 0)
            {
                SetUiForTeamWorkItemsLoadingState(true, false);
                ProjectStructureRepository.Instance.UpdateProjectIterationAndAreaPathAsync(selectedCatalogNode, selectedProjectTypeNode, () =>
                {
                    RunOnUIThread(() =>
                    {
                        if (selectedProjectTypeNode.IterationList.Count == 0)
                        {
                            ClearAllUiData();
                            SetTabStatusLabel(true, "An error occurred while loading Iterations for project.");
                            ReloadIterations.Visibility = Visibility.Visible;
                            return;
                        }
                        Load();
                    });
                });
                return;
            }


            SetUiForTeamWorkItemsLoadingState(false, false);
            var currentIteration = ProjectStructureRepository.Instance.GetCurrentIteration(selectedProjectTypeNode);
            if (currentIteration == null) return;

            if (!string.IsNullOrEmpty(mySelectedIterationPath) && !mySelectedIterationPath.Equals(currentIteration.DisplayedPath))
            {
                ClearAllUiData();
            }

            if (string.IsNullOrEmpty(mySelectedIterationPath))
            {
                mySelectedIterationPath = currentIteration.DisplayedPath;
                SelectedIterationLabel.Content = currentIteration.DisplayedPath;
            }
            else
            {
                SelectedIterationLabel.Content = mySelectedIterationPath;
            }

            CurrentIterationLabel.Content = currentIteration.DisplayedPath;

            UpdateIterationGridData();
        }
        public void CollapseAll()
        {
            foreach (var tfsWorkItem in myWorkItemsRepository.TfsWorkItems)
            {
                tfsWorkItem.IsExpanded = false;
            }
            UpdateIterationGridData();
        }

        private void GetIterationWorkItems(string iterationPath)
        {
            SetUiForTeamWorkItemsLoadingState(false, true);

            myWorkItemsRepository.TfsWorkItems.Clear();
            UpdateIterationGridData();

            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            if (selectedCatalogNode == null) return;

            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return;

            myWorkItemsRepository.WithIteration(iterationPath).GetWorkItems(selectedCatalogNode, selectedProjectTypeNode, null);
        }
        private void UpdateIterationGridData()
        {
            IterationWorkItemsGrid.ItemExpanded -= OnTeamsDataGridDataChanged;
            IterationWorkItemsGrid.ItemCollapsed -= OnTeamsDataGridDataChanged;

            var displayedWorkItems = GetDisplayedWorkItems(myWorkItemsRepository.TfsWorkItems);
            IterationWorkItemsGrid.ItemsSource = displayedWorkItems;
            IterationWorkItemsGrid.DataContext = displayedWorkItems;
            IterationWorkItemsDisplayedCountChanged?.Invoke(this, displayedWorkItems.Where(x => x.Level == 0).ToList().Count);

            IterationWorkItemsGrid.ItemExpanded += OnTeamsDataGridDataChanged;
            IterationWorkItemsGrid.ItemCollapsed += OnTeamsDataGridDataChanged;
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
        private void SetUiForTeamWorkItemsLoadingState(bool isLoadingIterations, bool isLoadingWorkItems)
        {
            if (isLoadingIterations)
            {
                SetTabStatusLabel(true, "Getting list of Iterations from TFS...");

                ClearAllUiData();
                return;
            }
            else
            {
                SetTabStatusLabel(false, string.Empty);

                PreviousIterationButton.IsEnabled = true;
                NextIterationButton.IsEnabled = true;
                GetIterationWorkItemsButton.IsEnabled = true;
            }

            if (isLoadingWorkItems)
            {
                SetTabStatusLabel(true, "Getting work items for Iteration from TFS...");
                PreviousIterationButton.IsEnabled = false;
                NextIterationButton.IsEnabled = false;
                GetIterationWorkItemsButton.IsEnabled = false;
            }
            else
            {
                SetTabStatusLabel(false, string.Empty);
                PreviousIterationButton.IsEnabled = true;
                NextIterationButton.IsEnabled = true;
                GetIterationWorkItemsButton.IsEnabled = true;
            }
        }
        private void SetTabStatusLabel(bool show, string text)
        {
            IterationWorkItemsTabStatusSection.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            IterationWorkItemsTabStatusLabel.Content = text;
        }


        private void OnTeamsDataGridDataChanged(object sender, EventArgs e)
        {
            UpdateIterationGridData();
        }


        private void OnWorkItemsUpdatingStatusChanged(object sender, EventArgs e)
        {
            RunOnUIThread(() =>
            {
                if (myWorkItemsRepository.IsUpdating)
                {
                    SetUiForTeamWorkItemsLoadingState(false, true);
                    myWorkItemsRepository.TfsWorkItems.Clear();
                    UpdateIterationGridData();
                    return;
                }
                SetUiForTeamWorkItemsLoadingState(false, false);

                if (myWorkItemsRepository.TfsWorkItems.Count == 0)
                {
                    var result = myWorkItemsRepository.LastTaskCompletionResult;
                    if (result != null)
                    {
                        SetTabStatusLabel(true, result.IsSuccess ? "No work items found." : result.DisplayMessage);
                    }
                    else
                    {
                        SetTabStatusLabel(true, "And error occurred while getting work items.");
                    }
                    return;
                }

                UpdateIterationGridData();
            });
        }

        private void GetIterationWorkItemsButton_OnClick(object sender, RoutedEventArgs e)
        {
            GetIterationWorkItems(mySelectedIterationPath);
        }

        private void PreviousIterationButton_OnClick(object sender, RoutedEventArgs e)
        {
            MoveToIteration(false);
        }
        private void NextIterationButton_OnClick(object sender, RoutedEventArgs e)
        {
            MoveToIteration(true);
        }
        private void MoveToIteration(bool getNext)
        {
            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return;

            var iteration = ProjectStructureRepository.Instance.GetIteration(selectedProjectTypeNode, mySelectedIterationPath, getNext);
            if (iteration == null) return;

            if (!string.IsNullOrEmpty(mySelectedIterationPath) && !mySelectedIterationPath.Equals(iteration.DisplayedPath))
            {
                myWorkItemsRepository.TfsWorkItems.Clear();
                SetTabStatusLabel(false, string.Empty);
                UpdateIterationGridData();
            }
            mySelectedIterationPath = iteration.DisplayedPath;

            SelectedIterationLabel.Content = iteration.DisplayedPath;
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

        private void ReloadIterations_OnClick(object sender, RoutedEventArgs e)
        {
            ReloadIterations.Visibility = Visibility.Collapsed;
            Load();
        }
    }
}
