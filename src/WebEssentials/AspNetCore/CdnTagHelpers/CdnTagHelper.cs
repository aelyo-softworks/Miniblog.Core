using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;

namespace WebEssentials.AspNetCore.CdnTagHelpers
{
	[HtmlTargetElement("img")]
	[HtmlTargetElement("audio")]
	[HtmlTargetElement("video")]
	[HtmlTargetElement("track")]
	[HtmlTargetElement("source")]
	[HtmlTargetElement("link", Attributes = "[rel=stylesheet]")]
	[HtmlTargetElement("link", Attributes = "[rel=alternate]")]
	[HtmlTargetElement("link", Attributes = "[rel=preload]")]
	[HtmlTargetElement("link", Attributes = "[rel$=image]")]
	[HtmlTargetElement("link", Attributes = "[rel$=icon]")]
	[HtmlTargetElement("link", Attributes = "[rel$=-icon-precomposed]")]
	[HtmlTargetElement("meta", Attributes = "[name$=image]")]
	[HtmlTargetElement("meta", Attributes = "[property=image]")]
	[HtmlTargetElement("script")]
	[HtmlTargetElement("*", Attributes = "cdn-prop")]
	public class CdnTagHelper(IConfiguration config) : TagHelper
	{
		private static readonly Dictionary<string, string[]> _attributes = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "audio", new[] { "src" } },
			{ "embed", new[] { "src" } },
			{ "img", new[] { "src", "srcset" } },
			{ "input", new[] { "src" } },
			{ "link", new[] { "href" } },
			{ "meta", new[] { "content" } },
			{ "menuitem", new[] { "icon" } },
			{ "script", new[] { "src" } },
			{ "source", new[] { "src", "srcset" } },
			{ "track", new[] { "src" } },
			{ "video", new[] { "poster", "src" } },
		};

		private readonly string? _cdnUrl = config["cdn:url"];
		public override int Order => int.MaxValue;

		public override void Process(TagHelperContext context, TagHelperOutput output)
		{
			if (string.IsNullOrWhiteSpace(_cdnUrl) || string.IsNullOrEmpty(output?.TagName))
				return;

			if (output.Attributes.ContainsName("no-cdn"))
			{
				output.Attributes.RemoveAll("cdn-prop");
				output.Attributes.RemoveAll("no-cdn");
				return;
			}

			if (_attributes.TryGetValue(output.TagName, out var attributeNames))
			{
				foreach (var attrName in attributeNames)
				{
					PrependCdnUrl(output, attrName);
				}
			}

			if (output.Attributes.TryGetAttribute("cdn-prop", out _))
			{
				var targetProp = GetValue("cdn-prop", output);
				PrependCdnUrl(output, targetProp);
				output.Attributes.RemoveAll("cdn-prop");
			}
		}

		private void PrependCdnUrl(TagHelperOutput output, string? attrName)
		{
			if (string.IsNullOrWhiteSpace(_cdnUrl))
				return;

			var attrValue = GetValue(attrName, output);

			// Don't modify absolute paths
			if (string.IsNullOrWhiteSpace(attrName) || string.IsNullOrWhiteSpace(attrValue) || attrValue.Contains("://") || attrValue.StartsWith("//") || attrValue.StartsWith("data:"))
				return;

			var values = attrValue.Split(',');
			string? modifiedValue = null;
			foreach (var value in values)
			{
				var fullUrl = _cdnUrl.Trim().TrimEnd('/') + "/" + value.Trim().TrimStart('~', '/');
				modifiedValue += fullUrl + ", ";
			}

			var result = new HtmlString((modifiedValue ?? attrValue).Trim(',', ' '));

			output.Attributes.SetAttribute(attrName, result);
		}

		public static string? GetValue(string? attrName, TagHelperOutput output)
		{
			if (string.IsNullOrEmpty(attrName) || output == null || !output.Attributes.TryGetAttribute(attrName, out var attr))
				return null;

			if (attr.Value is string stringValue)
				return stringValue;

			if (attr.Value is IHtmlContent content)
			{
				if (content is HtmlString htmlString)
					return htmlString.ToString();

				using var writer = new StringWriter();
				content.WriteTo(writer, HtmlEncoder.Default);
				return writer.ToString();
			}
			return null;
		}
	}
}
