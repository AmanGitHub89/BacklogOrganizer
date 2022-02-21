using System;
using System.Windows;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Reminders;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;
using BacklogOrganizer.Windows;


namespace BacklogOrganizer.UserControls.DataGrids
{
    /// <summary>
    /// Interaction logic for WorkItemGridActions.xaml
    /// </summary>
    public partial class WorkItemGridActions
    {
        public static event EventHandler WorkItemActiveStateChanged;

        public WorkItemGridActions()
        {
            InitializeComponent();
        }

        private void AddTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            var tfsWorkItem = GetWorkItem(sender);
            new AddTaskWindow(tfsWorkItem.ProjectCatalogNode, tfsWorkItem.ProjectInfoNode, tfsWorkItem, true).ShowDialog();
        }

        private void ToggleActiveStateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var tfsWorkItem = GetWorkItem(sender);
            var isOnHold = tfsWorkItem.IsOnHold;
            tfsWorkItem.IsOnHold = !isOnHold;

            var onHoldWorkItemIds = BacklogOrganizerConfiguration.OnHoldWorkItemIds.CommaSeparatedStringToList();
            if (isOnHold)
            {
                var index = onHoldWorkItemIds.IndexOf(tfsWorkItem.OnHoldFullName);
                onHoldWorkItemIds.RemoveAt(index);
            }
            else
            {
                onHoldWorkItemIds.Add(tfsWorkItem.OnHoldFullName);
            }
            BacklogOrganizerConfiguration.OnHoldWorkItemIds = string.Join(",", onHoldWorkItemIds);

            WorkItemActiveStateChanged?.Invoke(null, null);
        }

        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            var tfsWorkItem = GetWorkItem(sender);
            var addUpdateWorkItemWindow = new EditWorkItemWindow(tfsWorkItem.Id, tfsWorkItem.ProjectCatalogNode, tfsWorkItem.ProjectInfoNode);
            addUpdateWorkItemWindow.ShowDialog();
        }

        private void SetReminderButton_OnClick(object sender, RoutedEventArgs e)
        {
            var tfsWorkItem = GetWorkItem(sender);
            new SetReminderWindow(tfsWorkItem).ShowDialog();
        }

        private static TfsWorkItem GetWorkItem(object sender)
        {
            return ((FrameworkElement)sender).DataContext as TfsWorkItem;
        }
    }
}
