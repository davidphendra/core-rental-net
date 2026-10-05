namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>One run's out-of-band facts, carried from the endpoint into the invocation pipeline.</summary>
/// <remarks>
/// A record rather than a bare string so the run's facts travel together: the token is what the catalogue is
/// reached with today, and a second fact would join it here rather than become a second <c>AsyncLocal</c>.
/// </remarks>
public sealed record RunContext(string AccessToken);
