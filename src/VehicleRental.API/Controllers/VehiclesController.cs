using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.API.DTOs;
using VehicleRental.API.Services;

namespace VehicleRental.API.Controllers
{
    [ApiController]
    [Route("api/vehicles")]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        // GET /api/vehicles
        // 200 OK: returns all vehicles (public)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleReadDto>>> GetAll()
        {
            var vehicles = await _vehicleService.GetAllAsync();
            return Ok(vehicles);
        }

        // GET /api/vehicles/{id}
        // 200 OK: vehicle found
        // 404 Not Found: no vehicle with given id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<VehicleReadDto>> GetById(int id)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id);

            if (vehicle == null)
                return NotFound(new { error = $"Vehicle with id {id} was not found." });

            return Ok(vehicle);
        }

        // GET /api/vehicles/available?startDate=...&endDate=...
        // 200 OK: list of vehicles available in the given date range (may be empty)
        // 400 Bad Request: invalid date range
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<VehicleReadDto>>> GetAvailable(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var vehicles = await _vehicleService.GetAvailableAsync(startDate, endDate);
                return Ok(vehicles);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET /api/vehicles/search?category=SUV&minRate=50&maxRate=120&fuelType=Diesel&transmission=Automatic
        // 200 OK: filtered list of vehicles (may be empty)
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<VehicleReadDto>>> Search(
            [FromQuery] string? category,
            [FromQuery] decimal? minRate,
            [FromQuery] decimal? maxRate,
            [FromQuery] string? fuelType,
            [FromQuery] string? transmission)
        {
            var vehicles = await _vehicleService.SearchAsync(category, minRate, maxRate, fuelType, transmission);
            return Ok(vehicles);
        }

        // POST /api/vehicles
        // 201 Created: vehicle added successfully [Admin only]
        // 400 Bad Request: validation error
        // 409 Conflict: license plate already registered
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VehicleReadDto>> Create([FromBody] VehicleCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _vehicleService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        // PUT /api/vehicles/{id}
        // 200 OK: vehicle updated [Admin only]
        // 400 Bad Request: validation error
        // 404 Not Found: vehicle does not exist
        // 409 Conflict: license plate already in use by another vehicle
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VehicleReadDto>> Update(int id, [FromBody] VehicleUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _vehicleService.UpdateAsync(id, dto);

                if (updated == null)
                    return NotFound(new { error = $"Vehicle with id {id} was not found." });

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        // DELETE /api/vehicles/{id}
        // 204 No Content: vehicle deleted [Admin only]
        // 404 Not Found: vehicle does not exist
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _vehicleService.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { error = $"Vehicle with id {id} was not found." });

            return NoContent();
        }
    }
}
