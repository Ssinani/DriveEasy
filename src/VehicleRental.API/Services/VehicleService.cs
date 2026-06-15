using AutoMapper;
using VehicleRental.API.DTOs;
using VehicleRental.API.Models;
using VehicleRental.API.Repositories;

namespace VehicleRental.API.Services
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleReadDto>> GetAllAsync();
        Task<VehicleReadDto?> GetByIdAsync(int id);
        Task<IEnumerable<VehicleReadDto>> GetAvailableAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<VehicleReadDto>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType, string? transmission);
        Task<VehicleReadDto> CreateAsync(VehicleCreateDto dto);
        Task<VehicleReadDto?> UpdateAsync(int id, VehicleUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IMapper _mapper;

        public VehicleService(IVehicleRepository vehicleRepository, IMapper mapper)
        {
            _vehicleRepository = vehicleRepository;
            _mapper = mapper;
        }

        // Returns all vehicles mapped to read DTOs
        public async Task<IEnumerable<VehicleReadDto>> GetAllAsync()
        {
            var vehicles = await _vehicleRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<VehicleReadDto>>(vehicles);
        }

        // Returns a single vehicle by Id; null if not found
        public async Task<VehicleReadDto?> GetByIdAsync(int id)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);
            return vehicle == null ? null : _mapper.Map<VehicleReadDto>(vehicle);
        }

        // Returns vehicles not booked for the given date range
        // Business rule: startDate must be before endDate
        public async Task<IEnumerable<VehicleReadDto>> GetAvailableAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                throw new ArgumentException("End date must be after start date.");

            var vehicles = await _vehicleRepository.GetAvailableAsync(startDate, endDate);
            return _mapper.Map<IEnumerable<VehicleReadDto>>(vehicles);
        }

        // Applies optional filters and returns matching vehicles
        public async Task<IEnumerable<VehicleReadDto>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType, string? transmission)
        {
            var vehicles = await _vehicleRepository.SearchAsync(category, minRate, maxRate, fuelType, transmission);
            return _mapper.Map<IEnumerable<VehicleReadDto>>(vehicles);
        }

        // Validates license plate uniqueness, maps DTO to entity, persists and returns created vehicle
        public async Task<VehicleReadDto> CreateAsync(VehicleCreateDto dto)
        {
            if (await _vehicleRepository.LicensePlateExistsAsync(dto.LicensePlate))
                throw new InvalidOperationException($"License plate '{dto.LicensePlate.ToUpper()}' is already registered.");

            var vehicle = _mapper.Map<Vehicle>(dto);
            var created = await _vehicleRepository.CreateAsync(vehicle);
            return _mapper.Map<VehicleReadDto>(created);
        }

        // Validates existence and license plate uniqueness, applies update, returns updated vehicle
        public async Task<VehicleReadDto?> UpdateAsync(int id, VehicleUpdateDto dto)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(id);
            if (vehicle == null) return null;

            if (await _vehicleRepository.LicensePlateExistsAsync(dto.LicensePlate, excludeId: id))
                throw new InvalidOperationException($"License plate '{dto.LicensePlate.ToUpper()}' is already registered.");

            _mapper.Map(dto, vehicle);
            var updated = await _vehicleRepository.UpdateAsync(vehicle);
            return _mapper.Map<VehicleReadDto>(updated);
        }

        // Deletes a vehicle; returns false if not found
        public async Task<bool> DeleteAsync(int id)
        {
            if (!await _vehicleRepository.ExistsAsync(id))
                return false;

            await _vehicleRepository.DeleteAsync(id);
            return true;
        }
    }
}
