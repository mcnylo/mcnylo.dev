using mcnylo.dev.Data.Context;
using mcnylo.dev.Data.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace mcnylo.dev.Admin.Services
{
    public class AdminMfaService : IAdminMfaService
    {
        private const string Issuer = "mcnylo.dev";
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private const int TotpDigits = 6;
        private const int TotpPeriodSeconds = 30;
        private const int RecoveryCodeCount = 10;
        private const int RecoveryCodeHashIterations = 100_000;

        private readonly McNyloDbContext _dbContext;
        private readonly IDataProtector _secretProtector;

        // ========================================================================================

        public AdminMfaService(McNyloDbContext dbContext, IDataProtectionProvider dataProtectionProvider)
        {
            _dbContext = dbContext;
            _secretProtector = dataProtectionProvider.CreateProtector("mcnylo.dev.Admin.Mfa.SecretKey.v1");
        }

        // ========================================================================================

        public async Task<bool> IsMfaEnabledAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            return await _dbContext.AdminMfaSettings.AsNoTracking().AnyAsync(x => x.Username == username && x.IsEnabled);
        }
        public async Task<AdminMfaSetupInfo> GetOrCreateSetupAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new AdminMfaSetupInfo();
            }

            var settings = await GetOrCreateSettingsAsync(username);
            var secretKey = _secretProtector.Unprotect(settings.ProtectedSecretKey);

            var label = Uri.EscapeDataString($"{Issuer}:{username}");
            var issuer = Uri.EscapeDataString(Issuer);

            var authenticatorUri = $"otpauth://totp/{label}?secret={secretKey}&issuer={issuer}&digits={TotpDigits}&period={TotpPeriodSeconds}";

            return new AdminMfaSetupInfo
            {
                IsEnabled = settings.IsEnabled,
                ManualEntryKey = FormatSecretForDisplay(secretKey),
                AuthenticatorUri = authenticatorUri,
                QrCodeImageDataUrl = GenerateQrCodeImageDataUrl(authenticatorUri)
            };
        }
        public async Task<AdminMfaSetupResult> ConfirmSetupAsync(string username, string verificationCode)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new AdminMfaSetupResult
                {
                    Succeeded = false,
                    ErrorMessage = "Admin username is missing."
                };
            }

            var settings = await GetOrCreateSettingsAsync(username);

            if (settings.IsEnabled)
            {
                return new AdminMfaSetupResult
                {
                    Succeeded = false,
                    ErrorMessage = "MFA is already enabled."
                };
            }

            var secretKey = _secretProtector.Unprotect(settings.ProtectedSecretKey);

            if (!VerifyTotpCode(secretKey, verificationCode, settings.LastAcceptedTotpCounter, out _))
            {
                return new AdminMfaSetupResult
                {
                    Succeeded = false,
                    ErrorMessage = "The authenticator code was not valid. Check the code and try again."
                };
            }

            var recoveryCodes = GenerateRecoveryCodes();
            var now = DateTime.UtcNow;

            settings.IsEnabled = true;
            settings.EnabledUtc = now;
            settings.LastUsedUtc = null;
            settings.LastAcceptedTotpCounter = null;
            settings.RecoveryCodes.Clear();

            foreach (var recoveryCode in recoveryCodes)
            {
                settings.RecoveryCodes.Add(new AdminMfaRecoveryCode
                {
                    CodeHash = HashRecoveryCode(recoveryCode),
                    CreatedUtc = now
                });
            }

            await _dbContext.SaveChangesAsync();

            return new AdminMfaSetupResult
            {
                Succeeded = true,
                RecoveryCodes = recoveryCodes
            };
        }
        public async Task<AdminMfaVerificationResult> VerifyLoginCodeAsync(string username, string code)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new AdminMfaVerificationResult
                {
                    Succeeded = false,
                    ErrorMessage = "Admin username is missing."
                };
            }

            var settings = await _dbContext.AdminMfaSettings.Include(x => x.RecoveryCodes).SingleOrDefaultAsync(x => x.Username == username && x.IsEnabled);

            if (settings == null)
            {
                return new AdminMfaVerificationResult
                {
                    Succeeded = false,
                    ErrorMessage = "MFA is not configured for this admin account."
                };
            }

            var secretKey = _secretProtector.Unprotect(settings.ProtectedSecretKey);

            if (VerifyTotpCode(secretKey, code, settings.LastAcceptedTotpCounter, out var acceptedCounter))
            {
                settings.LastAcceptedTotpCounter = acceptedCounter;
                settings.LastUsedUtc = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new AdminMfaVerificationResult
                {
                    Succeeded = true
                };
            }

            var recoveryCode = settings.RecoveryCodes
                .Where(recoveryCode => recoveryCode.UsedUtc == null)
                .FirstOrDefault(recoveryCode => VerifyRecoveryCodeHash(code, recoveryCode.CodeHash));

            if (recoveryCode != null)
            {
                recoveryCode.UsedUtc = DateTime.UtcNow;
                settings.LastUsedUtc = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new AdminMfaVerificationResult
                {
                    Succeeded = true,
                    UsedRecoveryCode = true
                };
            }

            return new AdminMfaVerificationResult
            {
                Succeeded = false,
                ErrorMessage = "Invalid authenticator or recovery code."
            };
        }

        // ========================================================================================

        private async Task<AdminMfaSetting> GetOrCreateSettingsAsync(string username)
        {
            var settings = await _dbContext.AdminMfaSettings.Include(x => x.RecoveryCodes).SingleOrDefaultAsync(x => x.Username == username);

            if (settings != null)
            {
                return settings;
            }

            settings = new AdminMfaSetting
            {
                Username = username,
                ProtectedSecretKey = _secretProtector.Protect(GenerateBase32Secret()),
                CreatedUtc = DateTime.UtcNow
            };

            _dbContext.AdminMfaSettings.Add(settings);

            await _dbContext.SaveChangesAsync();

            return settings;
        }
        private static string GenerateBase32Secret()
        {
            return Base32Encode(RandomNumberGenerator.GetBytes(20));
        }
        private static string Base32Encode(byte[] bytes)
        {
            var output = new StringBuilder();
            var bits = 0;
            var value = 0;

            foreach (var currentByte in bytes)
            {
                value = (value << 8) | currentByte;
                bits += 8;

                while (bits >= 5)
                {
                    output.Append(Base32Alphabet[(value >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                output.Append(Base32Alphabet[(value << (5 - bits)) & 31]);
            }

            return output.ToString();
        }
        private static string FormatSecretForDisplay(string secretKey)
        {
            return string.Join(" ", secretKey.Chunk(4).Select(characters => new string(characters)));
        }
        private static bool VerifyTotpCode(string secretKey, string code, long? lastAcceptedCounter, out long acceptedCounter)
        {
            acceptedCounter = 0;

            var normalizedCode = NormalizeTotpCode(code);

            if (normalizedCode == null)
            {
                return false;
            }

            var secretBytes = Base32Decode(secretKey);
            var currentCounter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TotpPeriodSeconds;

            for (var offset = -1; offset <= 1; offset++)
            {
                var counter = currentCounter + offset;

                if (counter < 0 || counter <= lastAcceptedCounter)
                {
                    continue;
                }

                var expectedCode = ComputeTotpCode(secretBytes, counter);

                if (SecureEquals(normalizedCode, expectedCode))
                {
                    acceptedCounter = counter;
                    return true;
                }
            }

            return false;
        }
        private static string? NormalizeTotpCode(string code)
        {
            var normalizedCode = new string((code ?? "").Where(char.IsDigit).ToArray());

            if (normalizedCode.Length != TotpDigits)
            {
                return null;
            }

            return normalizedCode;
        }
        private static string ComputeTotpCode(byte[] secretBytes, long counter)
        {
            var counterBytes = BitConverter.GetBytes(counter);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            using var hmac = new HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(counterBytes);
            var offset = hash[^1] & 0x0f;

            var binaryCode =
                ((hash[offset] & 0x7f) << 24) |
                ((hash[offset + 1] & 0xff) << 16) |
                ((hash[offset + 2] & 0xff) << 8) |
                (hash[offset + 3] & 0xff);

            return (binaryCode % (int)Math.Pow(10, TotpDigits)).ToString($"D{TotpDigits}");
        }
        private static byte[] Base32Decode(string value)
        {
            var normalizedValue = new string((value ?? "")
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());

            var output = new List<byte>();
            var bits = 0;
            var buffer = 0;

            foreach (var character in normalizedValue)
            {
                var characterValue = Base32Alphabet.IndexOf(character);

                if (characterValue < 0)
                {
                    throw new FormatException("The MFA secret key is not valid Base32.");
                }

                buffer = (buffer << 5) | characterValue;
                bits += 5;

                if (bits >= 8)
                {
                    output.Add((byte)((buffer >> (bits - 8)) & 255));
                    bits -= 8;
                }
            }

            return output.ToArray();
        }
        private static bool SecureEquals(string value, string expectedValue)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value);
            var expectedBytes = Encoding.UTF8.GetBytes(expectedValue);

            return CryptographicOperations.FixedTimeEquals(SHA256.HashData(valueBytes), SHA256.HashData(expectedBytes));
        }
        private static List<string> GenerateRecoveryCodes()
        {
            var recoveryCodes = new List<string>();

            for (var index = 0; index < RecoveryCodeCount; index++)
            {
                var rawCode = Base32Encode(RandomNumberGenerator.GetBytes(10));

                recoveryCodes.Add($"{rawCode[..8]}-{rawCode[8..16]}");
            }

            return recoveryCodes;
        }
        private static string HashRecoveryCode(string code)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var normalizedCode = NormalizeRecoveryCode(code);
            var codeBytes = Encoding.UTF8.GetBytes(normalizedCode);

            var hash = Rfc2898DeriveBytes.Pbkdf2(codeBytes, salt, RecoveryCodeHashIterations, HashAlgorithmName.SHA256, 32);

            return $"pbkdf2-sha256:{RecoveryCodeHashIterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
        private static bool VerifyRecoveryCodeHash(string code, string storedHash)
        {
            var hashParts = storedHash.Split(':', 4);

            if (hashParts.Length != 4 || hashParts[0] != "pbkdf2-sha256")
            {
                return false;
            }

            if (!int.TryParse(hashParts[1], out var iterations))
            {
                return false;
            }

            var salt = Convert.FromBase64String(hashParts[2]);
            var expectedHash = Convert.FromBase64String(hashParts[3]);
            var normalizedCode = NormalizeRecoveryCode(code);
            var codeBytes = Encoding.UTF8.GetBytes(normalizedCode);

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(codeBytes, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        private static string NormalizeRecoveryCode(string code)
        {
            return new string((code ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        }
        private static string GenerateQrCodeImageDataUrl(string value)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeBytes = qrCode.GetGraphic(12);

            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }
    }
}
