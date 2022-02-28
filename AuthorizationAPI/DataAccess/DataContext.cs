using AuthorizationAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationAPI.DataAccess
{
    public class DataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
        : base(options)
        {
            Database.EnsureCreated();
        }
    }
}