# UltimateFlea

Мод на барахолку для SPT 4.0. Можно руками крутить пул предметов, цены и симулировать рынок (спрос/предложение, ранний вайп).

## Что нужно

- SPT ~4.0
- .NET 9 SDK, если собираешь сам

## Сборка

1. Открой `UltimateFlea.csproj` в VS / Rider
2. Build → Release
3. Папку из `bin\Release\UltimateFlea\` кинь в `SPT\user\mods\`
4. Запусти сервер

Конфиги — рядом с DLL в `config\`. Состояние экономики пишется в `data\economy_state.json`.

## ID предметов

Берёшь с https://db.sp-tarkov.com/ (название или 24-символьный ID).  
Для кастомных модов — из их `CustomItems` / локалей, на db.sp-tarkov их может не быть.

Кривой ID в конфиге просто пропускается, в лог уйдёт warning.

## Конфиги

### `mod.json`

| Поле | Зачем |
| --- | --- |
| `enabled` | Выключатель всего мода |
| `enablePoolManager` | Пул предметов |
| `enablePricing` | Ручные цены |
| `enableEconomy` | Симуляция |
| `priceSource` | Откуда база цен: пока только `local` |
| `preserveBasePrices` | Не давать SPT пересобрать prices из handbook |
| `disableTraderPriceFloor` | Не поднимать цену до цены торговца |
| `exactOfferPrices` | Убрать разброс 0.8–1.2 на офферах (для тестов удобно, на всё влияет) |
| `debug` | Больше логов |

### `pool.json`

| Поле | Зачем |
| --- | --- |
| `mode` | `blacklist` или `whitelist` |
| `add` / `remove` | Конкретные TPL |
| `addCategories` / `removeCategories` | По parent/baseclass |

Сначала смотрим на конкретный предмет, потом на категорию.

Если предмет из другого мода (типа WTT) сидит в item blacklist — `add` ещё и вытаскивает его оттуда, иначе офферов на барахолке не будет.

### `pricing.json`

| Поле | Зачем |
| --- | --- |
| `globalMultiplier` | На все цены |
| `fixedPrices` | Жёсткая цена в рублях, симуляция её не трогает |
| `itemMultipliers` | Множитель на TPL |
| `categoryMultipliers` | Множитель на parent |

### `economy.json`

| Поле | Зачем |
| --- | --- |
| `simIntervalMinutes` | Как часто тикает рынок |
| `demandPerBuy` / `supplyPerSell` | Сколько качает спрос/предложение за сделку |
| `priceElasticity` | Насколько сильно это двигает цену |
| `decayPerTick` | Затухание давления |
| `settleSpeed` | Как быстро цена догоняет цель |
| `noise` | Случайный шум |
| `minPriceFactor` / `maxPriceFactor` | Пол и потолок относительно базы |
| `wipe.*` | Ранний вайп: дни и стартовый множитель |

Вайп считается от `RegistrationDate` персонажа, не от установки мода.

## Что ещё не сделано

Живые сделки через Harmony и подтягивание цен с Tarkov.dev.
