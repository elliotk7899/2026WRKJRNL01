using Microsoft.AspNetCore.DataProtection;

namespace WRKJRNL01.Data.Services;

public class TokenEncryptionService
{
    private readonly IDataProtector protector;

    public TokenEncryptionService(
        IDataProtectionProvider provider)
    {
        protector =
            provider.CreateProtector(
                "GoogleRefreshTokens");
    }

    public string Encrypt(string token)
    {
        return protector.Protect(token);
    }

    public string Decrypt(string encryptedToken)
    {
        return protector.Unprotect(encryptedToken);
    }
}