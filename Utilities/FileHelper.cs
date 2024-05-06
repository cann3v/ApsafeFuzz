namespace ApSafeFuzz.Utilities;

public static class FileHelper
{
    public static string GetExtension(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        char extensionSeparator = '.';

        int extensionIndex = fileName.LastIndexOf(extensionSeparator);

        if (extensionIndex == -1 || extensionIndex == fileName.Length - 1)
        {
            return string.Empty;
        }

        return fileName.Substring(extensionIndex);
    }
}