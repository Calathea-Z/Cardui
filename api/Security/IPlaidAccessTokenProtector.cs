namespace Cardui.Api.Security;

public interface IPlaidAccessTokenProtector
{
    /// <summary>
    /// Encrypts a Plaid access token for storage and prefixes the protected value.
    /// </summary>
    string Protect(string accessToken);

    /// <summary>
    /// Decrypts a stored access token. A value without the protection prefix
    /// is returned unchanged so older plaintext tokens still work.
    /// </summary>
    string Unprotect(string storedAccessToken);
}
