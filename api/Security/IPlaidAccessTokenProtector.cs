namespace Cardui.Api.Security;

public interface IPlaidAccessTokenProtector
{
    /// <summary>
    /// Encrypts a Plaid access token for storage and prefixes the protected value.
    /// </summary>
    string Protect(string accessToken);

    /// <summary>
    /// Decrypts a stored access token. Plaintext and unreadable values are rejected.
    /// </summary>
    string Unprotect(string storedAccessToken);
}
