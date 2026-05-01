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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VpnNode>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.InterfaceName).HasMaxLength(50).IsRequired();
                b.Property(x => x.ServerAddressCidr).HasMaxLength(100).IsRequired();
                b.Property(x => x.AgentToken).HasMaxLength(200).IsRequired();
            });

            modelBuilder.Entity<VpnPeer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.PublicKey).HasColumnType("text").IsRequired();
                b.Property(x => x.AssignedIp).HasMaxLength(100).IsRequired();
                b.Property(x => x.ClientConfig).HasColumnType("text");

                b.HasOne(x => x.VpnNode)
                    .WithMany()
                    .HasForeignKey(x => x.VpnNodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SyncJob>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Status).HasMaxLength(50).IsRequired();
                b.Property(x => x.JobType).HasMaxLength(100).IsRequired();
                b.Property(x => x.PayloadJson).HasColumnType("text");
                b.Property(x => x.ResultMessage).HasColumnType("text");

                b.HasOne<VpnNode>()
                    .WithMany()
                    .HasForeignKey(x => x.VpnNodeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
