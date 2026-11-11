# UltimateFlea

Flea market mod for SPT 4.1. It lets you manage the item pool, set prices, and simulate supply, demand, early-wipe pricing, market events, and category trends.

## Requirements

- SPT ~4.1
- .NET 10 SDK if you want to build it yourself

## Web UI

After the SPT server starts, open the dashboard in a browser (**https**, not http: SPT uses a self-signed cert, so accept the browser warning):

- **https://127.0.0.1:6969/ultimateflea/app.html**
- Fallback: `https://127.0.0.1:6969/ultimateflea` (Blazor host → same UI)
- Also listed on the SPT landing page card (like QuestCodex)

`http://127.0.0.1:6969/...` → `ERR_EMPTY_RESPONSE` (SPT speaks HTTPS only).

Edit `mod` / `pool` / `pricing` / `economy` / `events` / `trends` / `levels`, toggle stash quick-list, and view economy overview + `economy_state.json`. Pool/pricing/economy may need a server restart for a full rebuild.

## Build

1. Open `UltimateFlea.csproj` in Visual Studio or Rider
2. Build the project in Release mode
3. Copy `bin\Release\UltimateFlea\` to `SPT\user\mods\`
4. Build `client/UltimateFlea.Client` and copy the DLL to `BepInEx\plugins\UltimateFlea\`
5. Start the server

Config files are stored in `config\` next to the DLL. Economy state is saved to `data\economy_state.json`.

## Building a release

From the repo root: `powershell -File tools/package.ps1` (optional `-TarkovDir "D:\SPT"`, default `C:\SPT`, needed for the client build).

It builds the server mod and the client plugin in Release and writes `dist/UltimateFlea-<version>.zip` (version read from `UltimateFlea.csproj`). If the client build fails the script warns and produces a server-only zip. Extract the zip into the SPT game folder root: the mod lands in `SPT_Runtime/user/mods/UltimateFlea/`, the client in `BepInEx/plugins/UltimateFlea/`. The zip contains the bundled data (`config/levels_bundled.json`, `config/pool_bundled.json`) but not the user-editable `config/*.json`, so updating never overwrites settings; missing configs fall back to built-in defaults identical to the shipped files. No `.pdb` files are included.

## Item IDs

Find item TPLs (24-char IDs) from:

- https://db.sp-tushonka.com/ (search by name or ID)
- Offline list in this repo: [docs/ITEM_IDS.md](https://github.com/SVAR316/UltimateFlea/blob/main/docs/ITEM_IDS.md) (EN + RU names)

Items from other mods may only be listed in their `CustomItems` files or locale files.  
Invalid IDs are skipped and logged as warnings.

## Config

### `mod.json`

| Field | Description |
| --- | --- |
| `enabled` | Enables or disables the whole mod |
| `enablePoolManager` | Enables item pool management |
| `enablePricing` | Enables custom prices |
| `enableEconomy` | Enables the economy simulation |
| `liveFlea` | `true` = live online prices from the provider named by `priceSource`. `false` = local handbook prices only, the network is never touched. If the key is missing (older `mod.json`), it defaults to `true` only when `priceSource` is `tarkovdev`/`tarkovmarket`, so existing setups behave as before |
| `priceSource` | Which live provider `liveFlea` uses: `tarkovdev` (default, `avg24hPrice` from `json.tarkov.dev`) or `tarkovmarket` (needs `tarkovMarketApiKey`). `local` is the legacy "no live source" value; with `liveFlea: true` it falls back to `tarkovdev` |
| `tarkovMarketApiKey` | Optional, empty by default. Used only with `priceSource: "tarkovmarket"`. Experimental, see below |
| `priceFetchTimeoutSeconds` | Timeout per attempt of the live price download (default `180`, range 10-1800) |
| `pvePrices` | Uses PvE Tarkov.dev prices instead of regular prices |
| `preserveBasePrices` | Stops SPT from rebuilding prices from the handbook |
| `disableTraderPriceFloor` | Allows flea prices below trader purchase prices |
| `offerSpread` | How individual offers vary around the reference price: `live` (default) = per-item band from the 24h low/high of the live feed, `spt` = SPT's global random 0.8-1.2 range for every item, `exact` = every offer at exactly the reference price. Only `exact` touches SPT's `PriceRanges`. See "Offer spread" below |
| `exactOfferPrices` | **Deprecated**, replaced by `offerSpread`. Only read when `offerSpread` is absent: `true` = `exact`, `false` = `spt`. It is dropped from `mod.json` the next time the config is saved |
| `playerOffersOnly` | Only real player listings. Disables virtual (FakePlayer) offers and trader assort listings on the flea |
| `quickListFromStash` | Client: stash context menu **UltimateFlea: List** lists immediately and shows price + tax toast. Disable to hide the menu item |
| `debug` | Enables additional logging |

### Client quick list

With `quickListFromStash: true` (also in BepInEx ConfigurationManager → UltimateFlea Client → Selling), right-click a stash item → **UltimateFlea: List**. The offer is created at the flea average price; a notification shows listed price and commission.

### `pool.json`

| Field | Description |
| --- | --- |
| `mode` | `blacklist` or `whitelist` |
| `add` / `remove` | Specific item TPLs |
| `addCategories` / `removeCategories` | Parent or base-class IDs. Nested by default: a category covers its whole subtree |
| `nestedCategories` | `true` (default): a category rule matches the item's parent or any ancestor category. `false`: direct parent only (old behaviour) |
| `useLiveBlacklist` | Default `false` (opt-in, it changes what players can trade). `true`: every item with `noFlea` in the live tarkov.dev data (`config/pool_bundled.json`, 1532 items) is treated as if listed in `remove`. Your exact-TPL `add` entries still win. The server log says how many entries were applied |

#### Blacklist mode

Blacklist mode keeps the original SPT flea market pool and only changes items covered by your rules:

- `remove` blocks a specific item.
- `removeCategories` blocks every item in that category, subcategories included (with `nestedCategories: false`, only direct children).
- `add` enables a specific item, even if its category is blocked.
- `addCategories` enables every item in that category, subcategories included.
- Items that match no rule are left unchanged.

Example:

```json
{
  "mode": "blacklist",
  "add": ["item_to_restore"],
  "remove": ["item_to_block"],
  "addCategories": [],
  "removeCategories": ["category_to_block"]
}
```

This blocks the category and the item in `remove`, while `item_to_restore` stays available as an explicit exception.

#### Whitelist mode

Whitelist mode starts with an empty pool:

- Only items listed in `add` or covered by `addCategories` are available.
- `remove` can exclude a specific item from an allowed category.
- Everything else is blocked, including items added by other mods.
- `removeCategories` is unnecessary in this mode because categories are already blocked unless listed in `addCategories`.

Example:

```json
{
  "mode": "whitelist",
  "add": ["single_allowed_item"],
  "remove": ["item_excluded_from_allowed_category"],
  "addCategories": ["allowed_category"],
  "removeCategories": []
}
```

Item rules are checked before category rules. If the same item appears in both `add` and `remove`, `add` currently wins. Category matching is nested by default (the item's parent or any ancestor category matches); set `nestedCategories` to `false` for direct-parent-only matching.

If another mod such as WTT puts an item in the global item blacklist, adding it here also removes it from that blacklist and its runtime cache.

### `pricing.json`

| Field | Description |
| --- | --- |
| `globalMultiplier` | Multiplier applied to every price |
| `fixedPrices` | Fixed RUB prices by item TPL; economy never changes these |
| `itemMultipliers` | Price multiplier by item TPL |
| `categoryMultipliers` | Price multiplier by parent category |

Use a 24-char TPL, not the item name. Get it from https://db.sp-tushonka.com/ or [docs/ITEM_IDS.md](https://github.com/SVAR316/UltimateFlea/blob/main/docs/ITEM_IDS.md).  
Requires `"enablePricing": true` in `mod.json`. Restart the server after edits.

Example: lock Graphics card to 200000 RUB forever:

```json
{
  "globalMultiplier": 1.0,
  "fixedPrices": {
    "57347ca924597744596b4e71": 200000
  },
  "itemMultipliers": {},
  "categoryMultipliers": {}
}
```

For a relative bump instead of a hard price, use `itemMultipliers`, e.g. `"57347ca924597744596b4e71": 1.5`.

### `economy.json`

On every tick, supply and demand decay, a target price is calculated, and the current price moves toward it. Items listed in `fixedPrices` are excluded from the simulation.

| Field | Description |
| --- | --- |
| `simIntervalMinutes` | Minutes between simulation ticks. Lower values make prices react faster and write to `economy_state.json` more often |
| `demandPerBuy` | Demand added when an item is purchased from the flea market |
| `supplyPerSell` | Supply added when a player's flea offer is sold |
| `priceElasticity` | How strongly the supply and demand difference affects the target price. `0` has almost no effect; `1` is strong |
| `decayPerTick` | Supply and demand decay per tick. `0.1` removes about 10% each tick |
| `settleSpeed` | How far the current price moves toward its target per tick. `0.15` is gradual; `1.0` is immediate |
| `noise` | Random variation around the target. `0` disables it; `0.03` adds light variation |
| `minPriceFactor` | Minimum price relative to the base price. `0.5` means half the base price |
| `maxPriceFactor` | Maximum price relative to the base price |
| `wipe.enabled` | Early-wipe price multiplier. Default on. Skipped automatically (treated as `1.0`, with a startup warning, `economy.json` is not rewritten) while live prices are active (`liveFlea: true`), because live averages already include the wipe stage |
| `wipe.forceWithLivePrices` | Default `false`. `true` applies the wipe multiplier on top of live prices anyway (stacking) |
| `wipe.startLengthDays` | Number of days the early-wipe phase lasts |
| `wipe.startMultiplier` | Multiplier on day zero. It decreases linearly to `1.0` by the end of the phase |

Wipe age is calculated from the character's `RegistrationDate`, not the mod installation date. If multiple profiles exist, the oldest PMC is used. Do not stack wipe/events/trends on top of `tarkovdev` unless you want prices above live.

### `events.json`

Timed price multipliers keyed to wipe age (same `RegistrationDate` clock as early wipe). Default local preset ships with light hideout/ammo/late-barter events. Several matching active events multiply together.

| Field | Description |
| --- | --- |
| `enabled` | Turns the whole events system on or off |
| `events[].id` | Label for logs |
| `events[].startDay` | Wipe age in days when the event starts (inclusive) |
| `events[].endDay` | Wipe age in days when the event ends (exclusive) |
| `events[].multiplier` | Price multiplier while the event is active |
| `events[].categories` | Parent category IDs the event applies to |
| `events[].items` | Specific item TPLs the event applies to |

Example: ammo costs more during wipe days 7-14.

```json
{
  "enabled": true,
  "events": [
    {
      "id": "ammo_week",
      "startDay": 7,
      "endDay": 14,
      "multiplier": 1.4,
      "categories": ["5485a8684bdc2da2598b456a"],
      "items": []
    }
  ]
}
```

### `trends.json`

Slow sine-wave drifts on parent categories over wipe age. Off by default. Formula: `1 + amplitude * sin(2π * ageDays / periodDays + phase)`.

| Field | Description |
| --- | --- |
| `enabled` | Turns the whole trends system on or off |
| `trends[].category` | Parent category ID |
| `trends[].periodDays` | Length of one full wave in days |
| `trends[].amplitude` | Peak deviation from 1.0. `0.15` is about +/-15%. Clamped to 0..0.5 |
| `trends[].phase` | Phase offset in radians |

Example:

```json
{
  "enabled": true,
  "trends": [
    {
      "category": "543be5cb4bdc2deb348b4568",
      "periodDays": 21,
      "amplitude": 0.15,
      "phase": 0
    }
  ]
}
```

Target price each tick is roughly:

`base * wipe * events * trends * (1 + supply/demand pressure + noise)`

Then the current price settles toward that target and is clamped by `minPriceFactor` / `maxPriceFactor`. Fixed prices skip this entirely.

Trader assort prices are never changed by this mod.

### `levels.json`

Level-locked flea trading, like live Tarkov. Off by default.

| Field | Description |
| --- | --- |
| `enabled` | Turns level locks on or off |
| `baseTable` | Table your rules are applied on top of: `bundled` (default, curated UltimateFlea table in `config/levels_bundled.json`, now generated from live tarkov.dev `minLevelForFlea`; see "Bundled live data"), `spt` (SPT's `tieredFlea` from `ragfair.json`) or `none`. A missing or broken bundled file falls back to `spt`, then `none` |
| `useSptDefaults` | Deprecated alias, used only when `baseTable` is absent: `true` = `spt`, `false` = `none` |
| `lockBuying` | Players below the level can't buy these items from other players. Offers show as locked on the flea |
| `lockSelling` | Players below the level can't list these items |
| `items` | Item TPL -> required level. `0` removes a lock from the base table |
| `categories` | Category / base class ID -> required level. Nested categories are included. `0` removes a default lock |

Priority is the same as SPT: ammo tier, then exact item, then the highest matching category. Trader offers are never locked.

Example: armor from level 30, graphics card from level 20, no lock on the default Assault rifle category:

```json
{
  "enabled": true,
  "baseTable": "bundled",
  "lockBuying": true,
  "lockSelling": true,
  "items": {
    "57347ca924597744596b4e71": 20
  },
  "categories": {
    "5448e54d4bdc2dcc718b4568": 30,
    "5447b5f14bdc2d61278b4567": 0
  }
}
```

The server enforces the locks on its own. The client plugin (see below) is optional.

## Client plugin

`client/UltimateFlea.Client` is a separate BepInEx plugin, packaged on its own. It loads the level rules from the server (`/ultimateflea/levellocks`).

Build it (needs an installed SPT client):

```
dotnet build client/UltimateFlea.Client -c Release -p:TarkovDir="C:\SPT"
```

Output: `client/UltimateFlea.Client/bin/Release/BepInEx/plugins/UltimateFlea/`. Copy the `BepInEx` folder into the SPT game folder.

## Trades and Tarkov.dev

Buying an item from the flea market increases its `demand`. A completed player offer increases its `supply`.  
### Price sources

Set `liveFlea` to `true` to use live prices. `priceSource` picks the provider:

- `tarkovdev` - `avg24hPrice` (fallback: `lastLowPrice`) from the static dump at [json.tarkov.dev](https://json.tarkov.dev/endpoints). `pvePrices` switches it to the PvE dump.
- `tarkovmarket` - **experimental, off by default.** Needs `tarkovMarketApiKey`; reads `api.tarkov-market.app/api/v1/items/all`. The response schema and auth header (`x-api-key`) could not be verified without a key, and PvE support there is unconfirmed. A response with fewer than 300 usable prices is rejected. With an empty key the mod uses `tarkovdev` instead.

### Offer spread

`offerSpread: "live"` (the default) gives each item its own offer band instead of SPT's one global 0.8-1.2. The tarkov.dev dump carries `low24hPrice` and `high24hPrice` next to `avg24hPrice`; the mod stores them as **ratios of the chosen reference price** (`low / ref`, `high / ref`, each clamped to 0.5-3.0), so they stay valid while the economy simulation moves the price. Entries where low or high is missing, zero or inverted get no band. When an offer is generated, SPT's price randomisation (`RagfairPriceService.RandomiseOfferPrice`) is replaced for that item by a draw from a triangular distribution over its band, peaking at the reference price (offers cluster near the live number and reach the 24h extremes only rarely). Example: red keycard (avg 886509, low 837111, high 1000000) gets 0.944x-1.128x; blue keycard (avg 459456, low 450000, high 855555) gets 0.979x-1.862x.

Items without band data (not in the feed, local-price fallback, `liveFlea: false`, tarkovmarket source) use SPT's normal 0.8-1.2 range, and weapon presets and pack offers always keep SPT's ranges. The bands are cached in `data/tarkovdev_prices-<mode>.json.spread`; a cache from an older version has no such file, so the next start downloads the dump in full once instead of answering 304. If the patch cannot be bound the mod logs a warning and SPT's own range applies.

Fallback chain: live download -> disk cache (`data/`) -> local handbook prices. Items missing from the live feed also use local prices (SPT's `prices.json` and the handbook); at startup one INFO line shows the price source, how many live prices were loaded and how many flea items fell back to local. The shipped `mod.json` has `liveFlea: true`; if the key is omitted, it is resolved from `priceSource` as before. With `liveFlea: false` the active source is always local and no request is made, whatever `priceSource` says.

Troubleshooting:

- The live fetch is a ~17 MB download. The mod requests gzip, streams the response, waits up to `priceFetchTimeoutSeconds` per attempt and revalidates with `ETag` / `If-Modified-Since`: when nothing changed (HTTP 304) the cached prices are reused without downloading again.
- The server log names the failed stage (`dns`, `tls`, `connect`, `timeout`, `http NNN`, `parse`) and how many prices are in effect afterwards.
- `api.tarkov.dev/graphql` being down does **not** affect this mod; it only reads the static `json.tarkov.dev` dump.
- To work fully offline, set `liveFlea` to `false`.

### Bundled live data and the generator

`config/levels_bundled.json` (per-item `minLevelForFlea`, items with a level above 0) and `config/pool_bundled.json` (`noFlea` items, used by `pool.useLiveBlacklist`) are generated from the tarkov.dev item dump `https://json.tarkov.dev/regular/items`. To refresh them (Python 3, stdlib only):

```
python tools/generate_bundles.py                    # downloads the dump
python tools/generate_bundles.py --input items.json # uses an already-downloaded dump
```

It rewrites both files in `config/` and prints counts per level and the `noFlea` count.

## Presets

Presets are one-click bundles of settings, available over the web API (`/ultimateflea/api/presets`) and the dashboard. A preset **replaces** each config file it targets (the other files are untouched), and goes through the same validation, clamping and atomic save as editing a config by hand. A `.bak` copy of the previous file is kept next to it. After a pool preset the flea pool is re-applied immediately; pricing and economy changes may need a server restart.

| Id | Touches | What it does |
|---|---|---|
| `vanilla_flat` | pool, pricing, economy | Base prices, almost frozen simulation; resets pool and pricing. |
| `casual_cheap` | pricing, economy | About 25% cheaper, gentle market. |
| `hardcore_scarce` | pool, pricing, economy | Removes keycards, armor plates, sniper rifles, machine guns and special items; higher prices; strong early-wipe premium. |
| `volatile_market` | economy | Strong reaction to trades and noise. |
| `stable_market` | economy | Weak reaction, little noise. |
| `early_wipe` | economy | Long, strong price premium at wipe start (best with `priceSource: local`). |
| `barter_friendly` | pricing | Barter, keys and electronics worth more; guns and ammo slightly less. |
| `live_tarkov` | economy, events, trends | A 120-day wipe model (early shortages, supply glut after flea unlock, mid-wipe ammo and armor inflation, pre-wipe trader sale). Estimated shape, not real price data; best with the local price source. Replaces all three configs and turns events and trends on. |
| `modded_items_flea` | pool | Puts tradable items added by your installed mods on the flea (computed at apply time). |

Item ids in a preset that your item database does not know are ignored, and the apply response carries a `warning` listing them.

### Modded items preset

`modded_items_flea` forces every tradable item added by your installed mods onto the flea. No item id is hardcoded. An item counts as added by a mod when it is not in the vanilla item list that ships with your server (`SPT_Data/database/templates/items.json`), or when SPT's mod item cache credits it to a mod; the cache alone is not used because it can be empty even with mods installed. Items whose mod cannot be pinned down are included too, since they are still not vanilla. Quest items, unsaleable items, containers (loot containers, stashes, corpses) and items with no flea or handbook price are skipped. The result is merged into `pool.json` `add`; your other pool settings stay. The apply response `warning` says how many items were added, from which mods, and how many were of unknown source. The preset is unavailable, with a reason, when no such items exist. The `unavailableReason`, `warning` and `error` fields of the preset API are objects with `en` and `ru` text.

Some mods ship items with `CanSellOnRagfair` set to false. You can force any such item onto the flea by hand through `pool.json` `add`. Example (an id specific to one release of the Manimal's CS Gas mod, yours may differ):

```json
{ "mode": "blacklist", "add": ["6a5d6a5f4ed8c025a0a2cff0"] }
```

### Partial failure

If a preset touches several configs and a later one fails to save, the earlier ones stay applied (there is no rollback). The response is HTTP 400 with `error` explaining which config failed, plus `applied` listing what was saved.

## Item catalogue endpoint

`GET /ultimateflea/api/items` returns every item (name and short name in English and Russian, category, and source mod) for the dashboard's item picker, so you do not have to type 24-character TPL ids. It is built on the first request, not at server start, then cached until restart, and sent gzipped with an `ETag`.

- SPT ships no item icons (the game renders them at runtime from its asset bundles), so category icons are the baseline. A category icon URL is included only when SPT's image router confirms it serves that file; otherwise it is `null`.
- Real item pictures come from the game client's own icon cache on your disk (`user/sptappdata/<version>/` next to the server's `mods` folder). Items that have a picture carry `"ic": true`, and `GET /ultimateflea/api/items/{tpl}/icon` serves it (404 otherwise). The files are BSG assets: they are read from where the client wrote them and are never copied into the mod or a release. Coverage is partial: roughly 1 in 4 items (about 1060 of 4553 on the machine this was developed on), only items you have already seen in game, so it grows as you play and a fresh install has none. Items that hold other items (weapons with attachments, armor with plates, rigs with contents) are not covered. The key formula is tied to the current client build; a future client change would simply reduce matches. If the cache folder is missing, the feature turns itself off with one log line.
- The "source" label of an item is best effort and is never a guess. An item is "vanilla" (`src` 0) only if its id is in the vanilla item list that ships with your server (`SPT_Data/database/templates/items.json`). Items credited by SPT's mod item cache get that mod. Items that are neither (typically ones a mod such as WTT writes straight into the item database, which the cache does not record) are attributed by reading the mod folders: an item goes to the mod whose JSON files define it (the id is a top-level key of an item definition), otherwise to the one mod that mentions its id. If no mod, or more than one mod, mentions it, or the vanilla list cannot be found, the item is shown with an unknown source (`src` -1). A mod that only references another mod's item id, or that builds ids in code, can still be mislabelled or left unknown.
- A missing name falls back in this order: Russian to English, then the item's template name, then its id.
