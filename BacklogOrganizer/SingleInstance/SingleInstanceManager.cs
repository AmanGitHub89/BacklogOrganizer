using System;

using BacklogOrganizer.Utilities;

using Microsoft.VisualBasic.ApplicationServices;

namespace BacklogOrganizer.SingleInstance
{
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private BacklogOrganizerApplication myApplication;

        public SingleInstanceManager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs)
        {
            // First time _application is launched
            try
            {
                //eventArgs.CommandLine;
                myApplication = new BacklogOrganizerApplication();
                myApplication.InitializeAppComponent();
                myApplication.Run();
            }
            catch (Exception ex)
            {
                Logger.Fatal(null, ex);
            }
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Subsequent launches
            base.OnStartupNextInstance(eventArgs);
            //eventArgs.CommandLine;
            myApplication.ActivateMainWindow(eventArgs);
        }
    }
}
