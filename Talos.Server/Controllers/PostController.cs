using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Talos.Server.Models.Dtos;
using Talos.Server.Services.Interfaces;

namespace Talos.Server.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;

    public PostController(IPostService postService)
    {
        _postService = postService;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // CREA POST + IA TAGS
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var userId = GetUserId();

        var post = await _postService.CreatePostAsync(
            userId,
            request.Title,
            request.Body,
            request.Status,
            request.TagIds
        );

        var response = new PostResponseDto
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            Status = post.Status,
            CreatedAt = post.CreatedAt,
            Tags = post.Tags.Select(t => t.Name).ToList()
        };

        return Ok(response);
    }

    // FEED
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed()
    {
        var userId = GetUserId();

        var posts = await _postService.GetFeedAsync(userId);

        var response = posts.Select(post => new PostResponseDto
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            Status = post.Status,
            CreatedAt = post.CreatedAt,
            Tags = post.Tags.Select(t => t.Name).ToList()
        });

        return Ok(response);
    }
}

public class CreatePostRequest
{
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Status { get; set; } = "published";
    public List<int>? TagIds { get; set; }
}