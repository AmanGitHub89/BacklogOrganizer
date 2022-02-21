using System.Windows;

using BacklogOrganizer.Reminders;
using BacklogOrganizer.TfsConnectors;
using BacklogOrganizer.Windows;

using Microsoft.VisualBasic.ApplicationServices;

using StartupEventArgs = System.Windows.StartupEventArgs;


namespace BacklogOrganizer.SingleInstance
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class BacklogOrganizerApplication
    {
        private static MainWindow myMainWindow;
        public static BacklogOrganizerApplication Instance { get; private set; }

        public void InitializeAppComponent()
        {
            Startup += Application_Startup;
            Instance = this;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            BacklogOrganizerTrayIcon.ShowTrayIcon();
            RemindersService.StartService(Current.Dispatcher);

            ProjectStructureRepository.Instance.ProjectStructureUpdated += (o, args) =>
            {
                if (!ProjectStructureRepository.Instance.IsUpdating && ProjectStructureRepository.Instance.TfsProjectStructure != null)
                    MyWorkItemService.Instance.Start();
            };
            ProjectStructureRepository.Instance.Load();
        }

        public void ActivateMainWindow(StartupNextInstanceEventArgs eventArgs)
        {
            if (myMainWindow == null)
            {
                myMainWindow = new MainWindow();
            }
            myMainWindow.Show();
            myMainWindow.WindowState = WindowState.Normal;
            myMainWindow.ShowFirstTab();
        }

        public void ReInitMainWindow()
        {
            myMainWindow?.Close();
            myMainWindow = new MainWindow();
            myMainWindow.Show();
            myMainWindow.WindowState = WindowState.Normal;
        }
    }
}
