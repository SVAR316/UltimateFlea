using SPTarkov.Server.Core.Models.Common;

namespace UltimateFlea.Pricing;

// База до симуляции. Пока local, потом можно прикрутить Tarkov.dev.
public interface IPriceSource
{
    string Id { get; }
    void Refresh();
    double? GetBasePrice(MongoId tpl);
}
