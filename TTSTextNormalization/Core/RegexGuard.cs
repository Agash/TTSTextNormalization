namespace TTSTextNormalization.Core;

/// <summary>
/// Shared match-timeout budget for the rules' regular expressions.
/// </summary>
internal static class RegexGuard
{
    /// <summary>
    /// Milliseconds a single match may take before it is abandoned.
    /// </summary>
    /// <remarks>
    /// This is a backstop against catastrophic backtracking, not a performance budget. The rules use
    /// source-generated expressions over short strings, which match in microseconds, so the ceiling
    /// only matters for pathological input. It is measured in wall-clock time, though, so a tight
    /// value misfires whenever the machine is busy rather than the pattern being bad: the rules used
    /// to allow 100-200ms each and started abandoning ordinary input once three test frameworks ran
    /// at once under coverage instrumentation. A second is still far below any real runaway match
    /// and no longer reports load as a pattern failure.
    /// </remarks>
    internal const int TimeoutMilliseconds = 1000;
}
