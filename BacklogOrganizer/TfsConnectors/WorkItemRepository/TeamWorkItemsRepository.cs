using System.Collections.Generic;
using System.Linq;

using BacklogOrganizer.Types;


namespace BacklogOrganizer.TfsConnectors.WorkItemRepository
{
    internal sealed class TeamWorkItemsRepository :AbstractWorkItemsRepository
    {
        private static TeamWorkItemsRepository myInstance;
        internal static TeamWorkItemsRepository Instance => myInstance ?? (myInstance = new TeamWorkItemsRepository());

        public override int TimeoutInSeconds { get; set; } = 60;
        protected override bool DuplicateParentsForUsers { get; set; } = true;

        private TeamWorkItemsRepository()
        {
        }

        protected override List<TfsWorkItem> GetOrderedList(List<TfsWorkItem> tfsWorkItems)
        {
            return tfsWorkItems.OrderBy(x => x.UserName).ThenBy(x => x.Type).ThenBy(x => x.State).ToList();
        }
    }
}
