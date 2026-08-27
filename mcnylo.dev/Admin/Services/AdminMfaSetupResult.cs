namespace mcnylo.dev.Admin.Services
{
    public class AdminMfaSetupResult
    {
        public bool Succeeded { get; set; }
        public string ErrorMessage { get; set; } = "";
        public List<string> RecoveryCodes { get; set; } = [];
    }
}
