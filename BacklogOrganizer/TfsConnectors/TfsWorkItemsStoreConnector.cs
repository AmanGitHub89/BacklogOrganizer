using System;
using System.Collections.Generic;
using System.Linq;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;

using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace BacklogOrganizer.TfsConnectors
{
    internal static class TfsWorkItemsStoreConnector
    {
        public static List<TfsWorkItem> GetAllWorkItemsInProject(WorkItemStore workItemStore, ProjectCatalogNode projectCatalogNode, ProjectInfoNode projectInfoNode,
                                                                          string iterationPath, List<string> assignedTo)
        {
            var workItemsQuery = string.Empty;
            try
            {
                var excludedWorkItemTypes = BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemTypes;
                var excludedWorkItemStates = BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemStates;

                workItemsQuery = "SELECT [System.Id], [System.WorkItemType], [System.State], [System.AssignedTo], [System.Title] FROM WorkItems";
                workItemsQuery += $" WHERE [System.TeamProject] = '{projectInfoNode.Name}'";
                if (assignedTo.Count > 0)
                {
                    workItemsQuery += $" AND [System.AssignedTo] IN ({GetSqlCsList(assignedTo)})";
                }
                if (!string.IsNullOrEmpty(iterationPath))
                {
                    workItemsQuery += $" AND [System.IterationPath] UNDER '{iterationPath}'";
                    workItemsQuery += $" AND [System.AreaPath] UNDER '{BacklogOrganizerConfiguration.TfsConfiguration.TfsTaskAreaPath}'";
                }
                workItemsQuery += $" AND [System.WorkItemType] NOT IN ({GetSqlCsList(excludedWorkItemTypes)})";
                workItemsQuery += $" AND [System.State] NOT IN ({GetSqlCsList(excludedWorkItemStates)})";
                workItemsQuery += GetExcludedWorkItemAreaPathsQuery();

                var workItemCollection = workItemStore.Query(workItemsQuery);
                return workItemCollection.OfType<WorkItem>().Select(x => TfsWorkItem.GetTfsWorkItem(x, projectCatalogNode, projectInfoNode)).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error - GetAllWorkItemsInProject - {ex.Message}{Environment.NewLine}{workItemsQuery}", null);
                throw;
            }
        }

        private static string GetExcludedWorkItemAreaPathsQuery()
        {
            var excludedWorkItemAreaPaths = BacklogOrganizerConfiguration.TfsConfiguration.ExcludedWorkItemAreaPaths;
            var workItemsQuery = string.Empty;
            foreach (var areaPath in excludedWorkItemAreaPaths)
            {
                workItemsQuery += $" AND [System.AreaPath] NOT UNDER '{areaPath}'";
            }

            return workItemsQuery;
        }

        public static void UpdateWorkItemLinks(WorkItemStore workItemStore, List<TfsWorkItem> tfsWorkItems, ProjectInfoNode projectInfoNode)
        {
            var queryString = string.Empty;
            try
            {
                var allItemIds = tfsWorkItems.Select(x => x.Id.ToString()).ToList();
                var parentItemIds = tfsWorkItems.Where(x => !x.Type.CaseInsensitiveContains("Task")).Select(x => x.Id.ToString()).ToList();
                var taskIds = allItemIds.Where(x => !parentItemIds.Any(y => y.Equals(x))).ToList();

                if (parentItemIds.Count == 0 || taskIds.Count == 0)
                {
                    return;
                }

                queryString = "SELECT Source.[System.Id], Target.[System.Id] FROM WorkItemLinks";
                queryString += $" WHERE Source.[System.TeamProject] = '{projectInfoNode.Name}' AND Target.[System.TeamProject] = '{projectInfoNode.Name}'";
                queryString += $" AND [System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward'";
                queryString += $" AND Source.[System.Id] IN ({GetSqlCsList(parentItemIds)}) AND Target.[System.Id] IN ({GetSqlCsList(taskIds)})";

                var query = new Query(workItemStore, queryString);
                var workItemLinkInfos = query.RunLinkQuery().ToList();

                //var parentLinkId = workItemStore.WorkItemLinkTypes.LinkTypeEnds["Parent"].Id;
                //var childLinkId = workItemStore.WorkItemLinkTypes.LinkTypeEnds["Parent"].Id;

                foreach (var workItemLinkInfo in workItemLinkInfos.Where(x => x.TargetId != 0).ToList())
                {
                    var workItem = tfsWorkItems.FirstOrDefault(x => x.Id == workItemLinkInfo.SourceId);
                    workItem?.RelatedWorkItemIds.Add(workItemLinkInfo.TargetId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error - UpdateWorkItemLinks - {ex.Message}{Environment.NewLine}queryString={queryString}", null);
                throw;
            }
        }

        public static void GetParentWorkItemsForOrphanTasks(WorkItemStore workItemStore, ProjectCatalogNode projectCatalogNode, ProjectInfoNode projectInfoNode, 
                                                            List<TfsWorkItem> tfsWorkItems, bool duplicateParentsForUsers)
        {
            var linksQuery = string.Empty;
            var workItemsQuery = string.Empty;
            try
            {
                var orphanTasks = tfsWorkItems.Where(x => x.IsATask && x.Level == 0).ToList();
                var orphanTaskIds = orphanTasks.Select(x => x.Id.ToString()).Distinct().ToList();

                if (orphanTaskIds.Count == 0)
                {
                    return;
                }

                linksQuery = "SELECT Source.[System.Id], Target.[System.Id] FROM WorkItemLinks";
                linksQuery += $" WHERE Source.[System.TeamProject] = '{projectInfoNode.Name}' AND Target.[System.TeamProject] = '{projectInfoNode.Name}'";
                linksQuery += $" AND [System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Reverse'";
                linksQuery += $" AND Source.[System.Id] IN ({GetSqlCsList(orphanTaskIds)}) mode(MustContain)";

                var query = new Query(workItemStore, linksQuery);
                var workItemLinkInfos = query.RunLinkQuery().Where(x => x.SourceId != 0 && orphanTaskIds.Contains(x.SourceId.ToString())).ToList();

                var parentLinkId = workItemStore.WorkItemLinkTypes.LinkTypeEnds["Parent"].Id;

                var parentItemIds = workItemLinkInfos.Where(x => x.LinkTypeId == parentLinkId && x.TargetId != 0).Select(x => x.TargetId.ToString()).ToList();

                if (parentItemIds.Count == 0)
                {
                    return;
                }

                workItemsQuery = "SELECT [System.Id], [System.WorkItemType], [System.State], [System.AssignedTo], [System.Title] FROM WorkItems";
                workItemsQuery += $" WHERE [System.TeamProject] = '{projectInfoNode.Name}'";
                workItemsQuery += $" AND [System.Id] IN ({GetSqlCsList(parentItemIds)})";

                var workItemCollection = workItemStore.Query(workItemsQuery);
                var parentWorkItems = workItemCollection.OfType<WorkItem>().ToList();

                var workItemLinkInfosWithValidLink = workItemLinkInfos.Where(x => x.TargetId != 0).ToList();
                foreach (var workItemLinkInfo in workItemLinkInfosWithValidLink)
                {
                    var task = orphanTasks.First(x => x.Id == workItemLinkInfo.SourceId);
                    var parent = parentWorkItems.First(x => x.Id == workItemLinkInfo.TargetId);

                    var userName = task.WorkItem.Fields["Assigned To"].Value.ToString();

                    TfsWorkItem parentWorkItem;
                    if (duplicateParentsForUsers)
                    {
                        //Try to get for same user first.
                        parentWorkItem = tfsWorkItems.FirstOrDefault(x => x.Id == workItemLinkInfo.TargetId && x.Level == 0 && x.IsSameUserName(userName));
                        //Else try if exists for some other user.
                        parentWorkItem = parentWorkItem ?? tfsWorkItems.FirstOrDefault(x => x.Id == workItemLinkInfo.TargetId && x.Level == 0);

                        if (parentWorkItem != null)
                        {
                            if (!parentWorkItem.IsSameUserName(userName))
                            {
                                //Parent might exist as another members task parent. Create new one for this member.
                                parentWorkItem = TfsWorkItem.GetTfsWorkItem(parentWorkItem.WorkItem, parentWorkItem.ProjectCatalogNode, parentWorkItem.ProjectInfoNode);
                            }
                        }
                        else
                        {
                            parentWorkItem = TfsWorkItem.GetTfsWorkItem(parent, projectCatalogNode, projectInfoNode);
                        }
                    }
                    else
                    {
                        parentWorkItem = tfsWorkItems.FirstOrDefault(x => x.Id == workItemLinkInfo.TargetId && x.Level == 0);
                        // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
                        if (parentWorkItem == null)
                        {
                            parentWorkItem = TfsWorkItem.GetTfsWorkItem(parent, projectCatalogNode, projectInfoNode);
                        }
                    }

                    if (parentWorkItem != null)
                    {
                        if (duplicateParentsForUsers)
                        {
                            parentWorkItem.UserName = userName;
                        }
                        parentWorkItem.RelatedWorkItemIds.Add(task.Id);
                        if (!tfsWorkItems.Contains(parentWorkItem))
                        {
                            tfsWorkItems.Add(parentWorkItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error - GetParentWorkItemsForOrphanTasks - {ex.Message}{Environment.NewLine}linksQuery={linksQuery}{Environment.NewLine}workItemsQuery={workItemsQuery}", null);
                throw;
            }
        }

        private static string GetSqlCsList(IEnumerable<string> stringList)
        {
            return string.Join(",", stringList.Select(x => "'" + x + "'"));
        }
    }
}
