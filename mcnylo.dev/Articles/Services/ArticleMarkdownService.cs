using Ganss.Xss;
using Markdig;

namespace mcnylo.dev.Articles.Services
{
    public class ArticleMarkdownService : IArticleMarkdownService
    {
        private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        private static readonly HtmlSanitizer HtmlSanitizer = CreateHtmlSanitizer();

        // ========================================================================================

        public string RenderToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "";
            }

            string html = Markdown.ToHtml(markdown, MarkdownPipeline);

            return HtmlSanitizer.Sanitize(html);
        }

        // ========================================================================================

        private static HtmlSanitizer CreateHtmlSanitizer()
        {
            HtmlSanitizer sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Add("blockquote");
            sanitizer.AllowedTags.Add("figure");
            sanitizer.AllowedTags.Add("figcaption");
            sanitizer.AllowedTags.Add("kbd");

            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedAttributes.Add("id");

            return sanitizer;
        }
    }
}
