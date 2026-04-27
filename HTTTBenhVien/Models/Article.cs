using System;
using System.Collections.Generic;

namespace HTTTBenhVien.Models;

public partial class Article
{
    public int ArticleId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? ThumbnailUrl { get; set; }

    public string? Summary { get; set; }

    public string Content { get; set; } = null!;

    public string Category { get; set; } = null!;

    public int? AuthorId { get; set; }

    public DateTime? PublishDate { get; set; }

    public string? Status { get; set; }

    public int? ViewCount { get; set; }

    public virtual Employee Author { get; set; } = null!;
}
