using Markdig;

namespace mcnylo.dev.Articles.Services
{
    public class ArticleMarkdownService : IArticleMarkdownService
    {
        private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().DisableHtml().Build();

        // ========================================================================================

        public string RenderToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "";
            }

            return Markdown.ToHtml(markdown, MarkdownPipeline);
        }
    }
}
