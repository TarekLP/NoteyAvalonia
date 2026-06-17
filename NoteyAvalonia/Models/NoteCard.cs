using System;
using System.Collections.Generic;

namespace NoteToolAvalonia.Models;

public enum NotePriority
{
    None,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Represents a single note document.
/// All metadata is stored as YAML frontmatter in the .md file itself,
/// so notes are portable and can be opened in Obsidian, VS Code, etc.
/// </summary>
public class NoteCard
{
    public string Id           { get; set; } = Guid.NewGuid().ToString();
    public string Title        { get; set; } = "New Note";
    public string Category     { get; set; } = string.Empty;
    public List<string> Tags   { get; set; } = new();

    /// <summary>
    /// GUIDs of other notes this note links to (for the references panel).
    /// </summary>
    public List<string> References { get; set; } = new();

    public NotePriority Priority  { get; set; } = NotePriority.None;
    public DateTime?    Deadline  { get; set; }
    public DateTime     CreatedAt    { get; set; } = DateTime.Now;
    public DateTime     LastModified { get; set; } = DateTime.Now;
    public bool         IsCompleted  { get; set; }
    public bool         IsPinned     { get; set; }
}
