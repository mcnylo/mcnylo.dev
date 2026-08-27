namespace mcnylo.dev.Admin.ViewModels.Media
{
    public class AdminMediaListVM
    {
        public List<AdminMediaListItemVM> MediaItems { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalMediaItems { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public int TotalReferencedMediaItems { get; set; } = 0;
        public int TotalUnreferencedMediaItems { get; set; } = 0;
        public string? SuccessMessage { get; set; } = null;
        public string? ErrorMessage { get; set; } = null;
    }
}
