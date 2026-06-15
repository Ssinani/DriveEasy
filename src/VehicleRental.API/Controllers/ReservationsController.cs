using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.API.DTOs;
using VehicleRental.API.Services;

namespace VehicleRental.API.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool IsAdmin =>
            User.IsInRole("Admin");

        // GET /api/reservations
        // 200 OK: all reservations returned [Admin only]
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ReservationReadDto>>> GetAll()
        {
            var reservations = await _reservationService.GetAllAsync();
            return Ok(reservations);
        }

        // GET /api/reservations/{id}
        // 200 OK: reservation returned
        // 403 Forbidden: customer trying to view another user's reservation
        // 404 Not Found: reservation does not exist
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReservationReadDto>> GetById(int id)
        {
            var reservation = await _reservationService.GetByIdAsync(id);

            if (reservation == null)
                return NotFound(new { error = $"Reservation with id {id} was not found." });

            if (!IsAdmin && reservation.UserId != CurrentUserId)
                return Forbid();

            return Ok(reservation);
        }

        // GET /api/reservations/my
        // 200 OK: current user's reservations returned
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<ReservationReadDto>>> GetMyReservations()
        {
            var reservations = await _reservationService.GetByUserIdAsync(CurrentUserId);
            return Ok(reservations);
        }

        // GET /api/reservations/status/{status}
        // 200 OK: reservations filtered by status [Admin only]
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ReservationReadDto>>> GetByStatus(string status)
        {
            var reservations = await _reservationService.GetByStatusAsync(status);
            return Ok(reservations);
        }

        // GET /api/reservations/estimate?vehicleId=1&startDate=...&endDate=...
        // 200 OK: cost breakdown with tiered discounts and tax
        // 400 Bad Request: invalid date range or vehicle not found
        [HttpGet("estimate")]
        public async Task<ActionResult<CostEstimateReadDto>> EstimateCost(
            [FromQuery] int vehicleId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var estimate = await _reservationService.EstimateCostAsync(vehicleId, startDate, endDate);
                return Ok(estimate);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST /api/reservations
        // 201 Created: reservation created successfully
        // 400 Bad Request: validation error or business rule violated
        // 404 Not Found: vehicle does not exist
        [HttpPost]
        public async Task<ActionResult<ReservationReadDto>> Create([FromBody] ReservationCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _reservationService.CreateAsync(CurrentUserId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        // PUT /api/reservations/{id}
        // 200 OK: reservation updated
        // 400 Bad Request: invalid dates or reservation not in Pending status
        // 403 Forbidden: customer modifying another user's reservation
        // 404 Not Found: reservation does not exist
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReservationReadDto>> Update(int id, [FromBody] ReservationUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _reservationService.UpdateAsync(id, CurrentUserId, dto, IsAdmin);

                if (updated == null)
                    return NotFound(new { error = $"Reservation with id {id} was not found." });

                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        // PATCH /api/reservations/{id}/cancel
        // 204 No Content: reservation cancelled
        // 400 Bad Request: cannot cancel completed/already cancelled reservation
        // 403 Forbidden: customer cancelling another user's reservation
        // 404 Not Found: reservation does not exist
        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _reservationService.CancelAsync(id, CurrentUserId, dto.Reason, IsAdmin);

                if (!result)
                    return NotFound(new { error = $"Reservation with id {id} was not found." });

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PATCH /api/reservations/{id}/confirm
        // 204 No Content: reservation confirmed [Admin only]
        // 400 Bad Request: reservation is not in Pending status
        [HttpPatch("{id:int}/confirm")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Confirm(int id)
        {
            var result = await _reservationService.ConfirmAsync(id);

            if (!result)
                return BadRequest(new { error = "Reservation could not be confirmed. It may not exist or is not in Pending status." });

            return NoContent();
        }

        // PATCH /api/reservations/{id}/complete
        // 204 No Content: reservation marked as completed [Admin only]
        // 400 Bad Request: reservation is not in Active or Confirmed status
        [HttpPatch("{id:int}/complete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Complete(int id)
        {
            var result = await _reservationService.CompleteAsync(id);

            if (!result)
                return BadRequest(new { error = "Reservation could not be completed. It may not exist or is not in Active/Confirmed status." });

            return NoContent();
        }
    }
}
