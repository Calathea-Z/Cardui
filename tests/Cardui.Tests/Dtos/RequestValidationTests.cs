using System.ComponentModel.DataAnnotations;
using Cardui.Api.Dtos.Category;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Dtos.Transaction;
using Xunit;

namespace Cardui.Tests.Dtos;

public class RequestValidationTests
{
    [Fact]
    public void CreateCategoryDto_RejectsTooLongName()
    {
        var dto = new CreateCategoryDto
        {
            Name = new string('x', 101)
        };

        Assert.False(IsValid(dto));
    }

    [Fact]
    public void TransactionQueryDto_RejectsPageSizeAboveApiLimit()
    {
        var dto = new TransactionQueryDto
        {
            PageSize = 101
        };

        Assert.False(IsValid(dto));
    }

    [Fact]
    public void UpdateTransactionDetailsDto_RejectsNotesAboveMaxLength()
    {
        var dto = new UpdateTransactionDetailsDto
        {
            Date = new DateOnly(2026, 7, 15),
            Notes = new string('x', 1001)
        };

        Assert.False(IsValid(dto));
    }

    [Fact]
    public void ExchangePublicTokenRequestDto_RejectsBlankPublicToken()
    {
        var dto = new ExchangePublicTokenRequestDto
        {
            PublicToken = ""
        };

        Assert.False(IsValid(dto));
    }

    private static bool IsValid(object value)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(value);

        return Validator.TryValidateObject(
            value,
            context,
            validationResults,
            validateAllProperties: true);
    }
}
