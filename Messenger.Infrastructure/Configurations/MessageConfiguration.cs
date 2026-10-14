using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Messenger.Domain.Entities;

namespace Messenger.Infrastructure.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.MessageType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Map Relationships
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages) // 👈 Map với Conversation.Messages
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.Messages) // 👈 Map với User.Messages
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new
        {
            m.ConversationId,
            m.CreatedAt
        })
        .IsDescending(false, true)
        .HasDatabaseName("IX_messages_conversation_created_at");
    }
}