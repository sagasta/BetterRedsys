namespace BetterRedsys.Testing;

/// <summary>
/// Official test card numbers, security codes, and credentials provided by Redsys for integration testing.
/// </summary>
public static class RedsysTestCards
{
    /// <summary>
    /// Test card resulting in an authorized payment (Response code 0000).
    /// </summary>
    public const string Authorized = "4548810000000003";

    /// <summary>
    /// Test card resulting in insufficient funds (Response code 0116).
    /// </summary>
    public const string InsufficientFunds = "4548810000000116";

    /// <summary>
    /// Test card resulting in an expired or rejected card (Response code 0180).
    /// </summary>
    public const string Expired = "4548810000000180";

    /// <summary>
    /// Test card resulting in a 3D Secure / PSD2 SCA authentication failure (Response code 0184).
    /// </summary>
    public const string Authentication3DsError = "4548810000000184";

    /// <summary>
    /// Test card resulting in general issuer declination (Response code 0190).
    /// </summary>
    public const string IssuerDeclined = "4548810000000190";

    /// <summary>
    /// Standard SMS OTP code used by the Redsys test environment to verify 3DS authentication.
    /// </summary>
    public const string SmsCode = "1234";

    /// <summary>
    /// Default 3-digit CVV accepted by Redsys test cards.
    /// </summary>
    public const string DefaultCvv = "123";

    /// <summary>
    /// Default valid future expiration date (MM/YY) for test cards.
    /// </summary>
    public const string DefaultExpiry = "12/28";
}

/// <summary>
/// Public test merchant credentials provided by Redsys for the official test sandbox environment
/// (https://sis-t.redsys.es:25443). Shared by every integrator: never use them in production.
/// </summary>
public static class RedsysTestCredentials
{
    /// <summary>
    /// Official public test merchant code (FUC: 999008881).
    /// </summary>
    public const string Fuc = "999008881";

    /// <summary>
    /// Official public test terminal number (1).
    /// </summary>
    public const string Terminal = "1";

    /// <summary>
    /// Official public test HMAC-SHA256 secret encryption key.
    /// </summary>
    public const string SecretKey = "sq7HjrUOBfKmC576ILgskD5srU870gJ7";
}
