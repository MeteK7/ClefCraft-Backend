using ClefCraft.Application.Features.Comments;
using Shouldly;

namespace ClefCraft.Application.UnitTests.Features.Comments
{
    // The excerpt is shown as plain text in the mention toast (Angular interpolation, no HTML
    // rendering), so it must not contain markup, HTML entities or Quill's invisible characters.
    public class ExcerptUtilsTests
    {
        // A comment exactly as the Quill editor with quill-mention stores it: &nbsp; for typed
        // spaces, and the mention wrapped in U+FEFF characters and nested spans.
        private const string QuillMentionComment =
            "<p>Please&nbsp;review&nbsp;<span class=\"mention\" data-index=\"0\" data-denotation-char=\"@\" " +
            "data-id=\"b83d0915-e482-4e9f-b1ff-46abde25a6e6\" data-value=\"Ben Collaborator\">﻿" +
            "<span contenteditable=\"false\"><span class=\"ql-mention-denotation-char\">@</span>" +
            "<span class=\"ql-mention-value\">Ben Collaborator</span></span>﻿</span>&nbsp;</p>";

        [Fact]
        public void QuillCommentWithAMention_BecomesReadablePlainText()
        {
            ExcerptUtils.PlainTextExcerpt(QuillMentionComment, 140).ShouldBe("Please review @Ben Collaborator");
        }

        [Theory]
        [InlineData("<p>Tom &amp; Jerry</p>", "Tom & Jerry")]
        [InlineData("<p>1 &lt; 2 &gt; 0</p>", "1 < 2 > 0")]
        [InlineData("<p>&quot;quoted&quot; and &#39;single&#39;</p>", "\"quoted\" and 'single'")]
        [InlineData("<p>non&nbsp;breaking</p>", "non breaking")]
        public void HtmlEntities_AreDecoded(string html, string expected)
        {
            ExcerptUtils.PlainTextExcerpt(html, 140).ShouldBe(expected);
        }

        [Fact]
        public void Paragraphs_AndLineBreaks_StaySeparatedBySpaces()
        {
            ExcerptUtils.PlainTextExcerpt("<p>First line</p><p>second<br>third</p>", 140)
                .ShouldBe("First line second third");
        }

        [Fact]
        public void InlineFormatting_DoesNotSplitWords()
        {
            ExcerptUtils.PlainTextExcerpt("<p>un<strong>believ</strong><em>able</em></p>", 140).ShouldBe("unbelievable");
        }

        [Fact]
        public void LongText_IsCutAtMaxLength_WithAnEllipsis()
        {
            var excerpt = ExcerptUtils.PlainTextExcerpt("<p>" + new string('a', 200) + "</p>", 140);

            excerpt.ShouldBe(new string('a', 140) + "…");
        }

        [Fact]
        public void MaxLength_CountsDecodedText_NotMarkupOrEntities()
        {
            // 10 visible characters, but far more than 10 characters of HTML.
            ExcerptUtils.PlainTextExcerpt("<p><strong>abc</strong>&nbsp;&amp;&nbsp;defg</p>", 10).ShouldBe("abc & defg");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("<p><br></p>")]
        [InlineData("<p>﻿&nbsp;</p>")]
        public void EmptyOrBlankComments_GiveAnEmptyExcerpt(string? html)
        {
            ExcerptUtils.PlainTextExcerpt(html!, 140).ShouldBe(string.Empty);
        }
    }
}
