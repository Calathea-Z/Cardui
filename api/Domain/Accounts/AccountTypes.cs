namespace Cardui.Api.Domain.Accounts;

public static class AccountTypes
{
    public const string Depository = "depository";
    public const string Investment = "investment";
    public const string Credit = "credit";
    public const string Loan = "loan";

    /// <summary>
    /// True for a depository account, which Cardui treats as cash.
    /// </summary>
    public static bool IsCash(string type) =>
        type.Equals(Depository, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for an investment account.
    /// </summary>
    public static bool IsInvestment(string type) =>
        type.Equals(Investment, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for a credit card. The stored type is "credit".
    /// </summary>
    public static bool IsCreditCard(string type) =>
        type.Equals(Credit, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for a loan.
    /// </summary>
    public static bool IsLoan(string type) =>
        type.Equals(Loan, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for cash or investments, the accounts that can be transfer legs.
    /// </summary>
    public static bool IsOwnedAsset(string type) =>
        IsCash(type) || IsInvestment(type);
}
