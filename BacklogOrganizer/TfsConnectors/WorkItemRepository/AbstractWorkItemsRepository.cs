using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;
using BacklogOrganizer.Windows;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;


namespace BacklogOrganizer.TfsConnectors.WorkItemRepository
{
    internal abstract class AbstractWorkItemsRepository : IWorkItemsRepository
    {
        private bool myIsUpdating;
        public event EventHandler WorkItemsUpdatingStatusChanged;
        public bool IsUpdating
        {
            get => myIsUpdating;
            private set
            {
                myIsUpdating = value;
                WorkItemsUpdatingStatusChanged?.Invoke(this, null);
            }
        }

        private RepositoryTaskRequest LastTaskRequest { get; set; }
        public RepositoryTaskCompletionResult LastTaskCompletionResult { get; private set; }

        public List<TfsWorkItem> TfsWorkItems { get; set; } = new List<TfsWorkItem>();


        public abstract int TimeoutInSeconds { get; set; }
        protected virtual string Iteration { get; set; } = string.Empty;
        protected virtual List<string> Users { get; set; } = new List<string>();
        protected virtual bool DuplicateParentsForUsers { get; set; } = false;

        protected AbstractWorkItemsRepository()
        {
            EditWorkItemWindow.WorkItemUpdated += OnWorkItemUpdated;
        }

        public IWorkItemsRepository WithIteration(string iteration)
        {
            Iteration = iteration;
            return this;
        }

        public IWorkItemsRepository WithUsers(List<string> users)
        {
            Users = users;
            return this;
        }

        protected abstract List<TfsWorkItem> GetOrderedList(List<TfsWorkItem> tfsWorkItems);

        public void GetWorkItems(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode, Action<RepositoryTaskCompletionResult> onCompleted)
        {
            if (IsUpdating || ProjectStructureRepository.Instance.TfsProjectStructure == null)
            {
                return;
            }

            var currentTaskRequest = new RepositoryTaskRequest(Guid.NewGuid());
            LastTaskRequest = currentTaskRequest;

            var currentCompletionResult = new RepositoryTaskCompletionResult(currentTaskRequest.TaskGuid);
            LastTaskCompletionResult = null;

            var task = Task.Run(() =>
            {
                GetWorkItemsInternal(catalogNode, projectInfoNode, currentCompletionResult);
            });
            Task.Run(() =>
            {
                IsUpdating = true;
                var isFinished = task.Wait(TimeSpan.FromSeconds(TimeoutInSeconds));
                if (!isFinished && currentTaskRequest.Equals(LastTaskRequest))
                {
                    var message = $"Timeout occurred while getting Work Items for [{catalogNode.Name}] [{projectInfoNode.Name}]";
                    currentCompletionResult = new RepositoryTaskCompletionResult(currentTaskRequest.TaskGuid)
                        { IsSuccess = false, DisplayMessage = message, Exception = new TimeoutException(message) };
                }

                if (currentTaskRequest.Equals(LastTaskRequest))
                {
                    LastTaskCompletionResult = currentCompletionResult;
                    onCompleted?.Invoke(LastTaskCompletionResult);
                    IsUpdating = false;
                }
            });
        }

        private void GetWorkItemsInternal(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode, RepositoryTaskCompletionResult completionResult)
        {
            try
            {
                using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath)))
                {
                    if (string.IsNullOrEmpty(BacklogOrganizerConfiguration.TfsUserName))
                    {
                        BacklogOrganizerConfiguration.TfsUserName = tfsConfigurationServer.AuthorizedIdentity.DisplayName;
                    }

                    var teamProjectCollection = tfsConfigurationServer.GetTeamProjectCollection(catalogNode.Id);
                    var workItemStore = teamProjectCollection.GetService<WorkItemStore>();

                    var projectWorkItems = TfsWorkItemsStoreConnector.GetAllWorkItemsInProject(workItemStore, catalogNode, projectInfoNode, Iteration, Users);

                    TfsWorkItemsStoreConnector.UpdateWorkItemLinks(workItemStore, projectWorkItems, projectInfoNode);

                    projectWorkItems = TfsWorkItem.LinkTasksWithWorkItems(projectWorkItems);
                    TfsWorkItemsStoreConnector.GetParentWorkItemsForOrphanTasks(workItemStore, catalogNode, projectInfoNode, projectWorkItems, DuplicateParentsForUsers);
                    projectWorkItems = TfsWorkItem.LinkTasksWithWorkItems(projectWorkItems);

                    TfsWorkItems = GetOrderedList(projectWorkItems);
                }
                completionResult.IsSuccess = true;
            }
            catch (Exception ex)
            {
                string message;
                if (ex.GetType().Name.Equals("TeamFoundationServiceUnavailableException"))
                {
                    message = "TFS services unavailable. Please check your internet connection and URA.";
                    Logger.Error(message, null);
                }
                else
                {
                    message = "Error occurred while getting TFS Work Items.";
                    Logger.Error(message, ex);
                    completionResult.Exception = ex;
                }

                completionResult.DisplayMessage = message;
                completionResult.IsSuccess = false;
            }
        }



        public TfsWorkItem GetWorkItem(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode, int workItemId, bool useCache)
        {
            if (useCache)
            {
                var cachedItem = TfsWorkItems.FirstOrDefault(x => x.Id == workItemId);
                if (cachedItem != null) return cachedItem;
            }

            var tfsWorkItem = GetWorkItemInternal(catalogNode, projectInfoNode, workItemId);
            var index = TfsWorkItems.FindIndex(x => x.Equals(tfsWorkItem) || x.ChildWorkItems.Any(y => y.Equals(tfsWorkItem)));
            if (index < 0) return tfsWorkItem;

            if (TfsWorkItems[index].Equals(tfsWorkItem))
            {
                tfsWorkItem.ChildWorkItems = TfsWorkItems[index].ChildWorkItems;
                TfsWorkItems.RemoveAt(index);
                TfsWorkItems.Add(tfsWorkItem);
            }
            else
            {
                var childIndex = TfsWorkItems[index].ChildWorkItems.FindIndex(x => x.Equals(tfsWorkItem));
                TfsWorkItems[index].ChildWorkItems.RemoveAt(childIndex);
                tfsWorkItem.Level = 1;
                TfsWorkItems[index].ChildWorkItems.Add(tfsWorkItem);
            }
            return tfsWorkItem;
        }
        protected void GetParentWorkItemsForTask(TfsWorkItem tfsWorkItem)
        {
            var parentWorkItems = new List<TfsWorkItem>();

            var parentLink = tfsWorkItem.WorkItem.Links.OfType<RelatedLink>().FirstOrDefault(x => x.LinkTypeEnd.Name.Equals("Parent"));
            if (parentLink == null) return;

            var parentWorkItem = GetWorkItemInternal(tfsWorkItem.ProjectCatalogNode, tfsWorkItem.ProjectInfoNode, parentLink.RelatedWorkItemId);
            if (parentWorkItem != null)
            {
                parentWorkItem.RelatedWorkItemIds.Add(tfsWorkItem.Id);
                parentWorkItems.Add(parentWorkItem);
            }

            TfsWorkItems.AddRange(parentWorkItems);
        }
        protected static TfsWorkItem GetWorkItemInternal(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode, int workItemId)
        {
            using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath)))
            {
                var teamProjectCollection = tfsConfigurationServer.GetTeamProjectCollection(catalogNode.Id);
                var workItemStore = teamProjectCollection.GetService<WorkItemStore>();
                var workItem = workItemStore.GetWorkItem(workItemId);
                return workItem == null ? null : TfsWorkItem.GetTfsWorkItem(workItem, catalogNode, projectInfoNode);
            }
        }

        public bool CreateTask(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode, string iterationPath, string title, 
                               int originalEstimate, TfsWorkItem parentWorkItem, string assignTo)
        {
            try
            {
                using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath)))
                {
                    var teamProjectCollection = tfsConfigurationServer.GetTeamProjectCollection(catalogNode.Id);
                    var workItemStore = teamProjectCollection.GetService<WorkItemStore>();
                    var teamProject = workItemStore.Projects[projectInfoNode.Name];
                    var workItemType = teamProject.WorkItemTypes["Task"];

                    var tfsTask = new WorkItem(workItemType)
                    {
                        Title = title,
                        State = "New",
                        AreaPath = BacklogOrganizerConfiguration.TfsConfiguration.TfsTaskAreaPath,
                        IterationPath = iterationPath,
                        Description = string.Empty
                    };
                    tfsTask.Fields["Assigned To"].Value = string.IsNullOrEmpty(assignTo) ? BacklogOrganizerConfiguration.TfsUserName : assignTo;
                    tfsTask.Fields["Original Estimate"].Value = originalEstimate;
                    tfsTask.Fields["Completed Work"].Value = 0;
                    tfsTask.Fields["Remaining Work"].Value = originalEstimate;

                    if (parentWorkItem != null)
                    {
                        var link = new WorkItemLink(workItemStore.WorkItemLinkTypes["System.LinkTypes.Hierarchy"].ReverseEnd, parentWorkItem.Id);
                        tfsTask.Links.Add(link);
                    }

                    var validity = tfsTask.Validate();
                    if (validity.Count != 0)
                    {
                        throw new Exception("Invalid Work Item");
                    }
                    tfsTask.Save();

                    var tfsWorkItem = TfsWorkItem.GetTfsWorkItem(tfsTask, catalogNode, projectInfoNode);
                    TfsWorkItems.Add(tfsWorkItem);

                    if (parentWorkItem == null)
                    {
                        GetParentWorkItemsForTask(tfsWorkItem);
                    }
                    else
                    {
                        if (TfsWorkItems.All(x => x.Id != parentWorkItem.Id))
                        {
                            TfsWorkItems.Add(parentWorkItem);
                        }
                        parentWorkItem.RelatedWorkItemIds.Add(tfsTask.Id);
                    }
                    TfsWorkItems = TfsWorkItem.LinkTasksWithWorkItems(TfsWorkItems);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error while creating task.", ex);
                return false;
            }
            return true;
        }

        private void RemoveCompletedTask(TfsWorkItem completedTask)
        {
            if (!TfsWorkItems.Remove(completedTask))
            {
                var parent = TfsWorkItems.FirstOrDefault(x => x.RelatedWorkItemIds.Contains(completedTask.Id));
                if (parent != null)
                {
                    parent.ChildWorkItems.Remove(completedTask);
                    parent.RelatedWorkItemIds.Remove(completedTask.Id);
                    if (parent.RelatedWorkItemIds.Count == 0)
                    {
                        TfsWorkItems.Remove(parent);
                    }
                }
            }
        }


        private void OnWorkItemUpdated(object sender, EventArgs e)
        {
            var tfsWorkItem = (TfsWorkItem) sender;
            if (!tfsWorkItem.IsATask) return;

            if (tfsWorkItem.State.CaseInsensitiveEquals("Closed") || tfsWorkItem.State.CaseInsensitiveEquals("Removed"))
            {
                RemoveCompletedTask(tfsWorkItem);
            }
            else
            {
                var index = TfsWorkItems.IndexOf(tfsWorkItem);
                if (index >= 0)
                {
                    TfsWorkItems.RemoveAt(index);
                    TfsWorkItems.Insert(index, tfsWorkItem);
                }
                else
                {
                    var parent = TfsWorkItems.FirstOrDefault(x => x.RelatedWorkItemIds.Contains(tfsWorkItem.Id));
                    if (parent != null)
                    {
                        index = parent.ChildWorkItems.IndexOf(tfsWorkItem);
                        if (index >= 0)
                        {
                            parent.ChildWorkItems.RemoveAt(index);
                            parent.ChildWorkItems.Insert(index, tfsWorkItem);
                        }
                    }
                }
            }

            WorkItemsUpdatingStatusChanged?.Invoke(this, null);
        }
    }
}
