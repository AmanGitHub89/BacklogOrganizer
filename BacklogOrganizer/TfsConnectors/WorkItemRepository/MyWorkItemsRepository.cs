using System.Collections.Generic;
using System.Linq;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Types;


namespace BacklogOrganizer.TfsConnectors.WorkItemRepository
{
    internal sealed class MyWorkItemsRepository : AbstractWorkItemsRepository
    {
        private static MyWorkItemsRepository myInstance;

        internal static MyWorkItemsRepository Instance => myInstance ?? (myInstance = new MyWorkItemsRepository());

        public override int TimeoutInSeconds { get; set; } = 30;

        protected override List<string> Users => new List<string> {BacklogOrganizerConfiguration.TfsUserName};

        private MyWorkItemsRepository()
        {
        }

        protected override List<TfsWorkItem> GetOrderedList(List<TfsWorkItem> tfsWorkItems)
        {
            return tfsWorkItems.OrderBy(x => x.Type).ThenBy(x => x.State).ThenBy(x => x.UserName).ToList();
        }
    }
}
