using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using VpnProduct.Domain.Entities;

namespace VpnProduct.Infrastructure.Data;

public class ApplicationDbContext
    : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<VpnNode> VpnNodes => Set<VpnNode>();

    public DbSet<VpnPeer> VpnPeers => Set<VpnPeer>();

    public DbSet<SyncJob> SyncJobs => Set<SyncJob>();
public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<VpnNode>()
            .HasMany(x => x.Peers)
            .WithOne()
            .HasForeignKey(x => x.VpnNodeId);

        builder.Entity<VpnPeer>()
            .Property(x => x.Name)
            .HasMaxLength(100);

        builder.Entity<SyncJob>()
            .Property(x => x.Status)
            .HasMaxLength(50);

builder.Entity<Subscription>()
    .Property(x => x.UserEmail)
    .HasMaxLength(200);
    }
}