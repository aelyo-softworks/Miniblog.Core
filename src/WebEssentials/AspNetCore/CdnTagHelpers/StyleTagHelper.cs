using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;

namespace WebEssentials.AspNetCore.CdnTagHelpers
{
	[HtmlTargetElement("style")]
	[HtmlTargetElement("link", Attributes = "inline")] // Support for LigerShark.WebOptimizer
	public partial class StyleTagHelper(IConfiguration config) : TagHelper
	{
		private readonly string? _cdnUrl = config["cdn:url"];
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
		private static readonly Regex _rxUrl = new(@"url\s*\(\s*([""']?)([^:)]+)\1\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

		public override int Order => base.Order + 100;

		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			if (string.IsNullOrWhiteSpace(_cdnUrl) || output?.TagName != "style")
				return;

			var content = await output.GetChildContentAsync();
			var css = content.GetContent();
			var matches = _rxUrl.Matches(content.GetContent()).Cast<Match>().Reverse();
			foreach (var match in matches)
			{
				var group = match.Groups[2];
				var value = group.Value;

				// Ignore references with protocols
				if (value.Contains("://") || value.StartsWith("//") || value.StartsWith("data:"))
					continue;

				var sep = value.StartsWith('/') ? "" : "/";
				css = css.Insert(group.Index, $"{_cdnUrl.TrimEnd('/')}{sep}");
			}
			output.Content.SetHtmlContent(css);
		}
	}
}
