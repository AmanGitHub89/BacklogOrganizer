using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.UserControls.DataGrids
{
    /// <summary>
    /// Interaction logic for IterationWorkItemsGrid.xaml
    /// </summary>
    public partial class IterationWorkItemsGrid
    {
        public IterationWorkItemsGrid()
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


        private void Expander_OnExpanded(object sender, RoutedEventArgs e)
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

        private void Expander_OnCollapsed(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)e.OriginalSource).DataContext is TfsWorkItem tfsWorkItem))
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
