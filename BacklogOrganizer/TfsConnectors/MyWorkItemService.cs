using System.Timers;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.TfsConnectors.WorkItemRepository;


namespace BacklogOrganizer.TfsConnectors
{
    internal class MyWorkItemService
    {
        private static MyWorkItemService myInstance;
        internal static MyWorkItemService Instance => myInstance ?? (myInstance = new MyWorkItemService());
        private MyWorkItemService()
        {

        }

        public void Start()
        {
            var timer = new Timer(BacklogOrganizerConfiguration.AppConfiguration.RefreshInterval * 60 * 1000) {Enabled = true};
            timer.Elapsed += Timer_OnElapsed;
            timer.Start();
            GetMyWorkItemsForSelectedProject();
        }

        private void Timer_OnElapsed(object sender, ElapsedEventArgs e)
        {
            GetMyWorkItemsForSelectedProject();
        }

        public void GetMyWorkItemsForSelectedProject()
        {
            var projectNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (projectNode == null) return;
            var catalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            MyWorkItemsRepository.Instance.GetWorkItems(catalogNode, projectNode, null);
        }
    }
}
