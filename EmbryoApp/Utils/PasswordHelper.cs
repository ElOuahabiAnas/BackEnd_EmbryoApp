namespace EmbryoApp.Utils;

public static class PasswordHelper
{
    public static string GenerateSecurePassword(int length = 12)
    {
        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@$?_-";
        var res = new char[length];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        for (int i = 0; i < length; i++)
        {
            res[i] = valid[bytes[i] % valid.Length];
        }
        return new string(res);
    }
}