using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Miniblog.Core.Models
{
	public class Comment
	{
		[Required]
		public string Author { get; set; } = string.Empty;

		[Required]
		public string Content { get; set; } = string.Empty;

		[Required, EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		public string ID { get; set; } = Guid.NewGuid().ToString();

		public bool IsAdmin { get; set; } = false;

		[Required]
		public DateTime PubDate { get; set; } = DateTime.UtcNow;

		public string GetGravatar()
		{
			var sb = new StringBuilder();
			if (Guid.TryParse(Email.Trim(), out var guid))
			{
				sb.Append(guid.ToString("N"));
			}
			else
			{
				var inputBytes = Encoding.UTF8.GetBytes(Email.Trim().ToLowerInvariant());
				var hashBytes = MD5.HashData(inputBytes);

				// Convert the byte array to hexadecimal string
				for (var i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("X2", CultureInfo.InvariantCulture));
				}
			}
			return $"https://www.gravatar.com/avatar/{sb.ToString().ToLowerInvariant()}?s=60&d=blank";
		}

		public string RenderContent() => Content;
	}
}
