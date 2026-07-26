using SPTarkov.Server.Core.Models.Common;

namespace UltimateFlea.Pricing;

// База до симуляции: local или tarkovdev.
public interface IPriceSource
{
    string Id { get; }
    void Refresh();
    double? GetBasePrice(MongoId tpl);
}
