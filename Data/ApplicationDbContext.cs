using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FindIt.Models;

namespace FindIt.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Dispute> Disputes => Set<Dispute>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Item relationships
        builder.Entity<Item>()
            .HasOne(i => i.User)
            .WithMany(u => u.ReportedItems)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Item>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Claim relationships
        builder.Entity<Claim>()
            .HasOne(c => c.Item)
            .WithMany(i => i.Claims)
            .HasForeignKey(c => c.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Claim>()
            .HasOne(c => c.Claimant)
            .WithMany(u => u.SubmittedClaims)
            .HasForeignKey(c => c.ClaimantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Message relationships
        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasOne(m => m.Item)
            .WithMany(i => i.Messages)
            .HasForeignKey(m => m.ItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Dispute relationships
        builder.Entity<Dispute>()
            .HasOne(d => d.Claim)
            .WithOne(c => c.Dispute)
            .HasForeignKey<Dispute>(d => d.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
