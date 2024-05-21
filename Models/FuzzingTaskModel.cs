using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApSafeFuzz.Models;

public class FuzzingTaskModel
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Fuzzer { get; set; }
    public int BuildId { get; set; }
    [ForeignKey("BuildId")]
    public UploadFileSettingsModel UploadFileSettingsModel { get; set; }
    public string? Environment { get; set; }
    public DateTime? CreateTime { get; set; }
    public string Status { get; set; } = "created";
}