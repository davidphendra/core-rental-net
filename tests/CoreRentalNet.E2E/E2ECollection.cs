using Xunit;

namespace CoreRentalNet.E2E;

[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<HostFixture>
{
    public const string Name = "e2e";
}
