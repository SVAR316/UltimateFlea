namespace UltimateFlea.Web;

/// <summary>
/// Presentation DTOs for the dashboard home page. These used to be anonymous objects
/// built inside EconomyEngine; keeping them here makes the wire contract explicit and
/// keeps presentation concerns out of the simulation.
/// </summary>
public sealed record ChartPayload(
    ChartSummary Summary,
    TradesChart Trades,
    FactorChart Factors,
    IReadOnlyList<HotItemRow> TopDemand,
    IReadOnlyList<HotItemRow> TopSupply,
    IReadOnlyList<CategoryRow> Categories,
    MoversChart Movers,
    IReadOnlyList<PressurePoint> Pressure,
    WipeChart Wipe,
    EventsChart Events);

public sealed record ChartSummary(
    int ItemCount,
    int FixedCount,
    int FloatingCount,
    double? AvgFactor,
    int AboveBase,
    int BelowBase,
    int AtBase);

public sealed record TradesChart(
    int WindowDays,
    int TotalBuys,
    int TotalSells,
    IReadOnlyList<TradeDayRow> Days);

public sealed record TradeDayRow(string Day, int Buys, int Sells);

public sealed record FactorChart(
    IReadOnlyList<string> Labels,
    IReadOnlyList<int> Counts);

public sealed record HotItemRow(
    string Tpl,
    string ShortTpl,
    string Name,
    double Value,
    double Factor);

public sealed record CategoryRow(
    string Id,
    string Name,
    int Items,
    double AvgFactor,
    double AvgDemand,
    double AvgSupply);

public sealed record MoversChart(
    IReadOnlyList<MoverRow> Up,
    IReadOnlyList<MoverRow> Down);

public sealed record MoverRow(
    string Tpl,
    string ShortTpl,
    string Name,
    double Factor,
    double BasePrice,
    double CurrentPrice);

public sealed record PressurePoint(double X, double Y);

public sealed record WipeChart(
    bool Enabled,
    double? AgeDays,
    double Multiplier,
    double StartLengthDays,
    double StartMultiplier,
    IReadOnlyList<WipePoint> Curve);

public sealed record WipePoint(double Day, double Multiplier);

public sealed record EventsChart(
    bool Enabled,
    double? AgeDays,
    double WindowDays,
    IReadOnlyList<EventRow> Items);

public sealed record EventRow(
    string Id,
    string Label,
    double StartDay,
    double EndDay,
    double Multiplier,
    bool Active);
