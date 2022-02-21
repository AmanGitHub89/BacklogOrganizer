using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;


namespace BacklogOrganizer.Windows
{
    public partial class AddRemoveItemsWindow : FixedSizeChildWindow
    {
        private readonly Action<List<string>> myOnSelectedListChanged;
        private List<string> myAllItems;
        private List<string> mySelectedItems;

        public AddRemoveItemsWindow(List<string> allItems, List<string> selectedItems, Action<List<string>> onSelectedListChanged)
        {
            InitializeComponent();

            myAllItems = allItems.Where(x => !selectedItems.Any(y => y.CaseInsensitiveEquals(x))).ToList();
            mySelectedItems = selectedItems;
            myOnSelectedListChanged = onSelectedListChanged;

            myAllItems.Sort();
            mySelectedItems.Sort();
        }

        private void AddRemoveItemsWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateLists();
        }

        private void UpdateLists()
        {
            AllItemsListBox.ItemsSource = myAllItems.ToList();
            AllItemsListBox.DataContext = myAllItems.ToList();

            SelectedItemsListBox.ItemsSource = mySelectedItems.ToList();
            SelectedItemsListBox.DataContext = mySelectedItems.ToList();
        }

        private void ToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            var allItemsListSelectedItems = AllItemsListBox.SelectedItems.Cast<string>().ToList();
            var selectedItemsListBoxSelectedItems = SelectedItemsListBox.SelectedItems.Cast<string>().ToList();
            if (allItemsListSelectedItems.Count > 0)
            {
                myAllItems = myAllItems.Where(x => !allItemsListSelectedItems.Any(y => y.Equals(x))).ToList();
                mySelectedItems.AddRange(allItemsListSelectedItems);
                AllItemsListBox.SelectedIndex = -1;
            }
            else if (selectedItemsListBoxSelectedItems.Count > 0)
            {
                mySelectedItems = mySelectedItems.Where(x => !selectedItemsListBoxSelectedItems.Any(y => y.Equals(x))).ToList();
                myAllItems.AddRange(selectedItemsListBoxSelectedItems);
                SelectedItemsListBox.SelectedIndex = -1;
            }
            myAllItems.Sort();
            mySelectedItems.Sort();
            UpdateLists();
        }

        private void AddRemoveItemsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            myOnSelectedListChanged(mySelectedItems);
        }

        private void AllItemsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItemsListBox.SelectedIndex = -1;
        }

        private void SelectedItemsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllItemsListBox.SelectedIndex = -1;
        }
    }
}
