namespace mcnylo.dev.Media.Services.Resume
{
    public class ResumePdfUploadResult
    {
        public bool Succeeded { get; set; } = false;
        public string RequestPath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public static ResumePdfUploadResult Success(string requestPath)
        {
            return new ResumePdfUploadResult
            {
                Succeeded = true,
                RequestPath = requestPath
            };
        }

        public static ResumePdfUploadResult Failure(string errorMessage)
        {
            return new ResumePdfUploadResult
            {
                Succeeded = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
