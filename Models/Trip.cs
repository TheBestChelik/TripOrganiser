using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TripOrganiser.Areas.Identity.Data;

namespace TripOrganiser.Models
{
    public class Trip
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DestinationCity { get; set; } = string.Empty;

        [Required]
        public string DepartureAddress { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureDateTime { get; set; }

        [Required]
        public DateTime ReturnDateTime { get; set; }

        [Required]
        public int Capacity { get; set; }

        public string? Description { get; set; }

        [Required]
        public string InitialOwnerId { get; set; } = string.Empty;

        [ForeignKey("InitialOwnerId")]
        public TripOrganiserUser? InitialOwner { get; set; } //navigational property

        public List<TripOrganisator> Organisers { get; set; } = new();
        public List<TripParticipant> Participants { get; set; } = new();
    }
}


