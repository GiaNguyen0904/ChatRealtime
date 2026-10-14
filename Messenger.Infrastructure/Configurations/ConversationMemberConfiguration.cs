using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Messenger.Domain.Entities;

namespace Messenger.Infrastructure.Configurations;

public class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("conversation_members");

        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.Id)
            .ValueGeneratedOnAdd();

        builder.Property(cm => cm.Role)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(cm => cm.JoinedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(cm => cm.LeftAt);

        builder.Property(cm => cm.LastReadMessageId);

        builder.Property(cm => cm.Nickname)
            .HasMaxLength(50)
            .IsRequired(false);
            

        // Khóa ngoại trỏ đến bảng Messages (OnDelete SetNull để xóa message không bị lỗi hay mất member)
        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(cm => cm.LastReadMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Map Relationships
        builder.HasOne(cm => cm.Conversation)
            .WithMany(c => c.Members) // Map với Conversation.Members
            .HasForeignKey(cm => cm.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cm => cm.User)
            .WithMany(u => u.ConversationMemberships) //  Map với User.ConversationMemberships
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cm => new
        {
            cm.ConversationId,
            cm.UserId
        })
        .HasDatabaseName("UX_conversation_members_active")
        .IsUnique()
        .HasFilter("\"LeftAt\" IS NULL");
    }
}