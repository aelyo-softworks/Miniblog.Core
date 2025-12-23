using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Miniblog.Core.Models;
using Miniblog.Core.Services;

namespace Miniblog.Core.Controllers
{
    using WebEssentials.AspNetCore.Pwa;

    public partial class BlogController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings, WebManifest manifest) : Controller
    {
        [Route("/blog/comment/{postId}")]
        [HttpPost]
        public async Task<IActionResult> AddComment(string postId, Comment comment)
        {
            var post = await blog.GetPostById(postId).ConfigureAwait(true);

            if (!ModelState.IsValid)
                return View(nameof(Post), post);

            if (post is null || !post.AreCommentsOpen(settings.Value.CommentsCloseAfterDays))
                return NotFound();

            ArgumentNullException.ThrowIfNull(comment);

            comment.IsAdmin = User.Identity!.IsAuthenticated;
            comment.Content = comment.Content.Trim();
            comment.Author = comment.Author.Trim();
            comment.Email = comment.Email.Trim();

            // the website form key should have been removed by javascript unless the comment was
            // posted by a spam robot
            if (!Request.Form.ContainsKey("website"))
            {
                post.Comments.Add(comment);
                await blog.SavePost(post).ConfigureAwait(false);
            }

            return Redirect($"{post.GetEncodedLink()}#{comment.ID}");
        }

        [Route("/blog/category/{category}/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Category(string category, int page = 0)
        {
            // get posts for the selected category.
            var posts = blog.GetPostsByCategory(category);

            // apply paging filter.
            var filteredPosts = posts.Skip(settings.Value.PostsPerPage * page).Take(settings.Value.PostsPerPage);

            // set the view option
            ViewData["ViewOption"] = settings.Value.ListView;

            ViewData[Constants.TotalPostCount] = await posts.CountAsync().ConfigureAwait(true);
            ViewData[Constants.Title] = $"{manifest.Name} {category}";
            ViewData[Constants.Description] = $"Articles posted in the {category} category";
            ViewData[Constants.prev] = $"/blog/category/{category}/{page + 1}/";
            ViewData[Constants.next] = $"/blog/category/{category}/{(page <= 1 ? null : page - 1 + "/")}";
            return View("~/Views/Blog/Index.cshtml", filteredPosts);
        }

        [Route("/blog/tag/{tag}/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Tag(string tag, int page = 0)
        {
            // get posts for the selected tag.
            var posts = blog.GetPostsByTag(tag);

            // apply paging filter.
            var filteredPosts = posts.Skip(settings.Value.PostsPerPage * page).Take(settings.Value.PostsPerPage);

            // set the view option
            ViewData["ViewOption"] = settings.Value.ListView;

            ViewData[Constants.TotalPostCount] = await posts.CountAsync().ConfigureAwait(true);
            ViewData[Constants.Title] = $"{manifest.Name} {tag}";
            ViewData[Constants.Description] = $"Articles posted in the {tag} tag";
            ViewData[Constants.prev] = $"/blog/tag/{tag}/{page + 1}/";
            ViewData[Constants.next] = $"/blog/tag/{tag}/{(page <= 1 ? null : page - 1 + "/")}";
            return View("~/Views/Blog/Index.cshtml", filteredPosts);
        }

        [Route("/blog/comment/{postId}/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
            var post = await blog.GetPostById(postId).ConfigureAwait(false);
            if (post is null)
                return NotFound();

            var comment = post.Comments.FirstOrDefault(c => c.ID.Equals(commentId, StringComparison.OrdinalIgnoreCase));
            if (comment is null)
                return NotFound();

            post.Comments.Remove(comment);
            await blog.SavePost(post).ConfigureAwait(false);

            return Redirect($"{post.GetEncodedLink()}#comments");
        }

        [Route("/blog/deletepost/{id}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeletePost(string id)
        {
            var existing = await blog.GetPostById(id).ConfigureAwait(false);
            if (existing is null)
                return NotFound();

            await blog.DeletePost(existing).ConfigureAwait(false);
            return Redirect("/");
        }

        [Route("/blog/edit/{id?}")]
        [HttpGet, Authorize]
        public async Task<IActionResult> Edit(string? id)
        {
            var categories = await blog.GetCategories().ToListAsync();
            categories.Sort();
            ViewData[Constants.AllCats] = categories;

            var tags = await blog.GetTags().ToListAsync();
            tags.Sort();
            ViewData[Constants.AllTags] = tags;

            if (string.IsNullOrEmpty(id))
                return View(new Post());

            var post = await blog.GetPostById(id).ConfigureAwait(false);
            return post is null ? NotFound() : (IActionResult)View(post);
        }

        [Route("/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Index([FromRoute] int page = 0)
        {
            // get published posts.
            var posts = blog.GetPosts();

            // apply paging filter.
            var filteredPosts = posts.Skip(settings.Value.PostsPerPage * page).Take(settings.Value.PostsPerPage);

            // set the view option
            ViewData[Constants.ViewOption] = settings.Value.ListView;

            ViewData[Constants.TotalPostCount] = await posts.CountAsync().ConfigureAwait(true);
            ViewData[Constants.Title] = manifest.Name;
            ViewData[Constants.Description] = manifest.Description;
            ViewData[Constants.prev] = $"/{page + 1}/";
            ViewData[Constants.next] = $"/{(page <= 1 ? null : $"{page - 1}/")}";

            return View("~/Views/Blog/Index.cshtml", filteredPosts);
        }

        [Route("/blog/{slug?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Post(string slug)
        {
            var post = await blog.GetPostBySlug(slug).ConfigureAwait(true);
            return post is null ? NotFound() : (IActionResult)View(post);
        }

        /// <remarks>This is for redirecting potential existing URLs from the old Miniblog URL format.</remarks>
        [Route("/post/{slug}")]
        [HttpGet]
        public IActionResult Redirects(string slug) => LocalRedirectPermanent($"/blog/{slug}");

#pragma warning disable ASP0018 // Unused route parameter
        [Route("/blog/{slug?}")]
#pragma warning restore ASP0018 // Unused route parameter
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> UpdatePost(Post post)
        {
            if (!ModelState.IsValid)
                return View(nameof(Edit), post);

            ArgumentNullException.ThrowIfNull(post);

            var existing = await blog.GetPostById(post.ID).ConfigureAwait(false) ?? post;
            var existingPostWithSameSlug = await blog.GetPostBySlug(existing.Slug).ConfigureAwait(true);

            if (existingPostWithSameSlug != null && existingPostWithSameSlug.ID != post.ID)
            {
                existing.Slug = Models.Post.CreateSlug(post.Title + DateTime.UtcNow.ToString("yyyyMMddHHmm"));
            }

            string categories = Request.Form[Constants.categories]!;
            string tags = Request.Form[Constants.tags]!;

            existing.Categories.Clear();
            categories.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim().ToLowerInvariant()).ToList().ForEach(existing.Categories.Add);
            existing.Tags.Clear();
            tags.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().ToLowerInvariant()).ToList().ForEach(existing.Tags.Add);
            existing.Title = post.Title.Trim();
            existing.Slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug.Trim() : Models.Post.CreateSlug(post.Title);
            existing.IsPublished = post.IsPublished;
            existing.Content = post.Content.Trim();
            existing.Excerpt = post.Excerpt.Trim();

            await SaveFilesToDisk(existing).ConfigureAwait(false);
            await blog.SavePost(existing).ConfigureAwait(false);
            return Redirect(post.GetEncodedLink());
        }

        private async Task SaveFilesToDisk(Post post)
        {
            var imgRegex = ImgRegex();
            var base64Regex = Base64Regex();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".gif", ".png", ".webp" };

            foreach (var match in imgRegex.Matches(post.Content).Cast<Match?>())
            {
                if (match is null)
                    continue;

                var doc = new XmlDocument();
                doc.LoadXml($"<root>{match.Value}</root>");

                var img = doc.FirstChild!.FirstChild;
                var srcNode = img!.Attributes!["src"];
                var fileNameNode = img.Attributes["data-filename"];

                // The HTML editor creates base64 DataURIs which we'll have to convert to image
                // files on disk
                if (srcNode is null || fileNameNode is null)
                    continue;

                var extension = System.IO.Path.GetExtension(fileNameNode.Value);

                // Only accept image files
                if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    continue;

                var base64Match = base64Regex.Match(srcNode.Value);
                if (base64Match.Success)
                {
                    var bytes = Convert.FromBase64String(base64Match.Groups["base64"].Value);
                    srcNode.Value = await blog.SaveFile(bytes, fileNameNode.Value).ConfigureAwait(false);

                    img.Attributes.Remove(fileNameNode);
                    post.Content = post.Content.Replace(match.Value, img.OuterXml, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [GeneratedRegex("<img[^>]+ />", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ImgRegex();

        [GeneratedRegex("data:[^/]+/(?<ext>[a-z]+);base64,(?<base64>.+)", RegexOptions.IgnoreCase)]
        private static partial Regex Base64Regex();
    }
}
