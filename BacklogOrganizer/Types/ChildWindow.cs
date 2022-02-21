using System.Windows;


namespace BacklogOrganizer.Types
{
    public class ChildWindow : Window
    {
        public ChildWindow()
        {
            ShowInTaskbar = false;
            Owner = Application.Current.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }

    public class FixedSizeChildWindow : ChildWindow
    {
        public FixedSizeChildWindow()
        {
            ResizeMode = ResizeMode.NoResize;
        }
    }
}
