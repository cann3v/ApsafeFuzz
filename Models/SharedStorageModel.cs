using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApSafeFuzz.Models;

public class SharedStorageModel
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; } = 0;
    public string IpAddress { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool? LastState { get; set; }
}