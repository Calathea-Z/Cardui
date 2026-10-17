using Cardui.Api.Domain.Conversations;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class ConversationRulesTests
{
    [Fact]
    public void RejectText_RequiresAValueInsideTheLimit()
    {
        Assert.Equal("Enter a message.", ConversationRules.RejectText("  ", 10, "message"));
        Assert.Equal(
            "A message must be 10 characters or fewer.",
            ConversationRules.RejectText("  12345678901  ", 10, "message"));
        Assert.Null(ConversationRules.RejectText("  hello  ", 10, "message"));
    }

    [Fact]
    public void RejectRole_AllowsThePersonOrTheAssistant()
    {
        Assert.Null(ConversationRules.RejectRole(ConversationMessageRole.User));
        Assert.Null(ConversationRules.RejectRole(ConversationMessageRole.Assistant));
        Assert.Equal(
            "A message is from the person or the assistant.",
            ConversationRules.RejectRole((ConversationMessageRole)9));
    }

    [Fact]
    public void RejectClosed_AllowsAnOpenConversation()
    {
        Assert.Null(ConversationRules.RejectClosed(ConversationStatus.Open));
        Assert.Equal(
            "This conversation is closed.",
            ConversationRules.RejectClosed(ConversationStatus.Closed));
    }

    [Fact]
    public void SameText_IgnoresCaseAndEndSpaces()
    {
        Assert.True(ConversationRules.SameText("  Cash buffer ", "cash buffer"));
        Assert.False(ConversationRules.SameText("Cash buffer", "Cash"));
        Assert.Equal("Cash buffer", ConversationRules.Normalize("  Cash buffer  "));
    }
}
