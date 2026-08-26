namespace mcnylo.dev.Media.Services.Projects
{
    public class ProjectImageUploadResult
    {
        public bool Succeeded { get; set; } = false;
        public string ErrorMessage { get; set; } = "";
        public string RequestPath { get; set; } = "";
        public string StoredFileName { get; set; } = "";

        public static ProjectImageUploadResult Success(string requestPath, string storedFileName)
        {
            return new ProjectImageUploadResult
            {
                Succeeded = true,
                RequestPath = requestPath,
                StoredFileName = storedFileName
            };
        }

        public static ProjectImageUploadResult Failure(string errorMessage)
        {
            return new ProjectImageUploadResult
            {
                Succeeded = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
