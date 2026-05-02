namespace Retro.Crt.Tests.Support;

/// <summary>
/// Tests that mutate <see cref="Console.Out"/>, environment variables, or
/// the cached <c>TerminalCapabilities</c> must serialise — they share
/// process-global state and would otherwise stomp on each other.
/// </summary>
[CollectionDefinition(Name)]
public sealed class EnvMutatingCollection
{
    public const string Name = "env-mutating";
}
