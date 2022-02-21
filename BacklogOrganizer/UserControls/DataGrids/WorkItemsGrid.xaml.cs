using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.UserControls.DataGrids
{
    /// <summary>
    /// Interaction logic for WorkItemsGrid.xaml
    /// </summary>
    public partial class WorkItemsGrid
    {
        public WorkItemsGrid()
        {
            InitializeComponent();
        }

        public static event EventHandler ItemExpanded;
        public static event EventHandler ItemCollapsed;

        public IEnumerable ItemsSource
        {
            get => WorkItemsDataGrid.ItemsSource;
            set => WorkItemsDataGrid.ItemsSource = value;
        }

        private void OnWorkItemIdClickHandler(object sender, RoutedEventArgs e)
        {
            var url = string.Empty;
            try
            {
                var textBlock = (TextBlock)sender;
                url = textBlock.Tag.ToString();
                Process.Start(url);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while opening url {url}", ex);
            }
        }

        private void WorkItemsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //if (!(WorkItemsDataGrid.SelectedItem is WorkItem workItem))
            //{
            //    return;
            //}

            //if (WorkItemsDataGrid.SelectedCells.Count > 0)
            //{
            //    var textDetailWindow = new TextDetailWindow(workItem.Uri.ToString());
            //    textDetailWindow.SetTitle(WorkItemsDataGrid.CurrentCell.Column.Header.ToString());
            //    textDetailWindow.ShowDialog();
            //}
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)e.OriginalSource).DataContext is TfsWorkItem tfsWorkItem))
                return;

            if (!(WorkItemsDataGrid.SelectedItem is TfsWorkItem tfsSelectedWorkItem))
                return;

            if (!tfsWorkItem.Equals(tfsSelectedWorkItem))
                return;

            if (tfsWorkItem.IsExpanded) return;

            tfsWorkItem.IsExpanded = true;
            ItemExpanded?.Invoke(this, null);
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement) e.OriginalSource).DataContext is TfsWorkItem tfsWorkItem))
                return;

            if (!(WorkItemsDataGrid.SelectedItem is TfsWorkItem tfsSelectedWorkItem))
                return;

            if (!tfsWorkItem.Equals(tfsSelectedWorkItem))
                return;

            if (!tfsWorkItem.IsExpanded) return;

            tfsWorkItem.IsExpanded = false;
            ItemCollapsed?.Invoke(this, null);
        }
    }
}
