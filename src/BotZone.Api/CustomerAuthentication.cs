using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Data.Entities;
using BotZone.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BotZone.Api;

// Extends the existing Identity/session endpoints; there is no separate customer auth system.
public static class CustomerAuthentication
{
    public static async Task<IResult> Login(JsonElement input, HttpContext context, UserManager<User> users, BotZoneDbContext db)
    {
        var email = input.Text("email").Trim();
        var client = input.Text("clientType", "web");
        Access.Ensure(client is "web" or "winforms", "VALIDATION_ERROR", "Client không hợp lệ.", 400);
        if (input.TryGetProperty("rememberMe", out var remember))
            Access.Ensure(remember.ValueKind is JsonValueKind.True or JsonValueKind.False,
                "VALIDATION_ERROR", "Lựa chọn ghi nhớ đăng nhập không hợp lệ.", 400);
        var user = await users.FindByEmailAsync(email);
        if (user is null || await users.IsLockedOutAsync(user) || !await users.CheckPasswordAsync(user, input.Text("password")))
        {
            if (user is not null) await users.AccessFailedAsync(user);
            throw new BusinessException("LOGIN_FAILED", "Email hoặc mật khẩu không đúng, hoặc tài khoản đang khóa.", 401);
        }
        if (user.PasswordResetRequired)
            throw new BusinessException("PASSWORD_RESET_REQUIRED", "Tài khoản cần đặt lại mật khẩu qua email.", 403);
        var staff = await db.StaffProfiles.AsNoTracking().FirstOrDefaultAsync(profile => profile.UserId == user.Id, context.RequestAborted);
        Access.Ensure(staff is not { Active: false }, "FORBIDDEN", "Tài khoản nhân viên đã bị khóa.", 403);
        if (client == "winforms") Access.Ensure(staff is not null, "FORBIDDEN", "Ứng dụng quản trị dành cho nhân viên.", 403);
        await users.ResetAccessFailedCountAsync(user);
        var session = new AuthSession { UserId = user.Id, ClientType = client, ExpiresAt = DateTime.UtcNow.AddHours(8) };
        db.AuthSessions.Add(session);
        await db.SaveChangesAsync(context.RequestAborted);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.Name),
            new Claim("sid", session.Id.ToString()), new Claim("stamp", user.SecurityStamp ?? "")
        };
        await context.SignInAsync(AuthEndpoints.Scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, AuthEndpoints.Scheme)),
            new AuthenticationProperties {
                ExpiresUtc = new DateTimeOffset(session.ExpiresAt),
                IsPersistent = client == "web" && input.Bool("rememberMe"),
                AllowRefresh = false
            });
        return Results.Ok(new { actor = new Actor(user.Id, user.Name, user.Email!, staff?.Role ?? "customer", user.EmailConfirmed) });
    }

    public static async Task<object> Register(JsonElement input, HttpContext context, UserManager<User> users,
        SqlStore sql, IConfiguration configuration)
    {
        Access.Ensure(!input.TryGetProperty("role", out _), "VALIDATION_ERROR", "Đăng ký không nhận vai trò.", 400);
        var name = input.Text("name").Trim();
        var email = input.Text("email").Trim();
        var phone = input.Text("phone", "").Trim();
        Access.Ensure(name.Length is >= 2 and <= 100, "VALIDATION_ERROR", "Tên phải có 2–100 ký tự.", 400);
        Access.Ensure(email.Length is > 3 and <= 254 && MailAddress.TryCreate(email, out var parsed)
            && string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase), "VALIDATION_ERROR", "Email không hợp lệ.", 400);
        // Optional for existing API clients; the new customer registration form requires it.
        if (phone.Length > 0) ValidatePhone(phone);
        JsonElement? shipping = null;
        if (input.TryGetProperty("address", out var address))
        {
            Access.Ensure(address.ValueKind == JsonValueKind.Object, "VALIDATION_ERROR", "Địa chỉ chưa đầy đủ.", 400);
            foreach (var field in new[] { "recipient", "province", "ward", "addressLine" })
                Access.Ensure(address.Text(field).Trim().Length is >= 2 and <= 300, "VALIDATION_ERROR", "Địa chỉ chưa đầy đủ.", 400);
            ValidatePhone(address.Text("phone").Trim());
            Access.Ensure(address.Text("country") == "VN", "VALIDATION_ERROR", "Chỉ giao tại Việt Nam.", 400);
            shipping = address;
        }
        var user = new User {
            Id = Guid.NewGuid().ToString(), Name = name, UserName = email, Email = email,
            PhoneNumber = phone.Length == 0 ? null : phone, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, LockoutEnabled = true
        };
        return await sql.Transaction<object>(null, async () =>
        {
            AuthEndpoints.Check(await users.CreateAsync(user, input.Text("password")), ApiLocalization.Locale(context));
            await sql.Exec("""
                INSERT INTO customer_profiles(user_id,display_name,phone) VALUES($1,$2,$3);
                INSERT INTO store_credit_accounts(user_id) VALUES($1);
                INSERT INTO loyalty_accounts(user_id) VALUES($1)
                """, user.Id, name, phone.Length == 0 ? null : phone);
            if (shipping is { } saved)
                await sql.Exec("""
                    INSERT INTO addresses(user_id,recipient,phone,province,ward,address_line,is_default)
                    VALUES($1,$2,$3,$4,$5,$6,true)
                    """, user.Id, saved.Text("recipient").Trim(), saved.Text("phone").Trim(),
                    saved.Text("province").Trim(), saved.Text("ward").Trim(), saved.Text("addressLine").Trim());
            await AuthEndpoints.Mail(sql, user, "verify-email", await users.GenerateEmailConfirmationTokenAsync(user),
                configuration, ApiLocalization.Locale(context));
            return new { registered = true, requiresEmailVerification = true };
        });
    }
    private static void ValidatePhone(string phone)
    {
        var digits = Regex.Replace(phone, @"[\s()+.\-]", "");
        Access.Ensure(phone.Length <= 30 && Regex.IsMatch(digits, "^[0-9]{9,15}$"),
            "VALIDATION_ERROR", "Số điện thoại phải có 9–15 chữ số.", 400);
    }
}
