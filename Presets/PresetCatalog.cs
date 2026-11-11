using System.Text.Json;

namespace UltimateFlea.Presets;

/// <summary>
/// Built-in presets. The JSON is embedded instead of shipped as files under config/: a preset
/// is part of the mod's code, not user data, so it can never be missing, stale or half-deployed.
/// </summary>
/// <remarks>
/// Built once from a static initialiser and never mutated afterwards, so Kestrel threads can read
/// it without any locking. Category ids are the item-tree parents (items.json "_parent"), the same
/// ids PoolManager and EconomyEngine match on, not handbook category ids.
/// A preset replaces each config it targets as a whole, except those listed in Partial (merged key by key);
/// configs it does not target are untouched.
/// </remarks>
public static class PresetCatalog
{
    public const string ModdedItemsPresetId = "modded_items_flea";

    public static IReadOnlyList<PresetDefinition> All { get; } = Build();

    public static PresetDefinition? Find(string id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    private static JsonElement Json(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.Clone();
    }

    private static PresetDefinition Static(
        string id,
        LocalizedText name,
        LocalizedText description,
        string[] tags,
        Dictionary<string, string> configs) =>
        new(id, name, description, tags, [], PresetKind.Static,
            configs.ToDictionary(kv => kv.Key, kv => Json(kv.Value), StringComparer.Ordinal));

    private const string NeutralWipe = """{"enabled":false,"startLengthDays":14,"startMultiplier":1.0}""";

    private static IReadOnlyList<PresetDefinition> Build() =>
    [
        Static("vanilla_flat",
            new("Vanilla / flat", "Ванильный / плоский"),
            new("Prices stay near the base values and the market simulation is almost frozen. Resets the pool and pricing configs.",
                "Цены держатся около базовых, симуляция рынка почти заморожена. Сбрасывает настройки пула и цен."),
            ["market"],
            new()
            {
                ["pool"] = """{"mode":"blacklist","add":[],"remove":[],"addCategories":[],"removeCategories":[]}""",
                ["pricing"] = """{"globalMultiplier":1.0,"fixedPrices":{},"itemMultipliers":{},"categoryMultipliers":{}}""",
                ["economy"] = $$"""{"simIntervalMinutes":120,"demandPerBuy":0.0,"supplyPerSell":0.0,"priceElasticity":0.0,"decayPerTick":0.2,"settleSpeed":0.5,"noise":0.0,"minPriceFactor":0.95,"maxPriceFactor":1.05,"wipe":{{NeutralWipe}}}""",
            }),

        Static("casual_cheap",
            new("Casual / cheap flea", "Казуальный / дешёвая барахолка"),
            new("Every item stays available and prices are about a quarter lower, with a gentle market.",
                "Доступны все предметы, цены примерно на четверть ниже базовых, рынок спокойный."),
            ["market", "pricing"],
            new()
            {
                ["pricing"] = """{"globalMultiplier":0.75,"fixedPrices":{},"itemMultipliers":{},"categoryMultipliers":{}}""",
                ["economy"] = $$"""{"simIntervalMinutes":60,"demandPerBuy":0.02,"supplyPerSell":0.03,"priceElasticity":0.2,"decayPerTick":0.12,"settleSpeed":0.15,"noise":0.02,"minPriceFactor":0.6,"maxPriceFactor":1.4,"wipe":{{NeutralWipe}}}""",
            }),

        // Removes keycards, armor plates, sniper rifles, machine guns and special items.
        Static("hardcore_scarce",
            new("Hardcore / scarce", "Хардкор / дефицит"),
            new("Removes keycards, armor plates, sniper rifles, machine guns and special items from the flea, raises prices and adds a strong early-wipe premium (works best with the local price source).",
                "Убирает с барахолки ключ-карты, бронеплиты, снайперские винтовки, пулемёты и спецпредметы, поднимает цены и добавляет сильную наценку в начале вайпа (лучше с локальным источником цен)."),
            ["market", "pool", "pricing"],
            new()
            {
                ["pool"] = """{"mode":"blacklist","add":[],"remove":[],"addCategories":[],"removeCategories":["5c164d2286f774194c5e69fa","644120aa86ffbe10ee032b6f","5447b6254bdc2dc3278b4568","5447bed64bdc2d97278b4568","5447e0e74bdc2d3c308b4567"]}""",
                ["pricing"] = """{"globalMultiplier":1.35,"fixedPrices":{},"itemMultipliers":{},"categoryMultipliers":{"5485a8684bdc2da71d8b4567":1.5,"543be5cb4bdc2deb348b4568":1.5,"5448e54d4bdc2dcc718b4568":1.4,"5448f3a64bdc2d60728b456a":1.5,"5448f3ac4bdc2dce718b4569":1.3,"5448f39d4bdc2d0a728b4568":1.3,"5c99f98d86f7745c314214b3":1.5}}""",
                ["economy"] = """{"simIntervalMinutes":60,"demandPerBuy":0.05,"supplyPerSell":0.02,"priceElasticity":0.5,"decayPerTick":0.05,"settleSpeed":0.08,"noise":0.04,"minPriceFactor":0.9,"maxPriceFactor":3.0,"wipe":{"enabled":true,"startLengthDays":21,"startMultiplier":2.5}}""",
            }),

        Static("volatile_market",
            new("Volatile market", "Волатильный рынок"),
            new("Prices react hard to trading and noise, and swing quickly.",
                "Цены сильно реагируют на сделки и шум и быстро колеблются."),
            ["market"],
            new()
            {
                ["economy"] = """{"simIntervalMinutes":30,"demandPerBuy":0.08,"supplyPerSell":0.08,"priceElasticity":0.9,"decayPerTick":0.05,"settleSpeed":0.25,"noise":0.08,"minPriceFactor":0.4,"maxPriceFactor":3.5,"wipe":{"enabled":true,"startLengthDays":14,"startMultiplier":1.8}}""",
            }),

        Static("stable_market",
            new("Stable market", "Стабильный рынок"),
            new("Low reaction to trades and little noise; prices barely drift.",
                "Слабая реакция на сделки и почти нет шума; цены почти не дрейфуют."),
            ["market"],
            new()
            {
                ["economy"] = """{"simIntervalMinutes":120,"demandPerBuy":0.01,"supplyPerSell":0.01,"priceElasticity":0.1,"decayPerTick":0.15,"settleSpeed":0.2,"noise":0.01,"minPriceFactor":0.85,"maxPriceFactor":1.3,"wipe":{"enabled":true,"startLengthDays":14,"startMultiplier":1.3}}""",
            }),

        // maxPriceFactor is raised so the 3.0x bump is not clipped by the default 2.0 cap.
        Static("early_wipe",
            new("Early wipe", "Начало вайпа"),
            new("A long, strong price premium at the start of a wipe that fades over a month. Works best with the local price source.",
                "Долгая и сильная наценка в начале вайпа, спадающая за месяц. Лучше всего работает с локальным источником цен."),
            ["market"],
            new()
            {
                ["economy"] = """{"simIntervalMinutes":60,"demandPerBuy":0.03,"supplyPerSell":0.03,"priceElasticity":0.3,"decayPerTick":0.08,"settleSpeed":0.1,"noise":0.03,"minPriceFactor":0.7,"maxPriceFactor":4.0,"wipe":{"enabled":true,"startLengthDays":30,"startMultiplier":3.0}}""",
            }),

        Static("barter_friendly",
            new("Barter-friendly", "Выгодный лут"),
            new("Barter goods, keys and electronics are worth more; guns and ammo slightly less. Replaces the pricing config.",
                "Бартер, ключи и электроника ценятся выше, оружие и патроны чуть дешевле. Заменяет настройки цен."),
            ["pricing"],
            new()
            {
                ["pricing"] = """{"globalMultiplier":1.0,"fixedPrices":{},"itemMultipliers":{},"categoryMultipliers":{"57864a66245977548f04a81f":1.3,"57864a3d24597754843f8721":1.3,"57864c8c245977548867e7f1":1.25,"590c745b86f7743cc433c5f2":1.2,"5c99f98d86f7745c314214b3":1.25,"5485a8684bdc2da71d8b4567":0.9,"5447b5f14bdc2d61278b4567":0.9}}""",
            }),

        // Targets economy, events and trends only. Every multiplier is an estimate of a plausible wipe
        // shape; no real price history was available. Worst-case stack, checked by hand: keys at day 0
        // are 1.3 (wipe) x 1.5 (event) x 1.155 (two trends at peak) = 2.25, 3.26 with saturated
        // pressure (x1.45), below maxPriceFactor 3.5. Lowest stack is a pre-wipe sale on armor plates,
        // 0.75 x 0.95 x 0.55 = 0.39, above minPriceFactor 0.35. No category carries two events at once.
        // trends.enabled defaults to false in the model, so the JSON sets it true explicitly.
        Static("live_tarkov",
            new("Live Tarkov (wipe cycle)", "Живой Тарков (цикл вайпа)"),
            new("A 120-day wipe model with early-wipe shortages, a supply glut after the flea opens, mid-wipe ammo and armor inflation and a pre-wipe trader sale. The multipliers are estimates of a typical wipe shape, not real Tarkov price data, and no market history is replayed. Replaces your economy, events and trends configs; best with the local price source.",
                "Модель вайпа на 120 дней: дефицит в начале, избыток после открытия барахолки, рост цен на патроны и броню в середине и распродажа торговцев перед вайпом. Множители представляют собой оценку типичной формы вайпа, а не реальные цены Таркова, и история рынка не воспроизводится. Заменяет ваши настройки economy, events и trends; лучше с локальным источником цен."),
            ["market", "events", "trends"],
            new()
            {
                ["economy"] = """{"simIntervalMinutes":60,"demandPerBuy":0.03,"supplyPerSell":0.03,"priceElasticity":0.4,"decayPerTick":0.08,"settleSpeed":0.15,"noise":0.05,"minPriceFactor":0.35,"maxPriceFactor":3.5,"wipe":{"enabled":true,"startLengthDays":21,"startMultiplier":1.3}}""",
                ["events"] = """
                    {"enabled":true,"events":[
                    {"id":"early_hideout_barter","startDay":0,"endDay":14,"multiplier":1.35,"categories":["57864bb7245977548b3b66c2","57864ada245977548638de91","57864c322459775490116fbf","57864ee62459775490116fc1","57864e4c24597754843f8723"],"items":[]},
                    {"id":"hideout_barter_tail","startDay":14,"endDay":30,"multiplier":1.12,"categories":["57864bb7245977548b3b66c2","57864ada245977548638de91","57864c322459775490116fbf","57864ee62459775490116fc1","57864e4c24597754843f8723"],"items":[]},
                    {"id":"early_meds_shortage","startDay":0,"endDay":14,"multiplier":1.3,"categories":["5448f3ac4bdc2dce718b4569","5448f39d4bdc2d0a728b4568","5448f3a14bdc2d27728b4569","5448f3a64bdc2d60728b456a","57864c8c245977548867e7f1"],"items":[]},
                    {"id":"meds_tail","startDay":14,"endDay":35,"multiplier":1.1,"categories":["5448f3ac4bdc2dce718b4569","5448f39d4bdc2d0a728b4568","5448f3a14bdc2d27728b4569","5448f3a64bdc2d60728b456a"],"items":[]},
                    {"id":"early_food_drink","startDay":0,"endDay":10,"multiplier":1.2,"categories":["5448e8d04bdc2ddf718b4569","5448e8d64bdc2dce718b4568"],"items":[]},
                    {"id":"early_ammo_scarcity","startDay":0,"endDay":14,"multiplier":1.15,"categories":["5485a8684bdc2da71d8b4567","543be5cb4bdc2deb348b4568"],"items":[]},
                    {"id":"early_quest_keys","startDay":0,"endDay":10,"multiplier":1.5,"categories":["5c99f98d86f7745c314214b3"],"items":[]},
                    {"id":"quest_keys_tail","startDay":10,"endDay":30,"multiplier":1.2,"categories":["5c99f98d86f7745c314214b3"],"items":[]},
                    {"id":"flea_unlock_supply_flood","startDay":19,"endDay":40,"multiplier":0.92,"categories":["57864a66245977548f04a81f","57864a3d24597754843f8721","590c745b86f7743cc433c5f2","5448e8d04bdc2ddf718b4569","5448e8d64bdc2dce718b4568"],"items":[]},
                    {"id":"late_wipe_supply_glut","startDay":40,"endDay":105,"multiplier":0.88,"categories":["57864a66245977548f04a81f","57864a3d24597754843f8721","590c745b86f7743cc433c5f2","5448e8d04bdc2ddf718b4569","5448e8d64bdc2dce718b4568"],"items":[]},
                    {"id":"midwipe_ammo_armor_inflation","startDay":30,"endDay":105,"multiplier":1.12,"categories":["5485a8684bdc2da71d8b4567","644120aa86ffbe10ee032b6f"],"items":[]},
                    {"id":"content_patch_keys","startDay":45,"endDay":52,"multiplier":1.25,"categories":["5c99f98d86f7745c314214b3","5c164d2286f774194c5e69fa"],"items":[]},
                    {"id":"festive_event_gear","startDay":90,"endDay":104,"multiplier":1.15,"categories":["5a341c4086f77401f2541505","5a341c4686f77469e155819e"],"items":[]},
                    {"id":"pre_wipe_trader_sale","startDay":105,"endDay":120,"multiplier":0.75,"categories":["5485a8684bdc2da71d8b4567","543be5cb4bdc2deb348b4568","5448e54d4bdc2dcc718b4568","644120aa86ffbe10ee032b6f","5448e5284bdc2dcb718b4567","5448f3ac4bdc2dce718b4569","5448f39d4bdc2d0a728b4568","5448f3a64bdc2d60728b456a","5448e8d04bdc2ddf718b4569","5448e8d64bdc2dce718b4568"],"items":[]}
                    ]}
                    """,
                ["trends"] = """
                    {"enabled":true,"trends":[
                    {"category":"5485a8684bdc2da71d8b4567","periodDays":7,"amplitude":0.04,"phase":0.0},
                    {"category":"5448f3ac4bdc2dce718b4569","periodDays":7,"amplitude":0.04,"phase":1.0},
                    {"category":"5448f39d4bdc2d0a728b4568","periodDays":7,"amplitude":0.04,"phase":1.0},
                    {"category":"5c99f98d86f7745c314214b3","periodDays":7,"amplitude":0.05,"phase":2.0},
                    {"category":"5c99f98d86f7745c314214b3","periodDays":45,"amplitude":0.10,"phase":0.0},
                    {"category":"5c164d2286f774194c5e69fa","periodDays":45,"amplitude":0.10,"phase":0.0},
                    {"category":"57864a66245977548f04a81f","periodDays":30,"amplitude":0.06,"phase":0.5},
                    {"category":"57864a3d24597754843f8721","periodDays":30,"amplitude":0.06,"phase":0.5},
                    {"category":"590c745b86f7743cc433c5f2","periodDays":30,"amplitude":0.06,"phase":0.5},
                    {"category":"57864bb7245977548b3b66c2","periodDays":30,"amplitude":0.06,"phase":0.5},
                    {"category":"5448e54d4bdc2dcc718b4568","periodDays":30,"amplitude":0.05,"phase":3.0},
                    {"category":"644120aa86ffbe10ee032b6f","periodDays":30,"amplitude":0.05,"phase":3.0}
                    ]}
                    """,
            }),

        // mod is partial: only the four keys below are written, so enabled / pvePrices / quickListFromStash /
        // tarkovMarketApiKey etc. keep their values. Applying also runs ResetService (price cache + state).
        new("live_clean",
            new("Live tarkov.dev prices (clean)", "Чистый live tarkov.dev"),
            new("Switches the flea to live tarkov.dev prices from a clean state. Overwrites your pool, pricing, economy, events and trends configs (pool reset to defaults, no multipliers, early-wipe bump off, events and trends off) and changes four keys in mod: liveFlea, priceSource, preserveBasePrices, offerSpread; other mod keys, including pvePrices, are left as they are. Also clears the price cache and economy state and refetches live prices, which can take a while. Each offer is spread around the live reference price using that item's own 24h low/high from tarkov.dev (offerSpread \"live\"); items without that data use SPT's normal 0.8-1.2 spread. Set offerSpread to \"exact\" yourself if you want every offer at exactly the reference price.",
                "Переключает барахолку на живые цены tarkov.dev с чистого состояния. Перезаписывает ваши конфиги pool, pricing, economy, events и trends (пул по умолчанию, без множителей, наценка начала вайпа выключена, события и тренды выключены) и меняет четыре ключа в mod: liveFlea, priceSource, preserveBasePrices, offerSpread; остальные ключи mod, включая pvePrices, не трогает. Также очищает кэш цен и состояние экономики и заново загружает живые цены, это может занять время. Разброс цен лотов берётся для каждого предмета из его собственных минимума и максимума за 24 часа с tarkov.dev (offerSpread \"live\"); для предметов без этих данных действует обычный разброс SPT 0.8-1.2. Если нужны лоты ровно по справочной цене, сами поставьте offerSpread в \"exact\"."),
            ["market", "pool", "pricing", "events", "trends", "mod"],
            [],
            PresetKind.Static,
            new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["mod"] = Json("""{"liveFlea":true,"priceSource":"tarkovdev","preserveBasePrices":true,"offerSpread":"live"}"""),
                ["pool"] = Json("""{"mode":"blacklist","add":[],"remove":[],"addCategories":[],"removeCategories":[],"nestedCategories":true,"useLiveBlacklist":false}"""),
                ["pricing"] = Json("""{"globalMultiplier":1.0,"fixedPrices":{},"itemMultipliers":{},"categoryMultipliers":{}}"""),
                ["economy"] = Json("""{"simIntervalMinutes":60,"demandPerBuy":0.03,"supplyPerSell":0.03,"priceElasticity":0.3,"decayPerTick":0.08,"settleSpeed":0.1,"noise":0.03,"minPriceFactor":0.7,"maxPriceFactor":2.0,"wipe":{"enabled":false,"startLengthDays":14,"startMultiplier":1.8,"forceWithLivePrices":false}}"""),
                ["events"] = Json("""{"enabled":false,"events":[]}"""),
                ["trends"] = Json("""{"enabled":false,"trends":[]}"""),
            })
        {
            Partial = ["mod"],
            ResetsData = true,
        },

        // No ids on purpose: item ids are specific to each mod release, so the TPL set is computed
        // from the live server when the preset is shown or applied (see ModdedItemsDetector).
        new(ModdedItemsPresetId,
            new("Modded items on the flea", "Предметы из модов на барахолке"),
            new("Forces every tradable item added by your installed mods onto the flea, including items the mod itself marked as not sellable there. Quest items, containers, unsaleable items and items without a price are skipped. The items are added to your current pool config and nothing is removed.",
                "Принудительно добавляет на барахолку все торгуемые предметы, которые добавили установленные у вас моды, в том числе те, что сам мод пометил как непродаваемые на ней. Квестовые предметы, контейнеры, непродаваемые предметы и предметы без цены пропускаются. Предметы добавляются в ваш текущий пул, ничего не удаляется."),
            ["pool", "mods"],
            [],
            PresetKind.Dynamic,
            null)
        {
            Targets = ["pool"],
        },
    ];
}
