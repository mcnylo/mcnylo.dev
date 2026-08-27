namespace mcnylo.dev.Admin.ViewModels.MFA
{
    public class AdminMfaSetupVM
    {
        public bool IsEnabled { get; set; }
        public string ManualEntryKey { get; set; } = "";
        public string AuthenticatorUri { get; set; } = "";
        public string VerificationCode { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string QrCodeImageDataUrl { get; set; } = "";
    }
}
