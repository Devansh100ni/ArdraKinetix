using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Authentication;

public class PasswordHasherService : IPasswordHasherService
{
    private const int SaltSize = 128 / 8; // 16 bytes
    private const int KeySize = 256 / 8;  // 32 bytes
    private const int Iterations = 100000;

    public string HashPassword(User user, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA512,
            iterationCount: Iterations,
            numBytesRequested: KeySize);

        byte[] outputBytes = new byte[1 + 4 + SaltSize + KeySize];
        outputBytes[0] = 0x01; // Format marker
        WriteNetworkByteOrder(outputBytes, 1, (uint)Iterations);
        Buffer.BlockCopy(salt, 0, outputBytes, 5, SaltSize);
        Buffer.BlockCopy(subkey, 0, outputBytes, 5 + SaltSize, KeySize);

        return Convert.ToBase64String(outputBytes);
    }

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        try
        {
            byte[] decoded = Convert.FromBase64String(hashedPassword);
            if (decoded.Length != 1 + 4 + SaltSize + KeySize || decoded[0] != 0x01)
            {
                return false;
            }

            uint iterations = ReadNetworkByteOrder(decoded, 1);
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(decoded, 5, salt, 0, SaltSize);

            byte[] expectedSubkey = new byte[KeySize];
            Buffer.BlockCopy(decoded, 5 + SaltSize, expectedSubkey, 0, KeySize);

            byte[] actualSubkey = KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: (int)iterations,
                numBytesRequested: KeySize);

            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
        catch
        {
            return false;
        }
    }

    private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
    {
        buffer[offset + 0] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)(value >> 0);
    }

    private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
    {
        return ((uint)(buffer[offset + 0]) << 24)
            | ((uint)(buffer[offset + 1]) << 16)
            | ((uint)(buffer[offset + 2]) << 8)
            | ((uint)(buffer[offset + 3]));
    }
}
