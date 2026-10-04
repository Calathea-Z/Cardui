using System.Text.Json;
using Cardui.Api.Configuration;
using Cardui.Api.Domain;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class EnumJsonTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    [Fact]
    public void SerializesClosedValuesAsMemberNames()
    {
        Assert.Equal("\"Plaid\"", JsonSerializer.Serialize(FinancialRecordSource.Plaid, Options));
        Assert.Equal(
            "\"BalanceReconciliation\"",
            JsonSerializer.Serialize(FinancialRecordProvenance.BalanceReconciliation, Options));
        Assert.Equal("\"PositiveIn\"", JsonSerializer.Serialize(CsvAmountSign.PositiveIn, Options));
        Assert.Equal("\"DayFirst\"", JsonSerializer.Serialize(CsvDateOrder.DayFirst, Options));
        Assert.Equal("\"Duplicate\"", JsonSerializer.Serialize(TransactionImportRowStatus.Duplicate, Options));
    }

    [Fact]
    public void DeserializesMemberNames()
    {
        Assert.Equal(
            FinancialRecordSource.Csv,
            JsonSerializer.Deserialize<FinancialRecordSource>("\"Csv\"", Options));
        Assert.Equal(
            CsvDateOrder.MonthFirst,
            JsonSerializer.Deserialize<CsvDateOrder>("\"MonthFirst\"", Options));
    }

    [Fact]
    public void RejectsANumericValue()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<FinancialRecordSource>("0", Options));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        CarduiJsonSerializer.UseEnumMemberNames(options);
        return options;
    }
}
