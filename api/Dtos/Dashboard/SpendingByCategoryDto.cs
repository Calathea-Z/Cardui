namespace Cardui.Api.Dtos.Dashboard;

public class SpendingByCategoryDto
{
    public Guid? CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public string? Color { get; set; }
    public decimal Amount { get; set; }
}