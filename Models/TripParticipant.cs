using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using TripOrganiser.Areas.Identity.Data;

namespace TripOrganiser.Models
{
    public class TripParticipant
    {
        [Key, Column(Order = 0)] // Composite key starts here
        public int TripId { get; set; }

        [Key, Column(Order = 1)] // Composite key continues here
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public TripOrganiserUser User { get; set; } // Navigation property

        [ForeignKey("TripId")]
        public Trip Trip { get; set; } // Navigation property
    }
}

