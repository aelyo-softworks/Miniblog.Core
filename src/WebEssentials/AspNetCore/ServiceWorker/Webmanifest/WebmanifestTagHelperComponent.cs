using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace WebEssentials.AspNetCore.Pwa
{
	internal class WebmanifestTagHelperComponent(PwaOptions options, IServiceProvider serviceProvider) : TagHelperComponent
	{
		private readonly string _link = "\t<link rel=\"manifest\" href=\"" + options.BaseRoute + PwaConstants.WebManifestRoute + "\" />\r\n";
		private const string _themeFormat = "\t<meta name=\"theme-color\" content=\"{0}\" />\r\n";

		/// <inheritdoc />
		public override int Order => 100;

		/// <inheritdoc />
		public override void Process(TagHelperContext context, TagHelperOutput output)
		{
			if (!options.RegisterWebmanifest)
				return;

			if (serviceProvider.GetService(typeof(WebManifest)) is not WebManifest manifest)
				return;

			if (string.Equals(context.TagName, "head", StringComparison.OrdinalIgnoreCase))
			{
				if (!string.IsNullOrEmpty(manifest.ThemeColor))
				{
					output.PostContent.AppendHtml(string.Format(_themeFormat, manifest.ThemeColor));
				}

				output.PostContent.AppendHtml(_link);
			}
		}
	}
}
