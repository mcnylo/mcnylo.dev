namespace mcnylo.dev.Articles.Services
{
    public interface IArticleMarkdownService
    {
        public string RenderToHtml(string markdown);
    }
}
