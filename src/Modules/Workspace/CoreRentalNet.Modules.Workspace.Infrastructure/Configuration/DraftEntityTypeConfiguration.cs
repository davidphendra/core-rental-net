using CoreRentalNet.Modules.Workspace.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Configuration;

internal sealed class DraftEntityTypeConfiguration : IEntityTypeConfiguration<Domain.Workspace>
{
    public void Configure(EntityTypeBuilder<Domain.Workspace> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Workspace_Draft");
        builder.HasKey(draft => draft.Id);
        builder.Property(draft => draft.Id)
            .HasConversion(id => id.Value, value => WorkspaceId.From(value))
            .ValueGeneratedNever();

        builder.Property(draft => draft.DraftTokenHash)
            .HasMaxLength(64)
            .IsRequired();

        // Lookup is always by token, and two drafts must never share one.
        builder.HasIndex(draft => draft.DraftTokenHash).IsUnique();

        builder.Property(draft => draft.State).HasConversion<int>().IsRequired();

        // The concurrency token, bumped by the aggregate on every mutation (ADR-0003).
        builder.Property(draft => draft.Version).IsConcurrencyToken().IsRequired();

        builder.Property(draft => draft.DeliveryAddress).HasMaxLength(200).IsRequired(false);

        builder.OwnsMany(draft => draft.Assignments, assignments =>
        {
            assignments.ToTable("Workspace_SlotAssignment");
            assignments.WithOwner().HasForeignKey("DraftId");
            assignments.HasKey("DraftId", nameof(SlotAssignment.Slot));
            assignments.Property(assignment => assignment.Slot).HasConversion<int>().IsRequired();
            assignments.Property(assignment => assignment.Sku).HasMaxLength(32).IsRequired();
            assignments.Property(assignment => assignment.Quantity).IsRequired();
        });

        builder.Metadata
            .FindNavigation(nameof(Domain.Workspace.Assignments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
