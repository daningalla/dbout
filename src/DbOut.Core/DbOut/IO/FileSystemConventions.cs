using DbOut.Options;

namespace DbOut.IO;

public static class FileSystemConventions
{
    public static IFileSystem CreateCacheFileSystem(IFileSystem fileSystem)
    {
        var root = fileSystem.Root;
        return root.CreateForChildPath(".cache", root.Compression);
    }

    public static IFileSystem CreateRestorePointFileSystem(IFileSystem fileSystem)
    {
        return fileSystem.Root.CreateForChildPath(".restore", FileCompression.None);
    }
}