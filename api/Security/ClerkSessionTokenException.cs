namespace Cardui.Api.Security;

public sealed class ClerkSessionTokenException : Exception
{
    public ClerkSessionTokenException(string message)
        : base(message)
    {
    }
}
