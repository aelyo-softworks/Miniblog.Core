using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;

namespace WebEssentials.AspNetCore.CdnTagHelpers
{
	[HtmlTargetElement("*", Attributes = _attrName)]
	public class CdnifyTagHelper(IConfiguration config) : TagHelper
	{
		private readonly string? _cdnUrl = config["cdn:url"];
		private const string _attrName = "cdnify";

		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput? output)
		{
			output?.Attributes?.RemoveAll(_attrName);

			if (string.IsNullOrWhiteSpace(_cdnUrl) || string.IsNullOrEmpty(output?.TagName))
				return;

			var html = output.Content.IsModified ? output.Content.GetContent() : (await output.GetChildContentAsync()).GetContent();
			var cdnified = html.CdnifyHtmlImageUrls(_cdnUrl);
			output.Content.SetHtmlContent(cdnified);
		}
	}
}
