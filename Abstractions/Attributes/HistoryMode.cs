using System;

namespace Abstractions.Attributes;

/// <summary>
/// Defines the history behavior for composite states
/// </summary>
public enum HistoryMode
{
    /// <summary>
    /// No history - always enter through initial substate
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Shallow history - remember only the direct child state
    /// </summary>
    Shallow = 1,
    
    /// <summary>
    /// Deep history - remember the full nested state path
    /// </summary>
    Deep = 2
}