using Microsoft.EntityFrameworkCore;
using VehicleRental.Core.Entities;
using VehicleRental.Core.Interfaces;
using VehicleRental.Infrastructure.Data;

namespace VehicleRental.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<bool> EmailExistsAsync(string email) =>
        await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<IEnumerable<User>> GetByRoleAsync(string role) =>
        await _dbSet.Where(u => u.Role == role).ToListAsync();
}
