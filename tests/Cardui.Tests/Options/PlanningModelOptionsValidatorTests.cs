using Cardui.Api.Options;
using Xunit;

namespace Cardui.Tests.Options;

public class PlanningModelOptionsValidatorTests
{
    [Fact]
    public void Validate_SucceedsWhenPlanningModelSettingsAreBlank()
    {
        var validator = new PlanningModelOptionsValidator();

        var result = validator.Validate(null, new PlanningModelOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_SucceedsWhenTheKeyAndModelAreBothSet()
    {
        var validator = new PlanningModelOptionsValidator();

        var result = validator.Validate(
            null,
            new PlanningModelOptions
            {
                ApiKey = "sk-test",
                Model = "configured-model"
            });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_FailsWhenOnlyTheKeyIsSet()
    {
        var validator = new PlanningModelOptionsValidator();

        var result = validator.Validate(
            null,
            new PlanningModelOptions
            {
                ApiKey = "sk-test"
            });

        Assert.True(result.Failed);
        Assert.Contains("PlanningModel:Model is required.", result.Failures);
    }

    [Fact]
    public void Validate_FailsWhenOnlyTheModelIsSet()
    {
        var validator = new PlanningModelOptionsValidator();

        var result = validator.Validate(
            null,
            new PlanningModelOptions
            {
                Model = "configured-model"
            });

        Assert.True(result.Failed);
        Assert.Contains("PlanningModel:ApiKey is required.", result.Failures);
    }
}
