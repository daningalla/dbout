using DbOut.Options;

namespace DbOut.Continuation;

public record RestorePointParameters(string Hash, RuntimeOptions Parameters);