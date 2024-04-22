using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApSafeFuzz.Models;

public class ClusterConfigurationModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? IpAddress { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}