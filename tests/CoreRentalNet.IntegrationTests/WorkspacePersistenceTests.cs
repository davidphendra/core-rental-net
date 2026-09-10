using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Commands;
using CoreRentalNet.Modules.Workspace.Domain;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using WorkspaceDraft = CoreRentalNet.Modules.Workspace.Domain.Workspace;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

public sealed class WorkspacePersistenceTests
{
    private const string RawToken = "integration-raw-token-0123456789";

    [Fact]
    public async Task A_draft_survives_a_round_trip_through_the_file()
    {
        await using var database = new SqliteTestDatabase();

        await using (var write = await database.CreateMigratedContextAsync())
        {
            var repository = new WorkspaceRepository(write);
            var draft = WorkspaceDraft.CreateNew(WorkspaceId.New(), DraftToken.FromRawToken(RawToken).Hash);
            draft.Assign(SlotId.Chair, "CHA449AGLBB0");
            draft.Assign(SlotId.Monitor, "MONJVAP81NPQ", 2);
            draft.SetDeliveryAddress("Villa Lotus, Canggu");

            await repository.AddAsync(draft);
            await repository.SaveChangesAsync();
        }

        await using var read = await database.CreateMigratedContextAsync();
        var loaded = await new WorkspaceRepository(read).FindByTokenAsync(DraftToken.FromRawToken(RawToken));

        loaded.Should().NotBeNull();
        loaded!.AssignmentFor(SlotId.Chair)!.Sku.Should().Be("CHA449AGLBB0");
        loaded.AssignmentFor(SlotId.Monitor)!.Quantity.Should().Be(2);
        loaded.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
        loaded.TotalUnits.Should().Be(3);
        loaded.State.Should().Be(DraftState.Draft);
    }

    [Fact] // DR-05
    public async Task Only_the_hash_is_stored_never_the_raw_token()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedContextAsync();

        var repository = new WorkspaceRepository(context);
        await repository.AddAsync(WorkspaceDraft.CreateNew(WorkspaceId.New(), DraftToken.FromRawToken(RawToken).Hash));
        await repository.SaveChangesAsync();

        var storedHash = await context.Database
            .SqlQueryRaw<string>("SELECT DraftTokenHash AS Value FROM Workspace_Draft")
            .ToListAsync();

        storedHash.Should().ContainSingle();
        storedHash[0].Should().Be(DraftToken.HashOf(RawToken));
        storedHash[0].Should().NotContain(RawToken);
    }

    [Fact] // DR-05
    public async Task Two_drafts_cannot_share_a_token()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedContextAsync();
        var repository = new WorkspaceRepository(context);

        await repository.AddAsync(WorkspaceDraft.CreateNew(WorkspaceId.New(), DraftToken.FromRawToken(RawToken).Hash));
        await repository.SaveChangesAsync();

        await repository.AddAsync(WorkspaceDraft.CreateNew(WorkspaceId.New(), DraftToken.FromRawToken(RawToken).Hash));

        var action = async () => await repository.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // WS-14
    public async Task The_draft_and_its_assignments_have_no_column_that_could_hold_a_price()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedContextAsync();

        var columns = new List<string>();

        foreach (var table in new[] { "Workspace_Draft", "Workspace_SlotAssignment" })
        {
            var names = await context.Database
                .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info({0})", table)
                .ToListAsync();

            names.Should().NotBeEmpty($"table {table} must exist");
            columns.AddRange(names);
        }

        foreach (var forbidden in new[] { "price", "amount", "total", "cost" })
        {
            columns.Should().NotContain(
                column => column.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                $"a draft column named like '{forbidden}' would mean a price is being stored (ADR-0006)");
        }
    }

    [Fact] // ARC-03
    public async Task Every_table_belongs_to_this_module()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedContextAsync();

        var allTables = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync();

        // Only EF Core's own bookkeeping is exempt; every application table must be prefixed.
        var tables = allTables
            .Where(table => !table.StartsWith("sqlite_", StringComparison.Ordinal))
            .Where(table => !table.StartsWith("__EF", StringComparison.Ordinal))
            .ToArray();

        tables.Should().NotBeEmpty();
        tables.Should().OnlyContain(table => table.StartsWith("Workspace_", StringComparison.Ordinal));
    }

    [Fact] // DR-06
    public async Task Two_contexts_editing_the_same_draft_produce_a_conflict_rather_than_a_lost_update()
    {
        await using var database = new SqliteTestDatabase();
        var id = WorkspaceId.New();

        await using (var seed = await database.CreateMigratedContextAsync())
        {
            var repository = new WorkspaceRepository(seed);
            await repository.AddAsync(WorkspaceDraft.CreateNew(id, DraftToken.FromRawToken(RawToken).Hash));
            await repository.SaveChangesAsync();
        }

        await using var first = await database.CreateMigratedContextAsync();
        await using var second = await database.CreateMigratedContextAsync();

        var token = DraftToken.FromRawToken(RawToken);
        var firstDraft = await new WorkspaceRepository(first).FindByTokenAsync(token);
        var secondDraft = await new WorkspaceRepository(second).FindByTokenAsync(token);

        firstDraft!.Assign(SlotId.Chair, "CHA449AGLBB0");
        await first.SaveChangesAsync();

        secondDraft!.Assign(SlotId.Monitor, "MONJVAP81NPQ");

        var action = async () => await second.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateConcurrencyException>();

        await using var verify = database.CreateContext();
        var winner = await new WorkspaceRepository(verify).FindByTokenAsync(token);
        winner!.AssignmentFor(SlotId.Chair).Should().NotBeNull("the first writer's change must be the one that survived");
        winner.AssignmentFor(SlotId.Monitor).Should().BeNull("the second writer must not have silently overwritten it");
    }

    [Fact]
    public async Task The_version_advances_on_every_saved_mutation()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedContextAsync();
        var repository = new WorkspaceRepository(context);
        var draft = WorkspaceDraft.CreateNew(WorkspaceId.New(), DraftToken.FromRawToken(RawToken).Hash);
        await repository.AddAsync(draft);
        await repository.SaveChangesAsync();
        var start = draft.Version;

        draft.Assign(SlotId.Chair, "CHA449AGLBB0");
        await repository.SaveChangesAsync();

        draft.Version.Should().BeGreaterThan(start);
        var stored = await new WorkspaceRepository(context).FindByTokenAsync(DraftToken.FromRawToken(RawToken));
        stored!.Version.Should().Be(draft.Version);
    }

    [Fact]
    public async Task A_converted_draft_is_still_readable_by_token()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedContextAsync();
        var repository = new WorkspaceRepository(context);
        var draft = WorkspaceDraft.CreateNew(WorkspaceId.New(), DraftToken.FromRawToken(RawToken).Hash);
        draft.Assign(SlotId.Chair, "CHA449AGLBB0");
        await repository.AddAsync(draft);
        await repository.SaveChangesAsync();

        draft.MarkConverted();
        await repository.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var stored = await new WorkspaceRepository(context).FindByTokenAsync(DraftToken.FromRawToken(RawToken));
        stored!.State.Should().Be(DraftState.Converted);
        stored.AssignmentFor(SlotId.Chair).Should().NotBeNull();
    }
}
