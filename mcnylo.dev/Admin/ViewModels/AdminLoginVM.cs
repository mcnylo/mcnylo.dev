namespace mcnylo.dev.Admin.ViewModels
{
    public class AdminLoginVM
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? ReturnUrl { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}
