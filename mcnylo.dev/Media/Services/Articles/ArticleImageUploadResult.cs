namespace mcnylo.dev.Media.Services.Articles
{
    public class ArticleImageUploadResult
    {
        public bool Succeeded { get; set; } = false;
        public string ErrorMessage { get; set; } = "";
        public string RequestPath { get; set; } = "";
        public string StoredFileName { get; set; } = "";

        public static ArticleImageUploadResult Success(string requestPath, string storedFileName)
        {
            return new ArticleImageUploadResult
            {
                Succeeded = true,
                RequestPath = requestPath,
                StoredFileName = storedFileName
            };
        }

        public static ArticleImageUploadResult Failure(string errorMessage)
        {
            return new ArticleImageUploadResult
            {
                Succeeded = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
