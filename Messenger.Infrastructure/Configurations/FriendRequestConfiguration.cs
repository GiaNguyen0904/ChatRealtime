using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Messenger.Domain.Entities;

namespace Messenger.Infrastructure.Configurations;

public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("friend_requests", t =>
        {
            t.HasCheckConstraint(
                "CK_friend_requests_different_users",
                "\"SenderId\" <> \"ReceiverId\"");
        });

        builder.HasKey(fr => fr.Id);

        builder.Property(fr => fr.Id)
            .ValueGeneratedOnAdd();

        builder.Property(fr => fr.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(fr => fr.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(fr => fr.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Map Relationships
        builder.HasOne(fr => fr.Sender)
            .WithMany(u => u.SentFriendRequests) // 👈 Map với User.SentFriendRequests
            .HasForeignKey(fr => fr.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fr => fr.Receiver)
            .WithMany(u => u.ReceivedFriendRequests) // 👈 Map với User.ReceivedFriendRequests
            .HasForeignKey(fr => fr.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(fr => new
        {
            fr.SenderId,
            fr.ReceiverId
        })
        .HasDatabaseName("UX_friend_requests_pending")
        .IsUnique()
        .HasFilter("\"Status\" = 'PENDING'");
    }
}