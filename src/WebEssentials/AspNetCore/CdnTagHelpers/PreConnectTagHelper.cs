using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;

namespace WebEssentials.AspNetCore.CdnTagHelpers
{
	[HtmlTargetElement("head")]
	public class PreConnectTagHelper(IConfiguration config) : TagHelper
	{
		private readonly string? _cdnUrl = config["cdn:url"];
		private readonly string? _dnsPrefetch = config["cdn:prefetch"];

		public override void Process(TagHelperContext context, TagHelperOutput output)
		{
			if (_dnsPrefetch == "False" || // opted out manually
				string.IsNullOrWhiteSpace(_cdnUrl) ||
				string.IsNullOrEmpty(output?.TagName))
				return;

			var url = new Uri(_cdnUrl, UriKind.Absolute);
			var link = new HtmlString($"<link rel=\"preconnect\" href=\"{url.OriginalString}\" />");
			output.PreContent.AppendHtml(link);
		}
	}
}
