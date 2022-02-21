using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;


namespace BacklogOrganizer.Types
{
    internal class SavedTfsTeamsData
    {
        public DateTime UpdatedDateTime { get; set; } = DateTime.Now;
        public List<SavedTfsTeams> SavedTfsTeamsList { get; set; } = new List<SavedTfsTeams>();
    }
    internal class SavedTfsTeams : IEquatable<SavedTfsTeams>
    {
        public string CatalogName { get; set; }
        public string ProjectName { get; set; }
        public List<SavedTfsTeam> TfsTeamsList { get; set; } = new List<SavedTfsTeam>();

        public bool Equals(SavedTfsTeams other)
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
            return Equals((SavedTfsTeams) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((CatalogName != null ? CatalogName.GetHashCode() : 0) * 397) ^ (ProjectName != null ? ProjectName.GetHashCode() : 0);
            }
        }
    }
    internal class SavedTfsTeam : IEquatable<SavedTfsTeam>
    {
        public Guid TeamId { get; set; }

        public string TeamName { get; set; }

        public List<string> TeamMembers { get; set; } = new List<string>();

        public List<string> OriginalTeamMembers { get; set; } = new List<string>();

        public List<string> TeamAdministrators { get; set; } = new List<string>();

        [JsonIgnore]
        public string TeamTooltip => TeamAdministrators.Count > 0 ? $"Administrators:{Environment.NewLine}{string.Join(Environment.NewLine, TeamAdministrators)}" : null;

        public List<string> GetTeamMembersToDisplay()
        {
            return TeamMembers.Count > 0 ? TeamMembers.Distinct().ToList() : OriginalTeamMembers.Distinct().ToList();
        }

        public bool Equals(SavedTfsTeam other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return TeamName == other.TeamName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SavedTfsTeam) obj);
        }

        public override int GetHashCode()
        {
            return (TeamName != null ? TeamName.GetHashCode() : 0);
        }
    }
}
