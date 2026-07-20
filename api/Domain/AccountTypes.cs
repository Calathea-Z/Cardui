namespace Cardui.Api.Domain;

public static class AccountTypes
{
    public const string Depository = "depository";
    public const string Investment = "investment";
    public const string Credit = "credit";
    public const string Loan = "loan";

    public static bool IsCash(string type) =>
        type.Equals(Depository, StringComparison.OrdinalIgnoreCase);

    public static bool IsInvestment(string type) =>
        type.Equals(Investment, StringComparison.OrdinalIgnoreCase);

    public static bool IsCreditCard(string type) =>
        type.Equals(Credit, StringComparison.OrdinalIgnoreCase);

    public static bool IsLoan(string type) =>
        type.Equals(Loan, StringComparison.OrdinalIgnoreCase);

    public static bool IsOwnedAsset(string type) =>
        IsCash(type) || IsInvestment(type);
}
