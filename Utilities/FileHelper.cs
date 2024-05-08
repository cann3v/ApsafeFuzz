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

    /// <summary>
    /// Метод для удаления файла.
    /// </summary>
    /// <param name="filePath">Путь к файлу, который необходимо удалить.</param>
    /// <returns>
    /// 0 - если файл успешно удален.
    /// 1 - если файл не существует.
    /// 2 - если произошла другая ошибка при удалении файла.
    /// </returns>
    public static int DeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return 0;
            }
            else
            {
                return 1;
            }
        }
        catch (Exception ex)
        {
            return 2;
        }
    }
}