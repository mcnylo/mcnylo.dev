namespace mcnylo.dev.Admin.Services
{
    public class AdminMfaSetupInfo
    {
        public bool IsEnabled { get; set; }
        public string ManualEntryKey { get; set; } = "";
        public string AuthenticatorUri { get; set; } = "";
        public string QrCodeImageDataUrl { get; set; } = "";
    }
}
