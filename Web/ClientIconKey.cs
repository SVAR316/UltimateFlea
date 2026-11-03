namespace UltimateFlea.Web;

/// <summary>
/// Reproduces the key the EFT client uses in its icon cache index (IconsHash.GetItemHash for an
/// item with no child items). It is not a hash of a string: it hashes the MongoId's binary form.
/// </summary>
/// <remarks>
/// Deliberately free of any SPT type so it can be tested on its own. The formula is tied to the
/// current client build; if BSG changes the constants, fewer keys match and fewer icons show up,
/// which is harmless. Items that hold child items (weapons with mods, armor with plates, rigs with
/// contents) mix in per-instance data and cannot be derived from a template id; they are not handled.
/// </remarks>
public static class ClientIconKey
{
    /// <summary>Every index key the client could have written for this template id, plain state first.</summary>
    public static IEnumerable<int> Candidates(string tpl)
    {
        if (!TryBaseHash(tpl, out var h))
        {
            yield break;
        }

        // Plain item.
        yield return Key(h, 0);

        // Togglable (on/off), foldable (folded/unfolded), unused ammo, and magazines by visible rounds.
        // The state the icon was rendered in is unknown, so every plausible state is a candidate.
        for (uint on = 0; on <= 1; on++)
        {
            yield return Key(h, 23 + on);
        }

        for (uint folded = 0; folded <= 1; folded++)
        {
            yield return Key(h, (23 + folded) << 1);
        }

        for (uint rounds = 0; rounds < 200; rounds++)
        {
            yield return Key(h, (23 + rounds) << 2);
        }

        yield return Key(h, 27u * 56u); // ammo, not used
        yield return Key(h, 27u * 42u); // ammo, used
    }

    private static int Key(uint mongoHash, uint extras) => unchecked((int)(17u ^ mongoHash ^ extras));

    private static bool TryBaseHash(string tpl, out uint hash)
    {
        hash = 0;
        if (tpl is null || tpl.Length != 24)
        {
            return false;
        }

        if (!uint.TryParse(tpl.AsSpan(0, 8), System.Globalization.NumberStyles.AllowHexSpecifier, null, out var ts)
            || !ulong.TryParse(tpl.AsSpan(8), System.Globalization.NumberStyles.AllowHexSpecifier, null, out var counter))
        {
            return false;
        }

        unchecked
        {
            hash = ts ^ ((uint)(counter >> 32) * 3637u) ^ ((uint)(counter & 0xffffffffUL) * 5807u);
        }

        return true;
    }
}
