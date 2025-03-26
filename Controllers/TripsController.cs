using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TripOrganiser.Data;
using TripOrganiser.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TripOrganiser.Areas.Identity.Data;
using System.Security.Claims;
using System.Collections;

namespace TripOrganiser.Controllers
{

    [Authorize]
    public class TripsController : Controller
    {

        private readonly UserManager<TripOrganiserUser> _userManager;
        private readonly TripOrganiserContext _context;

        public TripsController(TripOrganiserContext context, UserManager<TripOrganiserUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Trips
        public async Task<IActionResult> Index(string filter = "All")
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var trips = await _context.Trips
                .Include(t => t.InitialOwner)
                .Include(t => t.Participants) 
                .Include(t => t.Organisers)
                .ToListAsync();

            var filteredTrips = trips.Select(trip => new
            {
                trip.Id,
                trip.DestinationCity,
                trip.DepartureDateTime,
                trip.ReturnDateTime,
                trip.Capacity,
                trip.InitialOwnerId,
                trip.Description,
                isParticipant = trip.Participants?.Any(p => p.UserId == userId) ?? false,
                isOrganiser = trip.Organisers?.Any(p => p.UserId == userId) ?? false,
                isOwner = trip.InitialOwnerId == userId,
                participantCount = trip.Participants?.Count ?? 0,
                organisersCont = trip.Organisers?.Count ?? 0,
                InitialOwnerEmail = trip.InitialOwner?.Email,
            }).ToList();
            switch (filter)
            {
                case "MyTrips":
                    filteredTrips = filteredTrips.Where(t => t.isOwner).ToList();
                    break;
                case "OrganiserTrips":
                    filteredTrips = filteredTrips.Where(t => t.isOrganiser).ToList();
                    break;
                case "JoinedTrips":
                    filteredTrips = filteredTrips.Where(t => t.isParticipant).ToList();
                    break;
                case "All":
                default:
                    break;
            }
            ViewData["Filter"] = filter;

            return View(filteredTrips);
        }

        // GET: Trips/Details/5
        public async Task<IActionResult> Details(int? id)
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .Include(t => t.InitialOwner)
                .Include(t => t.Participants) // Include participants
                    .ThenInclude(p => p.User) // Include user details
                .Include(t => t.Organisers)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trip == null)
            {
                return NotFound();
            }

            var viewModel =  new TripDetailsViewModel
            {
                Trip = trip,
                isOwner = trip.InitialOwnerId == userId,
                isOrganiser = trip.Organisers.Any(o => o.UserId == userId),
                isParticipant = trip.Participants.Any(p => p.UserId == userId),
                ParticipantIds = trip.Participants.Select(p => p.UserId).ToList(),
                OrganiserIds = trip.Organisers.Select(p => p.UserId).ToList(),
                ParticipantsCount = trip.Participants?.Count ?? 0,
                OrganisersCount = trip.Organisers?.Count ?? 0,
                OwnerEmail = trip.InitialOwner?.Email ?? "",
                ParticipantEmails = trip.Participants.Select(p => p.User.Email).ToList(),
                OrganisersEmails = trip.Organisers.Select(p => p.User.Email).ToList(),

            };
            return View(viewModel);
        }

        // GET: Trips/Create
        public IActionResult Create()
        {
            //ViewData["InitialOwnerId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Trips/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DestinationCity,DepartureAddress,DepartureDateTime,ReturnDateTime,Capacity,Description")] Trip trip)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            trip.InitialOwnerId = user.Id;
            ModelState.Remove("RowVersion");
            //trip.RowVersion = new byte[0]; // Initialize RowVersion to an empty array

            if (ModelState.IsValid)
            {
                _context.Add(trip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                foreach (var kvp in ModelState)
                {
                    foreach (var error in kvp.Value.Errors)
                    {
                        Console.WriteLine($"Property: {kvp.Key}, Error: {error.ErrorMessage}");
                    }
                }
            }

            return View(trip);
        }

        // GET: Trips/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .Include(t => t.InitialOwner)
                .Include(t => t.Participants) // Include participants
                    .ThenInclude(p => p.User) // Include user details
                .Include(t => t.Organisers)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trip == null)
            {
                return NotFound();
            }

            if (trip.InitialOwnerId != userId && !trip.Organisers.Any(o => o.UserId == userId))
            {
                return Unauthorized(); // Ensure the user is the owner or an organiser
            }
            Console.WriteLine("RowVersion: " + BitConverter.ToString(trip.RowVersion));

            var viewModel = new TripDetailsViewModel
            {
                Trip = trip,
                ParticipantIds = trip.Participants.Select(p => p.UserId).ToList(),
                OrganiserIds = trip.Organisers.Select(p => p.UserId).ToList(),
                ParticipantsCount = trip.Participants?.Count ?? 0,
                OrganisersCount = trip.Organisers?.Count ?? 0,
                OwnerEmail = trip.InitialOwner?.Email ?? "",
                ParticipantEmails = trip.Participants.Select(p => p.User.Email).ToList(),
                OrganisersEmails = trip.Organisers.Select(p => p.User.Email).ToList(),

            };

            return View(viewModel);
        }

        // POST: Trips/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DestinationCity,DepartureAddress,DepartureDateTime,ReturnDateTime,Capacity,Description,RowVersion")] Trip trip)
        {
            if (id != trip.Id)
            {
                return NotFound();
            }
            Console.WriteLine("RowVersion: " + BitConverter.ToString(trip.RowVersion));
            //ModelState.Remove("trip.RowVersion");


            if (ModelState.IsValid)
            {



                _context.Attach(trip);
                _context.Entry(trip).Property(t => t.DestinationCity).IsModified = true;
                _context.Entry(trip).Property(t => t.DepartureAddress).IsModified = true;
                _context.Entry(trip).Property(t => t.DepartureDateTime).IsModified = true;
                _context.Entry(trip).Property(t => t.ReturnDateTime).IsModified = true;
                _context.Entry(trip).Property(t => t.Capacity).IsModified = true;
                _context.Entry(trip).Property(t => t.Description).IsModified = true;

                _context.Entry(trip).Property(t => t.RowVersion).IsModified = true; // Ensure concurrency check

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));

                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Concurrency conflict: Another user modified this record while you were editing.");
                }
            }
            else
            {
                foreach (var kvp in ModelState)
                {
                    foreach (var error in kvp.Value.Errors)
                    {
                        Console.WriteLine($"Property: {kvp.Key}, Error: {error.ErrorMessage}");
                    }
                }
            }
           
                

            var viewModel = new TripDetailsViewModel
            {
                Trip = trip,
                ParticipantIds = trip.Participants.Select(p => p.UserId).ToList(),
                OrganiserIds = trip.Organisers.Select(p => p.UserId).ToList(),
                ParticipantsCount = trip.Participants?.Count ?? 0,
                OrganisersCount = trip.Organisers?.Count ?? 0,
                OwnerEmail = trip.InitialOwner?.Email ?? "",
                ParticipantEmails = trip.Participants.Select(p => p.User.Email).ToList(),
                OrganisersEmails = trip.Organisers.Select(p => p.User.Email).ToList(),

            };
            return View(viewModel);
        }


        // GET: Trips/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trip = await _context.Trips
                .Include(t => t.InitialOwner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }

        // POST: Trips/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip != null)
            {
                _context.Trips.Remove(trip);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Trips/Join/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinTrip(int tripId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var trip = await _context.Trips.Include(t => t.Participants).FirstOrDefaultAsync(t => t.Id == tripId);
            if (trip == null) return NotFound();

            // Check if user is already a participant
            if (!trip.Participants.Any(p => p.UserId == user.Id))
            {
                trip.Participants.Add(new TripParticipant { TripId = tripId, UserId = user.Id });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Trips/Quit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitTrip(int tripId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var participant = await _context.TripParticipants.FirstOrDefaultAsync(p => p.TripId == tripId && p.UserId == user.Id);
            if (participant != null)
            {
                _context.TripParticipants.Remove(participant);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Trips/AddOrganiser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrganiser(int tripId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // Ensure the current user is the owner or an organiser
            var trip = await _context.Trips
                .Include(t => t.Organisers)
                .FirstOrDefaultAsync(t => t.Id == tripId);

            if (trip == null) return NotFound();

            if (trip.InitialOwnerId != currentUser.Id && !trip.Organisers.Any(o => o.UserId == currentUser.Id))
            {
                return Unauthorized();  // User must be the owner or an organiser to add another organiser
            }

            // Check if the user is already an organiser
            if (!trip.Organisers.Any(o => o.UserId == userId))
            {
                // Add the user as an organiser for the trip
                trip.Organisers.Add(new TripOrganisator { TripId = tripId, UserId = userId });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Edit), new { id = tripId });  // Redirect to the trip details page
        }

        // POST: Trips/RemoveOrganiser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveOrganiser(int tripId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            if (userId == currentUser.Id) return BadRequest();  // User cannot remove themselves as an organiser

            // Ensure the current user is the owner or an organiser
            var trip = await _context.Trips
                .Include(t => t.Organisers)
                .FirstOrDefaultAsync(t => t.Id == tripId);

            if (trip == null) return NotFound();

            if (trip.InitialOwnerId != currentUser.Id && !trip.Organisers.Any(o => o.UserId == currentUser.Id))
            {
                return Unauthorized();  // User must be the owner or an organiser to remove another organiser
            }

            var organiser = await _context.TripOrganisators.FirstOrDefaultAsync(o => o.TripId == tripId && o.UserId == userId);
            if (organiser != null)
            {
                // Remove the user as an organiser for the trip
                _context.TripOrganisators.Remove(organiser);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Edit), new { id = tripId });  // Redirect to the trip details page
        }






        private bool TripExists(int id)
        {
            return _context.Trips.Any(e => e.Id == id);
        }
    }
}
