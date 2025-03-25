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
        public async Task<IActionResult> Index()
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var trips = await _context.Trips
                .Include(t => t.InitialOwner)
                .Include(t => t.Participants) 
                .Include(t => t.Organisers)
                .ToListAsync();

            var tripList = trips.Select(trip => new
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


            return View(tripList);
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
                return Unauthorized(); // Ensure the user is logged in
            }
            trip.InitialOwnerId = user.Id;

            if (ModelState.IsValid)
            {
                _context.Add(trip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                // Log or inspect the error messages
                Console.WriteLine(error.ErrorMessage);
            }
            //ViewData["InitialOwnerId"] = new SelectList(_context.Users, "Id", "Id", trip.InitialOwnerId);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,DestinationCity,DepartureAddress,DepartureDateTime,ReturnDateTime,Capacity,Description")] Trip trip)
        {
            if (id != trip.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTrip = await _context.Trips.FindAsync(id);
                    if (existingTrip == null)
                    {
                        return NotFound();
                    }

                    // Update only the necessary fields
                    existingTrip.DestinationCity = trip.DestinationCity;
                    existingTrip.DepartureAddress = trip.DepartureAddress;
                    existingTrip.DepartureDateTime = trip.DepartureDateTime;
                    existingTrip.ReturnDateTime = trip.ReturnDateTime;
                    existingTrip.Capacity = trip.Capacity;
                    existingTrip.Description = trip.Description;

                    _context.Update(existingTrip);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TripExists(trip.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(trip);
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
