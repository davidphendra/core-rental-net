namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>Where a test hears about the requests the stand-in was sent.</summary>
/// <remarks>
/// One reason, and it is a specific one: the application never sets a response format of its own - the hosted
/// agent owns its own schema - so the only way to hold the <em>client's</em> translation of that schema
/// against the wire is to look at what it sent. A null callback is the process case, which has nobody to
/// tell.
/// </remarks>
internal sealed record RequestObserver(Action<string>? OnRequest);
