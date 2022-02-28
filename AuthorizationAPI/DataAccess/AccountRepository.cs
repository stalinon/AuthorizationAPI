using AuthorizationAPI.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AuthorizationAPI.DataAccess
{
    public class AccountRepository : Repository<Account>, IAccountRepository
    {
        private readonly DataContext _db;

        public AccountRepository(DataContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Account account)
        {
            _db.Accounts.Update(account);
        }
    }
}
