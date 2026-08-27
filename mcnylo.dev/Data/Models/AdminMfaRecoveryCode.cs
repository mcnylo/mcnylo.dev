namespace mcnylo.dev.Data.Models
{
    public class AdminMfaRecoveryCode
    {
        public int Id { get; set; }
        public int AdminMfaSettingId { get; set; }
        public string CodeHash { get; set; } = "";
        public DateTime CreatedUtc { get; set; }
        public DateTime? UsedUtc { get; set; }
        public AdminMfaSetting? AdminMfaSetting { get; set; }
    }
}
