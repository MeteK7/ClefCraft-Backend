using System.Text.RegularExpressions;

namespace ClefCraft.Application.Features.Comments
{
    public static class ExcerptUtils
    {
        // Strips Quill's HTML wrapper down to plain text for the mention notification toast —
        // the toast has no rich-text rendering surface, just a one-line message.
        public static string PlainTextExcerpt(string bodyHtml, int maxLength)
        {
            var plainText = Regex.Replace(bodyHtml ?? string.Empty, "<[^>]+>", " ");
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            return plainText.Length > maxLength
                ? plainText[..maxLength].TrimEnd() + "…"
                : plainText;
        }
    }
}
