using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Messenger.Domain.Entities;

namespace Messenger.Infrastructure.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships", t =>
        {
            t.HasCheckConstraint(
                "CK_friendships_user_order",
                "\"UserId\" < \"FriendId\"");
        });

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedOnAdd();

        builder.Property(f => f.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(f => new
        {
            f.UserId,
            f.FriendId
        })
        .IsUnique();

        // Map Relationships
        builder.HasOne(f => f.User)
            .WithMany(u => u.FriendshipsAsUser) // 👈 Map với User.FriendshipsAsUser
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Friend)
            .WithMany(u => u.FriendshipsAsFriend) // 👈 Map với User.FriendshipsAsFriend
            .HasForeignKey(f => f.FriendId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}