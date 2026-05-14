using System;

namespace NoteToolAvalonia.Models;

public enum NotePriority
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public class NoteCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Note";
    public NotePriority Priority { get; set; } = NotePriority.None;
    public string Tags { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public bool IsCompleted { get; set; }
    public string ColumnId { get; set; } = string.Empty;
}
