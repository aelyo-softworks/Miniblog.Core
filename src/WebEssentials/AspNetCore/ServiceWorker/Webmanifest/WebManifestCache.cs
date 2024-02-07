using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace WebEssentials.AspNetCore.Pwa
{
	internal partial class WebManifestCache(IWebHostEnvironment env, string fileName)
	{
		private readonly MemoryCache _cache = new(new MemoryCacheOptions());

		public WebManifest? GetManifest() => _cache.GetOrCreate("webmanifest", (entry) =>
		{
			var file = env.WebRootFileProvider.GetFileInfo(fileName);
			if (file?.PhysicalPath == null)
				return null;

			entry.AddExpirationToken(env.WebRootFileProvider.Watch(fileName));
			var json = File.ReadAllText(file.PhysicalPath);
			var manifest = JsonSerializer.Deserialize<WebManifest>(json);
			if (manifest == null)
				throw new InvalidOperationException("Null manifest");

			manifest.FileName = fileName;
			manifest.RawJson = JsonRegex().Replace(json, "$1");
			if (!manifest.IsValid(out var error))
				throw new InvalidOperationException(error);

			return manifest;
		});
		[GeneratedRegex("(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+")]
		private static partial Regex JsonRegex();
	}
}
