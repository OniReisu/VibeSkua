namespace Skua.Core.Interfaces;

public interface IOption
{
    string Category { get; set; }
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }
    object? DefaultValue { get; }
    bool Transient { get; }
    Type Type { get; }

    /// <summary>
    /// Optional fixed list of allowed values. When non-empty the options dialog shows a dropdown
    /// instead of a text box (enum options already get one). Scripts can build this list at load time.
    /// </summary>
    IReadOnlyList<string>? Choices => null;
}