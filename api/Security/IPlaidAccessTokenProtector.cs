namespace Cardui.Api.Security;

public interface IPlaidAccessTokenProtector
{
    string Protect(string accessToken);
    string Unprotect(string storedAccessToken);
}
