using System.Collections.Generic;
using System.Threading.Tasks;
using Miniblog.Core.Models;

namespace Miniblog.Core.Services
{
	public interface IBlogService
	{
		Task DeletePost(Post post);
		IAsyncEnumerable<string> GetCategories();
		IAsyncEnumerable<string> GetTags();
		Task<Post?> GetPostById(string id);
		Task<Post?> GetPostBySlug(string slug);
		IAsyncEnumerable<Post> GetPosts();
		IAsyncEnumerable<Post> GetPosts(int count, int skip = 0);
		IAsyncEnumerable<Post> GetPostsByCategory(string category);
		IAsyncEnumerable<Post> GetPostsByTag(string tag);
		Task<string> SaveFile(byte[] bytes, string fileName, string? suffix = null);
		Task SavePost(Post post);
	}
}
