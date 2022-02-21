using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.TfsConnectors;
using BacklogOrganizer.TfsConnectors.WorkItemRepository;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.Windows
{
    public partial class AddTaskWindow
    {
        private readonly ProjectCatalogNode myCatalogNode;
        private readonly ProjectInfoNode myProjectInfoNode;
        private TfsWorkItem myParentWorkItem;
        private string mySelectedIterationName;
        private string mySelectedIterationPath;
        public static event EventHandler TaskAdded;
        private readonly bool myShowAssignOption;
        private string myAssignTo;

        public AddTaskWindow(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode, TfsWorkItem parentWorkItem, bool showAssignOption)
        {
            InitializeComponent();
            myCatalogNode = catalogNode;
            myProjectInfoNode = projectInfoNode;
            myParentWorkItem = parentWorkItem;
            myShowAssignOption = showAssignOption;
        }

        private void AddTaskWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            AssignedToPanel.Visibility = Visibility.Collapsed;

            var currentIteration = ProjectStructureRepository.Instance.GetCurrentIteration(myProjectInfoNode);
            if (currentIteration == null)
            {
                MessageBoxHelper.ShowWarningMessage("Iteration not available.", "Could not load Iteration, please try again.");

                ProjectStructureRepository.Instance.UpdateProjectIterationAndAreaPathAsync(myCatalogNode, myProjectInfoNode);

                Close();
                return;
            }
            mySelectedIterationName = currentIteration.Name;
            mySelectedIterationPath = currentIteration.DisplayedPath;
            IterationLabel.Content = mySelectedIterationPath;
            if (myParentWorkItem != null)
            {
                ParentIdTextBox.Text = myParentWorkItem.Id.ToString();
            }

            AreaPathLabel.Content = $"Area Path : {BacklogOrganizerConfiguration.TfsConfiguration.TfsTaskAreaPath}";

            if (myShowAssignOption)
            {
                PopulateAssignToComboBox();
            }
        }

        private void PopulateAssignToComboBox()
        {
            var allTeamForProject = TfsTeamsRepository.Instance.GetAllTeamsForSelectedProject();
            if (allTeamForProject == null) return;

            var memberNames = new List<string>();
            foreach (var team in allTeamForProject.TfsTeamsList)
            {
                memberNames.AddRange(team.OriginalTeamMembers);
            }

            memberNames = memberNames.Distinct().ToList();
            memberNames.Sort();

            AssignToComboBox.ItemsSource = memberNames;
            AssignToComboBox.DataContext = memberNames;

            if (memberNames.Count > 0)
            {
                AssignedToPanel.Visibility = Visibility.Visible;
                var index = memberNames.IndexOf(BacklogOrganizerConfiguration.TfsUserName);
                if (index >= 0)
                {
                    AssignToComboBox.SelectedIndex = index;
                }
            }
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            int.TryParse(OriginalEstimateTextBox.Text.Trim(), out var originalEstimate);
            var taskCreated = MyWorkItemsRepository.Instance.CreateTask(myCatalogNode, myProjectInfoNode, mySelectedIterationPath, TitleTextBox.Text.Trim(),
                originalEstimate, myParentWorkItem, myAssignTo);
            if (taskCreated)
            {
                Close();
                TaskAdded?.Invoke(this, null);
            }
        }

        private void AssignToComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AssignToComboBox.SelectedItem is string assignTo)
            {
                myAssignTo = assignTo;
            }
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
            var iteration = ProjectStructureRepository.Instance.GetIteration(myProjectInfoNode, mySelectedIterationPath, getNext);
            if (iteration == null) return;

            if (mySelectedIterationPath.Equals(iteration.DisplayedPath)) return;

            mySelectedIterationPath = iteration.DisplayedPath;
            mySelectedIterationName = iteration.Name;

            UpdateSelectedIterationInfo();
        }

        private void UpdateSelectedIterationInfo()
        {
            IterationLabel.Content = mySelectedIterationPath;
            if (myParentWorkItem != null)
            {
                TitleTextBox.Text = $"{mySelectedIterationName} : {myParentWorkItem.Title}";
            }
        }

        private void ParentIdTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ParentIdTextBox.Text.Trim(), out var parentWorkItemId))
            {
                myParentWorkItem = MyWorkItemsRepository.Instance.GetWorkItem(myCatalogNode, myProjectInfoNode, parentWorkItemId, true);
                if (myParentWorkItem != null) UpdateSelectedIterationInfo();
            }
        }
    }
}
