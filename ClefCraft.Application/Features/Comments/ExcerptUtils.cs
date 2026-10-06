using System.Net;
using System.Text.RegularExpressions;

namespace ClefCraft.Application.Features.Comments
{
    public static class ExcerptUtils
    {
        // Line-level boundaries Quill emits; each becomes a space so separate lines don't run together.
        private static readonly Regex LineBreakTags = new(@"<\s*(br|/p|/div|/li|/h[1-6])\b[^>]*>", RegexOptions.IgnoreCase);
        private static readonly Regex AnyTag = new("<[^>]+>");
        private static readonly Regex Whitespace = new(@"\s+");

        // Strips Quill's HTML down to plain text for the mention notification toast. The toast shows
        // the excerpt as plain text, so entities must be decoded here or they'd appear literally
        // ("Please&nbsp;review"). Inline tags are removed without adding space, so a mention's
        // nested spans read "@Ben", and quill-mention's invisible U+FEFF markers are dropped.
        public static string PlainTextExcerpt(string bodyHtml, int maxLength)
        {
            var text = LineBreakTags.Replace(bodyHtml ?? string.Empty, " ");
            text = AnyTag.Replace(text, string.Empty);
            text = WebUtility.HtmlDecode(text).Replace("﻿", string.Empty);
            text = Whitespace.Replace(text, " ").Trim(); // \s also matches the decoded no-break spaces

            return text.Length > maxLength
                ? text[..maxLength].TrimEnd() + "…"
                : text;
        }
    }
}
