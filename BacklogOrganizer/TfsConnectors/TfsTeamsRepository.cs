using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BacklogOrganizer.Configuration;
using BacklogOrganizer.Types;
using BacklogOrganizer.Utilities;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;


namespace BacklogOrganizer.TfsConnectors
{
    internal class TfsTeamsRepository
    {
        private readonly List<SavedTfsTeams> myTfsProjectTeamsList = new List<SavedTfsTeams>();
        public IReadOnlyList<SavedTfsTeams> TfsProjectTeamsList => myTfsProjectTeamsList.ToList();

        private static TfsTeamsRepository myInstance;
        internal static TfsTeamsRepository Instance => myInstance ?? (myInstance = new TfsTeamsRepository());
        private TfsTeamsRepository()
        {
        }

        public void ClearData()
        {
            myTfsProjectTeamsList.Clear();
            BacklogOrganizerConfiguration.SavedTfsTeamsData = new SavedTfsTeamsData();
        }

        public SavedTfsTeams GetAllTeamsForSelectedProject()
        {
            var selectedCatalogNode = ProjectStructureRepository.Instance.GetSelectedCatalog();
            if (selectedCatalogNode == null) return null;

            var selectedProjectTypeNode = ProjectStructureRepository.Instance.GetSelectedProject();
            if (selectedProjectTypeNode == null) return null;

            return myTfsProjectTeamsList.FirstOrDefault(x =>
                x.CatalogName.CaseInsensitiveEquals(selectedCatalogNode.Name) &&
                x.ProjectName.CaseInsensitiveEquals(selectedProjectTypeNode.Name));
        }

        public async Task<SavedTfsTeams> GetAllTeams(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode)
        {
            var savedTfsTeams = new SavedTfsTeams();
            await Task.Run(() =>
            {
                try
                {
                    if (myTfsProjectTeamsList.Count == 0)
                    {
                        var savedTfsTeamsData = BacklogOrganizerConfiguration.SavedTfsTeamsData;
                        myTfsProjectTeamsList.AddRange(savedTfsTeamsData.SavedTfsTeamsList);
                    }

                    var teamsForProject = myTfsProjectTeamsList.FirstOrDefault(x =>
                        x.CatalogName.CaseInsensitiveEquals(catalogNode.Name) &&
                        x.ProjectName.CaseInsensitiveEquals(projectInfoNode.Name));
                    if (teamsForProject != null)
                    {
                        savedTfsTeams = teamsForProject;
                    }
                    else
                    {
                        using (var tfsConfigurationServer = TfsConfigurationServerFactory.GetConfigurationServer(new Uri(BacklogOrganizerConfiguration.TfsConfiguration.TfsServerPath)))
                        {
                            var tpc = tfsConfigurationServer.GetTeamProjectCollection(catalogNode.Id);

                            savedTfsTeams.CatalogName = catalogNode.Name;
                            savedTfsTeams.ProjectName = projectInfoNode.Name;

                            var teams = GetTeams(tpc, projectInfoNode);
                            savedTfsTeams.TfsTeamsList = teams;

                            myTfsProjectTeamsList.Add(savedTfsTeams);

                            var savedTfsTeamsData = BacklogOrganizerConfiguration.SavedTfsTeamsData;
                            if (savedTfsTeamsData == null || savedTfsTeamsData.SavedTfsTeamsList.Count == 0)
                            {
                                savedTfsTeamsData = new SavedTfsTeamsData();
                            }

                            savedTfsTeamsData.SavedTfsTeamsList.Add(savedTfsTeams);
                            BacklogOrganizerConfiguration.SavedTfsTeamsData = savedTfsTeamsData;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error while getting teams.", ex);
                }
            });
            return savedTfsTeams;
        }

        public List<SavedTfsTeam> GetMyTeams(ProjectCatalogNode catalogNode, ProjectInfoNode projectInfoNode)
        {
            var teamsForProject = myTfsProjectTeamsList.FirstOrDefault(x =>
                x.CatalogName.CaseInsensitiveEquals(catalogNode.Name) &&
                x.ProjectName.CaseInsensitiveEquals(projectInfoNode.Name));
            return teamsForProject != null ? teamsForProject.TfsTeamsList.Where(x => x.GetTeamMembersToDisplay().
                       CaseInsensitiveContains(BacklogOrganizerConfiguration.TfsUserName)).ToList() : new List<SavedTfsTeam>();
        }

        private static List<SavedTfsTeam> GetTeams(TfsTeamProjectCollection tpc, ProjectInfoNode projectInfoNode)
        {
            var tfsTeamService = tpc.GetService<TfsTeamService>();
            var teams = tfsTeamService.QueryTeams(projectInfoNode.Uri).ToList();

            var savedTeams = teams.Select(x => GetSavedTfsTeam(tpc, x)).ToList();

            var teamUsers = savedTeams.SelectMany(x => x.OriginalTeamMembers).ToList();
            var allUsersInProject = GetAllUsersInProject(tpc, projectInfoNode);
            var nonTeamUsers = allUsersInProject.Where(x => !teamUsers.Any(y => y.Equals(x))).ToList();

            savedTeams.Add(new SavedTfsTeam {TeamId = Guid.NewGuid(), TeamName = "Others", OriginalTeamMembers = nonTeamUsers});

            return savedTeams;
        }

        public static SavedTfsTeam GetSavedTfsTeam(TfsTeamProjectCollection tpc, TeamFoundationTeam team)
        {
            var savedTfsTeam = new SavedTfsTeam {TeamId = team.Identity.TeamFoundationId, TeamName = team.Name};

            var members = team.GetMembers(tpc, MembershipQuery.Expanded).ToList();
            var memberNames = members.Select(x => x.DisplayName).ToList();

            savedTfsTeam.OriginalTeamMembers = memberNames;

            savedTfsTeam.TeamAdministrators = GetTeamAdmins(tpc, team, members);

            return savedTfsTeam;
        }

        private static List<string> GetTeamAdmins(TfsTeamProjectCollection tpc, TeamFoundationTeam team, List<TeamFoundationIdentity> members)
        {
            try
            {
                var securityService = tpc.GetService<ISecurityService>();
                var securityNamespace = securityService.GetSecurityNamespace(FrameworkSecurity.IdentitiesNamespaceId);

                var token = IdentityHelper.CreateSecurityToken(team.Identity);

                var acl = securityNamespace.QueryAccessControlList(token, members.Select(m => m.Descriptor), true);

                // Retrieve the team administrator SIDs by querying the ACL entries.
                var entries = acl.AccessControlEntries;
                var admins = entries.Where(e => (e.Allow & 15) == 15).Select(e => e.Descriptor.Identifier);

                // Finally, retrieve the actual TeamFoundationIdentity objects from the SIDs.
                var adminIdentities = members.Where(m => admins.Contains(m.Descriptor.Identifier)).ToList();
                return adminIdentities.Select(x => x.DisplayName).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static List<string> GetAllUsersInProject(TfsTeamProjectCollection tpc, ProjectInfoNode projectInfoNode)
        {
            var ims = tpc.GetService<IIdentityManagementService>();

            var validGroupName = $"[{projectInfoNode.Name}]\\Contributors";
            var group = ims.ReadIdentity(IdentitySearchFactor.DisplayName, validGroupName, MembershipQuery.Expanded, ReadIdentityOptions.ExtendedProperties);

            var memberIds = group.Members.Select(x => x.Identifier).ToList();

            var members = ims.ReadIdentities(IdentitySearchFactor.Identifier, memberIds.ToArray(),
                MembershipQuery.Expanded, ReadIdentityOptions.ExtendedProperties);

            return members.Where(member => !member[0].IsContainer).Select(x => x[0].DisplayName).ToList();
        }
    }
}
