using ApSafeFuzz.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApSafeFuzz.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<ClusterConfigurationModel> ClusterConfiguration { get; set; } 
    public DbSet<UploadFileSettingsModel> UploadFileSettings { get; set; }
}
