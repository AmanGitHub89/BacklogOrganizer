using System;

using BacklogOrganizer.Types;


namespace BacklogOrganizer.Reminders
{
    public class Reminder
    {
        public int WorkItemId { get; set; }
        public ProjectCatalogNode CatalogNode { get; set; }
        public ProjectInfoNode ProjectNode { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string LastChanged { get; set; }

        public DateTime ReminderDateTime { get; set; }

        public string ReminderDescription { get; set; }

        public bool IsMissedReminder;

        public Reminder()
        {
        }

        public Reminder(DateTime reminderDateTime, string reminderDescription) : this()
        {
            ReminderDateTime = reminderDateTime;
            ReminderDescription = reminderDescription;
        }

        public Reminder(DateTime reminderDateTime, string reminderDescription, TfsWorkItem tfsWorkItem) : this(reminderDateTime, reminderDescription)
        {
            ReminderDateTime = reminderDateTime;
            ReminderDescription = reminderDescription;
            WorkItemId = tfsWorkItem.Id;
            CatalogNode = tfsWorkItem.ProjectCatalogNode;
            ProjectNode = tfsWorkItem.ProjectInfoNode;
            Type = tfsWorkItem.Type;
            Title = tfsWorkItem.Title;
            Url = tfsWorkItem.Url;
            LastChanged = tfsWorkItem.LastChanged;
        }
    }
}
