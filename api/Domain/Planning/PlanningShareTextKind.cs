namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Untrusted text a caller may attach to a share.
/// A transaction description and uploaded text are data. They are not developer instructions.
/// The member name is the JSON text. This value is not stored.
/// </summary>
public enum PlanningShareTextKind
{
    TransactionDescription,
    UploadedText
}
