using System;
using System.Windows.Forms;

using BacklogOrganizer.Properties;


namespace BacklogOrganizer.SingleInstance
{
    internal class BacklogOrganizerTrayIcon : ApplicationContext
    {
        private static BacklogOrganizerTrayIcon myInstance;
        private readonly NotifyIcon myTrayIcon;

        internal static void ShowTrayIcon()
        {
            if (myInstance == null)
            {
                myInstance = new BacklogOrganizerTrayIcon();
            }
        }

        private BacklogOrganizerTrayIcon()
        {
            myTrayIcon = new NotifyIcon
            {
                Icon = Resources.BacklogOrganizer3,
                ContextMenu = new ContextMenu(new[] {
                    new MenuItem("Open", OnOpenClick),
                    new MenuItem("Exit", OnExitClick)
                }),
                // ReSharper disable once LocalizableElement
                Text = "Backlog Organizer",
                Visible = true
            };
            myTrayIcon.DoubleClick += TrayIcon_OnDoubleClick;
        }

        private static void TrayIcon_OnDoubleClick(object sender, EventArgs e)
        {
            BacklogOrganizerApplication.Instance.ActivateMainWindow(null);
        }

        private static void OnOpenClick(object sender, EventArgs e)
        {
            BacklogOrganizerApplication.Instance.ActivateMainWindow(null);
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            myTrayIcon.Visible = false;
            Application.Exit();
            BacklogOrganizerApplication.Instance.Shutdown(0);
            Dispose();
        }
    }
}
