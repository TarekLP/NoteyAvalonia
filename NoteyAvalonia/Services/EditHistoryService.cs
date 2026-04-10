using System;
using System.Collections.Generic;
using System.Linq;

namespace NoteToolAvalonia.Services;

/// <summary>
/// Manages undo/redo history and revision snapshots for notes.
/// Tracks edits up to a maximum and stores timestamped revisions.
/// </summary>
public class EditHistoryService
{
	private class HistoryState
	{
		public string Content { get; set; } = string.Empty;
		public int CursorPosition { get; set; }
		public DateTime Timestamp { get; set; }
	}

	private readonly List<HistoryState> _undoStack = new();
	private readonly List<HistoryState> _redoStack = new();
	private readonly List<HistoryState> _revisions = new();
	private readonly int _maxUndoSteps;
	private readonly int _maxRevisions;
	private DateTime _lastRevisionTime = DateTime.Now;
	private readonly TimeSpan _revisionInterval;

	public int UndoCount => _undoStack.Count;
	public int RedoCount => _redoStack.Count;
	public int RevisionCount => _revisions.Count;

	public EditHistoryService(int maxUndoSteps = 50, int maxRevisions = 100)
	{
		_maxUndoSteps = maxUndoSteps;
		_maxRevisions = maxRevisions;
		_revisionInterval = TimeSpan.FromMinutes(5);
	}

	/// <summary>
	/// Push a new state onto the undo stack.
	/// This clears the redo stack when called (user made a new edit after undo).
	/// </summary>
	public void Push(string content, int cursorPosition)
	{
		var state = new HistoryState
		{
			Content = content,
			CursorPosition = cursorPosition,
			Timestamp = DateTime.Now
		};

		_undoStack.Add(state);
		_redoStack.Clear(); // Clear redo when new edit is made

		// Trim undo stack if it exceeds max
		if (_undoStack.Count > _maxUndoSteps)
		{
			_undoStack.RemoveAt(0);
		}

		// Auto-create revision if enough time has passed
		if (DateTime.Now - _lastRevisionTime >= _revisionInterval)
		{
			CreateRevision(content);
			_lastRevisionTime = DateTime.Now;
		}
	}

	/// <summary>
	/// Undo the last edit. Returns the previous state, or null if nothing to undo.
	/// </summary>
	public (string Content, int CursorPosition)? Undo()
	{
		if (_undoStack.Count == 0) return null;

		var current = _undoStack[_undoStack.Count - 1];
		_undoStack.RemoveAt(_undoStack.Count - 1);
		_redoStack.Add(current);

		if (_undoStack.Count == 0) return null;

		var previous = _undoStack[_undoStack.Count - 1];
		return (previous.Content, previous.CursorPosition);
	}

	/// <summary>
	/// Redo the last undone edit. Returns the next state, or null if nothing to redo.
	/// </summary>
	public (string Content, int CursorPosition)? Redo()
	{
		if (_redoStack.Count == 0) return null;

		var state = _redoStack[_redoStack.Count - 1];
		_redoStack.RemoveAt(_redoStack.Count - 1);
		_undoStack.Add(state);

		return (state.Content, state.CursorPosition);
	}

	/// <summary>
	/// Manually create a revision snapshot at the current moment.
	/// </summary>
	public void CreateRevision(string content)
	{
		var revision = new HistoryState
		{
			Content = content,
			CursorPosition = 0, // Revisions don't track cursor
			Timestamp = DateTime.Now
		};

		_revisions.Add(revision);

		// Trim revisions if exceeds max
		if (_revisions.Count > _maxRevisions)
		{
			_revisions.RemoveAt(0);
		}
	}

	/// <summary>
	/// Get all revisions, ordered by timestamp (oldest first).
	/// Returns tuples of (timestamp, content).
	/// </summary>
	public List<(DateTime Timestamp, string Content)> GetRevisions()
	{
		return _revisions
			.Select(r => (r.Timestamp, r.Content))
			.ToList();
	}

	/// <summary>
	/// Get a specific revision by index. Returns null if index is out of range.
	/// </summary>
	public string? GetRevision(int index)
	{
		if (index < 0 || index >= _revisions.Count) return null;
		return _revisions[index].Content;
	}

	/// <summary>
	/// Clear all undo/redo history and revisions.
	/// Use this when starting a new note or closing the editor.
	/// </summary>
	public void Clear()
	{
		_undoStack.Clear();
		_redoStack.Clear();
		_revisions.Clear();
		_lastRevisionTime = DateTime.Now;
	}

	/// <summary>
	/// Get a formatted string of the undo/redo stack sizes for debugging.
	/// </summary>
	public string GetDebugInfo()
	{
		return $"Undo: {UndoCount}, Redo: {RedoCount}, Revisions: {RevisionCount}";
	}
}