using System.Security.Cryptography;
using System.Text;

namespace FilmAPI.Services;

/// <summary>
/// TOTP (Time-based One-Time Password) — RFC 6238
/// Implementazione manuale senza dipendenze esterne.
/// </summary>
public static class TotpUtility
{
    private const int SecretLength = 20; // 160 bit
    private const int StepSeconds = 30;
    private const int CodeDigits = 6;
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Genera un secret casuale per il setup 2FA.</summary>
    public static byte[] GenerateSecret()
    {
        return RandomNumberGenerator.GetBytes(SecretLength);
    }

    /// <summary>Converte il secret in Base32 per il salvataggio / QR code.</summary>
    public static string ToBase32(byte[] secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (var b in secret)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                result.Append(alphabet[(buffer >> bitsLeft) & 0x1F]);
            }
        }

        if (bitsLeft > 0)
            result.Append(alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);

        return result.ToString();
    }

    /// <summary>Decodifica Base32 → byte[].</summary>
    public static byte[] FromBase32(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.TrimEnd('=').ToUpperInvariant();

        var result = new List<byte>();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (var c in base32)
        {
            var index = alphabet.IndexOf(c);
            if (index < 0) continue;

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                result.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return result.ToArray();
    }

    /// <summary>Genera il codice TOTP a 6 cifre corrente.</summary>
    public static string GenerateCode(byte[] secret, DateTime? timestamp = null)
    {
        var time = timestamp?.ToUniversalTime() ?? DateTime.UtcNow;
        var counter = (long)(time - UnixEpoch).TotalSeconds / StepSeconds;

        return ComputeHotp(secret, counter);
    }

    /// <summary>Verifica un codice TOTP (accetta finestra ±1 step).</summary>
    public static bool VerifyCode(byte[] secret, string code, DateTime? timestamp = null)
    {
        var time = timestamp?.ToUniversalTime() ?? DateTime.UtcNow;
        var counter = (long)(time - UnixEpoch).TotalSeconds / StepSeconds;

        // Prova counter corrente, precedente e successivo
        for (var offset = -1; offset <= 1; offset++)
        {
            if (ComputeHotp(secret, counter + offset) == code)
                return true;
        }

        return false;
    }

    /// <summary>Genera l'URL otpauth per il QR code.</summary>
    public static string GetQrCodeUri(string email, byte[] secret, string issuer = "CineBase")
    {
        var base32 = ToBase32(secret);
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedLabel = Uri.EscapeDataString($"{issuer}:{email}");
        return $"otpauth://totp/{encodedLabel}?secret={base32}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    private static string ComputeHotp(byte[] secret, long counter)
    {
        var counterBytes = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString($"D{CodeDigits}");
    }
}
