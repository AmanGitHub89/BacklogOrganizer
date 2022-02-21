using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BacklogOrganizer.Configuration;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Server;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace BacklogOrganizer.Types
{
    public class ProjectStructure
    {
        public List<ProjectCatalogNode> ProjectCatalogNodeList { get; set; } = new List<ProjectCatalogNode>();

        public void Save()
        {
            BacklogOrganizerConfiguration.ProjectStructure = this;
        }
    }

    /// <summary>
    /// Do not remove default/empty constructor and private setters as newtonsoft uses this to Deserialize
    /// </summary>
    public class ProjectCatalogNode
    {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public Guid Id { get; private set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Name { get; private set; }

        public List<ProjectInfoNode> ProjectInfoNodes { get; set; } = new List<ProjectInfoNode>();

        public ProjectCatalogNode()
        {

        }

        public ProjectCatalogNode(CatalogNode catalogNode)
        {
            TfsCatalogNode = catalogNode;
            Id = new Guid(catalogNode.Resource.Properties["InstanceId"]);
            Name = catalogNode.Resource.DisplayName;
        }

        /// <summary>
        /// Not to be used anywhere, except for debugging
        /// </summary>
        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public CatalogNode TfsCatalogNode { get; }
    }

    /// <summary>
    /// Do not remove default/empty constructor and private setters as newtonsoft uses this to Deserialize
    /// </summary>
    public class ProjectInfoNode
    {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public Guid Id { get; private set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public Guid CatalogId { get; private set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Name { get; private set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Uri { get; private set; }

        public ProjectInfoNode()
        {

        }
        public ProjectInfoNode(Guid id, Guid catalogId, ProjectInfo projectInfo)
        {
            Id = id;
            CatalogId = catalogId;
            TfsProjectInfo = projectInfo;
            Name = projectInfo.Name;
            Uri = projectInfo.Uri;
        }

        /// <summary>
        /// Not to be used anywhere, except for debugging
        /// </summary>
        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public ProjectInfo TfsProjectInfo { get; }

        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public List<TfsIteration> IterationList { get; set; } = new List<TfsIteration>();

        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public List<string> AreaPaths { get; set; } = new List<string>();
    }

    internal class ProjectStructureJsonContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);

            if (member.DeclaringType == typeof(ProjectCatalogNode))
            {
                switch (prop.PropertyName)
                {
                    case "Id":
                    case "Name":
                        prop.Writable = true;
                        break;
                }
            }
            else if (member.DeclaringType == typeof(ProjectInfoNode))
            {
                switch (prop.PropertyName)
                {
                    case "Id":
                    case "CatalogId":
                    case "Name":
                    case "Uri":
                        prop.Writable = true;
                        break;
                }
            }

            return prop;
        }
    }


    public class TfsIteration
    {
        public Guid ProjectInfoNodeId { get; }
        public Guid TfsIterationId { get; }

        public TfsIteration(Guid id, Guid projectInfoNodeId)
        {
            TfsIterationId = id;
            ProjectInfoNodeId = projectInfoNodeId;
        }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime FinishDate { get; set; }
        public string Path { get; set; }
        public string DisplayedPath {
            get
            {
                if (Path == null) return null;
                var returnValue = Path.Clone().ToString();
                if (Path.StartsWith("\\"))
                {
                    returnValue = Path.TrimStart('\\');
                }

                return returnValue.Replace("Iteration\\", string.Empty);

            }
        }
        public string NodeId { get; set; }
        public string ParentId { get; set; }
        public string ProjectId { get; set; }
    }

    public class TfsProjectTeams : IEquatable<TfsProjectTeams>
    {
        public string CatalogName { get; set; }
        public string ProjectName { get; set; }
        public List<TfsTeamFoundationTeam> Teams { get; set; }

        public bool Equals(TfsProjectTeams other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CatalogName == other.CatalogName && ProjectName == other.ProjectName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TfsProjectTeams) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((CatalogName != null ? CatalogName.GetHashCode() : 0) * 397) ^ (ProjectName != null ? ProjectName.GetHashCode() : 0);
            }
        }
    }
    public class TfsTeamFoundationTeam : IEquatable<TfsTeamFoundationTeam>
    {
        public TeamFoundationTeam Team { get; set; }
        public List<TeamFoundationIdentity> TeamMembers { get; set; }
        public List<string> TeamMemberNames => TeamMembers.Select(x => x.DisplayName).ToList();

        public TfsTeamFoundationTeam(TeamFoundationTeam team, List<TeamFoundationIdentity> teamMembers)
        {
            Team = team;
            TeamMembers = teamMembers;
        }

        public bool Equals(TfsTeamFoundationTeam other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Team, other.Team);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TfsTeamFoundationTeam) obj);
        }

        public override int GetHashCode()
        {
            return (Team != null ? Team.Name.GetHashCode() : 0);
        }
    }

}
