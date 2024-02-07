using System;

using Microsoft.AspNetCore.Mvc.Filters;

using WebEssentials.AspNetCore.OutputCaching;

namespace Microsoft.AspNetCore.Mvc
{
	/// <summary>
	/// Enables server-side output caching.
	/// </summary>
	public class OutputCacheAttribute : ActionFilterAttribute
	{
		private readonly string[] _fileDependencies;

		/// <summary>
		/// Enables server-side output caching.
		/// </summary>
		/// <param name="fileDependencies">Globbing patterns relative to the content root (not the wwwroot).</param>
		public OutputCacheAttribute(params string[] fileDependencies)
		{
			_fileDependencies = fileDependencies;

			if (fileDependencies.Length == 0)
			{
				_fileDependencies = ["**/*.*"];
			}
		}

		/// <summary>
		/// The name of the profile.
		/// </summary>
		public string? Profile { get; set; }

		/// <summary>
		/// The duration in seconds of how long to cache the response.
		/// </summary>
		public int Duration { get; set; }

		/// <summary>
		/// Comma separated list of HTTP headers to vary the caching by.
		/// </summary>
		public string? VaryByHeader { get; set; }

		/// <summary>
		/// Comma separated list of query string parameters to vary the caching by.
		/// </summary>
		public string? VaryByParam { get; set; }

		/// <summary>
		/// Comma separated list of arguments to vary the caching by using a custom function.
		/// </summary>
		public string? VaryByCustom { get; set; }

		/// <summary>
		/// Use absolute expiration instead of the default sliding expiration.
		/// </summary>
		public bool UseAbsoluteExpiration { get; set; }

		/// <summary>
		/// Executing the filter
		/// </summary>
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			if (!string.IsNullOrEmpty(Profile))
			{
				if (context.HttpContext.RequestServices.GetService(typeof(OutputCacheOptions)) is not OutputCacheOptions options || !options.Profiles.TryGetValue(Profile, out OutputCacheProfile? value))
					throw new ArgumentException($"The Profile '{Profile}' hasn't been created.");

				context.HttpContext.EnableOutputCaching(value);
			}
			else
			{
				context.HttpContext.EnableOutputCaching
				(
					slidingExpiration: TimeSpan.FromSeconds(Duration),
					varyByHeaders: VaryByHeader,
					varyByParam: VaryByParam,
					varyByCustom: VaryByCustom,
					fileDependencies: _fileDependencies,
					useAbsoluteExpiration: UseAbsoluteExpiration
				);
			}
		}
	}
}
