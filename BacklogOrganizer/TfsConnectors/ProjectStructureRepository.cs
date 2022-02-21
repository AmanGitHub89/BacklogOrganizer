using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;


namespace BacklogOrganizer.TfsConnectors
{
    internal class ProjectStructureRepository
    {
        public event EventHandler ProjectStructureUpdated;
        private static ProjectStructureRepository myInstance;

        public bool IsUpdating { get; private set; }
        private Exception myLastError;

        internal static ProjectStructureRepository Instance => myInstance ?? (myInstance = new ProjectStructureRepository());

        private ProjectStructureRepository()
        {
        }

        internal ProjectStructure TfsProjectStructure { get; private set; }

        public void Load()
        {
            if (TfsProjectStructure != null)
            {
                ProjectStructureUpdated?.Invoke(this, null);
                return;
            }
            var projectStructure = BacklogOrganizerConfiguration.ProjectStructure;
            if (projectStructure != null && projectStructure.ProjectCatalogNodeList.Count > 0)
            {
                TfsProjectStructure = projectStructure;
                ProjectStructureUpdated?.Invoke(this, null);
                return;
            }
            if (IsUpdating) return;
            var task = Task.Run(() =>
            {
                CreateTfsProjectStructure();
            });
            Task.Run(() =>
            {
                IsUpdating = true;
                myLastError = null;
                var isFinished = task.Wait(TimeSpan.FromSeconds(60));
                IsUpdating = false;
                if (!isFinished && myLastError == null)
                {
                    myLastError = new TimeoutException("Timeout occurred while loading TFS projects.");
                }
                ProjectStructureUpdated?.Invoke(this, null);
            });
        }

        public void ClearData()
        {
            BacklogOrganizerConfiguration.ProjectStructure.ProjectCatalogNodeList.Clear();
            BacklogOrganizerConfiguration.ProjectStructure.Save();
            TfsProjectStructure = null;
            ProjectStructureUpdated?.Invoke(this, null);
        }



        private void CreateTfsProjectStructure()
        {
            TfsProjectStructure = null;
            new ProjectStructure().Save();
            ProjectStructureUpdated?.Invoke(this, null);
            try
            {
                using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath)))
                {
                    if (string.IsNullOrEmpty(BacklogOrganizerConfiguration.TfsUserName))
                    {
                        BacklogOrganizerConfiguration.TfsUserName = tfsConfigurationServer.AuthorizedIdentity.DisplayName;
                    }

                    var interestedProjectCollections = BacklogOrganizerConfiguration.TfsConfiguration.IncludedProjectCollections;
                    var interestedProjectTypes = BacklogOrganizerConfiguration.TfsConfiguration.IncludedProjectTypes;

                    var tfsProjectStructure = new ProjectStructure();

                    var catalogNodes = tfsConfigurationServer.CatalogNode
                        .QueryChildren(new[] {CatalogResourceTypes.ProjectCollection}, false, CatalogQueryOptions.None).Where(x =>
                            interestedProjectCollections.Count == 0 || interestedProjectCollections.CaseInsensitiveContains(x.Resource.DisplayName))
                        .ToList();

                    Parallel.ForEach(catalogNodes, collectionNode =>
                    {
                        var collectionId = new Guid(collectionNode.Resource.Properties["InstanceId"]);
                        // ReSharper disable once AccessToDisposedClosure
                        var teamProjectCollection = tfsConfigurationServer.GetTeamProjectCollection(collectionId);
                        var commonStructureService = (ICommonStructureService) teamProjectCollection.GetService(typeof(ICommonStructureService));

                        var allProjects = commonStructureService.ListAllProjects()
                            .Where(x => interestedProjectTypes.Count == 0 || interestedProjectTypes.CaseInsensitiveContains(x.Name)).ToList();
                        if (allProjects.Count == 0)
                        {
                            return;
                        }

                        var catalogNode = new ProjectCatalogNode(collectionNode)
                        {
                            ProjectInfoNodes = allProjects.Select(x => new ProjectInfoNode(Guid.NewGuid(), collectionId, x)).ToList()
                        };
                        tfsProjectStructure.ProjectCatalogNodeList.Add(catalogNode);
                    });

                    tfsProjectStructure.ProjectCatalogNodeList = tfsProjectStructure.ProjectCatalogNodeList.OrderBy(x => x.Name).ToList();
                    foreach (var catalogNode in tfsProjectStructure.ProjectCatalogNodeList)
                    {
                        catalogNode.ProjectInfoNodes = catalogNode.ProjectInfoNodes.OrderBy(x => x.Name).ToList();
                    }

                    BacklogOrganizerConfiguration.ProjectStructure = tfsProjectStructure;
                    if (myLastError == null)
                    {
                        //Timeout error might have already been set.
                        TfsProjectStructure = tfsProjectStructure;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name.Equals("TeamFoundationServiceUnavailableException"))
                {
                    Logger.Error("TFS services unavailable. Please check your internet connection and URA.", null);
                }
                Logger.Fatal("CreateTfsProjectStructure - Error", ex);
            }
        }
        public Exception GetLastError()
        {
            return myLastError;
        }

        public ProjectCatalogNode GetSelectedCatalog()
        {
            if (TfsProjectStructure == null) return null;
            return TfsProjectStructure.ProjectCatalogNodeList.FirstOrDefault(x => x.Name.Equals(BacklogOrganizerConfiguration.LastSelectedProjectCatalogName)) ??
                   TfsProjectStructure.ProjectCatalogNodeList.FirstOrDefault();
        }

        public ProjectInfoNode GetSelectedProject()
        {
            var catalogNode = GetSelectedCatalog();
            return catalogNode?.ProjectInfoNodes.FirstOrDefault(x => x.Name.Equals(BacklogOrganizerConfiguration.LastSelectedProjectTypeName)) ??
                   catalogNode?.ProjectInfoNodes.FirstOrDefault();
        }


        //Iteration and Area Path
        public void UpdateProjectIterationAndAreaPathAsync(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode, Action onLoaded = null)
        {
            if (projectInfoNode.IterationList.Count > 0 && projectInfoNode.AreaPaths.Count > 0)
            {
                onLoaded?.Invoke();
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath)))
                    {
                        var teamProjectCollection = tfsConfigurationServer.GetTeamProjectCollection(catalogNode.Id);
                        var workItemStore = teamProjectCollection.GetService<WorkItemStore>();

                        projectInfoNode.IterationList = GetIterations(teamProjectCollection.GetService<ICommonStructureService4>(), projectInfoNode);
                        projectInfoNode.AreaPaths = GetAreaPaths(workItemStore, projectInfoNode.Name);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                onLoaded?.Invoke();
            });
        }

        private static List<TfsIteration> GetIterations(ICommonStructureService4 css, ProjectInfoNode projectInfoNode)
        {
            var structures = css.ListStructures(projectInfoNode.Uri);
            var iteration = structures.FirstOrDefault(n => n.StructureType.Equals("ProjectLifecycle"));
            if (iteration == null) return new List<TfsIteration>();
            var iterationsTree = css.GetNodesXml(new[] { iteration.Uri }, true);

            var baseName = projectInfoNode.Name + @"\";

            var iterations = new List<TfsIteration>();
            BuildIterationTree(iterationsTree.ChildNodes[0].ChildNodes, baseName, projectInfoNode.Id, ref iterations);

            iterations = iterations.OrderBy(x => x.StartDate).ToList();
            return iterations;
        }

        private static void BuildIterationTree(XmlNodeList nodeList, string baseName, Guid projectInfoNodeId, ref List<TfsIteration> iterations)
        {
            foreach (XmlNode node in nodeList)
            {
                if (node.ChildNodes.Count == 0 &&
                    node.Attributes?["NodeID"] != null &&
                    node.Attributes["Name"] != null &&
                    node.Attributes["StartDate"] != null &&
                    node.Attributes["FinishDate"] != null &&
                    node.Attributes["Path"] != null)
                {
                    // Found TfsIteration with start / end dates
                    var name = node.Attributes["Name"].Value;
                    var startDate = DateTime.Parse(node.Attributes["StartDate"].Value, CultureInfo.InvariantCulture);
                    var finishDate = DateTime.Parse(node.Attributes["FinishDate"].Value, CultureInfo.InvariantCulture);
                    var path = node.Attributes["Path"].Value;
                    var nodeId = node.Attributes["NodeID"].Value;
                    var parentId = node.Attributes["ParentID"].Value;
                    var projectId = node.Attributes["ProjectID"].Value;

                    iterations.Add(new TfsIteration(Guid.NewGuid(), projectInfoNodeId)
                    { Name = name, Path = path, StartDate = startDate, FinishDate = finishDate, NodeId = nodeId, ParentId = parentId, ProjectId = projectId });
                }
                else if (node.Attributes?["Name"] != null)
                {
                    // Found TfsIteration without start / end dates
                    //var name = node.Attributes["Name"].Value;
                }

                if (node.ChildNodes.Count > 0)
                {
                    var name = baseName;
                    if (node.Attributes?["Name"] != null)
                        name += node.Attributes["Name"].Value + @"\";

                    BuildIterationTree(node.ChildNodes, name, projectInfoNodeId, ref iterations);
                }
            }
        }

        private static List<string> GetAreaPaths(WorkItemStore workItemStore, string projectName)
        {
            var project = workItemStore.Projects.Cast<Project>().FirstOrDefault(x => x.Name == projectName);
            if (project == null)
                return new List<string>();

            var areaPaths = new List<string>();
            foreach (Node area in project.AreaRootNodes)
            {
                GetLeafNodeAreaPath(area, ref areaPaths);

                //areaPaths.AddRange(area.ChildNodes.OfType<Node>().Where(x => !x.HasChildNodes).Select(y => y.Name));
                //areaPaths.AddRange(from Node item in area.ChildNodes select item.Path);
            }

            return areaPaths;
        }

        private static void GetLeafNodeAreaPath(Node node, ref List<string> areaPaths)
        {
            if (node.HasChildNodes)
            {
                foreach (Node childNode in node.ChildNodes)
                {
                    GetLeafNodeAreaPath(childNode, ref areaPaths);
                }
            }

            areaPaths.Add(node.Path);
        }

        public TfsIteration GetCurrentIteration(ProjectInfoNode projectInfoNode)
        {
            var dateTimeNow = DateTime.Now;
            var iteration =  projectInfoNode.IterationList.FirstOrDefault(x => x.StartDate.Date <= dateTimeNow.Date && x.FinishDate.Date >= dateTimeNow.Date);
            return iteration ?? projectInfoNode.IterationList.FirstOrDefault(x => x.StartDate.Date.AddDays(-1).Date <= dateTimeNow.Date && x.FinishDate.Date >= dateTimeNow.Date);
        }

        public TfsIteration GetIteration(ProjectInfoNode projectInfoNode, string iterationPath, bool getNext)
        {
            if (string.IsNullOrEmpty(iterationPath)) return null;

            var inputIterationIndex = projectInfoNode.IterationList.FindIndex(x => !string.IsNullOrEmpty(x.DisplayedPath) && x.DisplayedPath.Equals(iterationPath));
            TfsIteration iteration = null;
            if (getNext && projectInfoNode.IterationList.Count > inputIterationIndex + 1)
            {
                iteration = projectInfoNode.IterationList[inputIterationIndex + 1];
            }
            else if (!getNext && inputIterationIndex > 0)
            {
                iteration = projectInfoNode.IterationList[inputIterationIndex - 1];
            }
            return iteration;
        }
    }
}
