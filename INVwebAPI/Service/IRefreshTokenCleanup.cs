namespace INVwebAPI.Service;

public interface IRefreshTokenCleanup
{
    int CleanupExpired();
}
