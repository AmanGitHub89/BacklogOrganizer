using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.TfsConnectors.WorkItemRepository;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;

using Microsoft.TeamFoundation.WorkItemTracking.Client;


namespace BacklogOrganizer.Windows
{
    /// <summary>
    /// Interaction logic for EditWorkItemWindow.xaml
    /// </summary>
    public partial class EditWorkItemWindow
    {
        private readonly TfsWorkItem myWorkItem;
        public static event EventHandler WorkItemUpdated;

        private bool myCanSaveDescription;
        private bool myCanSaveAdditionalInfo;

        private bool myIsAdditionalInfoLoaded;

        public EditWorkItemWindow(int workItemId, ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode)
        {
            InitializeComponent();
            myWorkItem = RefreshTask(workItemId, catalogNode, projectInfoNode);
        }

        private void AddUpdateWorkItemWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            AdditionalInfoTabItem.Visibility = Visibility.Collapsed;
            AcceptanceCriteriaTabItem.Visibility = Visibility.Collapsed;

            if (myWorkItem.IsATask)
            {
                TaskStateComboBox.ItemsSource = BacklogOrganizerConfiguration.TaskStates.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                OriginalEstimateTextBox.Text = GetFieldValue(myWorkItem, "Original Estimate");
                CompletedWorkTextBox.Text = GetFieldValue(myWorkItem, "Completed Work");
                RemainingWorkTextBox.Text = GetFieldValue(myWorkItem, "Remaining Work");

                TaskStateComboBox.SelectedItem = myWorkItem.State;
            }
            else
            {
                TaskEstimatesPanel.Visibility = Visibility.Collapsed;
                TaskStateComboBox.Visibility = Visibility.Collapsed;
                IterationInfoPanel.HorizontalAlignment = HorizontalAlignment.Left;
            }

            IterationNameLabel.Content = GetFieldValue(myWorkItem, "Iteration Path");
            TitleTextBox.Text = myWorkItem.Title;

            WorkItemType.Text = myWorkItem.Type;
            WorkItemIdLink.Inlines.Clear();
            WorkItemIdLink.Inlines.Add(myWorkItem.Id.ToString());

            LoadDescription();
            LoadAdditionalInfo();
            LoadAcceptanceCriteria();
        }


        private void LoadDescription()
        {
            try
            {
                var descriptionField = myWorkItem.WorkItem.Fields.OfType<Field>().First(x => x.Name.Equals("Description"));

                myCanSaveDescription = descriptionField.IsEditable;
                if (!myCanSaveDescription) DescriptionTextBox.DisableControls();

                var description = descriptionField.Value.ToString();
                DescriptionTextBox.SetTextFromHtml(description);
            }
            catch (Exception ex)
            {
                Logger.Error("Error while loading description.", ex);
                MessageBox.Show("An error occurred while loading description.", "Error!");
            }
        }
        private void LoadAdditionalInfo()
        {
            try
            {
                var additionalInfoField = myWorkItem.WorkItem.Fields.OfType<Field>().FirstOrDefault(x => x.Name.Equals("Additional Info"));
                if (additionalInfoField == null) return;

                myCanSaveAdditionalInfo = additionalInfoField.IsEditable;
                if (!myCanSaveAdditionalInfo) AdditionalInfoTextBox.IsReadOnly = true;

                AdditionalInfoTextBox.Text = additionalInfoField.Value.ToString();

                myIsAdditionalInfoLoaded = true;
                AdditionalInfoTabItem.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Error("Error while loading Addition Info.", ex);
            }
        }
        private void LoadAcceptanceCriteria()
        {
            try
            {
                var acceptanceCriteriaField = myWorkItem.WorkItem.Fields.OfType<Field>().FirstOrDefault(x => x.Name.Equals("Acceptance Criteria"));
                if (acceptanceCriteriaField == null) return;

                AcceptanceCriteriaTextBox.DisableControls();

                var acceptanceCriteria = acceptanceCriteriaField.Value.ToString();
                AcceptanceCriteriaTextBox.SetTextFromHtml(acceptanceCriteria);

                AcceptanceCriteriaTabItem.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Error("Error while loading acceptance criteria.", ex);
            }
        }


        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (myWorkItem.IsATask)
                {
                    var selectedState = TaskStateComboBox.SelectedItem as string;
                    myWorkItem.WorkItem.State = selectedState;
                    myWorkItem.WorkItem.Fields["Original Estimate"].Value = OriginalEstimateTextBox.Text.Trim();
                    myWorkItem.WorkItem.Fields["Completed Work"].Value = CompletedWorkTextBox.Text.Trim();
                    // ReSharper disable once PossibleNullReferenceException
                    myWorkItem.WorkItem.Fields["Remaining Work"].Value = selectedState.Equals("Closed") ? null : RemainingWorkTextBox.Text.Trim();
                }

                myWorkItem.WorkItem.Fields["Title"].Value = TitleTextBox.Text.Trim();

                if (myCanSaveDescription)
                {
                    myWorkItem.WorkItem.Fields["Description"].Value = DescriptionTextBox.ReadValue();
                }

                if (myIsAdditionalInfoLoaded && myCanSaveAdditionalInfo)
                {
                    myWorkItem.WorkItem.Fields["Additional Info"].Value = AdditionalInfoTextBox.Text.Trim();
                }

                if (!myWorkItem.WorkItem.IsValid())
                {
                    var issues = myWorkItem.WorkItem.Validate();
                    var message = issues.OfType<Field>().Aggregate("Save failed due to following fields : ",
                        (current, field) => current + $"{Environment.NewLine}{field.Name}");
                    throw new Exception(message);
                }

                myWorkItem.WorkItem.Save();
                //RefreshTask(myWorkItem);

                myWorkItem.UpdateWorkItem();

                WorkItemUpdated?.Invoke(myWorkItem, null);
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Error while updating task.", ex);
                MessageBox.Show(ex.Message, "An error occurred while saving the work item.");
            }
        }

        private static string GetFieldValue(TfsWorkItem tfsWorkItem, string fieldName)
        {
            var value = tfsWorkItem.WorkItem.Fields[fieldName].Value;
            return value == null ? string.Empty : value.ToString();
        }

        private static TfsWorkItem RefreshTask(int workItemId, ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode)
        {
            return MyWorkItemsRepository.Instance.GetWorkItem(catalogNode, projectInfoNode, workItemId, false);
        }

        private void WorkItemIdLink_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(myWorkItem.Url);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while opening url {myWorkItem.Url}", ex);
            }
        }
    }
}
