using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebEssentials.AspNetCore.CdnTagHelpers
{
	public static partial class CdnHelper
	{
		public static string CdnifyHtmlImageUrls(this string html, string cdnUrl)
		{
			string result = html;
			var matchCollection = ImgRegex().Matches(result);
			var matches = new List<Match>(matchCollection.Cast<Match>()).ToArray().Reverse();

			foreach (var match in matches)
			{
				var group = match.Groups["src"];
				var value = group.Value;
				if (value.Contains("://") || value.StartsWith("//") || value.StartsWith("data:"))
					continue;

				var sep = value.StartsWith("/") ? "" : "/";
				result = result.Insert(group.Index, $"{cdnUrl.TrimEnd('/')}{sep}");
			}

			return result;
		}

		[GeneratedRegex("<img[^>]+src=\"(?<src>[^\"]+)\"[^>]+>")]
		private static partial Regex ImgRegex();
	}
}
