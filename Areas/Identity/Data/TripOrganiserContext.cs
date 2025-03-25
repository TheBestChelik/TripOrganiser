using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TripOrganiser.Areas.Identity.Data;
using TripOrganiser.Models;

namespace TripOrganiser.Data;

public class TripOrganiserContext : IdentityDbContext<TripOrganiserUser>
{
    public TripOrganiserContext(DbContextOptions<TripOrganiserContext> options)
        : base(options)
    {
    }

    // DbSets for your entities
    public DbSet<Trip> Trips { get; set; }
    public DbSet<TripOrganisator> TripOrganisators { get; set; }
    public DbSet<TripParticipant> TripParticipants { get; set; }

    // Configure the relationships
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configuring the relationship between Trip and TripOrganiserUser (InitialOwner)
        builder.Entity<Trip>()
               .HasOne(t => t.InitialOwner)
               .WithMany()
               .HasForeignKey(t => t.InitialOwnerId)
               .OnDelete(DeleteBehavior.Restrict);

        // Configure the relationship for TripOrganisator
        builder.Entity<TripOrganisator>()
            .HasKey(to => new { to.TripId, to.UserId }); // Composite key
        builder.Entity<TripOrganisator>()
            .HasOne(to => to.Trip)
            .WithMany(t => t.Organisers)
            .HasForeignKey(to => to.TripId);
        builder.Entity<TripOrganisator>()
            .HasOne(to => to.User)
            .WithMany()
            .HasForeignKey(to => to.UserId);

        // Configure the relationship for TripParticipant
        builder.Entity<TripParticipant>()
            .HasKey(tp => new { tp.TripId, tp.UserId }); // Composite key for TripParticipant table

        builder.Entity<TripParticipant>()
            .HasOne(tp => tp.Trip) // A TripParticipant references a Trip
            .WithMany(t => t.Participants) // A Trip has many participants
            .HasForeignKey(tp => tp.TripId); // Foreign key in TripParticipant

        builder.Entity<TripParticipant>()
            .HasOne(tp => tp.User) // A TripParticipant references a User
            .WithMany() // A User can participate in many trips
            .HasForeignKey(tp => tp.UserId); // Foreign key in TripParticipant


    }

}
