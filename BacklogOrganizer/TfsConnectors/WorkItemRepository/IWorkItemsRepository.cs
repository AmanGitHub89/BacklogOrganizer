using System;
using System.Collections.Generic;

using BacklogOrganizer.Types;


namespace BacklogOrganizer.TfsConnectors.WorkItemRepository
{
    internal interface IWorkItemsRepository
    {
        event EventHandler WorkItemsUpdatingStatusChanged;
        bool IsUpdating { get; }
        int TimeoutInSeconds { get; set; }
        IWorkItemsRepository WithIteration(string iteration);
        IWorkItemsRepository WithUsers(List<string> users);
        List<TfsWorkItem> TfsWorkItems { get; set; }
        RepositoryTaskCompletionResult LastTaskCompletionResult { get; }

        void GetWorkItems(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode, Action<RepositoryTaskCompletionResult> onCompleted);
    }
    internal class RepositoryTaskRequest : IEquatable<RepositoryTaskRequest>
    {
        public Guid TaskGuid { get; }
        public RepositoryTaskRequest(Guid taskGuid)
        {
            TaskGuid = taskGuid;
        }

        public bool Equals(RepositoryTaskRequest other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return TaskGuid.Equals(other.TaskGuid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RepositoryTaskRequest) obj);
        }

        public override int GetHashCode()
        {
            return TaskGuid.GetHashCode();
        }
    }
    internal class RepositoryTaskCompletionResult
    {
        public Guid TaskGuid { get; }
        public bool IsSuccess { get; set; }
        public object Result { get; set; }
        public Exception Exception { get; set; }
        public string DisplayMessage { get; set; }

        public RepositoryTaskCompletionResult(Guid taskGuid)
        {
            TaskGuid = taskGuid;
        }
    }
}
