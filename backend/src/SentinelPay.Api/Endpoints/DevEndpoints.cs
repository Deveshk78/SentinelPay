using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SentinelPay.Domain.Entities;
using SentinelPay.Infrastructure.Persistence;
namespace SentinelPay.Api.Endpoints;

/// <summary>DEVELOPMENT ONLY (mapped only when ASPNETCORE_ENVIRONMENT=Development). Replaces a real login so you can try the API.
/// Never deploy with the Development environment: these endpoints mint tokens without any authentication.</summary>
public static class DevEndpoints
{
    public static void MapDevEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/dev").AllowAnonymous();
        g.MapPost("/seed", async (AppDbContext db, decimal? balance) =>
        {
            var owner = Guid.NewGuid();
            var account = Account.Open(owner, "INR", balance ?? 100_000m);
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                ownerId = owner, accountId = account.Id,
                token = Mint(app.Configuration, owner, ops: false),
                opsToken = Mint(app.Configuration, owner, ops: true),   // same user plus the "ops" role: needed for the log viewer
            });
        });
    }

    private static string Mint(IConfiguration cfg, Guid owner, bool ops)
    {
        var claims = new List<Claim> { new("sub", owner.ToString()) };
        if (ops) claims.Add(new Claim(ClaimTypes.Role, "ops"));
        var jwt = new JwtSecurityToken(cfg["Jwt:Issuer"], cfg["Jwt:Audience"], claims, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(8),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:SigningKey"]!)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
