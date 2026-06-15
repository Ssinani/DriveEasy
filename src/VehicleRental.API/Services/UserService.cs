using AutoMapper;
using VehicleRental.API.DTOs;
using VehicleRental.API.Repositories;

namespace VehicleRental.API.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserReadDto>> GetAllAsync();
        Task<UserReadDto?> GetByIdAsync(int id);
        Task<IEnumerable<UserReadDto>> GetByRoleAsync(string role);
        Task<UserReadDto?> UpdateAsync(int id, UserUpdateDto dto);
        Task<bool> DeactivateAsync(int id);
        Task<bool> ChangeRoleAsync(int id, string role);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        // Returns all users; admin-only endpoint
        public async Task<IEnumerable<UserReadDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserReadDto>>(users);
        }

        // Returns a single user by Id; null if not found
        public async Task<UserReadDto?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : _mapper.Map<UserReadDto>(user);
        }

        // Returns all users with the specified role (Admin or Customer)
        public async Task<IEnumerable<UserReadDto>> GetByRoleAsync(string role)
        {
            var users = await _userRepository.GetByRoleAsync(role);
            return _mapper.Map<IEnumerable<UserReadDto>>(users);
        }

        // Updates profile fields (name, phone, address, license); returns null if user not found
        public async Task<UserReadDto?> UpdateAsync(int id, UserUpdateDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.Address = dto.Address;
            user.DriverLicenseNumber = dto.DriverLicenseNumber;

            var updated = await _userRepository.UpdateAsync(user);
            return _mapper.Map<UserReadDto>(updated);
        }

        // Soft-deletes (deactivates) a user; returns false if not found
        public async Task<bool> DeactivateAsync(int id)
        {
            return await _userRepository.DeactivateAsync(id);
        }

        // Validates the target role and updates the user's role; admin-only
        public async Task<bool> ChangeRoleAsync(int id, string role)
        {
            if (role != "Admin" && role != "Customer")
                throw new ArgumentException($"Invalid role '{role}'. Allowed values: Admin, Customer.");

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.Role = role;
            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}
