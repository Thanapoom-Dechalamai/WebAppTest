using System.Security.Cryptography;
namespace Nontiwat_7362_L002.Services;

// Temporary passwords that Admin hands to a user after a reset. Characters that are easy to
// misread (0/O, 1/l/I) are left out because the password is read off the screen and typed.
public static class PasswordRules
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
    public const int TemporaryLength = 10;

    public static string GenerateTemporaryPassword() =>
        string.Create(TemporaryLength, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++) span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        });
}
