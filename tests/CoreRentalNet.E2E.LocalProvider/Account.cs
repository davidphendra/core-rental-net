namespace CoreRentalNet.E2E.LocalProvider;

internal sealed record Account(string Subject, string Name, string Email, string[] Permissions, string[] Roles);
