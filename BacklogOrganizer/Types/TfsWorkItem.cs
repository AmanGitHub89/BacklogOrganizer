using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Utilities;

using Microsoft.TeamFoundation.WorkItemTracking.Client;


namespace BacklogOrganizer.Types
{
    public class TfsWorkItem : IEquatable<TfsWorkItem>
    {
        public string UserName { get; set; }

        public int Id { get; }

        //0 based index for tree structure. i.e. Parent is 0 and child is 1. Currently only two levels.
        public int Level { get; set; }

        public ProjectCatalogNode ProjectCatalogNode { get; }

        public ProjectInfoNode ProjectInfoNode { get; }

        public string Type { get; private set; }

        public string State { get; private set; }

        public string Title { get; private set; }

        public string Url { get; private set; }

        public bool IsOnHold { get; set; }


        #region Binding Properties

        public Thickness LeftMargin => Level == 1 ? new Thickness(20, 0, 10, 0) : new Thickness(0, 0, 10, 0);

        public bool HasAnActiveTask => IsATask || (ChildWorkItems.Count > 0 && ChildWorkItems.Any(x => x.IsTaskActive));

        public Visibility ExpanderVisibility => HasChildren ? Visibility.Visible : Visibility.Hidden;

        public bool ActiveChildTasksAreUpdated
        {
            get
            {
                if (IsATask && !HasChildren) return IsTaskUpdated;
                var activeTasks = ChildWorkItems.Where(x => x.IsTaskActive).ToList();
                return activeTasks.Count > 0 && activeTasks.All(x => x.IsTaskUpdated);
            }
        }

        public string LastChanged
        {
            get
            {
                var timeSpan = DateTime.Now - WorkItem.ChangedDate;
                return timeSpan.Days > 0 ? $"Updated {timeSpan.Days} days ago." : $"Updated {timeSpan.Hours} hours ago.";
            }
        }

        public string ToolTipText {
            get
            {
                if (Level == 1 || !HasChildren) return LastChanged;
                if (ActiveChildTasksAreUpdated) return null;
                var text = string.Empty;
                foreach (var childWorkItem in ChildWorkItems.Where(x => x.IsTaskActive && !x.IsTaskUpdated))
                {
                    text += $"Task {childWorkItem.Id} {childWorkItem.LastChanged}{Environment.NewLine}";
                }
                text = text.Trim();
                return string.IsNullOrEmpty(text) ? null : text;
            }
        }

        public string DisplayUserName
        {
            get
            {
                var parts = UserName.Split(new[] { '(' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 0 ? parts[0] : UserName;
            }
        }

        #region Visibility Properties
        public Visibility AddTaskVisibility => Level == 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ToggleActiveStateVisibility => Level == 0 ? Visibility.Visible : Visibility.Collapsed;
        public string IterationPath => WorkItem.IterationPath;
        #endregion

        #endregion
        public string OnHoldFullName => $"{ProjectCatalogNode.Name}_{ProjectInfoNode.Name}_{Id}";

        public WorkItem WorkItem { get; private set; }

        public List<int> RelatedWorkItemIds { get; } = new List<int>();

        public List<TfsWorkItem> ChildWorkItems { get; set; } = new List<TfsWorkItem>();

        public bool IsTaskActive => IsATask && State.Equals("Active", StringComparison.OrdinalIgnoreCase);

        public bool IsTaskUpdated
        {
            get
            {
                var updated = WorkItem.ChangedDate >= DateTime.Now.AddHours(-BacklogOrganizerConfiguration.AppConfiguration.WorkItemNotUpdatedDuration);
                return updated;
            }
        }

        public bool IsATask => Type.CaseInsensitiveContains("Task");

        public bool HasChildren => ChildWorkItems.Count > 0;

        public bool IsExpanded { get; set; }

        protected TfsWorkItem(int id, ProjectCatalogNode projectCatalogNode, ProjectInfoNode projectInfoNode)
        {
            Id = id;
            ProjectCatalogNode = projectCatalogNode;
            ProjectInfoNode = projectInfoNode;
        }

        public static List<TfsWorkItem> LinkTasksWithWorkItems(List<TfsWorkItem> workItems)
        {
            var childWorkItems = workItems.Where(x => workItems.Any(y => y.RelatedWorkItemIds.Contains(x.Id))).ToList();

            foreach (var childWorkItem in childWorkItems)
            {
                workItems.Remove(childWorkItem);

                var parentWorkItems = workItems.Where(x => x.RelatedWorkItemIds.Contains(childWorkItem.Id)).ToList();
                foreach (var parentWorkItem in parentWorkItems)
                {
                    childWorkItem.Level = 1;
                    parentWorkItem.ChildWorkItems.Add(childWorkItem);
                }
            }

            return workItems;
        }

        public static TfsWorkItem GetTfsWorkItem(WorkItem workItem, ProjectCatalogNode projectCatalogNode, ProjectInfoNode projectInfoNode)
        {
            var tfsWorkItem = new TfsWorkItem(workItem.Id, projectCatalogNode, projectInfoNode)
            {
                Type = workItem.Type.Name,
                State = workItem.State,
                Title = workItem.Title,
                Url = $"{BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath}/{projectCatalogNode.Name}/{workItem.Project.Name}/_workitems/edit/{workItem.Id}",
                WorkItem = workItem,
                UserName = GetName(workItem)
            };

            tfsWorkItem.IsOnHold = BacklogOrganizerConfiguration.OnHoldWorkItemIds.CommaSeparatedStringToList().CaseInsensitiveContains(tfsWorkItem.OnHoldFullName);

            //if (workItem.RelatedLinkCount > 0)
            //{
            //    tfsWorkItem.RelatedWorkItemIds = workItem.Links.OfType<RelatedLink>().Where(x => x.LinkTypeEnd.Name.Equals("Child")).Select(y => y.RelatedWorkItemId).ToList();
            //}

            return tfsWorkItem;
        }

        public void UpdateWorkItem()
        {
            State = WorkItem.State;
            Title = WorkItem.Title;
        }

        private static string GetName(WorkItem workItem)
        {
            var value = workItem.Fields["Assigned To"].Value;
            return value == null ? "<unassigned>" : value.ToString();
        }


        public bool Equals(TfsWorkItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsSameUserName(other) && Id == other.Id && ProjectCatalogNode.Id.Equals(other.ProjectCatalogNode.Id) &&
                   ProjectInfoNode.Id.Equals(other.ProjectInfoNode.Id);
        }

        public bool IsSameUserName(TfsWorkItem other)
        {
            if (string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(other.UserName)) return true;
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(other.UserName)) return false;
            return UserName.CaseInsensitiveEquals(other.UserName);
        }

        public bool IsSameUserName(string userName)
        {
            if (string.IsNullOrEmpty(UserName) && string.IsNullOrEmpty(userName)) return true;
            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(userName)) return false;
            return UserName.CaseInsensitiveEquals(userName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TfsWorkItem) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (UserName != null ? UserName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Id;
                hashCode = (hashCode * 397) ^ (ProjectCatalogNode != null ? ProjectCatalogNode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProjectInfoNode != null ? ProjectInfoNode.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
