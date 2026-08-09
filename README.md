# UltimateFlea

Flea market mod for SPT 4.1. It lets you manage the item pool, set prices, and simulate supply, demand, early-wipe pricing, market events, and category trends.

## Requirements

- SPT ~4.1
- .NET 10 SDK if you want to build it yourself

## Build

1. Open `UltimateFlea.csproj` in Visual Studio or Rider
2. Build the project in Release mode
3. Copy `bin\Release\UltimateFlea\` to `SPT\user\mods\`
4. Start the server

Config files are stored in `config\` next to the DLL. Economy state is saved to `data\economy_state.json`.

## Item IDs

Use https://db.sp-tarkov.com/ to find item IDs. Search by item name or its 24-character ID.  
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
| `priceSource` | `local` or `tarkovdev`. Tarkov.dev uses `avg24hPrice` from `json.tarkov.dev` and falls back to cache or local prices |
| `pvePrices` | Uses PvE Tarkov.dev prices instead of regular prices |
| `preserveBasePrices` | Stops SPT from rebuilding prices from the handbook |
| `disableTraderPriceFloor` | Allows flea prices below trader purchase prices |
| `exactOfferPrices` | Removes the default 0.8-1.2 offer price spread. This affects the entire flea market |
| `debug` | Enables additional logging |

### `pool.json`

| Field | Description |
| --- | --- |
| `mode` | `blacklist` or `whitelist` |
| `add` / `remove` | Specific item TPLs |
| `addCategories` / `removeCategories` | Parent or base-class IDs |

#### Blacklist mode

Blacklist mode keeps the original SPT flea market pool and only changes items covered by your rules:

- `remove` blocks a specific item.
- `removeCategories` blocks every item whose direct parent matches the category ID.
- `add` enables a specific item, even if its category is blocked.
- `addCategories` enables every item whose direct parent matches the category ID.
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

Item rules are checked before category rules. If the same item appears in both `add` and `remove`, `add` currently wins. Category matching uses the item's direct parent ID and does not automatically include nested child categories.

If another mod such as WTT puts an item in the global item blacklist, adding it here also removes it from that blacklist and its runtime cache.

### `pricing.json`

| Field | Description |
| --- | --- |
| `globalMultiplier` | Multiplier applied to every price |
| `fixedPrices` | Fixed RUB prices by item TPL; economy never changes these |
| `itemMultipliers` | Price multiplier by item TPL |
| `categoryMultipliers` | Price multiplier by parent category |

Get item TPLs from https://db.sp-tarkov.com/ (24-char ID, not the item name).  
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
| `wipe.enabled` | Early-wipe price multiplier. Default on with `local`. Turn off if using `tarkovdev` (live averages already include wipe stage) |
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

## Trades and Tarkov.dev

Buying an item from the flea market increases its `demand`. A completed player offer increases its `supply`.  
Set `priceSource` to `"tarkovdev"` to use `avg24hPrice` (fallback: `lastLowPrice`) from [json.tarkov.dev](https://json.tarkov.dev/endpoints).

## Planned

- Level-locked flea sales (like live Tarkov)
