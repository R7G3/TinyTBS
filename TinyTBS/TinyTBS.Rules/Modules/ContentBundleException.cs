namespace TinyTBS.Rules.Modules;

/// <summary>Thrown when a <c>*.bundle.json</c> preset cannot be loaded or validated.</summary>
public sealed class ContentBundleException : Exception
{
    public ContentBundleException(string message)
        : base(message)
    {
    }

    public ContentBundleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
