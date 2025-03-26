namespace TripOrganiser.Models
{
    public class TripDetailsViewModel
    {
        public Trip Trip { get; set; } = null!;
        public string OwnerEmail { get; set; }
        public List<string> ParticipantIds { get; set; } = new();
        public List<string> OrganiserIds { get; set; } = new();
        public List<string> ParticipantEmails { get; set; } = new();
        public List<string> OrganisersEmails { get; set; } = new();
        public int ParticipantsCount { get; set; }
        public int OrganisersCount { get; set; }
        public bool isParticipant { get; set; }
        public bool isOrganiser { get; set; }
        public bool isOwner { get; set; }
    }
}
