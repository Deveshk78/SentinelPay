using FluentAssertions;
using SentinelPay.Api.Security;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;
namespace SentinelPay.Integration.Tests;

public class PiiMaskingTests
{
    private static LogEvent Run(params (string Name, string Value)[] props)
    {
        var e = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null, new MessageTemplateParser().Parse("test"),
            props.Select(p => new LogEventProperty(p.Name, new ScalarValue(p.Value))));
        new PiiMaskingEnricher().Enrich(e, null!); return e;
    }
    private static string V(LogEvent e, string k) => ((ScalarValue)e.Properties[k]).Value!.ToString()!;

    [Theory]
    [InlineData("Email")] [InlineData("EMAIL")] [InlineData("phone")] [InlineData("Authorization")] [InlineData("password")] [InlineData("CardNumber")] [InlineData("Token")]
    public void Sensitive_property_names_are_redacted_whatever_their_value(string name)
        => V(Run((name, "anything")), name).Should().Be("***");

    [Fact] public void Card_numbers_inside_other_properties_are_masked()
        => V(Run(("Note", "card 4111111111111111 declined")), "Note").Should().Be("card **** declined");
    [Fact] public void Email_addresses_inside_other_properties_are_masked()
        => V(Run(("Note", "contact jane.doe@example.com now")), "Note").Should().Be("contact ***@*** now");
    [Fact] public void Ordinary_values_are_left_alone()
        => V(Run(("MerchantId", "shop-1"), ("TransactionId", "c3f1a2b4-0000-4000-8000-000000000001")), "MerchantId").Should().Be("shop-1");
    [Fact] public void Short_numbers_are_not_mistaken_for_card_numbers()
        => V(Run(("Amount", "123456789012")), "Amount").Should().Be("123456789012");   // 12 digits
    [Fact(Skip = "Known gap: exception text and fully rendered messages are not scrubbed. Tracked in docs/TEST_FINDINGS.md")]
    public void Exception_text_is_scrubbed() { }
}
