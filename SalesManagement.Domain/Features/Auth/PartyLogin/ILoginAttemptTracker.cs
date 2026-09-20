namespace SalesManagement.Domain.Features.Auth.PartyLogin
{
    public interface ILoginAttemptTracker
    {
        TimeSpan? GetLockRemaining(string key);

        void RegisterFailure(string key);

        void Reset(string key);
    }
}
