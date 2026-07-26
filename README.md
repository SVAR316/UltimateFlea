# UltimateFlea

Flea market mod for SPT 4.0. It lets you manage the item pool, set prices, and simulate supply, demand, and early-wipe pricing.

## Requirements

- SPT ~4.0
- .NET 9 SDK if you want to build it yourself

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
| `fixedPrices` | Fixed RUB prices that are not changed by the economy simulation |
| `itemMultipliers` | Price multiplier by item TPL |
| `categoryMultipliers` | Price multiplier by parent category |

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
| `wipe.enabled` | Enables the early-wipe price multiplier |
| `wipe.startLengthDays` | Number of days the early-wipe phase lasts |
| `wipe.startMultiplier` | Multiplier on day zero. It decreases linearly to `1.0` by the end of the phase |

Wipe age is calculated from the character's `RegistrationDate`, not the mod installation date. If multiple profiles exist, the oldest PMC is used.

## Trades and Tarkov.dev

Buying an item from the flea market increases its `demand`. A completed player offer increases its `supply`.  
Set `priceSource` to `"tarkovdev"` to use `avg24hPrice` from [json.tarkov.dev](https://json.tarkov.dev/endpoints).

## Upcoming updates

- Support for locking flea market sales behind character level requirements, like in live Tarkov.
- Separate market events and more complex price trends as their own systems, not just the current supply/demand tick.
