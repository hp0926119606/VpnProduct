using Microsoft.EntityFrameworkCore;
using VpnProduct.Domain.Entities;

namespace VpnProduct.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<VpnNode> VpnNodes => Set<VpnNode>();
        public DbSet<VpnPeer> VpnPeers => Set<VpnPeer>();
        public DbSet<SyncJob> SyncJobs => Set<SyncJob>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<VpnNode>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.Property(x => x.InterfaceName).IsRequired().HasMaxLength(50);
                entity.Property(x => x.ServerAddressCidr).IsRequired().HasMaxLength(100);
                entity.Property(x => x.AgentToken).IsRequired().HasMaxLength(200);
            });

            builder.Entity<VpnPeer>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.Property(x => x.PublicKey).IsRequired();
                entity.Property(x => x.AssignedIp).IsRequired().HasMaxLength(100);
            });

            builder.Entity<SyncJob>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
                entity.Property(x => x.JobType).IsRequired().HasMaxLength(100);
            });
        }
    }
}
