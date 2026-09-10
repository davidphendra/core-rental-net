using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

public sealed class SqliteDatabaseTests
{
    [Fact]
    public async Task Opening_a_database_applies_write_ahead_logging_and_a_busy_timeout()
    {
        await using var database = new SqliteTestDatabase();

        using var connection = SqliteDatabase.Open(database.Settings);

        SqliteDatabase.ReadJournalMode(connection).Should().Be("WAL");
        SqliteDatabase.ReadBusyTimeoutMilliseconds(connection).Should().Be(30000);
    }

    [Fact]
    public async Task Write_ahead_logging_is_a_property_of_the_file_not_of_the_connection()
    {
        await using var database = new SqliteTestDatabase();

        using var first = SqliteDatabase.Open(database.Settings);
        SqliteDatabase.ReadJournalMode(first).Should().Be("WAL");

        // A later connection that never sets the pragma still sees WAL, which is why the
        // journal mode can be chosen once at startup and the busy timeout cannot.
        using var second = new Microsoft.Data.Sqlite.SqliteConnection(database.Settings.ConnectionString);
        second.Open();
        SqliteDatabase.ReadJournalMode(second).Should().Be("WAL");
    }

    [Fact]
    public async Task A_rollback_journal_can_be_selected_instead()
    {
        await using var database = new SqliteTestDatabase();
        var settings = new SqliteDatabaseSettings(database.Settings.DatabasePath, "DELETE", 5);

        using var connection = SqliteDatabase.Open(settings);

        SqliteDatabase.ReadJournalMode(connection).Should().Be("DELETE");
        SqliteDatabase.ReadBusyTimeoutMilliseconds(connection).Should().Be(5000);
    }

    [Fact]
    public void An_unknown_journal_mode_is_refused()
    {
        var action = () => new SqliteDatabaseSettings("x.db", "TURBO");

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*journal mode*");
    }

    [Fact]
    public void A_negative_busy_timeout_is_refused()
    {
        var action = () => new SqliteDatabaseSettings("x.db", "WAL", -1);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public async Task The_directory_is_created_when_it_does_not_exist()
    {
        var nested = Path.Combine(Path.GetTempPath(), $"core-rental-nested-{Guid.NewGuid():N}", "deeper", "test.db");

        try
        {
            using var connection = SqliteDatabase.Open(new SqliteDatabaseSettings(nested));

            File.Exists(nested).Should().BeTrue();
        }
        finally
        {
            var root = Path.GetDirectoryName(Path.GetDirectoryName(nested));
            if (root is not null && Directory.Exists(root))
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void The_connection_string_enables_foreign_keys()
    {
        new SqliteDatabaseSettings("x.db").ConnectionString.Should().Contain("Foreign Keys=True");
    }
}
