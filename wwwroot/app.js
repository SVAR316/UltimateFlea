/* ============================================================================
 * UltimateFlea web dashboard
 * No build step, no dependencies, no network except /ultimateflea/api/*.
 *
 * Sections in this file:
 *   1. constants
 *   2. i18n (en / ru)
 *   3. small utils
 *   4. app state (lang, theme, section, config draft, state table)
 *   5. toasts
 *   6. api
 *   7. icons
 *   8. svg chart module (registry, scales, tooltip, 8 charts + sparkline)
 *   9. config form editor (type inference -> controls)
 *  10. sections (overview / config / selling / settings / state / presets / item picker)
 *  11. shell + router + mount
 * ==========================================================================*/

/* ---------------------------------------------------------------- 1. consts */

const API = "/ultimateflea/api";
const LANG_KEY = "ultimateflea.ui.lang";
const THEME_KEY = "ultimateflea.ui.theme";
const AUTO_REFRESH_MS = 30000;
const STATE_PAGE_SIZE = 50;

/* ------------------------------------------------------------------ 2. i18n */

const I18N = {
  en: {
    brandSub: "Server console",
    loading: "Loading…",
    none: "none",
    on: "On",
    off: "Off",
    enabled: "Enabled",
    disabled: "Disabled",
    discard: "Discard unsaved edits?",
    unsaved: "Unsaved changes",
    restartBadge: "Restart recommended",

    navGroup: {
      overview: "Overview",
      market: "Market config",
      client: "Client",
      system: "System",
    },
    nav: {
      home: "Home",
      mod: "Mod",
      pool: "Pool",
      pricing: "Pricing",
      economy: "Economy",
      events: "Events",
      trends: "Trends",
      levels: "Levels",
      presets: "Presets",
      selling: "Selling",
      settings: "Settings",
      state: "Market state",
    },
    sub: {
      overview: "Live snapshot of pool, economy, trades, events and locks.",
      configRestart: "Writes config/*.json. A server restart is recommended for a full rebuild.",
      config: "Writes config/*.json and applies where possible.",
      selling: "Client stash context-menu quick list.",
      settings: "Language, theme and server maintenance for this dashboard.",
      state: "Readonly live price state, one row per item template.",
      presets: "Ready-made config sets. Preview one, then apply it to overwrite the configs it covers.",
    },

    act: {
      refresh: "Refresh",
      refreshing: "Refreshing…",
      reload: "Reload",
      reloadDisk: "Reload from disk",
      autoRefresh: "Auto-refresh",
      autoRefreshHint: "/overview is a heavy call, so auto-refresh runs every 30s and should not be polled faster.",
      save: "Save",
      saving: "Saving…",
      discard: "Discard",
      add: "Add",
      remove: "Remove",
      copyJson: "Copy JSON",
      copied: "JSON copied to clipboard.",
      copyFailed: "Could not copy to clipboard.",
      prev: "Previous",
      next: "Next",
      dismiss: "Dismiss",
    },

    theme: {
      label: "Theme",
      dark: "Dark",
      light: "Light",
      hint: "Stored in this browser. Follows your system preference on first visit.",
      toggle: "Switch theme",
    },

    settings: {
      language: "Language",
      languageHint: "Applies only to this web UI. Stored in the browser. Default: English.",
      en: "English",
      ru: "Russian",
      maintenance: "Maintenance",
      maintenanceHint: "Re-reads every config/*.json from disk and re-applies the item pool and level locks. Use it after editing files by hand.",
    },

    toast: {
      saved: "Saved.",
      savedRestart: "Saved. Restart the server for the full effect.",
      saveFailed: "Save failed",
      invalidJson: "Invalid JSON",
      reloaded: "Configs reloaded from disk.",
      reloadFailed: "Reload failed",
      loadFailed: "Load failed",
    },

    form: {
      tabForm: "Form",
      tabJson: "JSON",
      tabTable: "Table",
      tabRaw: "Raw JSON",
      jsonValid: "JSON is valid.",
      jsonLine: "line",
      jsonHint: "Escape hatch: edit the raw config. Save is disabled while the JSON does not parse.",
      emptyList: "Empty list.",
      emptyMap: "No entries.",
      entry: "Entry",
      addEntry: "Add entry",
      newId: "ID or TPL",
      newValue: "Value",
      keyHint: "Config key",
      restartNote: "pool / pricing / economy need a server restart before the change is fully rebuilt.",
      saveNote: "Saved into config/.",
      mapKey: "ID",
      mapValue: "Value",
      removeRow: "Remove row",
      showSecret: "Show value",
      hideSecret: "Hide value",
      secretNote: "This config holds a secret (an API key or token). The raw JSON below shows it in plain text, so take care when sharing your screen or pasting it anywhere.",
    },

    selling: {
      label: "Quick list from stash context menu",
      hint: "When on, right-clicking a stash item shows “UltimateFlea: List” and lists it immediately with a price + tax toast.",
    },

    kpi: {
      mod: "Mod",
      items: "Economy items",
      avgFactor: "Avg factor",
      wipeDay: "Wipe day",
      buys: "Buys (30d)",
      sells: "Sells (30d)",
      fixed: "Fixed prices",
      floating: "Floating prices",
      activeEvents: "Active events",
      stateEntries: "State entries",
      poolMode: "Pool mode",
      noProfile: "no profile yet",
    },

    chart: {
      trades: "Buys and sells",
      tradesSub: "Last {days} days",
      tradesEmpty: "No trades recorded yet. This fills in as flea buys and sells happen.",
      buys: "Buys",
      sells: "Sells",
      day: "Day",

      factors: "Price factor distribution",
      factorsSub: "Items per factor bucket",
      factorsCaption: "{above} above base, {at} at base, {below} below base",
      factorsEmpty: "No priced items yet.",
      atBase: "at base",
      items: "items",

      demand: "Hot demand",
      supply: "Hot supply",
      heatSub: "Top 10 by pressure",
      demandEmpty: "No strong demand right now.",
      supplyEmpty: "No strong supply right now.",
      factor: "Factor",
      demandLabel: "Demand",
      supplyLabel: "Supply",

      categories: "Category overview",
      categoriesSub: "Average factor; bar thickness = item count",
      categoriesEmpty: "No categories in the economy yet.",
      avgFactor: "Avg factor",
      avgDemand: "Avg demand",
      avgSupply: "Avg supply",

      movers: "Movers",
      moversSub: "Furthest from base price",
      moversEmpty: "Nothing has moved away from base price yet.",
      up: "Up",
      down: "Down",
      basePrice: "Base",
      currentPrice: "Now",

      pressure: "Supply / demand pressure",
      pressureSub: "x = demand - supply, y = price factor",
      pressureCaption: "Each dot is one item. Points right of the zero line are demand-driven and tend to sit above the 1.0 line; points left are oversupplied and drift below it.",
      pressureEmpty: "Not enough simulated items to plot pressure yet.",

      wipe: "Early-wipe curve",
      wipeSub: "Projected multiplier over wipe age",
      wipeOff: "Early-wipe pricing is disabled in economy.json.",
      wipeNoCurve: "Early wipe is on, but startLengthDays is 0, so there is no ramp to plot.",
      wipeNoProfile: "No profile found yet, so the wipe day is unknown and today is not marked.",
      wipeToday: "Today",
      wipeCurrent: "Current multiplier",
      wipeStart: "Start length {days} d, start multiplier ×{mult}",
      multiplier: "Multiplier",

      events: "Event timeline",
      eventsSub: "Across the first {days} wipe days",
      eventsOff: "The events system is disabled in events.json.",
      eventsEmpty: "No events configured.",
      active: "Active",
      inactive: "Inactive",
      eventDays: "Days {from}-{to}",

      noData: "No data",
      total: "Total",
      ariaTrades: "Line chart of daily flea buys and sells over {days} days. {buys} buys and {sells} sells in total.",
      ariaFactors: "Histogram of {n} items across 7 price-factor buckets.",
      ariaRanked: "Ranked bar chart of {n} items.",
      ariaCategories: "Ranked bar chart of {n} categories by average price factor.",
      ariaMovers: "Diverging bar chart of {n} items above and below base price.",
      ariaPressure: "Scatter plot of {n} items by demand minus supply against price factor.",
      ariaWipe: "Line chart of the early-wipe price multiplier over {n} wipe days.",
      ariaEvents: "Timeline of {n} configured market events over {days} wipe days.",
      ariaSpark: "Sparkline of the last {n} days.",
    },

    state: {
      search: "Filter by item template…",
      rows: "{shown} of {total} rows",
      page: "Page {page} of {pages}",
      readonly: "Readonly. Served live from the running economy simulation.",
      noRows: "No item matches this filter.",
      noState: "The economy state is empty. It fills once the simulation ticks.",
      col: {
        tpl: "Template",
        basePrice: "Base price",
        currentPrice: "Current price",
        factor: "Factor",
        demand: "Demand",
        supply: "Supply",
      },
    },

    overview: {
      activeEvents: "Active events",
      system: "System",
      version: "Version",
      webUi: "Web UI",
      priceSource: "Price source",
      poolManager: "Pool manager",
      pricing: "Pricing",
      economySim: "Economy sim",
      playerOffersOnly: "Player offers only",
      quickList: "Stash quick list",
      stateFile: "State file",
      present: "present",
      missing: "missing",
      levels: "Level locks",
      levelsDetail: "{items} items, {categories} categories, {rules} rules",
      poolDetail: "+{add} / -{remove} items, +{addCat} / -{removeCat} categories",
      trends: "Trends",
      trendsDetail: "{total} configured",
      sptDefaults: "SPT defaults",
    },

    presets: {
      intro: "A preset replaces whole configs with a ready-made set of values. Preview it first: applying overwrites the current values of every config it touches.",
      preview: "Preview",
      hidePreview: "Hide preview",
      loadingPreview: "Loading preview…",
      previewFailed: "Could not load the preview",
      apply: "Apply",
      applying: "Applying…",
      requires: "Requires",
      touches: "Touches",
      unavailable: "Unavailable",
      empty: "No presets are available.",
      absent: "Presets are not available on this server",
      absentHint: "This server build does not offer presets yet. Everything else works as usual; update the mod to get them.",
      loadFailed: "Could not load presets",
      confirm: "Apply preset “{name}”?\n\nThis OVERWRITES the current values of these configs: {configs}.\nYour existing settings in them will be replaced.",
      applied: "Preset applied: {configs}.",
      appliedRestart: "Preset applied. Restart the server for the full effect.",
      applyFailed: "Could not apply the preset",
      appliedTo: "Applied to",
      openConfig: "Open {name}",
    },

    links: {
      label: "Project links",
      githubTip: "UltimateFlea on GitHub (opens in a new tab)",
      spmodTip: "UltimateFlea on sp-mod (opens in a new tab)",
    },

    idname: {
      unknown: "Unknown ID",
      unknownHint: "Not in the item list: the ID is wrong, or the mod that adds it is not installed.",
    },

    picker: {
      pick: "Pick",
      pickTitle: "Choose from the item list instead of typing IDs",
      titleItem: "Pick items",
      titleCategory: "Pick categories",
      search: "Search by name (EN or RU), short name or ID…",
      source: "Source",
      allSources: "All sources",
      vanilla: "Vanilla",
      category: "Category",
      allCategories: "All categories",
      loading: "Loading the item list…",
      absent: "The item list is not available on this server",
      absentHint: "This server build has no item catalog yet. You can still type IDs by hand.",
      loadFailed: "Could not load the item list",
      noMatches: "Nothing matches.",
      count: "{n} matches",
      capped: "Showing the first {shown} of {n}. Narrow the search to see the rest.",
      selected: "Selected: {n}",
      insert: "Insert",
      have: "Already there",
      keys: "Arrows move, Enter picks, Ctrl+Enter inserts, Esc closes.",
      unknownSource: "Unknown source",
      unknownSourceHint: "Not a vanilla item, but which mod added it could not be determined.",
      iconNote: "Item pictures show only for items you have already seen in game; the rest get a category icon.",
      added: "Added {n}.",
      addedSkipped: "Added {n}, already present: {skipped}.",
    },

    help: {
      mod: {
        enabled: "Master switch for the whole mod. When off, the server skips the pool, prices, economy and level locks at startup. Takes effect after a restart.",
        enablePoolManager: "Applies pool.json, which decides what may appear on the flea. Off leaves SPT's default item pool untouched.",
        enablePricing: "Applies your prices from pricing.json (multipliers, fixed prices) at server start. Price setup runs when this or Economy is on, so with Economy on this adds nothing; alone it gives static prices with no simulation.",
        enableEconomy: "Runs the market simulation: buys push prices up, sales push them down, and wipe age, events and trends shift them over time. Off means prices never move after startup, and economy, events and trends have no effect.",
        liveFlea: "On: base prices come from the online provider in Price source. Off: local handbook prices only, the network is never touched. If the download fails, the mod falls back to the disk cache, then to local prices.",
        priceSource: "Online provider used when Live flea is on: tarkovdev (24h average from json.tarkov.dev, the default) or tarkovmarket (experimental, needs an API key). Ignored when Live flea is off.",
        tarkovMarketApiKey: "API key for tarkov-market. Only used when Live flea is on and Price source is tarkovmarket; with an empty key the mod uses tarkovdev instead. Never logged.",
        priceFetchTimeoutSeconds: "How long to wait per attempt for the live price download (about 17 MB). Default 180, allowed 10-1800. Raise it on a slow connection. Only matters when Live flea is on.",
        pvePrices: "Use tarkov.dev PvE prices instead of regular ones. Only matters when Live flea is on with tarkovdev as the source.",
        preserveBasePrices: "Stops SPT from rebuilding flea prices from the handbook over the ones this mod sets. Best left on. Applied at server start.",
        disableTraderPriceFloor: "Lets offers sit below what traders pay; normally SPT raises such prices up to the trader price. Turn on if you want cheap items to be really cheap. Applied at server start.",
        exactOfferPrices: "Removes the random 0.8-1.2 spread SPT puts on offer prices, so every offer lists at the exact price. Affects the whole flea. Applied at server start.",
        playerOffersOnly: "Only real player listings: no generated (FakePlayer) offers and no trader assortment on the flea. The flea stays empty until players list something. Applied at server start.",
        quickListFromStash: "Client option: adds “UltimateFlea: List” to the stash right-click menu to list an item instantly with a price and tax toast. Needs the client plugin. Off hides the menu entry.",
        debug: "Extra detail in the server log (wipe stage, active events and trends, every economy tick). For troubleshooting only; it makes the log noisy.",
      },
      pool: {
        mode: "blacklist keeps SPT's normal pool and only changes what your rules mention. whitelist keeps only the items you allow (add, addCategories) and removes everything else from the flea.",
        add: "Item IDs (TPL) forced onto the flea. A specific item beats category rules, and add beats remove for the same item.",
        remove: "Item IDs (TPL) forced off the flea, whatever the category rules say.",
        addCategories: "Category IDs (the items' parent class) whose items go onto the flea. In whitelist mode this is the list of allowed categories; leave it empty there and only the items in add survive.",
        removeCategories: "Category IDs whose items are removed from the flea. Blacklist mode only; whitelist mode ignores it.",
      },
      pricing: {
        globalMultiplier: "Multiplies every floating base price. 1.0 is unchanged, 2.0 doubles everything. Fixed prices are not affected. Rebuilt at server start.",
        fixedPrices: "Item ID to a hard price. These items skip every multiplier and the simulation, so the price never moves. Rebuilt at server start.",
        itemMultipliers: "Item ID to a multiplier on that item's base price. Stacks with the global and category multipliers. Ignored for fixed-price items.",
        categoryMultipliers: "Category ID (the items' parent class) to a multiplier for every item in it. Stacks with the global and item multipliers. Ignored for fixed-price items.",
      },
      economy: {
        simIntervalMinutes: "How often the market simulation ticks, in real minutes. Default 60, minimum 1, sane 15-180. Shorter means prices react faster and the state file is rewritten more often.",
        demandPerBuy: "Demand added to an item for each buy (demand is capped at 1.0). Default 0.03, allowed 0-1, sane 0.01-0.1. Higher means prices climb faster as players buy.",
        supplyPerSell: "Supply added to an item each time a player's offer sells (capped at 1.0). Default 0.03, allowed 0-1, sane 0.01-0.1. Higher means prices fall faster as players sell.",
        priceElasticity: "How strongly demand minus supply moves the target price: base × (1 + elasticity × (demand - supply)). Default 0.3, allowed 0-10, sane 0.1-1. At 0 trades never move prices.",
        decayPerTick: "Share of accumulated demand and supply that fades each tick, so old trades are forgotten. Default 0.08, allowed 0-1, sane 0.03-0.2. At 0 nothing is forgotten.",
        settleSpeed: "How much of the gap to the target price is closed each tick. Default 0.1, allowed 0-1, sane 0.05-0.3. Low drifts slowly and smoothly; 1 jumps straight to the target.",
        noise: "Random jitter on the target price each tick, up to ± this value (0.03 is ±3%). Default 0.03, minimum 0, sane 0-0.1. At 0 there is no random wobble.",
        minPriceFactor: "Price floor as a share of the base price: it never drops below base × this. Default 0.7, minimum 0.01, sane 0.3-1.",
        maxPriceFactor: "Price ceiling as a share of the base price: it never rises above base × this. Default 2.0, never below the minimum, sane 1.5-3. It also caps early-wipe, event and trend boosts.",
        wipe: "Early-wipe boost: prices start higher right after a wipe and ease back to normal. Wipe age counts from the oldest PMC profile's registration date.",
        wipe_enabled: "Turns the early-wipe bump on. Most useful with local prices; with tarkovdev leave it off, since live averages already include the wipe stage.",
        wipe_startLengthDays: "How many days the ramp lasts. Default 14. At 0 the bump is disabled.",
        wipe_startMultiplier: "Price multiplier on wipe day 0, easing linearly down to ×1.0 by the end of the ramp. Default 1.8, minimum 0.01. Still capped by the max price factor.",
      },
      events: {
        enabled: "Master switch for timed price events. Off means the events below are ignored. They also need Economy on.",
        events: "Timed price bumps tied to wipe age. While the wipe age is inside an event's day window, its multiplier is applied to the listed items and categories. Overlapping events multiply together.",
        events_id: "Name of the event. Only a label for you and the dashboard; it changes nothing.",
        events_startDay: "Wipe day the event starts on, inclusive. Counted from the oldest PMC profile's registration date.",
        events_endDay: "Wipe day the event ends on, exclusive: the event is already off on this day.",
        events_multiplier: "Price multiplier while the event is active: 1.15 is +15%, 0.9 is -10%. Applied to the target price, which is still clamped by the min and max price factor.",
        events_categories: "Category IDs (the items' parent class) the event hits.",
        events_items: "Individual item IDs (TPL) the event hits. An event with both lists empty does nothing.",
      },
      trends: {
        enabled: "Master switch for category trends. Off by default. Trends also need Economy on.",
        trends: "Slow sine-wave price drift per category over wipe age: 1 + amplitude × sin(2π × day / period + phase). Several trends on one category multiply together.",
        trends_category: "Category ID (the items' parent class) the wave applies to. Empty entries are ignored.",
        trends_periodDays: "Length of one full up-and-down cycle, in days. Default 21. Must be above 0 or the entry is ignored.",
        trends_amplitude: "Peak deviation from 1.0: 0.15 swings prices about ±15%. Default 0.15.",
        trends_phase: "Shift of the wave in radians. 0 starts at the normal price and rising; about 1.57 starts at the peak. Default 0.",
      },
      levels: {
        enabled: "Master switch for level locks: players below the required level cannot buy or sell the locked items on the flea. Off by default; the mod itself must be on too. Applied when you save.",
        useSptDefaults: "Start from SPT's built-in tiered flea levels (live-like), then apply your items and categories on top. Off means only your own rules.",
        lockBuying: "Block buying locked items below the required level. This also makes the flea UI show those offers as locked.",
        lockSelling: "Block listing and selling locked items below the required level.",
        items: "Item ID to the player level required for that item. 0 removes a lock that came from the SPT defaults. An item rule wins over its category.",
        categories: "Category ID to the player level required, nested subcategories included. 0 removes a default lock.",
      },
    },
  },

  ru: {
    brandSub: "Консоль сервера",
    loading: "Загрузка…",
    none: "нет",
    on: "Вкл",
    off: "Выкл",
    enabled: "Включён",
    disabled: "Отключён",
    discard: "Отменить несохранённые правки?",
    unsaved: "Несохранённые изменения",
    restartBadge: "Нужен перезапуск",

    navGroup: {
      overview: "Обзор",
      market: "Настройки рынка",
      client: "Клиент",
      system: "Система",
    },
    nav: {
      home: "Главная",
      mod: "Мод",
      pool: "Пул",
      pricing: "Цены",
      economy: "Экономика",
      events: "События",
      trends: "Тренды",
      levels: "Уровни",
      presets: "Пресеты",
      selling: "Продажа",
      settings: "Настройки",
      state: "Состояние рынка",
    },
    sub: {
      overview: "Текущий снимок пула, экономики, сделок, событий и блокировок.",
      configRestart: "Пишет в config/*.json. Для полной пересборки рекомендуется перезапуск сервера.",
      config: "Пишет в config/*.json и применяет изменения там, где это возможно.",
      selling: "Быстрый листинг из контекстного меню схрона.",
      settings: "Язык, тема и обслуживание сервера для этой панели.",
      state: "Живое состояние цен только для чтения, по строке на каждый шаблон предмета.",
      presets: "Готовые наборы конфигов. Посмотрите превью и примените: конфиги, которых он касается, будут перезаписаны.",
    },

    act: {
      refresh: "Обновить",
      refreshing: "Обновление…",
      reload: "Перезагрузить",
      reloadDisk: "Перечитать с диска",
      autoRefresh: "Автообновление",
      autoRefreshHint: "/overview: тяжёлый запрос. Автообновление идёт раз в 30 секунд, чаще опрашивать не стоит.",
      save: "Сохранить",
      saving: "Сохранение…",
      discard: "Отменить",
      add: "Добавить",
      remove: "Удалить",
      copyJson: "Копировать JSON",
      copied: "JSON скопирован в буфер обмена.",
      copyFailed: "Не удалось скопировать в буфер обмена.",
      prev: "Назад",
      next: "Вперёд",
      dismiss: "Закрыть",
    },

    theme: {
      label: "Тема",
      dark: "Тёмная",
      light: "Светлая",
      hint: "Хранится в этом браузере. При первом открытии берётся из настроек системы.",
      toggle: "Переключить тему",
    },

    settings: {
      language: "Язык",
      languageHint: "Только для этого веб-интерфейса. Хранится в браузере. По умолчанию: English.",
      en: "English",
      ru: "Русский",
      maintenance: "Обслуживание",
      maintenanceHint: "Перечитывает все config/*.json с диска и заново применяет пул предметов и блокировки по уровню. Пригодится после правки файлов вручную.",
    },

    toast: {
      saved: "Сохранено.",
      savedRestart: "Сохранено. Для полного эффекта перезапустите сервер.",
      saveFailed: "Не удалось сохранить",
      invalidJson: "Неверный JSON",
      reloaded: "Конфиги перечитаны с диска.",
      reloadFailed: "Не удалось перечитать конфиги",
      loadFailed: "Не удалось загрузить",
    },

    form: {
      tabForm: "Форма",
      tabJson: "JSON",
      tabTable: "Таблица",
      tabRaw: "Сырой JSON",
      jsonValid: "JSON корректен.",
      jsonLine: "строка",
      jsonHint: "Резервный режим: правка сырого конфига. Пока JSON не разбирается, сохранение недоступно.",
      emptyList: "Список пуст.",
      emptyMap: "Записей нет.",
      entry: "Запись",
      addEntry: "Добавить запись",
      newId: "ID или TPL",
      newValue: "Значение",
      keyHint: "Ключ конфига",
      restartNote: "Для pool / pricing / economy нужен перезапуск сервера, чтобы изменения пересобрались полностью.",
      saveNote: "Сохраняется в config/.",
      mapKey: "ID",
      mapValue: "Значение",
      removeRow: "Удалить строку",
      showSecret: "Показать значение",
      hideSecret: "Скрыть значение",
      secretNote: "В этом конфиге есть секрет (ключ API или токен). В сыром JSON ниже он виден открытым текстом, так что будьте осторожны при демонстрации экрана и не вставляйте его куда попало.",
    },

    selling: {
      label: "Быстрый листинг из контекстного меню схрона",
      hint: "Если включено, ПКМ по предмету в схроне показывает «UltimateFlea: List» и сразу выставляет лот с уведомлением о цене и комиссии.",
    },

    kpi: {
      mod: "Мод",
      items: "Предметов в экономике",
      avgFactor: "Средний фактор",
      wipeDay: "День вайпа",
      buys: "Покупки (30 д)",
      sells: "Продажи (30 д)",
      fixed: "Фиксированные цены",
      floating: "Плавающие цены",
      activeEvents: "Активные события",
      stateEntries: "Записей состояния",
      poolMode: "Режим пула",
      noProfile: "профиля ещё нет",
    },

    chart: {
      trades: "Покупки и продажи",
      tradesSub: "Последние {days} дней",
      tradesEmpty: "Сделок пока нет. График заполнится по мере покупок и продаж на барахолке.",
      buys: "Покупки",
      sells: "Продажи",
      day: "День",

      factors: "Распределение ценового фактора",
      factorsSub: "Предметов на корзину фактора",
      factorsCaption: "{above} выше базы, {at} по базе, {below} ниже базы",
      factorsEmpty: "Предметов с ценами пока нет.",
      atBase: "по базе",
      items: "предметов",

      demand: "Горячий спрос",
      supply: "Горячее предложение",
      heatSub: "Топ-10 по давлению",
      demandEmpty: "Сильного спроса сейчас нет.",
      supplyEmpty: "Сильного предложения сейчас нет.",
      factor: "Фактор",
      demandLabel: "Спрос",
      supplyLabel: "Предложение",

      categories: "Обзор категорий",
      categoriesSub: "Средний фактор; толщина полосы показывает число предметов",
      categoriesEmpty: "Категорий в экономике пока нет.",
      avgFactor: "Средний фактор",
      avgDemand: "Средний спрос",
      avgSupply: "Среднее предложение",

      movers: "Движение цен",
      moversSub: "Сильнее всего отклонились от базовой цены",
      moversEmpty: "Пока ничто не отклонилось от базовой цены.",
      up: "Вверх",
      down: "Вниз",
      basePrice: "База",
      currentPrice: "Сейчас",

      pressure: "Давление спроса и предложения",
      pressureSub: "x = спрос - предложение, y = ценовой фактор",
      pressureCaption: "Каждая точка обозначает один предмет. Точки справа от нулевой линии тянет спрос, и они обычно выше линии 1.0; точки слева перенасыщены предложением и уходят ниже неё.",
      pressureEmpty: "Пока недостаточно смоделированных предметов для графика давления.",

      wipe: "Кривая раннего вайпа",
      wipeSub: "Прогноз множителя по возрасту вайпа",
      wipeOff: "Цены раннего вайпа отключены в economy.json.",
      wipeNoCurve: "Ранний вайп включён, но startLengthDays равен 0, поэтому рампу строить не из чего.",
      wipeNoProfile: "Профиль пока не найден, поэтому день вайпа неизвестен и «сегодня» не отмечено.",
      wipeToday: "Сегодня",
      wipeCurrent: "Текущий множитель",
      wipeStart: "Длина старта {days} д, множитель старта ×{mult}",
      multiplier: "Множитель",

      events: "Хронология событий",
      eventsSub: "За первые {days} дней вайпа",
      eventsOff: "Система событий отключена в events.json.",
      eventsEmpty: "События не настроены.",
      active: "Активно",
      inactive: "Неактивно",
      eventDays: "Дни {from}-{to}",

      noData: "Нет данных",
      total: "Всего",
      ariaTrades: "Линейный график покупок и продаж на барахолке за {days} дней. Всего {buys} покупок и {sells} продаж.",
      ariaFactors: "Гистограмма: {n} предметов по 7 корзинам ценового фактора.",
      ariaRanked: "Рейтинговая столбчатая диаграмма по {n} предметам.",
      ariaCategories: "Рейтинговая столбчатая диаграмма по {n} категориям и их среднему ценовому фактору.",
      ariaMovers: "Двусторонняя диаграмма: {n} предметов выше и ниже базовой цены.",
      ariaPressure: "Диаграмма рассеяния: {n} предметов по разнице спроса и предложения против ценового фактора.",
      ariaWipe: "Линейный график множителя цен раннего вайпа за {n} дней вайпа.",
      ariaEvents: "Хронология {n} настроенных рыночных событий за {days} дней вайпа.",
      ariaSpark: "Спарклайн за последние {n} дней.",
    },

    state: {
      search: "Фильтр по шаблону предмета…",
      rows: "{shown} из {total} строк",
      page: "Страница {page} из {pages}",
      readonly: "Только чтение. Данные берутся прямо из работающей симуляции экономики.",
      noRows: "Под этот фильтр ничего не подходит.",
      noState: "Состояние экономики пусто. Оно заполнится после первого тика симуляции.",
      col: {
        tpl: "Шаблон",
        basePrice: "Базовая цена",
        currentPrice: "Текущая цена",
        factor: "Фактор",
        demand: "Спрос",
        supply: "Предложение",
      },
    },

    overview: {
      activeEvents: "Активные события",
      system: "Система",
      version: "Версия",
      webUi: "Веб-интерфейс",
      priceSource: "Источник цен",
      poolManager: "Менеджер пула",
      pricing: "Цены",
      economySim: "Симуляция экономики",
      playerOffersOnly: "Только лоты игроков",
      quickList: "Быстрый листинг из схрона",
      stateFile: "Файл состояния",
      present: "есть",
      missing: "отсутствует",
      levels: "Блокировки по уровню",
      levelsDetail: "{items} предметов, {categories} категорий, {rules} правил",
      poolDetail: "+{add} / -{remove} предметов, +{addCat} / -{removeCat} категорий",
      trends: "Тренды",
      trendsDetail: "настроено: {total}",
      sptDefaults: "Стандарты SPT",
    },

    presets: {
      intro: "Пресет целиком заменяет конфиги готовым набором значений. Сначала посмотрите превью: применение перезаписывает текущие значения всех конфигов, которых он касается.",
      preview: "Превью",
      hidePreview: "Скрыть превью",
      loadingPreview: "Загрузка превью…",
      previewFailed: "Не удалось загрузить превью",
      apply: "Применить",
      applying: "Применение…",
      requires: "Нужно",
      touches: "Затрагивает",
      unavailable: "Недоступен",
      empty: "Пресетов нет.",
      absent: "На этом сервере пресеты недоступны",
      absentHint: "В этой сборке сервера пресетов ещё нет. Всё остальное работает как обычно; обновите мод, чтобы они появились.",
      loadFailed: "Не удалось загрузить пресеты",
      confirm: "Применить пресет «{name}»?\n\nОн ПЕРЕЗАПИШЕТ текущие значения этих конфигов: {configs}.\nВаши нынешние настройки в них будут заменены.",
      applied: "Пресет применён: {configs}.",
      appliedRestart: "Пресет применён. Для полного эффекта перезапустите сервер.",
      applyFailed: "Не удалось применить пресет",
      appliedTo: "Применено к",
      openConfig: "Открыть: {name}",
    },

    links: {
      label: "Ссылки проекта",
      githubTip: "UltimateFlea на GitHub (откроется в новой вкладке)",
      spmodTip: "UltimateFlea на sp-mod (откроется в новой вкладке)",
    },

    idname: {
      unknown: "Неизвестный ID",
      unknownHint: "Нет в списке предметов: ID неверный или мод, который его добавляет, не установлен.",
    },

    picker: {
      pick: "Выбрать",
      pickTitle: "Выбрать из списка предметов вместо ввода ID руками",
      titleItem: "Выбор предметов",
      titleCategory: "Выбор категорий",
      search: "Поиск по названию (EN или RU), короткому имени или ID…",
      source: "Источник",
      allSources: "Все источники",
      vanilla: "Ванильные",
      category: "Категория",
      allCategories: "Все категории",
      loading: "Загружаю список предметов…",
      absent: "Список предметов недоступен на этом сервере",
      absentHint: "В этой сборке сервера ещё нет каталога предметов. ID по-прежнему можно вводить руками.",
      loadFailed: "Не удалось загрузить список предметов",
      noMatches: "Ничего не найдено.",
      count: "Найдено: {n}",
      capped: "Показаны первые {shown} из {n}. Уточните поиск, чтобы увидеть остальное.",
      selected: "Выбрано: {n}",
      insert: "Вставить",
      have: "Уже есть",
      keys: "Стрелки двигают, Enter выбирает, Ctrl+Enter вставляет, Esc закрывает.",
      unknownSource: "Неизвестный источник",
      unknownSourceHint: "Это не ванильный предмет, но определить, какой мод его добавил, не удалось.",
      iconNote: "Картинки есть только у предметов, которые вы уже видели в игре; у остальных значок категории.",
      added: "Добавлено: {n}.",
      addedSkipped: "Добавлено: {n}, уже были: {skipped}.",
    },

    help: {
      mod: {
        enabled: "Главный рубильник мода. Если выключено, при старте сервера мод не трогает ни пул, ни цены, ни экономику, ни блокировки по уровню. Работает после перезапуска.",
        enablePoolManager: "Применяет pool.json: какие предметы вообще допускаются на барахолку. Если выключено, остаётся стандартный пул SPT.",
        enablePricing: "Подставляет цены из pricing.json (множители, фиксированные цены) при старте сервера. Расчёт цен запускается, если включено это или «Экономика», так что при включённой экономике флаг ничего не добавляет; сам по себе даёт статичные цены без симуляции.",
        enableEconomy: "Запускает симуляцию рынка: покупки толкают цену вверх, продажи вниз, а возраст вайпа, события и тренды сдвигают её со временем. Если выключено, после старта цены стоят на месте, а economy, events и trends ни на что не влияют.",
        liveFlea: "Если включено, базовые цены тянутся из интернета у провайдера из Price source. Если выключено, используются только локальные цены из хэндбука, в сеть мод не лезет вообще. Если загрузка не удалась, берётся кэш с диска, а потом локальные цены.",
        priceSource: "Откуда брать цены, когда включён Live flea: tarkovdev (средняя за 24 часа с json.tarkov.dev, по умолчанию) или tarkovmarket (экспериментально, нужен ключ API). При выключенном Live flea не используется.",
        tarkovMarketApiKey: "Ключ API tarkov-market. Нужен, только если включён Live flea и выбран tarkovmarket; с пустым ключом мод возьмёт tarkovdev. В лог не пишется.",
        priceFetchTimeoutSeconds: "Сколько ждать одну попытку скачать живые цены (около 17 МБ). По умолчанию 180, допустимо 10-1800. На медленном интернете поднимите. Важно только при включённом Live flea.",
        pvePrices: "Брать с tarkov.dev цены PvE вместо обычных. Имеет смысл только при включённом Live flea и источнике tarkovdev.",
        preserveBasePrices: "Не даёт SPT перезаписать выставленные модом цены заново из хэндбука. Лучше оставить включённым. Применяется при старте сервера.",
        disableTraderPriceFloor: "Разрешает лотам стоить меньше, чем платит торговец; обычно SPT подтягивает такую цену до торговой. Включайте, если хотите, чтобы дешёвое реально было дешёвым. Применяется при старте сервера.",
        exactOfferPrices: "Убирает случайный разброс цен лотов 0.8-1.2, который SPT добавляет по умолчанию: все лоты встают ровно по цене. Касается всей барахолки. Применяется при старте сервера.",
        playerOffersOnly: "Только лоты реальных игроков: без сгенерированных (FakePlayer) и без ассортимента торговцев. Пока игроки сами ничего не выставят, барахолка пустая. Применяется при старте сервера.",
        quickListFromStash: "Клиентская опция: ПКМ по предмету в схроне показывает «UltimateFlea: List», предмет сразу выставляется, а в уведомлении видны цена и налог. Нужен клиентский плагин. Если выключено, пункт меню скрыт.",
        debug: "Подробный лог сервера: стадия вайпа, активные события и тренды, каждый тик экономики. Нужен только для отладки, лог сильно разрастается.",
      },
      pool: {
        mode: "blacklist: остаётся обычный пул SPT, меняется только то, что описано в правилах. whitelist: на барахолке остаётся только разрешённое (add, addCategories), всё остальное убирается.",
        add: "ID предметов (TPL), которые принудительно попадают на барахолку. Конкретный предмет важнее правил по категориям, а add важнее remove для того же предмета.",
        remove: "ID предметов (TPL), которые принудительно убираются с барахолки, что бы ни говорили правила по категориям.",
        addCategories: "ID категорий (родительский класс предметов), чьи предметы попадают на барахолку. В режиме whitelist это список разрешённых категорий; оставите пустым, и выживут только предметы из add.",
        removeCategories: "ID категорий, чьи предметы убираются с барахолки. Только для blacklist; в whitelist игнорируется.",
      },
      pricing: {
        globalMultiplier: "Множитель на все плавающие базовые цены. 1.0 без изменений, 2.0 всё вдвое дороже. Фиксированных цен не касается. Пересобирается при старте сервера.",
        fixedPrices: "ID предмета и жёсткая цена. Такие предметы не трогают ни множители, ни симуляция, цена никогда не двигается. Пересобирается при старте сервера.",
        itemMultipliers: "ID предмета и множитель на его базовую цену. Перемножается с общим и категорийным. Для предметов с фиксированной ценой игнорируется.",
        categoryMultipliers: "ID категории (родительский класс предметов) и множитель для всех предметов в ней. Перемножается с общим и предметным. Для предметов с фиксированной ценой игнорируется.",
      },
      economy: {
        simIntervalMinutes: "Как часто тикает симуляция рынка, в реальных минутах. По умолчанию 60, минимум 1, разумно 15-180. Чем меньше, тем быстрее цены реагируют и тем чаще перезаписывается файл состояния.",
        demandPerBuy: "Сколько спроса добавляет каждая покупка предмета (спрос ограничен 1.0). По умолчанию 0.03, допустимо 0-1, разумно 0.01-0.1. Чем больше значение, тем быстрее растёт цена, когда игроки покупают.",
        supplyPerSell: "Сколько предложения добавляет каждый проданный лот игрока (ограничено 1.0). По умолчанию 0.03, допустимо 0-1, разумно 0.01-0.1. Чем больше значение, тем быстрее падает цена, когда игроки продают.",
        priceElasticity: "Насколько сильно разница спроса и предложения двигает целевую цену: база × (1 + эластичность × (спрос - предложение)). По умолчанию 0.3, допустимо 0-10, разумно 0.1-1. При 0 сделки цену не двигают.",
        decayPerTick: "Какая доля накопленного спроса и предложения затухает каждый тик, чтобы старые сделки забывались. По умолчанию 0.08, допустимо 0-1, разумно 0.03-0.2. При 0 ничего не забывается.",
        settleSpeed: "Какую часть разрыва до целевой цены цена закрывает за тик. По умолчанию 0.1, допустимо 0-1, разумно 0.05-0.3. Малое значение даёт медленный плавный дрейф, а 1 прыгает сразу в цель.",
        noise: "Случайный шум целевой цены на каждом тике, до ± этого значения (0.03 означает ±3%). По умолчанию 0.03, минимум 0, разумно 0-0.1. При 0 случайных колебаний нет.",
        minPriceFactor: "Нижняя граница цены в долях базовой: ниже база × это значение она не упадёт. По умолчанию 0.7, минимум 0.01, разумно 0.3-1.",
        maxPriceFactor: "Верхняя граница цены в долях базовой: выше база × это значение она не поднимется. По умолчанию 2.0, не ниже минимума, разумно 1.5-3. Она же режет надбавки раннего вайпа, событий и трендов.",
        wipe: "Надбавка раннего вайпа: сразу после вайпа цены выше и постепенно возвращаются к норме. Возраст вайпа считается от даты регистрации самого старого PMC-профиля.",
        wipe_enabled: "Включает надбавку раннего вайпа. Больше всего толку с локальными ценами; с tarkovdev лучше выключить, живые средние уже учитывают стадию вайпа.",
        wipe_startLengthDays: "Сколько дней длится спад надбавки. По умолчанию 14. При 0 надбавка отключена.",
        wipe_startMultiplier: "Множитель цены в нулевой день вайпа, линейно спадает до ×1.0 к концу периода. По умолчанию 1.8, минимум 0.01. Всё равно упирается в максимальный фактор цены.",
      },
      events: {
        enabled: "Главный выключатель событий по времени. Если выключено, события ниже игнорируются. Нужна ещё и включённая экономика.",
        events: "Временные скачки цен, привязанные к возрасту вайпа. Пока возраст вайпа в окне дней события, его множитель действует на перечисленные предметы и категории. Пересекающиеся события перемножаются.",
        events_id: "Название события. Просто метка для вас и панели, на работу не влияет.",
        events_startDay: "День вайпа, с которого событие стартует (включительно). Считается от даты регистрации самого старого PMC-профиля.",
        events_endDay: "День вайпа, на котором событие заканчивается (не включительно): в этот день оно уже выключено.",
        events_multiplier: "Множитель цены, пока событие активно: 1.15 даёт +15%, 0.9 даёт -10%. Применяется к целевой цене, которая всё равно ограничена минимальным и максимальным фактором.",
        events_categories: "ID категорий (родительский класс предметов), на которые действует событие.",
        events_items: "ID отдельных предметов (TPL), на которые действует событие. Если оба списка пусты, событие ничего не делает.",
      },
      trends: {
        enabled: "Главный выключатель трендов по категориям. По умолчанию выкл. Нужна ещё и включённая экономика.",
        trends: "Медленный синусоидальный дрейф цен категории по возрасту вайпа: 1 + амплитуда × sin(2π × день / период + фаза). Несколько трендов на одну категорию перемножаются.",
        trends_category: "ID категории (родительский класс предметов), к которой применяется волна. Пустые записи игнорируются.",
        trends_periodDays: "Длина одного полного цикла вверх-вниз, в днях. По умолчанию 21. Должна быть больше 0, иначе запись игнорируется.",
        trends_amplitude: "Максимальное отклонение от 1.0: 0.15 качает цены примерно на ±15%. По умолчанию 0.15.",
        trends_phase: "Сдвиг волны в радианах. при 0 старт с обычной цены и вверх, при значении около 1.57 старт с пика. По умолчанию 0.",
      },
      levels: {
        enabled: "Главный выключатель блокировок по уровню: игроки ниже нужного уровня не могут покупать и продавать заблокированные предметы на барахолке. По умолчанию выкл; сам мод тоже должен быть включён. Применяется при сохранении.",
        useSptDefaults: "Взять за основу встроенные уровни tiered flea из SPT (как в живой игре) и накатить поверх свои предметы и категории. Если выключено, только ваши правила.",
        lockBuying: "Запретить покупку заблокированных предметов ниже нужного уровня. Заодно интерфейс барахолки показывает такие лоты заблокированными.",
        lockSelling: "Запретить выставлять и продавать заблокированные предметы ниже нужного уровня.",
        items: "ID предмета и уровень игрока, нужный для него. 0 снимает блокировку, пришедшую из дефолтов SPT. Правило по предмету важнее правила по категории.",
        categories: "ID категории и нужный уровень игрока, вложенные подкатегории включены. 0 снимает блокировку по умолчанию.",
      },
    },
  },
};

/* ----------------------------------------------------------------- 3. utils */

function escapeHtml(s) {
  return String(s ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

/** Look up an i18n string by dotted path, falling back to English then the path. */
function t(path) {
  const walk = (dict) => {
    let cur = dict;
    for (const p of path.split(".")) cur = cur == null ? cur : cur[p];
    return cur;
  };
  const v = walk(I18N[lang]);
  if (typeof v === "string") return v;
  const en = walk(I18N.en);
  return typeof en === "string" ? en : path;
}

/** t() plus {placeholder} interpolation. */
function tp(path, vars) {
  return t(path).replace(/\{(\w+)\}/g, (_, k) => (vars && k in vars ? String(vars[k]) : `{${k}}`));
}

function fmt(v, digits = 3) {
  if (v == null || (typeof v === "number" && !Number.isFinite(v))) return "-";
  if (typeof v !== "number") return String(v);
  const r = Math.round(v * 10 ** digits) / 10 ** digits;
  return String(r);
}

function fmtInt(v) {
  const n = Number(v);
  if (!Number.isFinite(n)) return "-";
  return n.toLocaleString(lang === "ru" ? "ru-RU" : "en-US");
}

function fmtFactor(v) {
  return Number.isFinite(v) ? v.toFixed(2) : "-";
}

function clamp(v, lo, hi) {
  return v < lo ? lo : v > hi ? hi : v;
}

function isPlainObject(v) {
  return v != null && typeof v === "object" && !Array.isArray(v);
}

function deepClone(v) {
  return JSON.parse(JSON.stringify(v ?? null));
}

function getAt(obj, parts) {
  let cur = obj;
  for (const p of parts) cur = cur == null ? cur : cur[p];
  return cur;
}

function setAt(obj, parts, value) {
  let cur = obj;
  for (let i = 0; i < parts.length - 1; i++) cur = cur[parts[i]];
  cur[parts[parts.length - 1]] = value;
}

function truncate(s, max) {
  const str = String(s ?? "");
  return str.length <= max ? str : `${str.slice(0, Math.max(1, max - 1))}…`;
}

/** Config keys keep their English spelling; these just read better capitalised. */
const ACRONYMS = { spt: "SPT", tpl: "TPL", id: "ID", ui: "UI", json: "JSON", pve: "PvE", pvp: "PvP" };

/** Turn a config key into a readable label: enablePoolManager -> "Enable Pool Manager". */
function humanKey(key) {
  const spaced = String(key)
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .trim();
  const words = spaced.split(/\s+/).map((w) => ACRONYMS[w.toLowerCase()] || w);
  const first = words[0] || "";
  words[0] = first.charAt(0).toUpperCase() + first.slice(1);
  return words.join(" ");
}

/* ------------------------------------------------------------- 4. app state */

const SECTIONS = [
  { id: "home", kind: "overview", group: "overview", icon: "home" },
  { id: "presets", kind: "presets", group: "market", icon: "presets" },
  { id: "mod", kind: "config", name: "mod", group: "market", icon: "sliders" },
  { id: "pool", kind: "config", name: "pool", restart: true, group: "market", icon: "layers" },
  { id: "pricing", kind: "config", name: "pricing", restart: true, group: "market", icon: "tag" },
  { id: "economy", kind: "config", name: "economy", restart: true, group: "market", icon: "activity" },
  { id: "events", kind: "config", name: "events", group: "market", icon: "calendar" },
  { id: "trends", kind: "config", name: "trends", group: "market", icon: "wave" },
  { id: "levels", kind: "config", name: "levels", group: "market", icon: "lock" },
  { id: "selling", kind: "selling", group: "client", icon: "cart" },
  { id: "settings", kind: "settings", group: "system", icon: "gear" },
  { id: "state", kind: "state", group: "system", icon: "table" },
];

const GROUP_ORDER = ["overview", "market", "client", "system"];

/** Outbound project links in the sidebar footer. Plain anchors: nothing is loaded from these hosts. */
const SIDE_LINKS = [
  { id: "github", label: "GitHub", href: "https://github.com/SVAR316/UltimateFlea", icon: "github" },
  { id: "spmod", label: "sp-mod", href: "https://sp-mod.com/mod/2844/ultimate-flea", icon: "cube" },
];

let root = null;
let sectionId = "home";
let lang = loadLang();
let theme = loadTheme();
let busy = false;
let loadToken = 0;

/** Brand block info, refreshed whenever /overview is fetched. */
let brand = { version: "", enabled: null };

/** Extra config names reported by GET /config but unknown to SECTIONS. */
let extraConfigs = [];

/** Config editor state for the current section. */
let cfg = null;

/** Overview state. */
let autoRefresh = false;
let autoTimer = 0;

/** State table. */
let stateRaw = null;
let stateRows = [];
let stateQuery = "";
let stateSort = { key: "factor", dir: -1 };
let statePage = 0;
let stateView = "table";

function loadLang() {
  try {
    return localStorage.getItem(LANG_KEY) === "ru" ? "ru" : "en";
  } catch {
    return "en";
  }
}

function loadTheme() {
  try {
    const v = localStorage.getItem(THEME_KEY);
    if (v === "light" || v === "dark") return v;
  } catch {
    /* ignore */
  }
  return window.matchMedia?.("(prefers-color-scheme: light)").matches ? "light" : "dark";
}

function applyTheme() {
  document.documentElement.setAttribute("data-theme", theme);
  colorCache = null;
}

function setTheme(next) {
  theme = next === "light" ? "light" : "dark";
  try { localStorage.setItem(THEME_KEY, theme); } catch { /* ignore */ }
  applyTheme();
  render();
  loadSection();
}

function setLang(next) {
  lang = next === "ru" ? "ru" : "en";
  try { localStorage.setItem(LANG_KEY, lang); } catch { /* ignore */ }
  document.documentElement.lang = lang;
  render();
  loadSection();
}

function isDirty() {
  return !!cfg && cfg.dirty;
}

function sectionList() {
  const extras = extraConfigs.map((name) => ({
    id: `cfg-${name}`, kind: "config", name, group: "market", icon: "sliders", raw: true,
  }));
  return [...SECTIONS, ...extras].map((s) => ({
    ...s,
    label: s.raw ? humanKey(s.name) : t(`nav.${s.id}`),
  }));
}

function currentSection() {
  const list = sectionList();
  return list.find((s) => s.id === sectionId) || list[0];
}

/* --------------------------------------------------------------- 5. toasts */

let toastSeq = 0;

function toast(message, kind = "") {
  const host = root?.querySelector("[data-toasts]");
  if (!host || !message) return;

  const id = `t${++toastSeq}`;
  const el = document.createElement("div");
  el.className = `toast ${kind}`.trim();
  el.setAttribute("role", kind === "err" ? "alert" : "status");
  el.dataset.toastId = id;
  el.innerHTML = `
    <span class="toast-msg"></span>
    <button class="toast-x" type="button" aria-label="${escapeHtml(t("act.dismiss"))}">✕</button>
  `;
  el.querySelector(".toast-msg").textContent = message;
  el.querySelector(".toast-x").addEventListener("click", () => dismissToast(el));
  host.appendChild(el);

  const ms = kind === "err" ? 7000 : 4500;
  window.setTimeout(() => dismissToast(el), ms);

  // Never stack more than 4.
  while (host.children.length > 4) dismissToast(host.firstElementChild, true);
}

function dismissToast(el, now = false) {
  if (!el || el.dataset.going) return;
  el.dataset.going = "1";
  if (now) { el.remove(); return; }
  el.classList.add("out");
  window.setTimeout(() => el.remove(), 260);
}

/* ------------------------------------------------------------------ 6. api */

async function getJson(url) {
  const res = await fetch(url, { headers: { Accept: "application/json" } });
  if (!res.ok) {
    // Keep the URL and status for the console; only the server's own {error} text is shown to users.
    const body = await res.json().catch(() => null);
    const err = new Error(`${url} -> HTTP ${res.status}`);
    err.status = res.status;
    err.serverMessage = body ? localized(body.error) : "";
    throw err;
  }
  return res.json();
}

/**
 * User-facing text for a failed request: a plain sentence, plus the server's own message when it
 * sent one. URLs and HTTP codes go to the console, never to the page.
 */
function failText(e, generic) {
  console.error(generic, e);
  const detail = e && typeof e === "object" ? e.serverMessage : "";
  return detail ? `${generic}: ${detail}` : generic;
}

async function postJson(url, body) {
  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body ?? {}),
  });
  const json = await res.json().catch(() => ({}));
  return { ok: res.ok, status: res.status, json };
}

async function saveConfig(name, body) {
  const { ok, status, json } = await postJson(`${API}/config/${encodeURIComponent(name)}`, body);
  if (!ok) {
    toast(localized(json.error) || t("toast.saveFailed"), "err");
    return false;
  }
  const warning = localized(json.warning);
  if (warning) toast(warning, "warn");
  else if (json.restartRecommended) toast(t("toast.savedRestart"), "warn");
  else toast(t("toast.saved"), "ok");
  return true;
}

async function reloadFromDisk() {
  const { ok, status, json } = await postJson(`${API}/reload`, {});
  if (!ok) {
    toast(localized(json.error) || t("toast.reloadFailed"), "err");
    return;
  }
  toast(localized(json.note) || t("toast.reloaded"), "ok");
}

/* ---------------------------------------------------------------- 7. icons */

const ICON_PATHS = {
  home: '<path d="M2.5 7.2 8 2.8l5.5 4.4V13a.5.5 0 0 1-.5.5H10v-4H6v4H3a.5.5 0 0 1-.5-.5V7.2Z"/>',
  sliders: '<path d="M2.5 4.5h7M11.5 4.5h2M2.5 11.5h2M6.5 11.5h7"/><circle cx="10.5" cy="4.5" r="1.6"/><circle cx="4.5" cy="11.5" r="1.6"/>',
  layers: '<path d="M8 2.2 14 5.3 8 8.4 2 5.3l6-3.1Z"/><path d="M2 8.4 8 11.5l6-3.1"/><path d="M2 11.2 8 14.3l6-3.1"/>',
  tag: '<path d="M8.4 2.2H13a.8.8 0 0 1 .8.8v4.6L7.4 14 2 8.6l6.4-6.4Z"/><circle cx="11" cy="5" r="1"/>',
  activity: '<path d="M1.8 8.5h3L6.6 4l2.6 8 1.6-3.5h3.4"/>',
  calendar: '<rect x="2.2" y="3.4" width="11.6" height="10.4" rx="1.2"/><path d="M2.2 6.6h11.6M5.4 2.2v2.4M10.6 2.2v2.4"/>',
  wave: '<path d="M1.6 9.4c1.4 0 1.4-4.2 2.8-4.2s1.4 4.2 2.8 4.2 1.4-4.2 2.8-4.2 1.4 4.2 2.8 4.2 1.4-1.6 1.6-1.6"/>',
  lock: '<rect x="3.2" y="7" width="9.6" height="6.8" rx="1.2"/><path d="M5.6 7V5.3a2.4 2.4 0 0 1 4.8 0V7"/>',
  cart: '<circle cx="6" cy="13" r="1.1"/><circle cx="11.6" cy="13" r="1.1"/><path d="M1.6 2.6h1.8l1.9 7.3h7.1l1.4-5.2H4.1"/>',
  gear: '<circle cx="8" cy="8" r="2.2"/><path d="M8 1.6v1.8M8 12.6v1.8M1.6 8h1.8M12.6 8h1.8M3.5 3.5l1.3 1.3M11.2 11.2l1.3 1.3M12.5 3.5l-1.3 1.3M4.8 11.2l-1.3 1.3"/>',
  presets: '<path d="M4.2 2.4h7.6a.8.8 0 0 1 .8.8v10.4L8 10.9l-4.6 2.7V3.2a.8.8 0 0 1 .8-.8Z"/><path d="M6 5.6h4"/>',
  close: '<path d="M3.6 3.6l8.8 8.8M12.4 3.6l-8.8 8.8"/>',
  search: '<circle cx="7" cy="7" r="4.2"/><path d="M10.2 10.2l3.4 3.4"/>',
  chevron: '<path d="M4 6.2l4 4 4-4"/>',
  check: '<path d="M3.4 8.4l3 3 6.2-6.8"/>',
  github: '<circle cx="4.5" cy="3.8" r="1.5"/><circle cx="4.5" cy="12.2" r="1.5"/><circle cx="11.5" cy="6.2" r="1.5"/><path d="M4.5 5.3v5.4M11.5 7.7c0 2.6-3 2.4-7 3.6"/>',
  cube: '<path d="M8 1.8l5.6 3.1v6.2L8 14.2l-5.6-3.1V4.9L8 1.8Z"/><path d="M2.4 4.9L8 8l5.6-3.1M8 8v6.2"/>',
  table: '<rect x="2.2" y="2.8" width="11.6" height="10.4" rx="1.2"/><path d="M2.2 6.2h11.6M2.2 9.8h11.6M6.4 2.8v10.4"/>',
  flea: '<path d="M2.4 6.2 3.8 2.6h8.4l1.4 3.6"/><path d="M2.4 6.2a1.8 1.8 0 0 0 3.6 0 1.8 1.8 0 0 0 3.6 0 1.8 1.8 0 0 0 3.6 0"/><path d="M3.8 7.8v5.6h8.4V7.8"/>',
  sun: '<circle cx="8" cy="8" r="3"/><path d="M8 1.4v1.6M8 13v1.6M1.4 8h1.6M13 8h1.6M3.4 3.4l1.1 1.1M11.5 11.5l1.1 1.1M12.6 3.4l-1.1 1.1M4.5 11.5l-1.1 1.1"/>',
  moon: '<path d="M13 9.6A5.6 5.6 0 0 1 6.4 3a5.6 5.6 0 1 0 6.6 6.6Z"/>',
  trash: '<path d="M3 4.6h10M6.2 4.6V3.2h3.6v1.4M4.4 4.6l.6 8.2h6l.6-8.2"/>',
  plus: '<path d="M8 3.4v9.2M3.4 8h9.2"/>',
  reload: '<path d="M13.4 8a5.4 5.4 0 1 1-1.9-4.1"/><path d="M13.6 2.2v3.2h-3.2"/>',
  eye: '<path d="M1.4 8S3.8 3.6 8 3.6 14.6 8 14.6 8 12.2 12.4 8 12.4 1.4 8 1.4 8Z"/><circle cx="8" cy="8" r="2.1"/>',
  eyeOff: '<path d="M6.3 4a5.6 5.6 0 0 1 1.7-.3c4.2 0 6.6 4.3 6.6 4.3a11 11 0 0 1-2 2.5M4 5.3A11 11 0 0 0 1.4 8s2.4 4.4 6.6 4.4a5.9 5.9 0 0 0 2.5-.6"/><path d="M6.6 6.7a2.1 2.1 0 0 0 2.9 2.9"/><path d="m2.4 2.4 11.2 11.2"/>',
};

function icon(name, size = 16) {
  const d = ICON_PATHS[name] || ICON_PATHS.sliders;
  return `<svg width="${size}" height="${size}" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">${d}</svg>`;
}

/* =========================================================================
 * 8. SVG chart module
 *
 *   chartMount(hostEl, drawFn)  -> registers hostEl, draws, keeps it sized
 *   drawFn(width) -> HTML string (an <svg> or an empty-state <div>)
 *
 * Shared pieces: colors() reads the CSS tokens, scale()/ticks() do the math,
 * axisGrid() draws the frame, and any element carrying data-tip gets the one
 * floating tooltip on hover or keyboard focus.
 * ========================================================================= */

const chartRegistry = new Map();
let chartObserver = null;
let colorCache = null;
let tipEl = null;
let tipBound = false;

/** All chart colours, read once per theme from the token block. */
function colors() {
  if (colorCache) return colorCache;
  const cs = getComputedStyle(document.documentElement);
  const g = (n, fallback) => cs.getPropertyValue(n).trim() || fallback;
  colorCache = {
    buys: g("--c-buys", "#5ca8e8"),
    sells: g("--c-sells", "#4fc08a"),
    neutral: g("--c-neutral", "#8c7ae0"),
    amber: g("--c-amber", "#e0b33c"),
    up: g("--c-up", "#4fc08a"),
    down: g("--c-down", "#e8706f"),
    grid: g("--c-grid", "#242c38"),
    axis: g("--c-axis", "#768294"),
    point: g("--c-point", "#5ca8e8"),
    accent: g("--accent", "#4fc08a"),
    text: g("--text", "#e7edf6"),
    dim: g("--text-dim", "#aab6c7"),
  };
  return colorCache;
}

function chartMount(hostEl, drawFn) {
  if (!hostEl) return;
  chartRegistry.set(hostEl, drawFn);
  ensureChartObserver();
  chartObserver.observe(hostEl);
  paintChart(hostEl);
}

function paintChart(hostEl) {
  const fn = chartRegistry.get(hostEl);
  if (!fn) return;
  const w = Math.max(260, Math.round(hostEl.clientWidth || 600));
  if (hostEl.dataset.w === String(w)) return;
  hostEl.dataset.w = String(w);
  hostEl.innerHTML = fn(w);
}

function ensureChartObserver() {
  if (chartObserver) return;
  let queued = false;
  chartObserver = new ResizeObserver((entries) => {
    if (queued) return;
    queued = true;
    requestAnimationFrame(() => {
      queued = false;
      for (const e of entries) {
        if (chartRegistry.has(e.target)) paintChart(e.target);
      }
    });
  });
}

function clearCharts() {
  if (chartObserver) {
    for (const el of chartRegistry.keys()) chartObserver.unobserve(el);
  }
  chartRegistry.clear();
}

/* ---- tooltip (one shared element, delegated) ---- */

function ensureTooltip() {
  if (tipBound) return;
  tipBound = true;
  tipEl = document.createElement("div");
  tipEl.className = "uf-tip";
  tipEl.setAttribute("role", "tooltip");
  document.body.appendChild(tipEl);

  const show = (target, x, y) => {
    const text = target.getAttribute("data-tip");
    if (!text) return;
    tipEl.textContent = text;
    tipEl.classList.add("show");
    const pad = 10;
    const r = tipEl.getBoundingClientRect();
    const left = clamp(x, r.width / 2 + pad, window.innerWidth - r.width / 2 - pad);
    const top = clamp(y, r.height + pad, window.innerHeight - pad);
    tipEl.style.left = `${left}px`;
    tipEl.style.top = `${top}px`;
  };
  const hide = () => tipEl.classList.remove("show");

  document.addEventListener("pointermove", (e) => {
    const target = e.target instanceof Element ? e.target.closest("[data-tip]") : null;
    if (target) show(target, e.clientX, e.clientY);
    else hide();
  }, { passive: true });

  document.addEventListener("focusin", (e) => {
    const target = e.target instanceof Element ? e.target.closest("[data-tip]") : null;
    if (!target) { hide(); return; }
    const b = target.getBoundingClientRect();
    show(target, b.left + b.width / 2, b.top);
  });

  document.addEventListener("focusout", hide);
  document.addEventListener("scroll", hide, { passive: true, capture: true });
  window.addEventListener("keydown", (e) => { if (e.key === "Escape") hide(); });
}

/* ---- scales + frame helpers ---- */

function scale(d0, d1, r0, r1) {
  const span = d1 - d0 || 1;
  return (v) => r0 + ((v - d0) / span) * (r1 - r0);
}

/** A "nice" upper bound and evenly spaced tick values for 0..max. */
function niceTicks(max, count = 4) {
  const safeMax = max > 0 ? max : 1;
  const raw = safeMax / count;
  const mag = 10 ** Math.floor(Math.log10(raw));
  const step = [1, 2, 2.5, 5, 10].find((m) => m * mag >= raw) * mag;
  const top = Math.ceil(safeMax / step) * step;
  const out = [];
  for (let v = 0; v <= top + step / 2; v += step) out.push(Math.round(v * 1e6) / 1e6);
  return { top, ticks: out };
}

function svgWrap(w, h, ariaLabel, body) {
  return `<svg viewBox="0 0 ${w} ${h}" width="${w}" height="${h}" role="img" aria-label="${escapeHtml(ariaLabel)}">`
    + `<title>${escapeHtml(ariaLabel)}</title>${body}</svg>`;
}

function emptyChart(message) {
  return `<div class="empty">${icon("activity", 20)}<span>${escapeHtml(message)}</span></div>`;
}

/** Horizontal gridlines + y labels + the left/bottom axis lines. */
function yGrid(pad, plotW, plotH, ticks, yScale, labelFn) {
  const c = colors();
  let out = "";
  for (const v of ticks) {
    const y = yScale(v);
    out += `<line class="c-grid" x1="${pad.l}" y1="${y.toFixed(1)}" x2="${pad.l + plotW}" y2="${y.toFixed(1)}" stroke="${c.grid}"/>`;
    out += `<text class="c-axis" x="${pad.l - 6}" y="${(y + 3.5).toFixed(1)}" text-anchor="end" fill="${c.axis}">${escapeHtml(labelFn(v))}</text>`;
  }
  out += `<line x1="${pad.l}" y1="${pad.t}" x2="${pad.l}" y2="${pad.t + plotH}" stroke="${c.grid}"/>`;
  return out;
}

/* ---- chart 1: trades, 30 days ------------------------------------------- */

function chartTrades(w, trades) {
  const days = trades?.days || [];
  if (!days.length) return emptyChart(t("chart.tradesEmpty"));

  const c = colors();
  const h = 280;
  const pad = { l: 44, r: 14, t: 14, b: 34 };
  const plotW = w - pad.l - pad.r;
  const plotH = h - pad.t - pad.b;
  const maxVal = Math.max(1, ...days.map((d) => Math.max(d.buys || 0, d.sells || 0)));
  const { top, ticks } = niceTicks(maxVal, 4);
  const x = scale(0, Math.max(1, days.length - 1), pad.l, pad.l + plotW);
  const y = scale(0, top, pad.t + plotH, pad.t);
  const step = plotW / Math.max(1, days.length);

  // weekend bands
  let bands = "";
  days.forEach((d, i) => {
    const dow = new Date(`${d.day}T00:00:00`).getDay();
    if (dow !== 0 && dow !== 6) return;
    bands += `<rect class="c-band" x="${(x(i) - step / 2).toFixed(1)}" y="${pad.t}" width="${step.toFixed(1)}" height="${plotH}" fill="${c.grid}" fill-opacity="0.5"/>`;
  });

  const line = (key) => days.map((d, i) => `${i === 0 ? "M" : "L"}${x(i).toFixed(1)} ${y(d[key] || 0).toFixed(1)}`).join(" ");
  const area = (key) => `${line(key)} L${x(days.length - 1).toFixed(1)} ${y(0).toFixed(1)} L${x(0).toFixed(1)} ${y(0).toFixed(1)} Z`;

  let body = bands;
  body += yGrid(pad, plotW, plotH, ticks, y, (v) => fmtInt(v));

  body += `<path d="${area("buys")}" fill="${c.buys}" fill-opacity="0.14"/>`;
  body += `<path d="${area("sells")}" fill="${c.sells}" fill-opacity="0.14"/>`;
  body += `<path d="${line("buys")}" fill="none" stroke="${c.buys}" stroke-width="2" stroke-linejoin="round"/>`;
  body += `<path d="${line("sells")}" fill="none" stroke="${c.sells}" stroke-width="2" stroke-linejoin="round"/>`;

  // x labels every 5th day
  days.forEach((d, i) => {
    if (i % 5 !== 0 && i !== days.length - 1) return;
    body += `<text class="c-axis" x="${x(i).toFixed(1)}" y="${h - 12}" text-anchor="middle" fill="${c.axis}">${escapeHtml(String(d.day).slice(5))}</text>`;
  });

  // hover columns: crosshair + both dots + tooltip
  days.forEach((d, i) => {
    const cx = x(i);
    const tip = `${d.day}\n${t("chart.buys")}: ${fmtInt(d.buys || 0)}\n${t("chart.sells")}: ${fmtInt(d.sells || 0)}`;
    body += `<g class="c-hit-g" tabindex="0" role="img" data-tip="${escapeHtml(tip)}"`
      + ` aria-label="${escapeHtml(tip.replaceAll("\n", ", "))}">`
      + `<rect class="c-hit" x="${(cx - step / 2).toFixed(1)}" y="${pad.t}" width="${step.toFixed(1)}" height="${plotH}"/>`
      + `<line class="c-cross" x1="${cx.toFixed(1)}" y1="${pad.t}" x2="${cx.toFixed(1)}" y2="${pad.t + plotH}"/>`
      + `<circle class="c-dot" cx="${cx.toFixed(1)}" cy="${y(d.buys || 0).toFixed(1)}" r="3.5" fill="${c.buys}"/>`
      + `<circle class="c-dot" cx="${cx.toFixed(1)}" cy="${y(d.sells || 0).toFixed(1)}" r="3.5" fill="${c.sells}"/>`
      + `</g>`;
  });

  const aria = tp("chart.ariaTrades", {
    days: days.length,
    buys: trades?.totalBuys ?? 0,
    sells: trades?.totalSells ?? 0,
  });
  return svgWrap(w, h, aria, body);
}

/* ---- chart 2: price factor distribution --------------------------------- */

function chartFactors(w, factors) {
  const labels = factors?.labels || [];
  const counts = factors?.counts || [];
  if (!labels.length || !counts.some((v) => v > 0)) return emptyChart(t("chart.factorsEmpty"));

  const c = colors();
  const n = labels.length;
  // Narrow cards cannot fit 7 bucket labels side by side, so tilt them.
  const tilt = (w - 58) / n < 52;
  const h = tilt ? 280 : 260;
  const pad = { l: 44, r: 14, t: 14, b: tilt ? 60 : 40 };
  const plotW = w - pad.l - pad.r;
  const plotH = h - pad.t - pad.b;
  const { top, ticks } = niceTicks(Math.max(...counts), 4);
  const y = scale(0, top, pad.t + plotH, pad.t);
  const gap = Math.min(10, plotW / (n * 5));
  const barW = (plotW - gap * (n - 1)) / n;
  const atBaseIndex = labels.findIndex((l) => l === "0.95-1.05");

  let body = yGrid(pad, plotW, plotH, ticks, y, (v) => fmtInt(v));

  labels.forEach((label, i) => {
    const v = counts[i] || 0;
    const bx = pad.l + i * (barW + gap);
    const by = y(v);
    const bh = Math.max(0, pad.t + plotH - by);
    const isBase = i === atBaseIndex;
    const fill = isBase ? c.accent : c.neutral;
    const tip = `${label}\n${fmtInt(v)} ${t("chart.items")}${isBase ? ` (${t("chart.atBase")})` : ""}`;
    body += `<g tabindex="0" role="img" data-tip="${escapeHtml(tip)}" aria-label="${escapeHtml(tip.replaceAll("\n", ", "))}">`
      + `<rect x="${bx.toFixed(1)}" y="${by.toFixed(1)}" width="${barW.toFixed(1)}" height="${bh.toFixed(1)}" rx="3"`
      + ` fill="${fill}" fill-opacity="${isBase ? 0.95 : 0.6}"`
      + (isBase ? ` stroke="${c.accent}" stroke-width="1.5"` : "")
      + `/></g>`;
    const lx = bx + barW / 2;
    const ly = tilt ? h - 38 : h - 22;
    body += `<text class="c-axis" x="${lx.toFixed(1)}" y="${ly}"`
      + (tilt ? ` text-anchor="end" transform="rotate(-38 ${lx.toFixed(1)} ${ly})"` : ` text-anchor="middle"`)
      + ` fill="${isBase ? c.accent : c.axis}">${escapeHtml(label)}</text>`;
  });

  const total = counts.reduce((a, b) => a + (b || 0), 0);
  return svgWrap(w, h, tp("chart.ariaFactors", { n: total }), body);
}

/* ---- chart 3: ranked horizontal bars (hot demand / hot supply) ---------- */

function chartRanked(w, rows, opts) {
  if (!rows.length) return emptyChart(opts.empty);

  const c = colors();
  const rowH = 26;
  const pad = { l: Math.min(230, Math.max(120, Math.round(w * 0.34))), r: 58, t: 8, b: 8 };
  const h = pad.t + pad.b + rows.length * rowH;
  const plotW = Math.max(40, w - pad.l - pad.r);
  const maxV = Math.max(1e-6, ...rows.map((r) => r.value || 0), opts.domainMax || 0);
  const maxChars = Math.max(8, Math.floor((pad.l - 10) / 6.1));

  let body = "";
  rows.forEach((r, i) => {
    const top = pad.t + i * rowH;
    const bh = rowH - 10;
    const bw = Math.max(2, ((r.value || 0) / maxV) * plotW);
    const tip = `${r.name}\n${r.tpl}\n${opts.valueLabel}: ${fmt(r.value)}\n${t("chart.factor")}: ${fmtFactor(r.factor)}`;
    body += `<g tabindex="0" role="img" data-tip="${escapeHtml(tip)}" aria-label="${escapeHtml(tip.replaceAll("\n", ", "))}">`
      + `<rect class="c-hit" x="0" y="${top}" width="${w}" height="${rowH}"/>`
      + `<text class="c-label" x="0" y="${(top + rowH / 2 + 4).toFixed(1)}" fill="${c.dim}">${escapeHtml(truncate(r.name, maxChars))}</text>`
      + `<rect x="${pad.l}" y="${(top + 5).toFixed(1)}" width="${bw.toFixed(1)}" height="${bh}" rx="3" fill="${opts.color}" fill-opacity="0.75"/>`
      + `<text class="c-val" x="${w - 2}" y="${(top + rowH / 2 + 4).toFixed(1)}" text-anchor="end" fill="${c.dim}">×${escapeHtml(fmtFactor(r.factor))}</text>`
      + `</g>`;
  });

  return svgWrap(w, h, tp("chart.ariaRanked", { n: rows.length }), body);
}

/* ---- chart 4: category overview ----------------------------------------- */

function chartCategories(w, cats) {
  if (!cats.length) return emptyChart(t("chart.categoriesEmpty"));

  const c = colors();
  const sorted = [...cats].sort((a, b) => (b.avgFactor || 0) - (a.avgFactor || 0));
  const rowH = 30;
  const pad = { l: Math.min(250, Math.max(130, Math.round(w * 0.32))), r: 56, t: 10, b: 24 };
  const h = pad.t + pad.b + sorted.length * rowH;
  const plotW = Math.max(40, w - pad.l - pad.r);
  const maxF = Math.max(1.05, ...sorted.map((x) => x.avgFactor || 0));
  const maxItems = Math.max(1, ...sorted.map((x) => x.items || 0));
  const x = scale(0, maxF, pad.l, pad.l + plotW);
  const maxChars = Math.max(8, Math.floor((pad.l - 10) / 6.1));
  const plotH = sorted.length * rowH;

  // 1.0 reference line
  let body = `<line class="c-zero" x1="${x(1).toFixed(1)}" y1="${pad.t}" x2="${x(1).toFixed(1)}" y2="${pad.t + plotH}" stroke="${c.axis}"/>`;
  body += `<text class="c-axis" x="${x(1).toFixed(1)}" y="${h - 8}" text-anchor="middle" fill="${c.axis}">1.0</text>`;

  sorted.forEach((cat, i) => {
    const top = pad.t + i * rowH;
    const thickness = 7 + ((cat.items || 0) / maxItems) * (rowH - 16);
    const bw = Math.max(2, x(cat.avgFactor || 0) - pad.l);
    const tip = `${cat.name}\n${t("chart.items")}: ${fmtInt(cat.items)}\n${t("chart.avgFactor")}: ${fmtFactor(cat.avgFactor)}`
      + `\n${t("chart.avgDemand")}: ${fmt(cat.avgDemand)}\n${t("chart.avgSupply")}: ${fmt(cat.avgSupply)}`;
    body += `<g tabindex="0" role="img" data-tip="${escapeHtml(tip)}" aria-label="${escapeHtml(tip.replaceAll("\n", ", "))}">`
      + `<rect class="c-hit" x="0" y="${top}" width="${w}" height="${rowH}"/>`
      + `<text class="c-label" x="0" y="${(top + rowH / 2 + 4).toFixed(1)}" fill="${c.dim}">${escapeHtml(truncate(cat.name, maxChars))}</text>`
      + `<rect x="${pad.l}" y="${(top + (rowH - thickness) / 2).toFixed(1)}" width="${bw.toFixed(1)}" height="${thickness.toFixed(1)}" rx="3"`
      + ` fill="${(cat.avgFactor || 0) >= 1 ? c.up : c.down}" fill-opacity="0.7"/>`
      + `<text class="c-val" x="${w - 2}" y="${(top + rowH / 2 + 4).toFixed(1)}" text-anchor="end" fill="${c.dim}">${escapeHtml(fmtFactor(cat.avgFactor))}</text>`
      + `</g>`;
  });

  return svgWrap(w, h, tp("chart.ariaCategories", { n: sorted.length }), body);
}

/* ---- chart 5: movers (diverging) ---------------------------------------- */

function chartMovers(w, movers) {
  const up = movers?.up || [];
  const down = movers?.down || [];
  const rows = [...up, ...[...down].reverse()];
  if (!rows.length) return emptyChart(t("chart.moversEmpty"));

  const c = colors();
  const rowH = 26;
  const pad = { l: Math.min(230, Math.max(120, Math.round(w * 0.3))), r: 56, t: 10, b: 26 };
  const h = pad.t + pad.b + rows.length * rowH;
  const plotW = Math.max(60, w - pad.l - pad.r);
  const maxDev = Math.max(0.05, ...rows.map((r) => Math.abs((r.factor || 1) - 1)));
  const cx = pad.l + plotW / 2;
  const half = plotW / 2;
  const maxChars = Math.max(8, Math.floor((pad.l - 10) / 6.1));
  const plotH = rows.length * rowH;

  let body = `<line class="c-zero" x1="${cx.toFixed(1)}" y1="${pad.t}" x2="${cx.toFixed(1)}" y2="${pad.t + plotH}" stroke="${c.axis}"/>`;
  body += `<text class="c-axis" x="${cx.toFixed(1)}" y="${h - 8}" text-anchor="middle" fill="${c.axis}">1.0</text>`;

  rows.forEach((r, i) => {
    const top = pad.t + i * rowH;
    const dev = (r.factor || 1) - 1;
    const len = (Math.abs(dev) / maxDev) * half;
    const bx = dev >= 0 ? cx : cx - len;
    const tip = `${r.name}\n${r.tpl}\n${t("chart.factor")}: ${fmtFactor(r.factor)}`
      + `\n${t("chart.basePrice")} ${fmtInt(r.basePrice)} → ${t("chart.currentPrice")} ${fmtInt(r.currentPrice)}`;
    body += `<g tabindex="0" role="img" data-tip="${escapeHtml(tip)}" aria-label="${escapeHtml(tip.replaceAll("\n", ", "))}">`
      + `<rect class="c-hit" x="0" y="${top}" width="${w}" height="${rowH}"/>`
      + `<text class="c-label" x="0" y="${(top + rowH / 2 + 4).toFixed(1)}" fill="${c.dim}">${escapeHtml(truncate(r.name, maxChars))}</text>`
      + `<rect x="${bx.toFixed(1)}" y="${(top + 5).toFixed(1)}" width="${Math.max(2, len).toFixed(1)}" height="${rowH - 10}" rx="3"`
      + ` fill="${dev >= 0 ? c.up : c.down}" fill-opacity="0.78"/>`
      + `<text class="c-val" x="${w - 2}" y="${(top + rowH / 2 + 4).toFixed(1)}" text-anchor="end" fill="${dev >= 0 ? c.up : c.down}">×${escapeHtml(fmtFactor(r.factor))}</text>`
      + `</g>`;
  });

  return svgWrap(w, h, tp("chart.ariaMovers", { n: rows.length }), body);
}

/* ---- chart 6: supply/demand pressure scatter ---------------------------- */

function chartPressure(w, points) {
  if (!points.length) return emptyChart(t("chart.pressureEmpty"));

  const c = colors();
  const h = 300;
  const pad = { l: 44, r: 16, t: 14, b: 34 };
  const plotW = w - pad.l - pad.r;
  const plotH = h - pad.t - pad.b;
  const ys = points.map((p) => p.y || 0);
  const yMin = Math.min(0.9, ...ys);
  const yMax = Math.max(1.1, ...ys);
  // Demand and supply are both 0..1, so x lands in roughly -1..1. Size the axis
  // to the data (rounded out to a half step) rather than assuming a full -2..2.
  const xMax = clamp(Math.ceil(Math.max(0.5, ...points.map((p) => Math.abs(p.x || 0))) * 2) / 2, 0.5, 2);
  const x = scale(-xMax, xMax, pad.l, pad.l + plotW);
  const y = scale(yMin, yMax, pad.t + plotH, pad.t);

  let body = "";
  for (let i = -2; i <= 2; i++) {
    const v = Math.round((i * xMax) / 2 * 100) / 100;
    body += `<line class="c-grid" x1="${x(v).toFixed(1)}" y1="${pad.t}" x2="${x(v).toFixed(1)}" y2="${pad.t + plotH}" stroke="${c.grid}"/>`;
    body += `<text class="c-axis" x="${x(v).toFixed(1)}" y="${h - 12}" text-anchor="middle" fill="${c.axis}">${v > 0 ? "+" : ""}${escapeHtml(fmt(v, 2))}</text>`;
  }
  const yStep = (yMax - yMin) / 4;
  for (let i = 0; i <= 4; i++) {
    const v = yMin + yStep * i;
    body += `<line class="c-grid" x1="${pad.l}" y1="${y(v).toFixed(1)}" x2="${pad.l + plotW}" y2="${y(v).toFixed(1)}" stroke="${c.grid}"/>`;
    body += `<text class="c-axis" x="${pad.l - 6}" y="${(y(v) + 3.5).toFixed(1)}" text-anchor="end" fill="${c.axis}">${escapeHtml(v.toFixed(2))}</text>`;
  }

  // reference lines: zero pressure and factor 1.0
  body += `<line class="c-zero" x1="${x(0).toFixed(1)}" y1="${pad.t}" x2="${x(0).toFixed(1)}" y2="${pad.t + plotH}" stroke="${c.axis}"/>`;
  if (1 >= yMin && 1 <= yMax) {
    body += `<line class="c-zero" x1="${pad.l}" y1="${y(1).toFixed(1)}" x2="${pad.l + plotW}" y2="${y(1).toFixed(1)}" stroke="${c.axis}"/>`;
  }

  for (const p of points) {
    const px = x(clamp(p.x || 0, -xMax, xMax));
    const py = y(clamp(p.y || 0, yMin, yMax));
    body += `<circle cx="${px.toFixed(1)}" cy="${py.toFixed(1)}" r="3" fill="${c.point}" fill-opacity="0.3"/>`;
  }

  return svgWrap(w, h, tp("chart.ariaPressure", { n: points.length }), body);
}

/* ---- chart 7: wipe curve ------------------------------------------------- */

function chartWipe(w, wipe) {
  if (!wipe?.enabled) return emptyChart(t("chart.wipeOff"));
  const curve = wipe.curve || [];
  // The server builds a curve whenever startLengthDays > 0; a missing profile
  // still yields one, it only leaves ageDays null (so there is no today marker).
  if (!curve.length) return emptyChart(t("chart.wipeNoCurve"));

  const c = colors();
  const h = 280;
  const pad = { l: 44, r: 16, t: 20, b: 34 };
  const plotW = w - pad.l - pad.r;
  const plotH = h - pad.t - pad.b;
  const dayMin = curve[0].day ?? 0;
  const dayMax = curve[curve.length - 1].day ?? 1;
  const mults = curve.map((p) => p.multiplier || 1);
  const mMin = Math.min(1, ...mults);
  const mMax = Math.max(1.05, ...mults);
  const x = scale(dayMin, dayMax, pad.l, pad.l + plotW);
  const y = scale(mMin, mMax, pad.t + plotH, pad.t);

  let body = "";
  for (let i = 0; i <= 4; i++) {
    const v = mMin + ((mMax - mMin) / 4) * i;
    body += `<line class="c-grid" x1="${pad.l}" y1="${y(v).toFixed(1)}" x2="${pad.l + plotW}" y2="${y(v).toFixed(1)}" stroke="${c.grid}"/>`;
    body += `<text class="c-axis" x="${pad.l - 6}" y="${(y(v) + 3.5).toFixed(1)}" text-anchor="end" fill="${c.axis}">×${escapeHtml(v.toFixed(2))}</text>`;
  }

  const path = curve.map((p, i) => `${i === 0 ? "M" : "L"}${x(p.day).toFixed(1)} ${y(p.multiplier).toFixed(1)}`).join(" ");
  body += `<path d="${path} L${x(dayMax).toFixed(1)} ${(pad.t + plotH).toFixed(1)} L${x(dayMin).toFixed(1)} ${(pad.t + plotH).toFixed(1)} Z" fill="${c.accent}" fill-opacity="0.12"/>`;
  body += `<path d="${path}" fill="none" stroke="${c.accent}" stroke-width="2" stroke-linejoin="round"/>`;

  const tickEvery = Math.max(1, Math.round(curve.length / 8));
  curve.forEach((p, i) => {
    if (i % tickEvery !== 0 && i !== curve.length - 1) return;
    body += `<text class="c-axis" x="${x(p.day).toFixed(1)}" y="${h - 12}" text-anchor="middle" fill="${c.axis}">${escapeHtml(fmt(p.day, 1))}</text>`;
  });

  curve.forEach((p) => {
    const tip = `${t("chart.day")} ${fmt(p.day, 1)}\n${t("chart.multiplier")}: ×${fmt(p.multiplier)}`;
    body += `<g tabindex="0" role="img" data-tip="${escapeHtml(tip)}" aria-label="${escapeHtml(tip.replaceAll("\n", ", "))}">`
      + `<circle cx="${x(p.day).toFixed(1)}" cy="${y(p.multiplier).toFixed(1)}" r="7" fill="transparent"/>`
      + `<circle cx="${x(p.day).toFixed(1)}" cy="${y(p.multiplier).toFixed(1)}" r="2.5" fill="${c.accent}"/></g>`;
  });

  // today marker
  if (wipe.ageDays != null) {
    const d = clamp(wipe.ageDays, dayMin, dayMax);
    const mx = x(d);
    body += `<line class="c-today" x1="${mx.toFixed(1)}" y1="${pad.t - 6}" x2="${mx.toFixed(1)}" y2="${pad.t + plotH}" stroke="${c.amber}"/>`;
    const anchor = mx > pad.l + plotW * 0.7 ? "end" : "start";
    const tx = anchor === "end" ? mx - 6 : mx + 6;
    body += `<text class="c-val" x="${tx.toFixed(1)}" y="${pad.t - 8}" text-anchor="${anchor}" fill="${c.amber}">`
      + `${escapeHtml(t("chart.wipeToday"))} ${escapeHtml(fmt(wipe.ageDays, 1))}, ×${escapeHtml(fmt(wipe.multiplier))}</text>`;
  }

  return svgWrap(w, h, tp("chart.ariaWipe", { n: curve.length }), body);
}

/* ---- chart 8: event timeline -------------------------------------------- */

function chartEvents(w, events) {
  if (!events?.enabled) return emptyChart(t("chart.eventsOff"));
  const items = events.items || [];
  if (!items.length) return emptyChart(t("chart.eventsEmpty"));

  const c = colors();
  const rowH = 30;
  const pad = { l: Math.min(240, Math.max(130, Math.round(w * 0.3))), r: 16, t: 14, b: 28 };
  const h = pad.t + pad.b + items.length * rowH;
  const plotW = Math.max(60, w - pad.l - pad.r);
  const windowDays = Math.max(1, events.windowDays || 60);
  const x = scale(0, windowDays, pad.l, pad.l + plotW);
  const maxChars = Math.max(8, Math.floor((pad.l - 12) / 6.1));
  const plotH = items.length * rowH;

  let body = "";
  const gridStep = windowDays <= 10 ? 2 : windowDays <= 30 ? 5 : windowDays <= 90 ? 15 : 30;
  for (let d = 0; d <= windowDays; d += gridStep) {
    body += `<line class="c-grid" x1="${x(d).toFixed(1)}" y1="${pad.t}" x2="${x(d).toFixed(1)}" y2="${pad.t + plotH}" stroke="${c.grid}"/>`;
    body += `<text class="c-axis" x="${x(d).toFixed(1)}" y="${h - 10}" text-anchor="middle" fill="${c.axis}">${escapeHtml(fmt(d, 1))}</text>`;
  }

  items.forEach((ev, i) => {
    const top = pad.t + i * rowH;
    const a = clamp(ev.startDay ?? 0, 0, windowDays);
    const b = clamp(ev.endDay ?? windowDays, 0, windowDays);
    const bx = x(Math.min(a, b));
    const bw = Math.max(3, x(Math.max(a, b)) - bx);
    const col = ev.active ? c.accent : c.neutral;
    const label = `${ev.label || ev.id} ×${fmt(ev.multiplier)}`;
    const tip = `${ev.label || ev.id}\n${tp("chart.eventDays", { from: fmt(ev.startDay, 1), to: fmt(ev.endDay, 1) })}`
      + `\n${t("chart.multiplier")}: ×${fmt(ev.multiplier)}\n${ev.active ? t("chart.active") : t("chart.inactive")}`;
    body += `<g tabindex="0" role="img" data-tip="${escapeHtml(tip)}" aria-label="${escapeHtml(tip.replaceAll("\n", ", "))}">`
      + `<rect class="c-hit" x="0" y="${top}" width="${w}" height="${rowH}"/>`
      + `<text class="c-label" x="0" y="${(top + rowH / 2 + 4).toFixed(1)}" fill="${ev.active ? c.text : c.dim}">${escapeHtml(truncate(label, maxChars))}</text>`
      + `<rect x="${bx.toFixed(1)}" y="${(top + 6).toFixed(1)}" width="${bw.toFixed(1)}" height="${rowH - 14}" rx="4"`
      + ` fill="${col}" fill-opacity="${ev.active ? 0.85 : 0.35}"`
      + (ev.active ? ` stroke="${c.accent}" stroke-width="1.5"` : "")
      + `/></g>`;
  });

  if (events.ageDays != null) {
    const mx = x(clamp(events.ageDays, 0, windowDays));
    body += `<line class="c-today" x1="${mx.toFixed(1)}" y1="${pad.t - 6}" x2="${mx.toFixed(1)}" y2="${pad.t + plotH}" stroke="${c.amber}"/>`;
    body += `<text class="c-val" x="${mx.toFixed(1)}" y="${pad.t - 9}" text-anchor="middle" fill="${c.amber}">${escapeHtml(t("chart.wipeToday"))}</text>`;
  }

  return svgWrap(w, h, tp("chart.ariaEvents", { n: items.length, days: fmt(windowDays, 1) }), body);
}

/* ---- sparkline (KPI cards) ---------------------------------------------- */

function sparkline(w, days, key, color) {
  if (!days.length) return "";
  const h = 30;
  const maxV = Math.max(1, ...days.map((d) => d[key] || 0));
  const x = scale(0, Math.max(1, days.length - 1), 1, w - 1);
  const y = scale(0, maxV, h - 2, 2);
  const line = days.map((d, i) => `${i === 0 ? "M" : "L"}${x(i).toFixed(1)} ${y(d[key] || 0).toFixed(1)}`).join(" ");
  const body = `<path d="${line} L${x(days.length - 1).toFixed(1)} ${h} L${x(0).toFixed(1)} ${h} Z" fill="${color}" fill-opacity="0.16"/>`
    + `<path d="${line}" fill="none" stroke="${color}" stroke-width="1.5" stroke-linejoin="round"/>`;
  return svgWrap(w, h, tp("chart.ariaSpark", { n: days.length }), body);
}

/* =========================================================================
 * 9. Config form editor
 *
 * Controls are inferred from the fetched JSON's value types:
 *   boolean                -> toggle switch
 *   number                 -> number input (step from the key name)
 *   string                 -> <select> when the valid set is known, else text
 *   object                 -> labelled fieldset, recursive
 *   object used as a map   -> editable id/value row list (MAP_FIELDS)
 *   array of strings       -> editable row list with add/remove
 *   array of objects       -> repeatable card per entry (ARRAY_TEMPLATES)
 * ========================================================================= */

/** schema path -> allowed string values. */
const ENUMS = {
  "pool.mode": ["blacklist", "whitelist"],
  // Live provider only; `liveFlea: false` is what selects local handbook prices.
  "mod.priceSource": ["tarkovdev", "tarkovmarket"],
};

/** Keys whose value is a credential: masked in the form, flagged on the JSON tab. */
const SECRET_KEY_RE = /apikey|token|secret|password/i;

/** True when any key anywhere in the config looks like a credential. */
function hasSecretField(value) {
  if (Array.isArray(value)) return value.some(hasSecretField);
  if (!isPlainObject(value)) return false;
  return Object.entries(value).some(([k, v]) => SECRET_KEY_RE.test(k) || hasSecretField(v));
}

/** Objects used as id -> number maps rather than fixed-shape objects. */
const MAP_FIELDS = new Set([
  "pricing.fixedPrices",
  "pricing.itemMultipliers",
  "pricing.categoryMultipliers",
  "levels.items",
  "levels.categories",
]);

/** Arrays that hold plain strings (so an empty one still renders correctly). */
const STRING_ARRAYS = new Set([
  "pool.add",
  "pool.remove",
  "pool.addCategories",
  "pool.removeCategories",
  "events.events[].categories",
  "events.events[].items",
  "trends.trends[].categories",
  "trends.trends[].items",
]);

/** Factories for "add entry" on arrays of objects. */
const ARRAY_TEMPLATES = {
  "events.events": () => ({ id: "new_event", startDay: 0, endDay: 7, multiplier: 1.1, categories: [], items: [] }),
  "trends.trends": () => ({ category: "", periodDays: 21, amplitude: 0.15, phase: 0 }),
};

/** Config keys that have a proper translated label instead of a humanised key. */
const LABEL_KEYS = { "mod.quickListFromStash": "selling.label" };

/** Which key names identify an entry in a card list. */
const ENTRY_TITLE_KEYS = ["id", "label", "name", "category", "tpl"];

function fieldLabel(schemaPath, key) {
  return LABEL_KEYS[schemaPath] ? t(LABEL_KEYS[schemaPath]) : humanKey(key);
}

/**
 * Help text for a schema path, or "" when none is written (extra configs, unknown keys).
 * Nested paths are flattened with "_" because a group's own text and its children share a level:
 * economy.wipe -> help.economy.wipe, economy.wipe.enabled -> help.economy.wipe_enabled.
 */
function helpText(schemaPath) {
  const [name, ...rest] = schemaPath.split(".");
  if (!rest.length) return "";
  const key = `help.${name}.${rest.join("_").replace(/[]/g, "")}`;
  const text = t(key);
  return text === key ? "" : text;
}

function helpHtml(schemaPath) {
  // The selling view already carries its own hint under the toggle.
  if (cfg?.only) return "";
  const text = helpText(schemaPath);
  return text ? `<p class="field-help">${escapeHtml(text)}</p>` : "";
}

function numberStep(key, value) {
  if (/multiplier|amplitude|factor|phase|elasticity|noise|speed|decay|per[A-Z]/.test(key)) return "0.01";
  if (/day|days|second|minute|hour|level|count|length/i.test(key)) return "1";
  return Number.isInteger(value) ? "1" : "0.01";
}

function schemaChild(schemaPath, key) {
  return `${schemaPath}.${key}`;
}

function arrayItemSchema(schemaPath) {
  return `${schemaPath}[]`;
}

/* ---- field renderers (all return HTML strings) ---- */

function renderValueControl(schemaPath, parts, key, value) {
  const pathAttr = escapeHtml(parts.join("/"));

  // A null in the config has no type to infer from; keep it editable without
  // forcing it to become an empty string on save.
  if (value === null) {
    return `<input type="text" class="mono" value="" placeholder="null"`
      + ` data-path="${pathAttr}" data-kind="null" aria-label="${escapeHtml(fieldLabel(schemaPath, key))}" />`;
  }

  if (typeof value === "boolean") {
    const labelText = fieldLabel(schemaPath, key);
    return `<input type="checkbox" class="switch" role="switch" data-path="${pathAttr}" data-kind="bool"`
      + ` aria-label="${escapeHtml(labelText)}" aria-checked="${value ? "true" : "false"}" ${value ? "checked" : ""} />`
      + `<span class="switch-text" data-switch-text>${escapeHtml(value ? t("on") : t("off"))}</span>`;
  }

  if (typeof value === "number") {
    return `<input type="number" step="${escapeHtml(numberStep(key, value))}" value="${escapeHtml(String(value))}"`
      + ` data-path="${pathAttr}" data-kind="num" aria-label="${escapeHtml(fieldLabel(schemaPath, key))}" />`;
  }

  // Credentials: masked by default, with an explicit show/hide toggle. The input
  // still carries the real value, so an untouched field saves unchanged.
  if (SECRET_KEY_RE.test(key)) {
    return `<input type="password" class="mono" value="${escapeHtml(String(value ?? ""))}"`
      + ` data-path="${pathAttr}" data-kind="str" data-secret="1" autocomplete="new-password"`
      + ` spellcheck="false" aria-label="${escapeHtml(fieldLabel(schemaPath, key))}" />`
      + `<button type="button" class="x-btn" data-act="peek" aria-pressed="false"`
      + ` aria-label="${escapeHtml(t("form.showSecret"))}" title="${escapeHtml(t("form.showSecret"))}">${icon("eye", 14)}</button>`;
  }

  const options = ENUMS[schemaPath];
  if (options) {
    // A value outside the known set stays visible and selected instead of being silently replaced.
    const list = options.includes(value) ? options : [String(value), ...options];
    return dropdownHtml({
      value: String(value), label: fieldLabel(schemaPath, key),
      options: list.map((o) => ({ value: o, label: o })),
      attrs: `data-path="${pathAttr}" data-kind="str"`,
    });
  }

  const input = `<input type="text" class="mono" value="${escapeHtml(String(value ?? ""))}"`
    + ` data-path="${pathAttr}" data-kind="str" aria-label="${escapeHtml(fieldLabel(schemaPath, key))}" />`;
  const idKind = PICKERS[schemaPath];
  return (idKind ? idCell(input, idKind, value) : input) + pickBtn(schemaPath, pathAttr, "field");
}

function renderStringList(schemaPath, parts, arr) {
  const pathAttr = escapeHtml(parts.join("/"));
  const idKind = PICKERS[schemaPath];
  const rows = arr.map((v, i) => {
    const input = `<input type="text" class="mono" value="${escapeHtml(String(v ?? ""))}"
             data-path="${escapeHtml([...parts, String(i)].join("/"))}" data-kind="str"
             aria-label="${escapeHtml(`${humanKey(parts[parts.length - 1])} ${i + 1}`)}" />`;
    return `
    <div class="row-item">
      ${idKind ? idCell(input, idKind, v) : input}
      <button type="button" class="x-btn" data-act="rm-item" data-path="${pathAttr}" data-index="${i}"
              aria-label="${escapeHtml(t("form.removeRow"))}" title="${escapeHtml(t("form.removeRow"))}">${icon("trash", 14)}</button>
    </div>`;
  }).join("");

  return `<div class="rows" data-list="${pathAttr}">
    ${rows || `<div class="row-empty">${escapeHtml(t("form.emptyList"))}</div>`}
    <div class="row-add">
      <input type="text" class="mono" data-new-for="${pathAttr}" placeholder="${escapeHtml(t("form.newId"))}"
             aria-label="${escapeHtml(t("form.newId"))}" />
      ${pickBtn(schemaPath, pathAttr, "list")}
      <button type="button" class="btn" data-act="add-item" data-path="${pathAttr}">${icon("plus", 14)}${escapeHtml(t("act.add"))}</button>
    </div>
  </div>`;
}

function renderMap(schemaPath, parts, obj) {
  const pathAttr = escapeHtml(parts.join("/"));
  const keys = Object.keys(obj);
  const idKind = PICKERS[schemaPath];
  const rows = keys.map((k) => {
    const v = obj[k];
    const isNum = typeof v === "number";
    const keyInput = `<input type="text" class="mono" value="${escapeHtml(k)}" data-map="${pathAttr}" data-map-field="key"
             aria-label="${escapeHtml(t("form.mapKey"))}" />`;
    return `
    <div class="row-item" data-map-row="${escapeHtml(k)}">
      ${idKind ? idCell(keyInput, idKind, k) : keyInput}
      <input type="${isNum ? "number" : "text"}" ${isNum ? `step="${escapeHtml(numberStep(k, v))}"` : ""}
             value="${escapeHtml(String(v ?? ""))}" data-map="${pathAttr}" data-map-field="value"
             data-num="${isNum ? "1" : "0"}" aria-label="${escapeHtml(t("form.mapValue"))}" />
      <button type="button" class="x-btn" data-act="rm-map" data-path="${pathAttr}" data-key="${escapeHtml(k)}"
              aria-label="${escapeHtml(t("form.removeRow"))}" title="${escapeHtml(t("form.removeRow"))}">${icon("trash", 14)}</button>
    </div>`;
  }).join("");

  return `<div class="rows" data-map-list="${pathAttr}">
    ${rows || `<div class="row-empty">${escapeHtml(t("form.emptyMap"))}</div>`}
    <div class="row-add">
      <input type="text" class="mono" data-new-key="${pathAttr}" placeholder="${escapeHtml(t("form.mapKey"))}"
             aria-label="${escapeHtml(t("form.mapKey"))}" />
      ${pickBtn(schemaPath, pathAttr, "map")}
      <input type="number" step="0.01" data-new-val="${pathAttr}" placeholder="${escapeHtml(t("form.mapValue"))}"
             aria-label="${escapeHtml(t("form.mapValue"))}" />
      <button type="button" class="btn" data-act="add-map" data-path="${pathAttr}">${icon("plus", 14)}${escapeHtml(t("act.add"))}</button>
    </div>
  </div>`;
}

function renderObjectList(schemaPath, parts, arr) {
  const pathAttr = escapeHtml(parts.join("/"));
  const itemSchema = arrayItemSchema(schemaPath);

  const cards = arr.map((entry, i) => {
    const titleKey = ENTRY_TITLE_KEYS.find((k) => entry && entry[k] != null && entry[k] !== "");
    const rawTitle = titleKey ? String(entry[titleKey]) : "";
    const titleKind = titleKey === "category" ? "category" : titleKey === "tpl" ? "item" : "";
    const title = rawTitle ? (titleKind && idLookupName(rawTitle, titleKind)) || rawTitle : `${t("form.entry")} ${i + 1}`;
    const titleAttrs = titleKind ? ` data-idtitle="${titleKind}" data-idval="${escapeHtml(rawTitle)}"` : "";
    const inner = isPlainObject(entry)
      ? Object.keys(entry).map((k) =>
          renderField(schemaChild(itemSchema, k), [...parts, String(i), k], k, entry[k])).join("")
      : `<div class="row-empty">${escapeHtml(t("form.emptyList"))}</div>`;
    return `<div class="obj-card">
      <div class="obj-card-head">
        <div class="obj-card-title">${icon("calendar", 14)}<span${titleAttrs}>${escapeHtml(truncate(title, 48))}</span></div>
        <button type="button" class="x-btn" data-act="rm-obj" data-path="${pathAttr}" data-index="${i}"
                aria-label="${escapeHtml(t("act.remove"))}" title="${escapeHtml(t("act.remove"))}">${icon("trash", 14)}</button>
      </div>
      <div class="fset-body">${inner}</div>
    </div>`;
  }).join("");

  const canAdd = !!ARRAY_TEMPLATES[schemaPath] || arr.some(isPlainObject);
  return `<div class="obj-list" data-obj-list="${pathAttr}">
    ${cards || `<div class="row-empty">${escapeHtml(t("form.emptyList"))}</div>`}
    ${canAdd ? `<div class="row-add"><button type="button" class="btn" data-act="add-obj" data-path="${pathAttr}" data-schema="${escapeHtml(schemaPath)}">${icon("plus", 14)}${escapeHtml(t("form.addEntry"))}</button></div>` : ""}
  </div>`;
}

/** One config key: either a simple field row, or a fieldset for composites. */
function renderField(schemaPath, parts, key, value) {
  const keyHint = `<span class="key" title="${escapeHtml(t("form.keyHint"))}">${escapeHtml(schemaPath.split(".").slice(1).join("."))}</span>`;
  const head = `<div class="field-head"><span class="field-name">${escapeHtml(fieldLabel(schemaPath, key))}</span>${keyHint}</div>`;

  // map-like object
  if (isPlainObject(value) && MAP_FIELDS.has(schemaPath)) {
    return `<fieldset class="fset"><legend>${escapeHtml(fieldLabel(schemaPath, key))} ${keyHint}</legend>
      <div class="fset-body">${helpHtml(schemaPath)}${renderMap(schemaPath, parts, value)}</div></fieldset>`;
  }

  // nested object -> recursive fieldset
  if (isPlainObject(value)) {
    const inner = Object.keys(value).map((k) =>
      renderField(schemaChild(schemaPath, k), [...parts, k], k, value[k])).join("");
    return `<fieldset class="fset"><legend>${escapeHtml(fieldLabel(schemaPath, key))} ${keyHint}</legend>
      <div class="fset-body">${helpHtml(schemaPath)}${inner || `<div class="row-empty">${escapeHtml(t("form.emptyMap"))}</div>`}</div></fieldset>`;
  }

  if (Array.isArray(value)) {
    const stringy = STRING_ARRAYS.has(schemaPath)
      || (value.length > 0 && value.every((v) => typeof v === "string"));
    const body = stringy
      ? renderStringList(schemaPath, parts, value)
      : renderObjectList(schemaPath, parts, value);
    return `<fieldset class="fset"><legend>${escapeHtml(fieldLabel(schemaPath, key))} ${keyHint}
        <span class="card-sub">${escapeHtml(String(value.length))}</span></legend>
      <div class="fset-body">${helpHtml(schemaPath)}${body}</div></fieldset>`;
  }

  return `<div class="field">${head}<div class="field-ctl">${renderValueControl(schemaPath, parts, key, value)}</div>${helpHtml(schemaPath)}</div>`;
}

function renderForm() {
  const { name, draft, only } = cfg;
  if (!isPlainObject(draft)) {
    return `<div class="card"><p class="note">${escapeHtml(t("form.jsonHint"))}</p></div>`;
  }
  const keys = Object.keys(draft).filter((k) => !only || only.includes(k));
  if (!keys.length) return `<div class="empty">${escapeHtml(t("form.emptyMap"))}</div>`;
  return `<div class="form" data-form>${keys.map((k) => renderField(`${name}.${k}`, [k], k, draft[k])).join("")}</div>`;
}

/* ---- JSON tab ---- */

/**
 * Pull a 1-based line number out of a JSON.parse error.
 * Engines report either "… at position N (line L column C)" or a quoted
 * snippet of the source ("Unexpected token 'x', …\"<snippet>\"… is not valid
 * JSON"); handle both, and give up quietly otherwise.
 */
function jsonErrorLine(message, text) {
  const lineAt = (offset) => text.slice(0, clamp(offset, 0, text.length)).split("\n").length;

  const byPos = /at position (\d+)(?:\s*\(line (\d+))?/.exec(message);
  if (byPos) return byPos[2] ? Number(byPos[2]) : lineAt(Number(byPos[1]));

  const bySnippet = /,\s*(?:\.\.\.)?"([\s\S]*?)"(?:\.\.\.)?\s+is not valid JSON/.exec(message);
  if (bySnippet) {
    const snippet = bySnippet[1];
    const at = text.indexOf(snippet);
    if (at >= 0) {
      const token = /Unexpected token '(.)'/.exec(message);
      const inner = token ? snippet.indexOf(token[1]) : -1;
      return lineAt(at + (inner >= 0 ? inner : 0));
    }
  }
  return null;
}

function validateJson(text) {
  try {
    return { ok: true, value: JSON.parse(text) };
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    return { ok: false, message: msg, line: jsonErrorLine(msg, text) };
  }
}

function renderJsonTab() {
  const text = cfg.jsonText;
  const check = validateJson(text);
  const secretNote = cfg.hasSecret
    ? `<div class="warn-box">${icon("lock", 14)}<span>${escapeHtml(t("form.secretNote"))}</span></div>`
    : "";
  return `<div class="stack tight">
    ${secretNote}
    <div class="code-wrap"><textarea class="code" data-json spellcheck="false"
      aria-label="${escapeHtml(cfg.name)} JSON"
      aria-invalid="${check.ok ? "false" : "true"}">${escapeHtml(text)}</textarea></div>
    <div data-json-status>${renderJsonStatus(check)}</div>
    <p class="note tight">${escapeHtml(t("form.jsonHint"))}</p>
  </div>`;
}

function renderJsonStatus(check) {
  if (check.ok) return `<span class="pill ok">${escapeHtml(t("form.jsonValid"))}</span>`;
  const where = check.line ? ` (${t("form.jsonLine")} ${check.line})` : "";
  return `<div class="json-err">${icon("activity", 14)}<span>${escapeHtml(t("toast.invalidJson"))}${escapeHtml(where)}: ${escapeHtml(check.message)}</span></div>`;
}

/* ---- dirty tracking + save bar ---- */

function syncDirty() {
  if (!cfg) return;
  const current = cfg.tab === "json" ? cfg.jsonText : JSON.stringify(cfg.draft, null, 2);
  cfg.dirty = cfg.tab === "json"
    ? current.trim() !== cfg.baseline.trim()
    : current !== cfg.baseline;
  syncSaveBar();
}

function saveBarHtml() {
  if (!cfg?.dirty) return "";
  const invalid = cfg.tab === "json" && !validateJson(cfg.jsonText).ok;
  return `<div class="savebar" role="region" aria-label="${escapeHtml(t("unsaved"))}">
    <div class="savebar-text">
      <span class="dot is-on" aria-hidden="true"></span>
      <span>${escapeHtml(t("unsaved"))}</span>
      ${cfg.restart ? `<span class="pill warn">${escapeHtml(t("restartBadge"))}</span>` : ""}
    </div>
    <div class="savebar-actions">
      <button type="button" class="btn" data-act="cfg-discard">${escapeHtml(t("act.discard"))}</button>
      <button type="button" class="btn primary" data-act="cfg-save" ${invalid ? "disabled" : ""}>${escapeHtml(t("act.save"))}</button>
    </div>
  </div>`;
}

function syncSaveBar() {
  const host = root?.querySelector("[data-savebar]");
  if (!host) return;
  host.innerHTML = saveBarHtml();
}

/* ---- form event wiring ---- */

function rerenderCfgBody() {
  const host = root?.querySelector("[data-cfg-body]");
  if (!host) return;
  host.innerHTML = cfg.tab === "json" ? renderJsonTab() : renderForm();
  syncSaveBar();
}

/** Bodies whose delegated listeners are already attached (the body element survives loadSection). */
const cfgBound = new WeakSet();
const stateBound = new WeakSet();

function bindCfgSection(bodyEl, section) {
  // tab switch
  bodyEl.querySelectorAll("[data-cfg-tab]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const next = btn.getAttribute("data-cfg-tab");
      if (next === cfg.tab) return;
      if (next === "json") {
        cfg.jsonText = JSON.stringify(cfg.draft, null, 2);
      } else {
        const check = validateJson(cfg.jsonText);
        if (!check.ok) {
          toast(`${t("toast.invalidJson")}: ${check.message}`, "err");
          return;
        }
        cfg.draft = check.value;
      }
      cfg.tab = next;
      bodyEl.querySelectorAll("[data-cfg-tab]").forEach((b) => {
        b.setAttribute("aria-selected", b === btn ? "true" : "false");
      });
      rerenderCfgBody();
      syncDirty();
    });
  });

  // The tab buttons above are rebuilt on every load; the listeners below sit on the
  // persistent body, so attach them once or a Reload would double every click.
  if (cfgBound.has(bodyEl)) return;
  cfgBound.add(bodyEl);

  // value edits
  bodyEl.addEventListener("input", (e) => {
    const el = e.target;
    if (!(el instanceof HTMLElement)) return;

    if (el.hasAttribute("data-json")) {
      cfg.jsonText = el.value;
      const check = validateJson(cfg.jsonText);
      el.setAttribute("aria-invalid", check.ok ? "false" : "true");
      const status = bodyEl.querySelector("[data-json-status]");
      if (status) status.innerHTML = renderJsonStatus(check);
      syncDirty();
      return;
    }

    if (el.hasAttribute("data-map")) { commitMapEdit(el); refreshIdNameFor(el); return; }
    if (el.hasAttribute("data-path")) { commitField(el); refreshIdNameFor(el); }
  });

  bodyEl.addEventListener("change", (e) => {
    const el = e.target;
    if (el instanceof HTMLElement && el.hasAttribute("data-path") && el.dataset.kind === "bool") {
      commitField(el);
    }
  });

  // structural buttons
  bodyEl.addEventListener("click", (e) => {
    const btn = e.target instanceof Element ? e.target.closest("[data-act]") : null;
    if (!btn || !bodyEl.contains(btn)) return;
    const act = btn.getAttribute("data-act");
    const parts = (btn.getAttribute("data-path") || "").split("/").filter(Boolean);

    if (act === "pick") { void openPicker(btn); return; }

    if (act === "peek") {
      const input = btn.parentElement?.querySelector("[data-secret]");
      if (!input) return;
      const show = input.type === "password";
      input.type = show ? "text" : "password";
      const label = show ? t("form.hideSecret") : t("form.showSecret");
      btn.setAttribute("aria-pressed", show ? "true" : "false");
      btn.setAttribute("aria-label", label);
      btn.setAttribute("title", label);
      btn.innerHTML = icon(show ? "eyeOff" : "eye", 14);
      return;
    }

    if (act === "rm-item") {
      const arr = getAt(cfg.draft, parts);
      if (Array.isArray(arr)) arr.splice(Number(btn.getAttribute("data-index")), 1);
      rerenderCfgBody(); syncDirty(); return;
    }
    if (act === "add-item") {
      const input = bodyEl.querySelector(`[data-new-for="${CSS.escape(parts.join("/"))}"]`);
      const val = (input?.value || "").trim();
      if (!val) { input?.focus(); return; }
      const arr = getAt(cfg.draft, parts);
      if (Array.isArray(arr)) arr.push(val);
      rerenderCfgBody(); syncDirty(); return;
    }
    if (act === "rm-map") {
      const map = getAt(cfg.draft, parts);
      if (isPlainObject(map)) delete map[btn.getAttribute("data-key")];
      rerenderCfgBody(); syncDirty(); return;
    }
    if (act === "add-map") {
      const sel = CSS.escape(parts.join("/"));
      const kIn = bodyEl.querySelector(`[data-new-key="${sel}"]`);
      const vIn = bodyEl.querySelector(`[data-new-val="${sel}"]`);
      const k = (kIn?.value || "").trim();
      if (!k) { kIn?.focus(); return; }
      const nv = Number(vIn?.value);
      const map = getAt(cfg.draft, parts);
      if (isPlainObject(map)) map[k] = Number.isFinite(nv) ? nv : 0;
      rerenderCfgBody(); syncDirty(); return;
    }
    if (act === "rm-obj") {
      const arr = getAt(cfg.draft, parts);
      if (Array.isArray(arr)) arr.splice(Number(btn.getAttribute("data-index")), 1);
      rerenderCfgBody(); syncDirty(); return;
    }
    if (act === "add-obj") {
      const schema = btn.getAttribute("data-schema") || "";
      const arr = getAt(cfg.draft, parts);
      if (!Array.isArray(arr)) return;
      const make = ARRAY_TEMPLATES[schema];
      const first = arr.find(isPlainObject);
      arr.push(make ? make() : (first ? blankLike(first) : {}));
      rerenderCfgBody(); syncDirty(); return;
    }
    if (act === "cfg-save") { void doSaveCfg(section); return; }
    if (act === "cfg-discard") {
      cfg.draft = deepClone(cfg.original);
      cfg.jsonText = JSON.stringify(cfg.draft, null, 2);
      rerenderCfgBody();
      syncDirty();
    }
  });
}

/** Same shape as an existing entry, but with empty/zero values. */
function blankLike(sample) {
  const out = {};
  for (const [k, v] of Object.entries(sample)) {
    if (typeof v === "number") out[k] = 0;
    else if (typeof v === "boolean") out[k] = false;
    else if (Array.isArray(v)) out[k] = [];
    else if (isPlainObject(v)) out[k] = blankLike(v);
    else out[k] = "";
  }
  return out;
}

function commitField(el) {
  const parts = (el.getAttribute("data-path") || "").split("/").filter(Boolean);
  if (!parts.length) return;
  const kind = el.dataset.kind;

  if (kind === "bool") {
    setAt(cfg.draft, parts, !!el.checked);
    el.setAttribute("aria-checked", el.checked ? "true" : "false");
    const text = el.parentElement?.querySelector("[data-switch-text]");
    if (text) text.textContent = el.checked ? t("on") : t("off");
  } else if (kind === "num") {
    const n = Number(el.value);
    if (el.value === "" || !Number.isFinite(n)) { el.setAttribute("aria-invalid", "true"); return; }
    el.removeAttribute("aria-invalid");
    setAt(cfg.draft, parts, n);
  } else if (kind === "null") {
    const raw = el.value.trim();
    let next = raw;
    if (raw === "") next = null;
    else if (raw === "true") next = true;
    else if (raw === "false") next = false;
    else if (Number.isFinite(Number(raw))) next = Number(raw);
    setAt(cfg.draft, parts, next);
  } else {
    setAt(cfg.draft, parts, el.value);
  }
  syncDirty();
}

function commitMapEdit(el) {
  const parts = (el.getAttribute("data-map") || "").split("/").filter(Boolean);
  const row = el.closest("[data-map-row]");
  if (!parts.length || !row) return;
  const oldKey = row.getAttribute("data-map-row");
  const map = getAt(cfg.draft, parts);
  if (!isPlainObject(map)) return;

  if (el.dataset.mapField === "key") {
    const newKey = el.value.trim();
    if (!newKey || newKey === oldKey) return;
    // rebuild to keep ordering stable while renaming
    const rebuilt = {};
    for (const k of Object.keys(map)) rebuilt[k === oldKey ? newKey : k] = map[k];
    setAt(cfg.draft, parts, rebuilt);
    row.setAttribute("data-map-row", newKey);
  } else {
    if (el.dataset.num === "1") {
      const n = Number(el.value);
      if (el.value === "" || !Number.isFinite(n)) { el.setAttribute("aria-invalid", "true"); return; }
      el.removeAttribute("aria-invalid");
      map[oldKey] = n;
    } else {
      map[oldKey] = el.value;
    }
  }
  syncDirty();
}

async function doSaveCfg(section) {
  let payload;
  if (cfg.tab === "json") {
    const check = validateJson(cfg.jsonText);
    if (!check.ok) { toast(`${t("toast.invalidJson")}: ${check.message}`, "err"); return; }
    payload = check.value;
  } else {
    payload = cfg.draft;
  }

  const ok = await saveConfig(cfg.name, payload);
  if (!ok) return;
  cfg.original = deepClone(payload);
  cfg.draft = deepClone(payload);
  cfg.jsonText = JSON.stringify(cfg.draft, null, 2);
  cfg.baseline = cfg.tab === "json" ? cfg.jsonText : JSON.stringify(cfg.draft, null, 2);
  cfg.dirty = false;
  syncSaveBar();
  if (section?.name === "mod") void refreshBrand();
}

/* =========================================================================
 * 10. Sections
 * ========================================================================= */

/* ---- overview ----------------------------------------------------------- */

function kpiCard(label, value, hint, sparkKey) {
  return `<div class="card kpi">
    <span class="kpi-label">${escapeHtml(label)}</span>
    <span class="kpi-value">${escapeHtml(value)}</span>
    ${hint ? `<span class="kpi-hint">${escapeHtml(hint)}</span>` : ""}
    ${sparkKey ? `<div class="kpi-spark" data-spark="${escapeHtml(sparkKey)}"></div>` : ""}
  </div>`;
}

function chartCard(id, title, sub, col, captionHtml) {
  return `<section class="card chart-card ${col}">
    <div class="card-head">
      <h2 class="card-title">${escapeHtml(title)}</h2>
      ${sub ? `<span class="card-sub">${escapeHtml(sub)}</span>` : ""}
    </div>
    <div class="chart-host" data-chart="${escapeHtml(id)}"></div>
    ${captionHtml || ""}
  </section>`;
}

function legend(entries) {
  return `<div class="chart-legend">${entries.map((e) =>
    `<span><span class="lg-swatch" style="background:var(${e.token})"></span>${escapeHtml(e.label)}${e.value != null ? ` <b>${escapeHtml(String(e.value))}</b>` : ""}</span>`
  ).join("")}</div>`;
}

function renderOverview(data) {
  const ch = data.charts || {};
  const sum = ch.summary || {};
  const trades = ch.trades || {};
  const eco = data.economy || {};
  const windowDays = trades.windowDays ?? 30;

  const activeEvents = data.events?.active || [];
  const eventRows = activeEvents.length
    ? activeEvents.map((e) => `<div class="row"><span class="pill ok">${escapeHtml(t("chart.active"))}</span>
        <span>${escapeHtml(e.label || e.id || "")}</span><span class="key">×${escapeHtml(fmt(e.multiplier))}</span></div>`).join("")
    : `<p class="note tight">${escapeHtml(t("none"))}</p>`;

  const factorsCaption = `<p class="note tight">${escapeHtml(tp("chart.factorsCaption", {
    above: fmtInt(sum.aboveBase ?? 0),
    at: fmtInt(sum.atBase ?? 0),
    below: fmtInt(sum.belowBase ?? 0),
  }))}</p>`;

  const wipe = ch.wipe || {};
  const wipeCaption = wipe.enabled
    ? `<p class="note tight">${escapeHtml(tp("chart.wipeStart", {
        days: fmt(wipe.startLengthDays, 1), mult: fmt(wipe.startMultiplier),
      }))}${wipe.ageDays != null
        ? `. ${escapeHtml(t("chart.wipeCurrent"))} ×${escapeHtml(fmt(wipe.multiplier))}`
        : `. ${escapeHtml(t("chart.wipeNoProfile"))}`}</p>`
    : "";

  const pool = data.pool || {};
  const levels = data.levels || {};

  return `
    <div class="kpis">
      ${kpiCard(t("kpi.mod"), data.mod?.enabled ? t("enabled") : t("disabled"), String(data.mod?.priceSource || ""))}
      ${kpiCard(t("kpi.items"), fmtInt(sum.itemCount ?? eco.itemCount), `${fmtInt(sum.floatingCount ?? 0)} ${t("kpi.floating").toLowerCase()}`)}
      ${kpiCard(t("kpi.avgFactor"), fmtFactor(sum.avgFactor ?? eco.averageFactor), `${fmtInt(sum.fixedCount ?? eco.fixedCount)} ${t("kpi.fixed").toLowerCase()}`)}
      ${kpiCard(t("kpi.wipeDay"), eco.wipeAgeDays == null ? "-" : fmt(eco.wipeAgeDays, 1),
        eco.wipeAgeDays == null ? t("kpi.noProfile") : `×${fmt(eco.wipeMultiplier)}`)}
      ${kpiCard(t("kpi.buys"), fmtInt(trades.totalBuys ?? 0), "", "buys")}
      ${kpiCard(t("kpi.sells"), fmtInt(trades.totalSells ?? 0), "", "sells")}
    </div>

    <div class="chart-grid">
      ${chartCard("trades", t("chart.trades"), tp("chart.tradesSub", { days: windowDays }), "col-12",
        legend([
          { token: "--c-buys", label: t("chart.buys"), value: fmtInt(trades.totalBuys ?? 0) },
          { token: "--c-sells", label: t("chart.sells"), value: fmtInt(trades.totalSells ?? 0) },
        ]))}

      ${chartCard("factors", t("chart.factors"), t("chart.factorsSub"), "col-6", factorsCaption)}
      ${chartCard("movers", t("chart.movers"), t("chart.moversSub"), "col-6",
        legend([
          { token: "--c-up", label: t("chart.up") },
          { token: "--c-down", label: t("chart.down") },
        ]))}

      ${chartCard("demand", t("chart.demand"), t("chart.heatSub"), "col-6")}
      ${chartCard("supply", t("chart.supply"), t("chart.heatSub"), "col-6")}

      ${chartCard("categories", t("chart.categories"), t("chart.categoriesSub"), "col-12")}

      ${chartCard("pressure", t("chart.pressure"), t("chart.pressureSub"), "col-6",
        `<p class="note tight">${escapeHtml(t("chart.pressureCaption"))}</p>`)}
      ${chartCard("wipe", t("chart.wipe"), t("chart.wipeSub"), "col-6", wipeCaption)}

      ${chartCard("events", t("chart.events"), tp("chart.eventsSub", { days: fmt(ch.events?.windowDays ?? 30, 0) }), "col-12")}

      <section class="card col-6">
        <div class="card-head"><h2 class="card-title">${escapeHtml(t("overview.activeEvents"))}</h2>
          <span class="card-sub">${escapeHtml(fmtInt(data.events?.total ?? 0))}</span></div>
        <div class="stack tight">${eventRows}</div>
      </section>

      <section class="card col-6">
        <div class="card-head"><h2 class="card-title">${escapeHtml(t("overview.system"))}</h2></div>
        <dl class="kv">
          <dt>${escapeHtml(t("overview.version"))}</dt><dd>${escapeHtml(String(data.version || "").split("+")[0])}</dd>
          <dt>${escapeHtml(t("overview.webUi"))}</dt><dd class="mono">${escapeHtml(data.webUi || "/ultimateflea/app.html")}</dd>
          <dt>${escapeHtml(t("overview.priceSource"))}</dt><dd>${escapeHtml(String(data.mod?.priceSource || "-"))}</dd>
          <dt>${escapeHtml(t("overview.poolManager"))}</dt><dd>${escapeHtml(data.mod?.enablePoolManager ? t("on") : t("off"))}</dd>
          <dt>${escapeHtml(t("overview.pricing"))}</dt><dd>${escapeHtml(data.mod?.enablePricing ? t("on") : t("off"))}</dd>
          <dt>${escapeHtml(t("overview.economySim"))}</dt><dd>${escapeHtml(data.mod?.enableEconomy ? t("on") : t("off"))}</dd>
          <dt>${escapeHtml(t("overview.playerOffersOnly"))}</dt><dd>${escapeHtml(data.mod?.playerOffersOnly ? t("on") : t("off"))}</dd>
          <dt>${escapeHtml(t("overview.quickList"))}</dt><dd>${escapeHtml(data.mod?.quickListFromStash ? t("on") : t("off"))}</dd>
          <dt>${escapeHtml(t("kpi.poolMode"))}</dt><dd>${escapeHtml(String(pool.mode || "-"))}</dd>
          <dt>${escapeHtml(t("overview.levels"))}</dt><dd>${escapeHtml(levels.enabled ? t("on") : t("off"))}</dd>
          <dt>${escapeHtml(t("overview.trends"))}</dt><dd>${escapeHtml(data.trends?.enabled ? t("on") : t("off"))}</dd>
          <dt>${escapeHtml(t("overview.stateFile"))}</dt><dd>${escapeHtml(data.stateFileExists ? t("overview.present") : t("overview.missing"))}, ${escapeHtml(fmtInt(data.stateEntries ?? 0))}</dd>
        </dl>
        <p class="note tight">${escapeHtml(tp("overview.poolDetail", {
          add: fmtInt(pool.add ?? 0), remove: fmtInt(pool.remove ?? 0),
          addCat: fmtInt(pool.addCategories ?? 0), removeCat: fmtInt(pool.removeCategories ?? 0),
        }))}</p>
        <p class="note tight">${escapeHtml(tp("overview.levelsDetail", {
          items: fmtInt(levels.items ?? 0), categories: fmtInt(levels.categories ?? 0), rules: fmtInt(levels.rules ?? 0),
        }))}${levels.useSptDefaults ? `. ${escapeHtml(t("overview.sptDefaults"))}` : ""}</p>
      </section>
    </div>
  `;
}

function mountOverviewCharts(bodyEl, data) {
  const ch = data.charts || {};
  const host = (id) => bodyEl.querySelector(`[data-chart="${id}"]`);

  chartMount(host("trades"), (w) => chartTrades(w, ch.trades));
  chartMount(host("factors"), (w) => chartFactors(w, ch.factors));
  chartMount(host("movers"), (w) => chartMovers(w, ch.movers));
  chartMount(host("demand"), (w) => chartRanked(w, (ch.topDemand || []).map(mapHeat), {
    color: colors().buys, empty: t("chart.demandEmpty"), valueLabel: t("chart.demandLabel"), domainMax: 1,
  }));
  chartMount(host("supply"), (w) => chartRanked(w, (ch.topSupply || []).map(mapHeat), {
    color: colors().amber, empty: t("chart.supplyEmpty"), valueLabel: t("chart.supplyLabel"), domainMax: 1,
  }));
  chartMount(host("categories"), (w) => chartCategories(w, ch.categories || []));
  chartMount(host("pressure"), (w) => chartPressure(w, ch.pressure || []));
  chartMount(host("wipe"), (w) => chartWipe(w, ch.wipe));
  chartMount(host("events"), (w) => chartEvents(w, ch.events));

  const days = ch.trades?.days || [];
  const spark = (key, token) => {
    const el = bodyEl.querySelector(`[data-spark="${key}"]`);
    if (el) chartMount(el, (w) => sparkline(Math.max(60, w), days, key, colors()[token]));
  };
  spark("buys", "buys");
  spark("sells", "sells");
}

function mapHeat(x) {
  return { name: x.name || x.shortTpl || x.tpl, tpl: x.tpl, value: x.value, factor: x.factor };
}

/* ---- state table -------------------------------------------------------- */

function buildStateRows(raw) {
  const out = [];
  for (const [tpl, v] of Object.entries(raw || {})) {
    const base = Number(v?.basePrice) || 0;
    const cur = Number(v?.currentPrice) || 0;
    out.push({
      tpl,
      basePrice: base,
      currentPrice: cur,
      factor: base > 0 ? cur / base : 0,
      demand: Number(v?.demand) || 0,
      supply: Number(v?.supply) || 0,
    });
  }
  return out;
}

/** Name line for the state table; empty while the catalog is missing, "unknown ID" when it has no entry. */
function stateNameHtml(tpl) {
  const html = idNameHtml(tpl, "item");
  return html ? `<span class="tpl-name">${html}</span>` : "";
}

function filteredStateRows() {
  const q = pickNorm(stateQuery.trim());
  const rows = q ? stateRows.filter((r) => r.tpl.toLowerCase().includes(q) || pickNorm(idLookupName(r.tpl, "item")).includes(q)) : stateRows;
  const { key, dir } = stateSort;
  return [...rows].sort((a, b) => {
    const av = a[key];
    const bv = b[key];
    if (typeof av === "string") return av.localeCompare(bv) * dir;
    return (av - bv) * dir;
  });
}

const STATE_COLS = [
  { key: "tpl", numeric: false },
  { key: "basePrice", numeric: true },
  { key: "currentPrice", numeric: true },
  { key: "factor", numeric: true },
  { key: "demand", numeric: true },
  { key: "supply", numeric: true },
];

function factorCue(f) {
  const cls = f > 1.02 ? "up" : f < 0.98 ? "down" : "flat";
  return `<span class="f-cue ${cls}">${escapeHtml(fmtFactor(f))}</span>`;
}

function renderStateTable() {
  if (!stateRows.length) return `<div class="empty">${escapeHtml(t("state.noState"))}</div>`;

  const all = filteredStateRows();
  const pages = Math.max(1, Math.ceil(all.length / STATE_PAGE_SIZE));
  statePage = clamp(statePage, 0, pages - 1);
  const page = all.slice(statePage * STATE_PAGE_SIZE, statePage * STATE_PAGE_SIZE + STATE_PAGE_SIZE);

  const head = STATE_COLS.map((c) => {
    const active = stateSort.key === c.key;
    const sortAttr = active ? ` aria-sort="${stateSort.dir === 1 ? "ascending" : "descending"}"` : "";
    const arrow = active ? (stateSort.dir === 1 ? "↑" : "↓") : "";
    return `<th scope="col" class="${c.numeric ? "r" : ""}"${sortAttr}>
      <button type="button" data-sort="${escapeHtml(c.key)}">${escapeHtml(t(`state.col.${c.key}`))}<span aria-hidden="true">${arrow}</span></button>
    </th>`;
  }).join("");

  const body = page.length
    ? page.map((r) => `<tr>
        <td class="tpl" title="${escapeHtml(r.tpl)}">${stateNameHtml(r.tpl)}<span>${escapeHtml(r.tpl)}</span></td>
        <td class="r num">${escapeHtml(fmtInt(Math.round(r.basePrice)))}</td>
        <td class="r num">${escapeHtml(fmtInt(Math.round(r.currentPrice)))}</td>
        <td class="r">${factorCue(r.factor)}</td>
        <td class="r num"><span class="meter" aria-hidden="true"><i style="width:${(clamp(r.demand, 0, 1) * 100).toFixed(0)}%"></i></span> ${escapeHtml(fmt(r.demand))}</td>
        <td class="r num"><span class="meter supply" aria-hidden="true"><i style="width:${(clamp(r.supply, 0, 1) * 100).toFixed(0)}%"></i></span> ${escapeHtml(fmt(r.supply))}</td>
      </tr>`).join("")
    : `<tr><td colspan="6"><div class="empty">${escapeHtml(t("state.noRows"))}</div></td></tr>`;

  return `
    <div class="table-toolbar">
      <input type="search" class="search" data-state-search value="${escapeHtml(stateQuery)}"
             placeholder="${escapeHtml(t("state.search"))}" aria-label="${escapeHtml(t("state.search"))}" />
      <span class="table-count">${escapeHtml(tp("state.rows", { shown: fmtInt(all.length), total: fmtInt(stateRows.length) }))}</span>
    </div>
    <div class="table-scroll">
      <table class="data"><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table>
    </div>
    <div class="pager">
      <span class="pager-info">${escapeHtml(tp("state.page", { page: statePage + 1, pages }))}</span>
      <div class="pager-nav">
        <button type="button" class="btn" data-page="-1" ${statePage === 0 ? "disabled" : ""}>${escapeHtml(t("act.prev"))}</button>
        <button type="button" class="btn" data-page="1" ${statePage >= pages - 1 ? "disabled" : ""}>${escapeHtml(t("act.next"))}</button>
      </div>
    </div>
    <p class="note">${escapeHtml(t("state.readonly"))}</p>
  `;
}

function renderStateSection() {
  const tabs = `<div class="tabs" role="tablist" aria-label="${escapeHtml(t("nav.state"))}">
    <button type="button" class="tab" role="tab" data-state-view="table" aria-selected="${stateView === "table"}">${escapeHtml(t("form.tabTable"))}</button>
    <button type="button" class="tab" role="tab" data-state-view="raw" aria-selected="${stateView === "raw"}">${escapeHtml(t("form.tabRaw"))}</button>
  </div>`;

  const view = stateView === "raw"
    ? `<div class="code-wrap"><textarea class="code" readonly spellcheck="false"
         aria-label="${escapeHtml(t("form.tabRaw"))}">${escapeHtml(JSON.stringify(stateRaw ?? {}, null, 2))}</textarea></div>
       <p class="note">${escapeHtml(t("state.readonly"))}</p>`
    : renderStateTable();

  return `<div class="stack tight"><div class="row">${tabs}</div><div data-state-view-body>${view}</div></div>`;
}

function bindStateSection(bodyEl) {
  if (stateBound.has(bodyEl)) return;
  stateBound.add(bodyEl);

  const rerender = () => {
    const host = bodyEl.querySelector("[data-state-view-body]");
    if (!host) return;
    host.innerHTML = stateView === "raw"
      ? `<div class="code-wrap"><textarea class="code" readonly spellcheck="false"
           aria-label="${escapeHtml(t("form.tabRaw"))}">${escapeHtml(JSON.stringify(stateRaw ?? {}, null, 2))}</textarea></div>
         <p class="note">${escapeHtml(t("state.readonly"))}</p>`
      : renderStateTable();
  };

  bodyEl.addEventListener("click", (e) => {
    const el = e.target instanceof Element ? e.target.closest("button") : null;
    if (!el) return;

    if (el.hasAttribute("data-state-view")) {
      stateView = el.getAttribute("data-state-view") === "raw" ? "raw" : "table";
      bodyEl.querySelectorAll("[data-state-view]").forEach((b) => {
        b.setAttribute("aria-selected", b === el ? "true" : "false");
      });
      rerender();
      return;
    }
    if (el.hasAttribute("data-sort")) {
      const key = el.getAttribute("data-sort");
      if (stateSort.key === key) stateSort.dir = -stateSort.dir;
      else stateSort = { key, dir: key === "tpl" ? 1 : -1 };
      statePage = 0;
      rerender();
      return;
    }
    if (el.hasAttribute("data-page")) {
      statePage += Number(el.getAttribute("data-page"));
      rerender();
    }
  });

  let debounce = 0;
  bodyEl.addEventListener("input", (e) => {
    const el = e.target;
    if (!(el instanceof HTMLElement) || !el.hasAttribute("data-state-search")) return;
    stateQuery = el.value;
    statePage = 0;
    window.clearTimeout(debounce);
    debounce = window.setTimeout(() => {
      rerender();
      root?.querySelector("[data-state-search]")?.focus();
    }, 160);
  });
}

async function copyStateJson() {
  const text = JSON.stringify(stateRaw ?? {}, null, 2);
  try {
    await navigator.clipboard.writeText(text);
    toast(t("act.copied"), "ok");
  } catch {
    toast(t("act.copyFailed"), "err");
  }
}

/* ---- settings ----------------------------------------------------------- */

function renderSettings() {
  return `<div class="stack">
    <section class="card narrow">
      <div class="card-head"><h2 class="card-title">${escapeHtml(t("settings.language"))}</h2></div>
      <div class="opt-row" role="radiogroup" aria-label="${escapeHtml(t("settings.language"))}">
        <label class="opt"><input type="radio" name="uf-lang" value="en" ${lang === "en" ? "checked" : ""} /><span>${escapeHtml(t("settings.en"))}</span></label>
        <label class="opt"><input type="radio" name="uf-lang" value="ru" ${lang === "ru" ? "checked" : ""} /><span>${escapeHtml(t("settings.ru"))}</span></label>
      </div>
      <p class="note">${escapeHtml(t("settings.languageHint"))}</p>
    </section>

    <section class="card narrow">
      <div class="card-head"><h2 class="card-title">${escapeHtml(t("theme.label"))}</h2></div>
      <div class="opt-row" role="radiogroup" aria-label="${escapeHtml(t("theme.label"))}">
        <label class="opt"><input type="radio" name="uf-theme" value="dark" ${theme === "dark" ? "checked" : ""} />${icon("moon", 15)}<span>${escapeHtml(t("theme.dark"))}</span></label>
        <label class="opt"><input type="radio" name="uf-theme" value="light" ${theme === "light" ? "checked" : ""} />${icon("sun", 15)}<span>${escapeHtml(t("theme.light"))}</span></label>
      </div>
      <p class="note">${escapeHtml(t("theme.hint"))}</p>
    </section>

    <section class="card narrow">
      <div class="card-head"><h2 class="card-title">${escapeHtml(t("settings.maintenance"))}</h2></div>
      <button type="button" class="btn" data-act="reload-disk">${icon("reload", 15)}${escapeHtml(t("act.reloadDisk"))}</button>
      <p class="note">${escapeHtml(t("settings.maintenanceHint"))}</p>
    </section>
  </div>`;
}

/* ---- presets ------------------------------------------------------------ */

/** Preset list + per-card UI state. Rebuilt on every section load. */
const presetUi = {
  status: "idle",           // idle | ok | absent (older server build) | error
  error: "",
  list: [],
  previewId: "",            // id shown in the preview overlay, "" when closed
  previews: new Map(),      // id -> "loading" | { configs } | { error }
  applying: "",             // id being applied, "" when idle
  applied: new Map(),       // id -> config names written by the last apply
};

/** The body element survives loadSection(), so bind its listener only once. */
const presetBound = new WeakSet();

/** Server strings are { en, ru } objects; tolerate a bare string too. */
/** Messages from the server are either a plain string (older builds) or { en, ru }. */
function localized(v) {
  if (typeof v === "string") return v;
  const s = v && typeof v === "object" ? v[lang] || v.en : "";
  return typeof s === "string" ? s : "";
}

/** Config names as the nav shows them (Pool, Economy, ...), humanised when unknown. */
function configLabel(name) {
  const key = `nav.${name}`;
  const label = t(key);
  return label === key ? humanKey(name) : label;
}

function sectionIdForConfig(name) {
  const found = sectionList().find((s) => s.kind === "config" && s.name === name);
  return found ? found.id : null;
}

async function loadPresets() {
  presetUi.previewId = "";
  presetUi.previews.clear();
  presetUi.applied.clear();
  try {
    const data = await getJson(`${API}/presets`);
    presetUi.list = Array.isArray(data?.presets) ? data.presets.filter((p) => p && typeof p.id === "string") : [];
    presetUi.status = "ok";
  } catch (e) {
    presetUi.list = [];
    // A 404 just means an older server build without the endpoints: not an error.
    presetUi.status = e?.status === 404 ? "absent" : "error";
    presetUi.error = failText(e, t("presets.loadFailed"));
  }
}

function renderPresetPreview(id) {
  const entry = presetUi.previews.get(id);
  if (entry === "loading" || !entry) return `<p class="note tight">${escapeHtml(t("presets.loadingPreview"))}</p>`;
  if (entry.error) return `<div class="err-box">${escapeHtml(entry.error)}</div>`;

  const blocks = Object.entries(entry.configs).map(([name, value]) => `
    <div class="preset-cfg">
      <span class="pill">${escapeHtml(configLabel(name))}</span>
      <div class="code-wrap"><textarea class="code compact" readonly spellcheck="false"
        aria-label="${escapeHtml(configLabel(name))} JSON">${escapeHtml(JSON.stringify(value, null, 2))}</textarea></div>
    </div>`).join("");
  return `<div class="preset-preview">${blocks || `<p class="note tight">${escapeHtml(t("form.emptyMap"))}</p>`}</div>`;
}

/** Overlay shell, kept generic (.overlay) so other dialogs can reuse it. */
function renderPresetOverlay() {
  const p = presetUi.list.find((x) => x.id === presetUi.previewId);
  if (!p) return "";
  return `<div class="overlay" data-preset-act="close-preview" data-backdrop="1">
    <div class="overlay-panel" role="dialog" aria-modal="true" aria-label="${escapeHtml(localized(p.name) || p.id)}">
      <div class="overlay-head">
        <h2 class="card-title">${escapeHtml(localized(p.name) || p.id)}</h2>
        <button type="button" class="btn icon ghost" data-preset-act="close-preview" data-close
                aria-label="${escapeHtml(t("act.dismiss"))}" title="${escapeHtml(t("act.dismiss"))}">${icon("close", 14)}</button>
      </div>
      <div class="overlay-body">${renderPresetPreview(p.id)}</div>
    </div>
  </div>`;
}

function renderPresetCard(p) {
  const id = escapeHtml(p.id);
  const available = p.available !== false;
  const name = localized(p.name) || p.id;
  const desc = localized(p.description);
  const targets = Array.isArray(p.targets) ? p.targets : [];
  const requires = Array.isArray(p.requires) ? p.requires : [];
  const applying = presetUi.applying === p.id;
  const applied = presetUi.applied.get(p.id);

  const touches = targets.length
    ? `<div class="preset-meta"><span class="preset-meta-label">${escapeHtml(t("presets.touches"))}</span>${
        targets.map((n) => `<span class="pill">${escapeHtml(configLabel(n))}</span>`).join("")}</div>`
    : "";
  const reason = available ? "" : `<div class="warn-box">${icon("lock", 14)}<span>${
    escapeHtml(localized(p.unavailableReason) || t("presets.unavailable"))}</span></div>`;
  const result = applied
    ? `<div class="preset-meta"><span class="preset-meta-label">${escapeHtml(t("presets.appliedTo"))}</span>${
        applied.map((n) => {
          const sid = sectionIdForConfig(n);
          const label = escapeHtml(configLabel(n));
          return sid
            ? `<button type="button" class="pill ok" data-preset-act="open-config" data-name="${escapeHtml(sid)}"
                title="${escapeHtml(tp("presets.openConfig", { name: configLabel(n) }))}">${label}</button>`
            : `<span class="pill ok">${label}</span>`;
        }).join("")}</div>`
    : "";

  return `<article class="card preset${available ? "" : " is-unavailable"}" data-preset="${id}">
    <div class="preset-head">
      <h2 class="card-title">${escapeHtml(name)}</h2>
      ${requires.length ? `<span class="pill${available ? "" : " warn"}">${escapeHtml(t("presets.requires"))}: ${escapeHtml(requires.join(", "))}</span>` : ""}
    </div>
    ${desc ? `<p class="preset-desc">${escapeHtml(desc)}</p>` : ""}
    ${touches}
    ${reason}
    ${result}
    <div class="preset-actions">
      <button type="button" class="btn" data-preset-act="preview" data-id="${id}" aria-haspopup="dialog" ${available ? "" : "disabled"}>${escapeHtml(t("presets.preview"))}</button>
      <button type="button" class="btn primary" data-preset-act="apply" data-id="${id}" ${available && !presetUi.applying ? "" : "disabled"}>${escapeHtml(applying ? t("presets.applying") : t("presets.apply"))}</button>
    </div>
  </article>`;
}

function renderPresets() {
  if (presetUi.status === "absent") {
    return `<div class="empty"><strong>${escapeHtml(t("presets.absent"))}</strong><span>${escapeHtml(t("presets.absentHint"))}</span></div>`;
  }
  if (presetUi.status === "error") {
    return `<div class="err-box">${escapeHtml(presetUi.error)}</div>`;
  }
  if (!presetUi.list.length) return `<div class="empty">${escapeHtml(t("presets.empty"))}</div>`;
  return `<div class="stack tight">
    <p class="note tight">${escapeHtml(t("presets.intro"))}</p>
    <div class="preset-list">${presetUi.list.map(renderPresetCard).join("")}</div>
    ${renderPresetOverlay()}
  </div>`;
}

/** Redraw in place; a no-op once the user has navigated to another section. */
function repaintPresets() {
  if (currentSection().kind !== "presets") return;
  const body = root?.querySelector("[data-body]");
  if (body) body.innerHTML = renderPresets();
}

function closePresetPreview() {
  if (!presetUi.previewId) return;
  presetUi.previewId = "";
  repaintPresets();
}

async function openPresetPreview(id) {
  if (presetUi.list.find((x) => x.id === id)?.available === false) return;
  presetUi.previewId = id;
  const cached = presetUi.previews.get(id);
  if (!cached || cached.error) {
    presetUi.previews.set(id, "loading");
    repaintPresets();
    try {
      const data = await getJson(`${API}/presets/${encodeURIComponent(id)}`);
      presetUi.previews.set(id, { configs: isPlainObject(data?.configs) ? data.configs : {} });
    } catch (e) {
      presetUi.previews.set(id, { error: failText(e, t("presets.previewFailed")) });
    }
  }
  // The user may have closed or switched the overlay while the request was in flight.
  if (presetUi.previewId !== id) return;
  repaintPresets();
  root?.querySelector("[data-close]")?.focus();
}

async function applyPreset(id) {
  const p = presetUi.list.find((x) => x.id === id);
  if (!p || p.available === false || presetUi.applying) return;

  const configs = (Array.isArray(p.targets) ? p.targets : []).map(configLabel).join(", ");
  if (!confirm(tp("presets.confirm", { name: localized(p.name) || p.id, configs }))) return;

  presetUi.applying = id;
  repaintPresets();
  try {
    const { ok, status, json } = await postJson(`${API}/presets/${encodeURIComponent(id)}/apply`, {});
    if (!ok) {
      console.error("preset apply failed", status, json);
      toast(localized(json.error) || t("presets.applyFailed"), "err");
      return;
    }
    const applied = Array.isArray(json.applied) ? json.applied : (p.targets || []);
    presetUi.applied.set(id, applied);
    const warning = localized(json.warning);
    if (warning) toast(warning, "warn");
    else if (json.restartRecommended) toast(t("presets.appliedRestart"), "warn");
    else toast(tp("presets.applied", { configs: applied.map(configLabel).join(", ") }), "ok");
    // Config editors always fetch fresh values when opened, so there is no stale draft to
    // patch; the result row links straight to the configs that changed.
    if (applied.includes("mod")) void refreshBrand();
  } catch (e) {
    toast(failText(e, t("presets.applyFailed")), "err");
  } finally {
    presetUi.applying = "";
    repaintPresets();
  }
}

let presetKeysBound = false;

function bindPresets(bodyEl) {
  if (!presetKeysBound) {
    presetKeysBound = true;
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && presetUi.previewId) closePresetPreview();
    });
  }
  if (presetBound.has(bodyEl)) return;
  presetBound.add(bodyEl);
  bodyEl.addEventListener("click", (e) => {
    const btn = e.target instanceof Element ? e.target.closest("[data-preset-act]") : null;
    if (!btn || btn.disabled) return;
    const act = btn.getAttribute("data-preset-act");
    const id = btn.getAttribute("data-id") || "";
    // The backdrop carries the close action too, but a click inside the panel must not close it.
    if (act === "close-preview" && btn.hasAttribute("data-backdrop") && e.target !== btn) return;
    if (act === "preview") void openPresetPreview(id);
    else if (act === "close-preview") closePresetPreview();
    else if (act === "apply") void applyPreset(id);
    else if (act === "open-config") navigate(btn.getAttribute("data-name"));
  });
}

/* ---- dropdown ----------------------------------------------------------- */

/*
 * A native <select> cannot have its option list themed, so every select is a button plus a
 * popup. A hidden native <select> stays underneath as the source of truth: the form handlers
 * keep reading its value and keep receiving its input/change events, exactly as before.
 *
 * The popup is appended to <body>, not next to the button: that escapes overflow clipping and,
 * for the picker overlay (whose backdrop-filter would trap a fixed child), its containing block.
 */

const DD_MAX_HEIGHT = 288;
const DD_GAP = 4;
const DD_EDGE = 8;

let ddSeq = 0;
let ddOpen = null;          // { btn, native, popup, active }
let ddBound = false;
let ddTypeBuf = "";
let ddTypeTimer = 0;

/** options: [{ value, label }]. attrs: raw attributes for the hidden <select> (data-path etc.). */
function dropdownHtml({ value, options, attrs = "", label }) {
  const cur = options.find((o) => o.value === value) || options[0];
  const id = `dd-${++ddSeq}`;
  const opts = options.map((o) =>
    `<option value="${escapeHtml(o.value)}"${o === cur ? " selected" : ""}>${escapeHtml(o.label)}</option>`).join("");
  return `<div class="dd" data-dd>
    <select hidden tabindex="-1" aria-hidden="true" ${attrs}>${opts}</select>
    <button type="button" class="dd-btn" role="combobox" aria-haspopup="listbox" aria-expanded="false"
            aria-controls="${id}" aria-label="${escapeHtml(`${label}: ${cur?.label ?? ""}`)}" data-dd-label="${escapeHtml(label)}">
      <span class="dd-val">${escapeHtml(cur?.label ?? "")}</span>${icon("chevron", 14)}
    </button>
  </div>`;
}

function ddOptionEls() {
  return ddOpen ? [...ddOpen.popup.children] : [];
}

function ddSetActive(i, scroll = true) {
  if (!ddOpen) return;
  const els = ddOptionEls();
  if (!els.length) return;
  const next = clamp(i, 0, els.length - 1);
  els[ddOpen.active]?.classList.remove("is-active");
  ddOpen.active = next;
  els[next].classList.add("is-active");
  ddOpen.btn.setAttribute("aria-activedescendant", els[next].id);
  if (scroll) els[next].scrollIntoView({ block: "nearest" });
}

/** Place the popup under the button, or above it when there is no room below. */
function ddPosition() {
  if (!ddOpen) return;
  const { btn, popup } = ddOpen;
  const r = btn.getBoundingClientRect();
  const below = window.innerHeight - r.bottom - DD_EDGE - DD_GAP;
  const above = r.top - DD_EDGE - DD_GAP;

  popup.style.minWidth = `${Math.round(r.width)}px`;
  popup.style.maxHeight = `${DD_MAX_HEIGHT}px`;
  const natural = Math.min(popup.scrollHeight, DD_MAX_HEIGHT);
  const flip = natural > below && above > below;
  const room = Math.max(80, flip ? above : below);
  popup.style.maxHeight = `${Math.min(DD_MAX_HEIGHT, room)}px`;

  const h = Math.min(natural, room);
  popup.style.top = `${Math.round(flip ? r.top - DD_GAP - h : r.bottom + DD_GAP)}px`;
  const w = popup.offsetWidth;
  popup.style.left = `${Math.round(clamp(r.left, DD_EDGE, Math.max(DD_EDGE, window.innerWidth - w - DD_EDGE)))}px`;
  popup.classList.toggle("is-flipped", flip);
}

function openDropdown(btn) {
  const native = btn.parentElement?.querySelector("select");
  if (!native || btn.disabled) return;
  closeDropdown();

  const popup = document.createElement("div");
  popup.className = "dd-popup";
  popup.id = btn.getAttribute("aria-controls") || "";
  popup.setAttribute("role", "listbox");
  popup.setAttribute("aria-label", btn.getAttribute("data-dd-label") || "");
  popup.innerHTML = [...native.options].map((o, i) =>
    `<div class="dd-opt" role="option" id="${escapeHtml(popup.id)}-o${i}" data-dd-i="${i}"
          aria-selected="${o.selected}">${escapeHtml(o.textContent)}</div>`).join("");
  document.body.appendChild(popup);

  ddOpen = { btn, native, popup, active: native.selectedIndex };
  btn.setAttribute("aria-expanded", "true");
  btn.parentElement.classList.add("is-open");
  ddPosition();
  ddSetActive(native.selectedIndex);
}

function closeDropdown(refocus = false) {
  if (!ddOpen) return;
  const { btn, popup } = ddOpen;
  ddOpen = null;
  popup.remove();
  btn.setAttribute("aria-expanded", "false");
  btn.removeAttribute("aria-activedescendant");
  btn.parentElement?.classList.remove("is-open");
  if (refocus && btn.isConnected) btn.focus();
}

/** Commit an option: update the hidden select and fire the events a native select would. */
function pickDropdown(i) {
  if (!ddOpen) return;
  const { btn, native } = ddOpen;
  const changed = native.selectedIndex !== i;
  if (changed) {
    native.selectedIndex = i;
    const text = native.options[i].textContent;
    btn.querySelector(".dd-val").textContent = text;
    btn.setAttribute("aria-label", `${btn.getAttribute("data-dd-label")}: ${text}`);
  }
  closeDropdown(true);
  if (changed) {
    native.dispatchEvent(new Event("input", { bubbles: true }));
    native.dispatchEvent(new Event("change", { bubbles: true }));
  }
}

/** Type-ahead like a native select: jump to the next option starting with what was typed. */
function ddTypeAhead(ch) {
  window.clearTimeout(ddTypeTimer);
  ddTypeTimer = window.setTimeout(() => { ddTypeBuf = ""; }, 700);
  ddTypeBuf += ch.toLowerCase();
  const els = ddOptionEls();
  const from = ddTypeBuf.length > 1 ? ddOpen.active : ddOpen.active + 1;
  for (let k = 0; k < els.length; k++) {
    const idx = (from + k) % els.length;
    if (els[idx].textContent.trim().toLowerCase().startsWith(ddTypeBuf)) { ddSetActive(idx); return; }
  }
}

function bindDropdowns() {
  if (ddBound) return;
  ddBound = true;

  document.addEventListener("click", (e) => {
    const target = e.target instanceof Element ? e.target : null;
    const btn = target?.closest(".dd-btn");
    if (btn) {
      if (ddOpen && ddOpen.btn === btn) closeDropdown();
      else openDropdown(btn);
      return;
    }
    const opt = target?.closest(".dd-opt");
    if (opt && ddOpen?.popup.contains(opt)) { pickDropdown(Number(opt.getAttribute("data-dd-i"))); return; }
    closeDropdown();
  });

  // Capture phase, and handled keys are stopped, so Esc or arrows on an open dropdown do not
  // also reach the picker overlay or its row navigation underneath.
  document.addEventListener("keydown", (e) => {
    const onBtn = e.target instanceof Element && e.target.closest(".dd-btn");
    if (!ddOpen && !onBtn) return;
    const eat = () => { e.preventDefault(); e.stopPropagation(); };

    if (!ddOpen) {
      if (e.key === "Enter" || e.key === " " || e.key === "ArrowDown" || e.key === "ArrowUp") {
        eat();
        openDropdown(e.target.closest(".dd-btn"));
      } else if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
        eat();
        openDropdown(e.target.closest(".dd-btn"));
        if (ddOpen) ddTypeAhead(e.key);
      }
      return;
    }

    switch (e.key) {
      case "ArrowDown": eat(); ddSetActive(ddOpen.active + 1); break;
      case "ArrowUp": eat(); ddSetActive(ddOpen.active - 1); break;
      case "Home": eat(); ddSetActive(0); break;
      case "End": eat(); ddSetActive(ddOptionEls().length - 1); break;
      case "Enter": eat(); pickDropdown(ddOpen.active); break;
      case " ":
        eat();
        if (ddTypeBuf) ddTypeAhead(" ");
        else pickDropdown(ddOpen.active);
        break;
      case "Escape": eat(); closeDropdown(true); break;
      case "Tab": closeDropdown(); break;
      default:
        if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) { eat(); ddTypeAhead(e.key); }
    }
  }, true);

  // A fixed popup would drift from its button if the page moved under it, so just close.
  window.addEventListener("resize", () => closeDropdown());
  window.addEventListener("scroll", (e) => {
    if (ddOpen && !ddOpen.popup.contains(e.target)) closeDropdown();
  }, true);
}

/* ---- item picker -------------------------------------------------------- */

const PICK_MAX = 100;
const PICK_DEBOUNCE_MS = 120;

/** Item catalog from GET /items, fetched once on first open and kept for the page session. */
const catalog = {
  status: "idle",           // idle | loading | ok | absent (older server build) | error
  error: "",
  items: [],                // { id, en, ru, se, sr, cat, src (string), ic (real icon exists), hay }
  cats: [],                 // { id, name, nameRu, icon, hay }
  catById: new Map(),
  itemById: new Map(),
  sources: new Map(),       // String(id) -> { id, guid, name, author }
  promise: null,
};

/** Open picker session, null when closed. */
let picker = null;
let pickerSeq = 0;
let pickerHost = null;
let pickerKeysBound = false;
let pickerDebounce = 0;
const pickerBound = new WeakSet();

/** schema path -> what the field holds. Everything else keeps plain manual entry. */
const PICKERS = {
  "pool.add": "item",
  "pool.remove": "item",
  "pool.addCategories": "category",
  "pool.removeCategories": "category",
  "pricing.fixedPrices": "item",
  "pricing.itemMultipliers": "item",
  "pricing.categoryMultipliers": "category",
  "levels.items": "item",
  "levels.categories": "category",
  "events.events[].categories": "category",
  "events.events[].items": "item",
  "trends.trends[].categories": "category",
  "trends.trends[].items": "item",
  "trends.trends[].category": "category",
};

/** A multiplier of 0 would zero a price; 1 is the neutral starting row. */
const PICK_MAP_DEFAULTS = { "pricing.itemMultipliers": 1, "pricing.categoryMultipliers": 1 };

/** Lowercase, and treat ё as е so either spelling finds the same item. */
function pickNorm(s) {
  return String(s ?? "").toLowerCase().replace(/ё/g, "е");
}

function pickHash(str, mod) {
  let h = 0;
  for (let i = 0; i < str.length; i++) h = (h * 31 + str.charCodeAt(i)) | 0;
  return Math.abs(h) % mod;
}

/** Only same-origin paths: the dashboard never fetches an image from elsewhere. */
function pickSafeIcon(icon) {
  return typeof icon === "string" && icon.startsWith("/") && !icon.startsWith("//") && !icon.includes("\\") ? icon : null;
}

function indexCatalog(data) {
  const sources = new Map();
  for (const s of Array.isArray(data?.sources) ? data.sources : []) {
    if (s) sources.set(String(s.id), s);
  }

  const cats = (Array.isArray(data?.categories) ? data.categories : [])
    .filter((c) => c && typeof c.id === "string")
    .map((c) => ({ ...c, icon: pickSafeIcon(c.icon), hay: pickNorm(`${c.id} ${c.name} ${c.nameRu}`) }));

  // The haystack is built once here, not per keystroke.
  const items = (Array.isArray(data?.items) ? data.items : [])
    .filter((it) => it && typeof it.id === "string")
    .map((it) => {
      // 0 = vanilla, an id in `sources` = that mod, -1 = unknown. Anything else (an id with no
      // sources entry, a missing or odd value) is folded into -1 so it never renders blank.
      let src = String(it.src ?? 0);
      if (src !== "0" && !sources.has(src)) src = "-1";
      const srcName = src === "0" ? "vanilla" : src === "-1" ? "unknown неизвестный" : (sources.get(src)?.name || "");
      return {
        id: it.id, en: it.en || "", ru: it.ru || "", se: it.se || "", sr: it.sr || "", cat: it.cat || "", src, ic: it.ic === true,
        hay: pickNorm(`${it.id} ${it.en} ${it.ru} ${it.se} ${it.sr} ${srcName}`),
      };
    });

  catalog.sources = sources;
  catalog.cats = cats;
  catalog.catById = new Map(cats.map((c) => [c.id, c]));
  catalog.items = items;
  catalog.itemById = new Map(items.map((it) => [it.id, it]));
}

function loadCatalog() {
  if (catalog.status === "ok" || catalog.status === "absent") return Promise.resolve();
  if (catalog.promise) return catalog.promise;
  catalog.status = "loading";
  catalog.promise = (async () => {
    try {
      indexCatalog(await getJson(`${API}/items`));
      catalog.status = "ok";
    } catch (e) {
      // 404 = older server build; anything else may be transient, so it is retried on the next open.
      catalog.status = e?.status === 404 ? "absent" : "error";
      catalog.error = failText(e, t("picker.loadFailed"));
    } finally {
      catalog.promise = null;
    }
  })();
  return catalog.promise;
}

/** Readable name for an id, or "" when the catalog has no entry (or is not loaded). */
function idLookupName(id, kind) {
  if (catalog.status !== "ok") return "";
  const hit = kind === "category" ? catalog.catById.get(id) : catalog.itemById.get(id);
  if (!hit) return "";
  return kind === "category" ? pickCatName(hit) : pickName(hit);
}

/**
 * Name line under/over an id: the localized name (plus a badge for modded items), or a plain
 * "unknown ID" when the catalog is loaded and has no such entry. Empty until the catalog is ready,
 * so the form never waits on the fetch.
 */
function idNameHtml(id, kind) {
  const v = String(id ?? "").trim();
  if (!v || catalog.status !== "ok") return "";
  const hit = kind === "category" ? catalog.catById.get(v) : catalog.itemById.get(v);
  if (!hit) {
    return `<span class="id-unknown" title="${escapeHtml(t("idname.unknownHint"))}">${escapeHtml(t("idname.unknown"))}</span>`;
  }
  const name = kind === "category" ? pickCatName(hit) : pickName(hit);
  const badge = kind === "item" && hit.src !== "0" ? pickSourceBadge(hit) : "";
  return `<span class="id-name-text">${escapeHtml(name)}</span>${badge}`;
}

/** An id input with its name line above it. */
function idCell(inputHtml, kind, value) {
  return `<div class="id-cell"><div class="id-name" data-idname data-kind="${kind}">${idNameHtml(value, kind)}</div>${inputHtml}</div>`;
}

function idNameValue(el) {
  const input = el.closest(".id-cell")?.querySelector("input");
  return input ? input.value : el.getAttribute("data-idval") || "";
}

function fillIdName(el) {
  el.innerHTML = idNameHtml(idNameValue(el), el.getAttribute("data-kind"));
}

/** Fill in names on the current page once the catalog is ready, without re-rendering anything. */
function refreshIdNameFor(input) {
  const name = input.closest(".id-cell")?.querySelector("[data-idname]");
  if (name) fillIdName(name);
}

function refreshIdNames() {
  root?.querySelectorAll("[data-idname]").forEach(fillIdName);
  root?.querySelectorAll("[data-idtitle]").forEach((el) => {
    const name = idLookupName(el.getAttribute("data-idval") || "", el.getAttribute("data-idtitle"));
    if (name) el.textContent = truncate(name, 48);
  });
}

/** Config sections that hold TPL or category ids; only these trigger the catalog fetch. */
const ID_SECTIONS = new Set(["pool", "pricing", "events", "trends", "levels"]);

/** Sections with id fields call this: one fetch per page session, shared with the picker. */
async function ensureIdNames() {
  await loadCatalog();
  if (catalog.status === "ok") refreshIdNames();
}

/** The "pick" button next to a manual input; disabled once the server is known to lack the catalog. */
function pickBtn(schemaPath, pathAttr, mode) {
  const kind = PICKERS[schemaPath];
  if (!kind) return "";
  const off = catalog.status === "absent";
  const title = escapeHtml(off ? t("picker.absent") : t("picker.pickTitle"));
  const attrs = `data-act="pick" data-path="${pathAttr}" data-kind="${kind}" data-mode="${mode}"`
    + ` data-schema="${escapeHtml(schemaPath)}" title="${title}" ${off ? "disabled" : ""}`;
  if (mode === "field") {
    return `<button type="button" class="x-btn" ${attrs} aria-label="${escapeHtml(t("picker.pick"))}">${icon("search", 14)}</button>`;
  }
  return `<button type="button" class="btn" ${attrs}>${icon("search", 14)}${escapeHtml(t("picker.pick"))}</button>`;
}

function pickName(it) {
  return (lang === "ru" ? it.ru || it.en : it.en || it.ru) || it.id;
}

function pickAlt(it) {
  const alt = lang === "ru" ? it.en : it.ru;
  return alt && alt !== pickName(it) ? alt : "";
}

function pickCatName(c) {
  return (lang === "ru" ? c.nameRu || c.name : c.name || c.nameRu) || c.id;
}

function pickInitials(text) {
  const words = String(text || "").split(/[\s\-_./]+/).filter(Boolean);
  const chars = words.length >= 2
    ? [Array.from(words[0])[0], Array.from(words[1])[0]]
    : Array.from(words[0] || "?").slice(0, 2);
  return chars.join("").toUpperCase();
}

function pickSourceBadge(it) {
  if (it.src === "0") return `<span class="src-v">${escapeHtml(t("picker.vanilla"))}</span>`;
  const s = catalog.sources.get(it.src);
  // Not vanilla, but the mod could not be determined: dashed and quiet, so it reads as "unknown"
  // rather than as a mod name or as vanilla.
  if (!s) {
    return `<span class="src-badge unk" title="${escapeHtml(t("picker.unknownSourceHint"))}">${escapeHtml(t("picker.unknownSource"))}</span>`;
  }
  const name = s.name || `#${it.src}`;
  const tip = [s.name, s.author && `(${s.author})`, s.guid].filter(Boolean).join(" ");
  return `<span class="src-badge m${pickHash(it.src, 4)}" title="${escapeHtml(tip)}">${escapeHtml(truncate(name, 22))}</span>`;
}

function pickRow(it, i) {
  const isItem = picker.kind === "item";
  const cat = isItem ? catalog.catById.get(it.cat) : it;
  const name = isItem ? pickName(it) : pickCatName(it);
  const alt = isItem ? pickAlt(it) : (lang === "ru" ? it.name : it.nameRu);
  const short = isItem ? (lang === "ru" ? it.sr || it.se : it.se || it.sr) : name;
  const multi = picker.mode === "list";
  const sel = picker.selected.has(it.id);
  const have = picker.have.has(it.id);
  const glyph = cat?.icon;
  // Never request the icon route for an item without `ic`: that would be thousands of 404s.
  const real = isItem && it.ic ? pickSafeIcon(`${API}/items/${encodeURIComponent(it.id)}/icon`) : null;

  const cls = ["pick-row", i === picker.active ? "is-active" : "", sel ? "is-sel" : "", have ? "is-have" : ""].filter(Boolean).join(" ");
  return `<div class="${cls}" id="pick-opt-${i}" role="option" aria-selected="${sel}" data-pick-i="${i}">
    ${multi ? `<span class="pick-check" aria-hidden="true">${sel ? icon("check", 12) : ""}</span>` : ""}
    <span class="ico ph-${pickHash(cat?.id || it.id, 5)}" aria-hidden="true"><span>${escapeHtml(pickInitials(short || name))}</span>${
      glyph ? `<img src="${escapeHtml(glyph)}" alt="" loading="lazy" decoding="async" />` : ""}${
      real ? `<img class="real" src="${escapeHtml(real)}" alt="" loading="lazy" decoding="async" />` : ""}</span>
    <span class="pick-main">
      <span class="pick-name">${escapeHtml(name)}${have ? ` <span class="pill">${escapeHtml(t("picker.have"))}</span>` : ""}</span>
      <span class="pick-sub"><span class="mono">${escapeHtml(it.id)}</span>${alt ? ` ${escapeHtml(alt)}` : ""}</span>
    </span>
    ${isItem ? `<span class="pick-meta">${pickSourceBadge(it)}</span>` : ""}
  </div>`;
}

function pickerToolsHtml() {
  const q = escapeHtml(t("picker.search"));
  let filters = "";
  if (picker.kind === "item") {
    const srcOpts = [{ value: "all", label: t("picker.allSources") }, { value: "0", label: t("picker.vanilla") }, { value: "-1", label: t("picker.unknownSource") },
      ...[...catalog.sources.values()].map((s) => ({ value: String(s.id), label: s.name || String(s.id) }))];
    const catOpts = [{ value: "all", label: t("picker.allCategories") },
      ...[...catalog.cats].sort((a, b) => pickCatName(a).localeCompare(pickCatName(b), lang))
        .map((c) => ({ value: c.id, label: pickCatName(c) }))];
    filters = dropdownHtml({ value: picker.src, options: srcOpts, attrs: "data-picker-src", label: t("picker.source") })
      + dropdownHtml({ value: picker.cat, options: catOpts, attrs: "data-picker-cat", label: t("picker.category") });
  }
  return `<div class="picker-tools">
    <input type="search" class="search" data-picker-q placeholder="${q}" aria-label="${q}" role="combobox"
           aria-controls="pick-list" aria-expanded="true" autocomplete="off" spellcheck="false" />
    ${filters}
  </div>`;
}

function paintPicker() {
  const host = ensurePickerHost();
  if (!picker) { host.innerHTML = ""; return; }

  const title = escapeHtml(t(picker.kind === "item" ? "picker.titleItem" : "picker.titleCategory"));
  let inner;
  if (catalog.status === "ok") {
    inner = `${pickerToolsHtml()}
      <div class="overlay-body picker-body" id="pick-list" data-picker-list role="listbox"
           ${picker.mode === "list" ? 'aria-multiselectable="true"' : ""}></div>
      <div class="picker-foot" data-picker-foot></div>`;
  } else if (catalog.status === "absent") {
    inner = `<div class="overlay-body"><div class="empty"><strong>${escapeHtml(t("picker.absent"))}</strong><span>${escapeHtml(t("picker.absentHint"))}</span></div></div>`;
  } else if (catalog.status === "error") {
    inner = `<div class="overlay-body"><div class="err-box">${escapeHtml(catalog.error)}</div></div>`;
  } else {
    inner = `<div class="overlay-body"><p class="note tight">${escapeHtml(t("picker.loading"))}</p></div>`;
  }

  host.innerHTML = `<div class="overlay" data-picker-backdrop>
    <div class="overlay-panel picker" role="dialog" aria-modal="true" aria-label="${title}">
      <div class="overlay-head">
        <h2 class="card-title">${title}</h2>
        <button type="button" class="btn icon ghost" data-pick-act="close"
                aria-label="${escapeHtml(t("act.dismiss"))}" title="${escapeHtml(t("act.dismiss"))}">${icon("close", 14)}</button>
      </div>
      ${inner}
    </div>
  </div>`;
}

function runPickerSearch() {
  if (!picker) return;
  // Every word must match somewhere in the haystack, in any order.
  const words = pickNorm(picker.query).split(/\s+/).filter(Boolean);
  const isItem = picker.kind === "item";
  const out = [];
  for (const it of isItem ? catalog.items : catalog.cats) {
    if (isItem) {
      if (picker.src !== "all" && it.src !== picker.src) continue;
      if (picker.cat !== "all" && it.cat !== picker.cat) continue;
    }
    let ok = true;
    for (const w of words) {
      if (!it.hay.includes(w)) { ok = false; break; }
    }
    if (ok) out.push(it);
  }
  picker.matches = out;
  picker.active = 0;
  renderPickerList();
  renderPickerFoot();
}

/** Only the first PICK_MAX matches are in the DOM; the footer says how many more there are. */
function renderPickerList(keepScroll = false) {
  const list = pickerHost?.querySelector("[data-picker-list]");
  if (!list || !picker) return;
  const top = list.scrollTop;
  const shown = picker.matches.slice(0, PICK_MAX);
  list.innerHTML = shown.length
    ? shown.map(pickRow).join("")
    : `<div class="empty">${escapeHtml(t("picker.noMatches"))}</div>`;
  list.scrollTop = keepScroll ? top : 0;
  pickerHost.querySelector("[data-picker-q]")?.setAttribute("aria-activedescendant", shown.length ? `pick-opt-${picker.active}` : "");
}

function renderPickerFoot() {
  const foot = pickerHost?.querySelector("[data-picker-foot]");
  if (!foot || !picker) return;
  const n = picker.matches.length;
  const count = n > PICK_MAX ? tp("picker.capped", { shown: PICK_MAX, n }) : tp("picker.count", { n });
  const multi = picker.mode === "list";
  foot.innerHTML = `<div class="picker-foot-text"><span>${escapeHtml(count)}</span><span class="picker-hint">${escapeHtml(t("picker.keys"))}</span>${
    picker.kind === "item" ? `<span class="picker-hint">${escapeHtml(t("picker.iconNote"))}</span>` : ""}</div>${
    multi ? `<div class="picker-foot-act"><span class="card-sub">${escapeHtml(tp("picker.selected", { n: picker.selected.size }))}</span>
      <button type="button" class="btn primary" data-pick-act="insert" ${picker.selected.size ? "" : "disabled"}>${escapeHtml(t("picker.insert"))}</button></div>` : ""}`;
}

function movePickerActive(delta) {
  const list = pickerHost?.querySelector("[data-picker-list]");
  const n = Math.min(picker?.matches.length || 0, PICK_MAX);
  if (!list || !n) return;
  picker.active = (picker.active + delta + n) % n;
  list.querySelector(".pick-row.is-active")?.classList.remove("is-active");
  const row = list.children[picker.active];
  row?.classList.add("is-active");
  row?.scrollIntoView({ block: "nearest" });
  pickerHost.querySelector("[data-picker-q]")?.setAttribute("aria-activedescendant", `pick-opt-${picker.active}`);
}

function activatePickerRow(i) {
  const it = picker?.matches[i];
  if (!it) return;
  if (picker.mode !== "list") { commitPick([it.id]); return; }
  if (picker.selected.has(it.id)) picker.selected.delete(it.id);
  else picker.selected.add(it.id);
  picker.active = i;
  renderPickerList(true);
  renderPickerFoot();
}

function insertPicked() {
  if (picker && picker.selected.size) commitPick([...picker.selected]);
}

function closePicker() {
  if (!picker) return;
  picker = null;
  window.clearTimeout(pickerDebounce);
  paintPicker();
}

/** Write the picked ids into the draft, the same way the manual Add buttons do. */
function commitPick(ids) {
  const s = picker;
  if (!s || !cfg || cfg.name !== s.cfgName || !ids.length) { closePicker(); return; }
  const target = getAt(cfg.draft, s.parts);
  let focusId = null;

  if (s.mode === "list" && Array.isArray(target)) {
    const fresh = ids.filter((id) => !target.includes(id));
    target.push(...fresh);
    toast(fresh.length === ids.length
      ? tp("picker.added", { n: fresh.length })
      : tp("picker.addedSkipped", { n: fresh.length, skipped: ids.length - fresh.length }), "ok");
  } else if (s.mode === "map" && isPlainObject(target)) {
    focusId = ids[0];
    if (!(focusId in target)) target[focusId] = PICK_MAP_DEFAULTS[s.schema] ?? 0;
  } else if (s.mode === "field") {
    setAt(cfg.draft, s.parts, ids[0]);
  }

  closePicker();
  rerenderCfgBody();
  syncDirty();

  if (focusId != null) {
    const row = [...(root?.querySelectorAll("[data-map-row]") || [])].find((r) => r.getAttribute("data-map-row") === focusId);
    const input = row?.querySelector('[data-map-field="value"]');
    input?.focus();
    input?.select?.();
  }
}

async function openPicker(btn) {
  const parts = (btn.getAttribute("data-path") || "").split("/").filter(Boolean);
  const seq = ++pickerSeq;
  const target = cfg ? getAt(cfg.draft, parts) : null;
  const have = Array.isArray(target) ? target.map(String)
    : isPlainObject(target) ? Object.keys(target)
    : typeof target === "string" && target ? [target] : [];

  picker = {
    seq, parts, cfgName: cfg?.name,
    kind: btn.getAttribute("data-kind") === "category" ? "category" : "item",
    mode: btn.getAttribute("data-mode") || "list",
    schema: btn.getAttribute("data-schema") || "",
    query: "", src: "all", cat: "all",
    selected: new Set(), have: new Set(have), matches: [], active: 0,
  };
  paintPicker();

  await loadCatalog();
  if (!picker || picker.seq !== seq) return;
  paintPicker();
  if (catalog.status === "absent") {
    // Remember it so the buttons in the form stay disabled with a calm note.
    rerenderCfgBody();
    return;
  }
  if (catalog.status === "ok") {
    runPickerSearch();
    pickerHost?.querySelector("[data-picker-q]")?.focus();
  }
}

function ensurePickerHost() {
  if (!pickerHost || !root?.contains(pickerHost)) {
    pickerHost = document.createElement("div");
    pickerHost.setAttribute("data-picker-host", "");
    root?.appendChild(pickerHost);
  }
  bindPicker(pickerHost);
  return pickerHost;
}

function bindPicker(host) {
  if (!pickerKeysBound) {
    pickerKeysBound = true;
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && picker) closePicker();
    });
  }
  if (pickerBound.has(host)) return;
  pickerBound.add(host);

  host.addEventListener("click", (e) => {
    const target = e.target instanceof Element ? e.target : null;
    if (!target) return;
    if (target.hasAttribute("data-picker-backdrop")) { closePicker(); return; }
    const act = target.closest("[data-pick-act]");
    if (act) {
      if (act.getAttribute("data-pick-act") === "close") closePicker();
      else if (act.getAttribute("data-pick-act") === "insert") insertPicked();
      return;
    }
    const row = target.closest("[data-pick-i]");
    if (row) activatePickerRow(Number(row.getAttribute("data-pick-i")));
  });

  host.addEventListener("input", (e) => {
    const el = e.target;
    if (!(el instanceof HTMLElement) || !el.hasAttribute("data-picker-q") || !picker) return;
    picker.query = el.value;
    window.clearTimeout(pickerDebounce);
    pickerDebounce = window.setTimeout(runPickerSearch, PICK_DEBOUNCE_MS);
  });

  host.addEventListener("change", (e) => {
    const el = e.target;
    if (!(el instanceof HTMLElement) || !picker) return;
    if (el.hasAttribute("data-picker-src")) picker.src = el.value;
    else if (el.hasAttribute("data-picker-cat")) picker.cat = el.value;
    else return;
    runPickerSearch();
  });

  host.addEventListener("keydown", (e) => {
    if (!picker || catalog.status !== "ok") return;
    if (e.key === "ArrowDown" || e.key === "ArrowUp") {
      e.preventDefault();
      movePickerActive(e.key === "ArrowDown" ? 1 : -1);
    } else if (e.key === "Enter") {
      const target = e.target instanceof Element ? e.target : null;
      if (target?.closest("button, select")) return;
      e.preventDefault();
      if (e.ctrlKey || e.metaKey) insertPicked();
      else activatePickerRow(picker.active);
    }
  });

  // Image errors do not bubble, so listen in the capture phase. Layers stack real icon, then
  // category glyph, then initials, so dropping a broken image reveals the next level down.
  host.addEventListener("error", (e) => {
    if (e.target instanceof HTMLImageElement) e.target.remove();
  }, true);
}

/* =========================================================================
 * 11. Shell, router, mount
 * ========================================================================= */

function subtitle(section) {
  switch (section.kind) {
    case "overview": return t("sub.overview");
    case "config": return section.restart ? t("sub.configRestart") : t("sub.config");
    case "selling": return t("sub.selling");
    case "settings": return t("sub.settings");
    case "state": return t("sub.state");
    case "presets": return t("sub.presets");
    default: return "";
  }
}

/** Header actions, built synchronously so they never jump while loading. */
function actionsHtml(section) {
  const spin = `<span class="spinner" data-busy hidden aria-hidden="true"></span>`;
  const themeBtn = `<button type="button" class="btn icon ghost" data-act="theme"
    aria-label="${escapeHtml(t("theme.toggle"))}" title="${escapeHtml(t("theme.toggle"))}">${icon(theme === "dark" ? "sun" : "moon", 16)}</button>`;

  if (section.kind === "overview") {
    return `${spin}
      <label class="opt" title="${escapeHtml(t("act.autoRefreshHint"))}">
        <input type="checkbox" class="switch" role="switch" data-act="auto" ${autoRefresh ? "checked" : ""}
               aria-label="${escapeHtml(t("act.autoRefresh"))}" />
        <span>${escapeHtml(t("act.autoRefresh"))}</span>
      </label>
      <button type="button" class="btn" data-act="reload-disk">${icon("reload", 15)}${escapeHtml(t("act.reloadDisk"))}</button>
      <button type="button" class="btn primary" data-act="refresh">${escapeHtml(t("act.refresh"))}</button>
      ${themeBtn}`;
  }
  if (section.kind === "config" || section.kind === "selling" || section.kind === "presets") {
    return `${spin}<button type="button" class="btn" data-act="refresh">${escapeHtml(t("act.reload"))}</button>${themeBtn}`;
  }
  if (section.kind === "state") {
    return `${spin}
      <button type="button" class="btn" data-act="copy-state">${escapeHtml(t("act.copyJson"))}</button>
      <button type="button" class="btn primary" data-act="refresh">${escapeHtml(t("act.refresh"))}</button>
      ${themeBtn}`;
  }
  return `${spin}${themeBtn}`;
}

function navHtml(section) {
  const list = sectionList();
  return GROUP_ORDER.map((group) => {
    const items = list.filter((s) => s.group === group);
    if (!items.length) return "";
    return `<div class="nav-group" role="group" aria-label="${escapeHtml(t(`navGroup.${group}`))}">
      <div class="nav-group-title">${escapeHtml(t(`navGroup.${group}`))}</div>
      ${items.map((s) => `<button type="button" class="nav-btn" data-nav="${escapeHtml(s.id)}"
        title="${escapeHtml(s.label)}" ${s.id === section.id ? 'aria-current="page"' : ""}>
        ${icon(s.icon)}<span>${escapeHtml(s.label)}</span></button>`).join("")}
    </div>`;
  }).join("");
}

function render() {
  if (!root) return;
  closeDropdown();
  // The picker host lives inside root and is wiped with it.
  picker = null;
  pickerHost = null;
  const section = currentSection();
  const dotClass = brand.enabled == null ? "" : brand.enabled ? "is-on" : "is-off";
  const dotLabel = brand.enabled == null ? "" : brand.enabled ? t("enabled") : t("disabled");

  root.innerHTML = `
    <div class="shell">
      <aside class="side">
        <div class="brand">
          <span class="brand-mark" aria-hidden="true">${icon("flea", 18)}</span>
          <span class="brand-text">
            <span class="brand-name">UltimateFlea</span>
            <span class="brand-meta">
              <span class="dot ${dotClass}" role="img" aria-label="${escapeHtml(dotLabel)}"></span>
              <span>${escapeHtml(brand.version ? `v${brand.version}` : t("brandSub"))}</span>
            </span>
          </span>
        </div>
        <nav aria-label="${escapeHtml(t("brandSub"))}">${navHtml(section)}</nav>
        <div class="side-links" role="group" aria-label="${escapeHtml(t("links.label"))}">
          ${SIDE_LINKS.map((l) => `<a class="side-link" href="${escapeHtml(l.href)}" target="_blank" rel="noopener noreferrer"
            title="${escapeHtml(t(`links.${l.id}Tip`))}" aria-label="${escapeHtml(t(`links.${l.id}Tip`))}">${icon(l.icon)}<span>${escapeHtml(l.label)}</span></a>`).join("")}
        </div>
      </aside>
      <main class="main">
        <header class="top">
          <div class="top-text">
            <h1>${escapeHtml(section.label)}</h1>
            <p>${escapeHtml(subtitle(section))}</p>
          </div>
          <div class="actions">${actionsHtml(section)}</div>
        </header>
        <div class="body" data-body></div>
      </main>
      <div class="toasts" data-toasts aria-live="polite" aria-atomic="false"></div>
    </div>
  `;

  bindShell();
}

function bindShell() {
  root.querySelectorAll("[data-nav]").forEach((btn) => {
    btn.addEventListener("click", () => navigate(btn.getAttribute("data-nav")));
  });

  const actions = root.querySelector(".actions");
  actions?.addEventListener("click", (e) => {
    const el = e.target instanceof Element ? e.target.closest("[data-act]") : null;
    if (!el) return;
    const act = el.getAttribute("data-act");
    if (act === "refresh") void loadSection();
    else if (act === "theme") setTheme(theme === "dark" ? "light" : "dark");
    else if (act === "reload-disk") void reloadFromDisk();
    else if (act === "copy-state") void copyStateJson();
  });

  actions?.addEventListener("change", (e) => {
    const el = e.target;
    if (el instanceof HTMLElement && el.getAttribute("data-act") === "auto") {
      setAutoRefresh(!!el.checked);
    }
  });
}

function navigate(nextId) {
  if (!nextId || nextId === sectionId) return;
  if (isDirty() && !confirm(t("discard"))) return;
  cfg = null;
  sectionId = nextId;
  if (nextId !== "home") setAutoRefresh(false);
  render();
  void loadSection();
}

function setAutoRefresh(on) {
  autoRefresh = !!on;
  window.clearInterval(autoTimer);
  autoTimer = 0;
  if (!autoRefresh) return;
  autoTimer = window.setInterval(() => {
    if (sectionId === "home" && !busy) void loadSection({ silent: true });
  }, AUTO_REFRESH_MS);
}

function setBusy(on) {
  busy = on;
  const spinner = root?.querySelector("[data-busy]");
  if (spinner) spinner.hidden = !on;
  root?.querySelectorAll('.actions [data-act="refresh"]').forEach((b) => { b.disabled = on; });
  root?.querySelector("[data-body]")?.setAttribute("aria-busy", on ? "true" : "false");
}

function skeletonBody() {
  return `<div class="kpis">${Array.from({ length: 4 }, () =>
    `<div class="card kpi"><div class="skeleton" style="width:50%"></div><div class="skeleton" style="height:28px"></div></div>`).join("")}</div>
    <div class="chart-grid">
      <div class="card col-12"><div class="skeleton" style="height:220px"></div></div>
      <div class="card col-6"><div class="skeleton" style="height:180px"></div></div>
      <div class="card col-6"><div class="skeleton" style="height:180px"></div></div>
    </div>`;
}

async function refreshBrand() {
  try {
    const data = await getJson(`${API}/overview`);
    brand = { version: String(data.version || "").split("+")[0], enabled: !!data.mod?.enabled };
    const meta = root?.querySelector(".brand-meta");
    if (meta) {
      meta.innerHTML = `<span class="dot ${brand.enabled ? "is-on" : "is-off"}" role="img"
        aria-label="${escapeHtml(brand.enabled ? t("enabled") : t("disabled"))}"></span>
        <span>${escapeHtml(brand.version ? `v${brand.version}` : t("brandSub"))}</span>`;
    }
  } catch {
    /* brand info is cosmetic */
  }
}

async function loadConfigNames() {
  try {
    const data = await getJson(`${API}/config`);
    const known = new Set(SECTIONS.filter((s) => s.name).map((s) => s.name));
    const extras = (data.configs || []).filter((n) => typeof n === "string" && !known.has(n));
    if (extras.length) {
      extraConfigs = extras;
      // Only the nav changes, so refresh it in place and leave the body alone.
      const nav = root?.querySelector(".side nav");
      if (nav) {
        nav.innerHTML = navHtml(currentSection());
        nav.querySelectorAll("[data-nav]").forEach((btn) => {
          btn.addEventListener("click", () => navigate(btn.getAttribute("data-nav")));
        });
      }
    }
  } catch {
    /* fall back to the built-in section list */
  }
}

async function loadSection(opts = {}) {
  if (!root) return;
  const section = currentSection();
  const body = root.querySelector("[data-body]");
  if (!body) return;

  const token = ++loadToken;
  clearCharts();
  setBusy(true);
  if (!opts.silent) {
    body.innerHTML = section.kind === "overview"
      ? skeletonBody()
      : `<p class="note">${escapeHtml(t("loading"))}</p>`;
  }

  try {
    if (section.kind === "overview") {
      const data = await getJson(`${API}/overview`);
      if (token !== loadToken) return;
      brand = { version: String(data.version || "").split("+")[0], enabled: !!data.mod?.enabled };
      const meta = root.querySelector(".brand-meta");
      if (meta) {
        meta.innerHTML = `<span class="dot ${brand.enabled ? "is-on" : "is-off"}" role="img"
          aria-label="${escapeHtml(brand.enabled ? t("enabled") : t("disabled"))}"></span>
          <span>${escapeHtml(brand.version ? `v${brand.version}` : t("brandSub"))}</span>`;
      }
      body.innerHTML = renderOverview(data);
      mountOverviewCharts(body, data);
      return;
    }

    if (section.kind === "settings") {
      body.innerHTML = renderSettings();
      body.querySelectorAll('input[name="uf-lang"]').forEach((input) => {
        input.addEventListener("change", () => setLang(input.value));
      });
      body.querySelectorAll('input[name="uf-theme"]').forEach((input) => {
        input.addEventListener("change", () => setTheme(input.value));
      });
      body.querySelector('[data-act="reload-disk"]')?.addEventListener("click", () => void reloadFromDisk());
      return;
    }

    if (section.kind === "presets") {
      await loadPresets();
      if (token !== loadToken) return;
      body.innerHTML = renderPresets();
      bindPresets(body);
      return;
    }

    if (section.kind === "state") {
      const data = await getJson(`${API}/state`);
      if (token !== loadToken) return;
      stateRaw = data;
      stateRows = buildStateRows(data);
      statePage = 0;
      body.innerHTML = renderStateSection();
      bindStateSection(body);
      void loadCatalog().then(() => {
        // Repaint once with names, unless the user has already moved on.
        if (token === loadToken && catalog.status === "ok") body.innerHTML = renderStateSection();
      });
      return;
    }

    // config + selling share the form editor
    const name = section.kind === "selling" ? "mod" : section.name;
    const data = await getJson(`${API}/config/${encodeURIComponent(name)}`);
    if (token !== loadToken) return;

    const isSelling = section.kind === "selling";
    cfg = {
      name,
      original: deepClone(data),
      draft: deepClone(data),
      jsonText: JSON.stringify(data, null, 2),
      baseline: JSON.stringify(data, null, 2),
      tab: "form",
      dirty: false,
      restart: !!section.restart,
      only: isSelling ? ["quickListFromStash"] : null,
      hasSecret: hasSecretField(data),
    };

    const tabs = isSelling ? "" : `<div class="tabs" role="tablist" aria-label="${escapeHtml(section.label)}">
      <button type="button" class="tab" role="tab" data-cfg-tab="form" aria-selected="true">${escapeHtml(t("form.tabForm"))}</button>
      <button type="button" class="tab" role="tab" data-cfg-tab="json" aria-selected="false">${escapeHtml(t("form.tabJson"))}</button>
    </div>`;

    body.innerHTML = `<div class="stack tight">
      <div class="row">
        ${tabs}
        ${section.restart ? `<span class="pill warn">${escapeHtml(t("restartBadge"))}</span>` : ""}
        <span class="grow"></span>
      </div>
      ${isSelling ? `<p class="note tight">${escapeHtml(t("selling.hint"))}</p>` : ""}
      <div data-cfg-body>${renderForm()}</div>
      <p class="note">${escapeHtml(t("form.saveNote"))}${section.restart ? ` ${escapeHtml(t("form.restartNote"))}` : ""}</p>
      <div class="savebar-slot" data-savebar></div>
    </div>`;

    bindCfgSection(body, section);
    if (!isSelling && ID_SECTIONS.has(name)) void ensureIdNames();
  } catch (e) {
    if (token !== loadToken) return;
    const msg = failText(e, t("toast.loadFailed"));
    body.innerHTML = `<div class="err-box">${escapeHtml(msg)}</div>`;
    toast(msg, "err");
  } finally {
    if (token === loadToken) setBusy(false);
  }
}

/* ---- unsaved-changes guard ---- */

function onBeforeUnload(e) {
  if (!isDirty()) return;
  e.preventDefault();
  e.returnValue = "";
}

/* ---- entry points ---- */

export function mount(rootId) {
  root = document.getElementById(rootId);
  if (!root) return;
  document.documentElement.lang = lang;
  applyTheme();
  ensureTooltip();
  bindDropdowns();
  window.addEventListener("beforeunload", onBeforeUnload);
  render();
  void loadSection();
  void loadConfigNames();
}

export function unmount() {
  window.removeEventListener("beforeunload", onBeforeUnload);
  window.clearInterval(autoTimer);
  autoTimer = 0;
  autoRefresh = false;
  clearCharts();
  chartObserver?.disconnect();
  chartObserver = null;
  cfg = null;
  stateRaw = null;
  stateRows = [];
  if (root) root.innerHTML = "";
  root = null;
}

if (document.getElementById("uf-root")) {
  mount("uf-root");
}
