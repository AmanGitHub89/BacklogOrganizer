using System.Collections.Generic;
using System.Linq;

using BacklogOrganizer.Types;


namespace BacklogOrganizer.TfsConnectors.WorkItemRepository
{
    internal sealed class IterationWorkItemsRepository : AbstractWorkItemsRepository
    {
        private static IterationWorkItemsRepository myInstance;

        internal static IterationWorkItemsRepository Instance => myInstance ?? (myInstance = new IterationWorkItemsRepository());
        public override int TimeoutInSeconds { get; set; } = 30;

        private IterationWorkItemsRepository()
        {
        }

        protected override List<TfsWorkItem> GetOrderedList(List<TfsWorkItem> tfsWorkItems)
        {
            return tfsWorkItems.OrderBy(x => x.Type).ThenBy(x => x.State).ThenBy(x => x.UserName).ToList();
        }
    }
}
