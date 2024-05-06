using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApSafeFuzz.Models;

public class UploadFileSettingsModel
{
    public const string UploadFile = "UploadFile";
    public string FilePath { get; set; } = Directory.GetCurrentDirectory();

    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? Owner { get; set; }
    public DateTime UploadTime { get; set; }
    public string UploadName { get; set; }
    public string InternalName { get; set; }
}
