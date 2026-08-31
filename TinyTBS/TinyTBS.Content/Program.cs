namespace TinyTBS.Content;

internal static class Program
{
    private static int Main(string[] args)
    {
        var builder = new TinyTbsContentBuilder();
        builder.Run(args);
        return builder.FailedToBuild > 0 ? 1 : 0;
    }
}
