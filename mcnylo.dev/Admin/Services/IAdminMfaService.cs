namespace mcnylo.dev.Admin.Services
{
    public interface IAdminMfaService
    {
        public Task<bool> IsMfaEnabledAsync(string username);
        public Task<AdminMfaSetupInfo> GetOrCreateSetupAsync(string username);
        public Task<AdminMfaSetupResult> ConfirmSetupAsync(string username, string verificationCode);
        public Task<AdminMfaVerificationResult> VerifyLoginCodeAsync(string username, string code);
    }
}
