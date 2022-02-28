using AuthorizationAPI.Entities;

namespace AuthorizationAPI.DataAccess
{
    public interface IAccountRepository : IRepository<Account>
    {
        void Update(Account account);
    }
}
