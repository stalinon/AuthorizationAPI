using AuthorizationAPI.Entities;
using AuthorizationAPI.Models.Dto;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AuthorizationAPI.DataAccess.Repository
{
    public class UserRepository : IUserRepository
    {
        private DataContext _context;
        private IMapper _mapper;

        public UserRepository(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<UserDto> CreateUpdateUser(UserDto userDto)
        {
            var user = _mapper.Map<User>(userDto);
            if (user.Id > 0)
            {
                _context.Users.Update(user);
            }
            else
            {
                _context.Users.Add(user);
            }
            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (user == null)
                    return false;
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserDto> GetUserById(int id)
        {
            var user = await _context.Users.Where(x => x.Id == id).FirstOrDefaultAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return _mapper.Map<List<UserDto>>(users);
        }

        public async Task<UserDto> FirstOrDefault(Expression<Func<UserDto, bool>> filter = null, string includeProperties = null, bool isTracking = true)
        {
            IQueryable<UserDto> query = _mapper.Map<DbSet<UserDto>>(_context.Users);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property);
                }
            }

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync();
        }
    }
}
