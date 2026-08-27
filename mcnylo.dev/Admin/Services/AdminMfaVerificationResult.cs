namespace mcnylo.dev.Admin.Services
{
    public class AdminMfaVerificationResult
    {
        public bool Succeeded { get; set; }
        public bool UsedRecoveryCode { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}
