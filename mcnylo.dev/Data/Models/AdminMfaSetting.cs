namespace mcnylo.dev.Data.Models
{
    public class AdminMfaSetting
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string ProtectedSecretKey { get; set; } = "";
        public bool IsEnabled { get; set; }
        public long? LastAcceptedTotpCounter { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? EnabledUtc { get; set; }
        public DateTime? LastUsedUtc { get; set; }
        public List<AdminMfaRecoveryCode> RecoveryCodes { get; set; } = [];
    }
}
