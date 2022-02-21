using System.Windows;

using BacklogOrganizer.Reminders;
using BacklogOrganizer.Types;
using BacklogOrganizer.Windows;


namespace BacklogOrganizer.UserControls.DataGrids
{
    /// <summary>
    /// Interaction logic for CommonWorkItemGridActions.xaml
    /// </summary>
    public partial class CommonWorkItemGridActions
    {
        public CommonWorkItemGridActions()
        {
            InitializeComponent();
        }

        private void AddTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            var tfsWorkItem = GetWorkItem(sender);
            new AddTaskWindow(tfsWorkItem.ProjectCatalogNode, tfsWorkItem.ProjectInfoNode, tfsWorkItem, true).ShowDialog();
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
