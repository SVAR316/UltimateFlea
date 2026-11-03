using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Common;

namespace UltimateFlea.Util;

/// <summary>
/// Shared TPL/ID parsing for everything that carries raw ids (configs, remote feeds, caches).
/// One warning format instead of a copy of the parse-and-warn block per call site.
/// </summary>
public static class MongoIds
{
    /// <summary>
    /// Parses <paramref name="raw"/> into a <see cref="MongoId"/>, warning once when it is not a
    /// valid id. <paramref name="context"/> names the source for the log line, e.g. "pool config".
    /// </summary>
    public static bool TryParse<TOwner>(string? raw, ISptLogger<TOwner> logger, string context, out MongoId id)
    {
        if (string.IsNullOrWhiteSpace(raw) || !MongoId.IsValidMongoId(raw))
        {
            logger.Warning($"[UltimateFlea] Ignoring invalid ID in {context}: '{raw}'");
            id = default;
            return false;
        }

        id = new MongoId(raw);
        return true;
    }
}
