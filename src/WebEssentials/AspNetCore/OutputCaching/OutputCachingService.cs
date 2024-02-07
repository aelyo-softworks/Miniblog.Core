using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace WebEssentials.AspNetCore.OutputCaching
{
	internal class OutputCachingService(IOutputCacheKeysProvider cacheKeysProvider, OutputCacheOptions cacheOptions) : IOutputCachingService
	{
		private IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

		public bool TryGetValue(HttpContext context, out OutputCacheResponse? response)
		{
			response = null;
			return _cache.TryGetValue(cacheKeysProvider.GetCacheProfileCacheKey(context.Request), out OutputCacheProfile? profile) &&
				profile != null &&
				_cache.TryGetValue(cacheKeysProvider.GetRequestCacheKey(context, profile), out response);
		}

		public void Set(HttpContext context, OutputCacheResponse response)
		{
			if (context.IsOutputCachingEnabled(out var profile) && profile != null)
			{
				AddProfileToCache(context, profile);
				AddResponseToCache(context, profile, response);
			}
		}

		private void AddProfileToCache(HttpContext context, OutputCacheProfile profile)
		{
			var profileCacheEntryOptions = new MemoryCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = cacheOptions.ProfileCacheDuration
			};
			_cache.Set(cacheKeysProvider.GetCacheProfileCacheKey(context.Request), profile, profileCacheEntryOptions);
		}

		private void AddResponseToCache(HttpContext context, OutputCacheProfile profile, OutputCacheResponse response)
		{
			var hostingEnvironment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
			var options = profile.BuildMemoryCacheEntryOptions(hostingEnvironment);
			_cache.Set(cacheKeysProvider.GetRequestCacheKey(context, profile), response, options);
		}

		public void Clear() => Interlocked.Exchange(ref _cache, new MemoryCache(new MemoryCacheOptions()))?.Dispose();
		public void Remove(string cacheKey) => _cache.Remove(cacheKey);
	}
}
