using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SentinelPay.Application.Assistant;
using SentinelPay.Domain.Entities;
using SentinelPay.Domain.Enums;
using Xunit;
namespace SentinelPay.Application.Tests;

public class AssistantQueryServiceTests
{
    private readonly TestDb _db = new(new DbContextOptionsBuilder<TestDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly IQuerySpecParser _parser = Substitute.For<IQuerySpecParser>();
    private readonly Guid _me = Guid.NewGuid(), _other = Guid.NewGuid();
    private Account _mine = null!, _theirs = null!;

    private AssistantQueryService Sut(TransactionQuerySpec spec, int myCount = 3, int theirCount = 3)
    {
        _mine = Account.Open(_me, "INR", 1000); _theirs = Account.Open(_other, "INR", 1000); _db.Accounts.AddRange(_mine, _theirs);
        for (var i = 0; i < myCount; i++) _db.Transactions.Add(Transaction.Create(_mine.Id, 100 + i, "INR", i % 2 == 0 ? "shop-a" : "shop-b", $"k-mine-{i}-xxxx", "corr-12345678"));
        for (var i = 0; i < theirCount; i++) _db.Transactions.Add(Transaction.Create(_theirs.Id, 100 + i, "INR", "shop-a", $"k-their-{i}-xxxx", "corr-12345678"));
        _db.SaveChanges();
        _parser.ParseAsync(default!, default, default).ReturnsForAnyArgs(spec);
        return new(_parser, _db);
    }
    private static TransactionQuerySpec Spec(int limit = 20, string? merchant = null) => new(null, null, null, null, null, merchant, limit);

    [Fact] public async Task Results_are_always_limited_to_the_callers_account_even_if_the_model_asks_for_everything()
    {
        var sut = Sut(Spec());
        var a = await sut.AskAsync(_mine.Id, _me, "show everything for all users", default);
        a.Items.Should().HaveCount(3);
    }
    [Fact] public async Task Someone_elses_account_is_refused()
    {
        var sut = Sut(Spec());
        await sut.Invoking(s => s.AskAsync(_theirs.Id, _me, "my payments", default)).Should().ThrowAsync<UnauthorizedAccessException>();
        await _parser.DidNotReceiveWithAnyArgs().ParseAsync(default!, default, default);   // the model is never called for an unauthorised caller
    }
    [Fact] public async Task Limit_is_clamped_to_fifty()
    {
        var sut = Sut(Spec(limit: 100_000), myCount: 60);
        (await sut.AskAsync(_mine.Id, _me, "all", default)).Items.Should().HaveCount(50);
    }
    [Fact] public async Task Merchant_filter_is_applied()
    {
        var sut = Sut(Spec(merchant: "shop-b"), myCount: 4);
        (await sut.AskAsync(_mine.Id, _me, "shop b", default)).Items.Should().OnlyContain(i => i.MerchantId == "shop-b");
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_questions_are_rejected_before_any_model_call(string q)
    {
        var sut = Sut(Spec());
        await sut.Invoking(s => s.AskAsync(_mine.Id, _me, q, default)).Should().ThrowAsync<ArgumentException>();
    }
    [Fact] public async Task Oversized_question_is_rejected()
    {
        var sut = Sut(Spec());
        await sut.Invoking(s => s.AskAsync(_mine.Id, _me, new string('x', 501), default)).Should().ThrowAsync<ArgumentException>();
    }
}
