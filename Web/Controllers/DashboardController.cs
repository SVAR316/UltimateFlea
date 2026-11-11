using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UltimateFlea.Economy;
using UltimateFlea.LevelLocks;
using UltimateFlea.Pool;
using UltimateFlea.Presets;

namespace UltimateFlea.Web.Controllers;

[ApiController]
[Route("ultimateflea/api")]
public class DashboardController(
    ConfigManager configManager,
    DashboardService dashboard,
    EconomyEngine economyEngine,
    PoolManager poolManager,
    LevelLockService levelLocks,
    PresetService presets,
    ItemCatalogService itemCatalog,
    ResetService resetService) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    [HttpGet("overview")]
    public IActionResult Overview() => new JsonResult(dashboard.GetOverview(), JsonOptions);

    [HttpGet("config")]
    public IActionResult ListConfigs() =>
        Ok(new { configs = configManager.ConfigNames });

    [HttpGet("config/{name}")]
    public IActionResult GetConfig(string name)
    {
        try
        {
            return new JsonResult(configManager.GetConfigObject(name), JsonOptions);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("config/{name}")]
    public IActionResult SaveConfig(string name, [FromBody] JsonElement body)
    {
        if (!configManager.TrySaveConfig(name, body, out var error, out var restartRecommended))
        {
            return BadRequest(new { error });
        }

        if (name.Equals("pool", StringComparison.OrdinalIgnoreCase) && configManager.Mod.EnablePoolManager)
        {
            try { poolManager.Apply(); }
            catch (Exception ex) { return Ok(new { ok = true, restartRecommended = true, warning = ex.Message }); }
        }

        if (name.Equals("levels", StringComparison.OrdinalIgnoreCase))
        {
            try { levelLocks.Apply(); }
            catch (Exception ex) { return Ok(new { ok = true, restartRecommended = true, warning = ex.Message }); }
        }

        return Ok(new { ok = true, restartRecommended });
    }

    [HttpGet("items")]
    public IActionResult Items()
    {
        var blob = itemCatalog.Get();

        Response.Headers.ETag = blob.ETag;
        Response.Headers.CacheControl = "private, max-age=0, must-revalidate";
        Response.Headers.Vary = "Accept-Encoding";

        if (Request.Headers.IfNoneMatch.Any(v => v is not null && v.Contains(blob.ETag, StringComparison.Ordinal)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        // Pre-compressed once at build time; sending it ourselves does not depend on the host's
        // response compression being configured.
        var acceptsGzip = Request.Headers.AcceptEncoding.Any(v => v is not null && v.Contains("gzip", StringComparison.OrdinalIgnoreCase));
        if (acceptsGzip)
        {
            Response.Headers.ContentEncoding = "gzip";
            return File(blob.Gzip, "application/json");
        }

        return File(blob.Json, "application/json");
    }

    [HttpGet("items/{tpl}/icon")]
    public IActionResult ItemIcon(string tpl)
    {
        if (!itemCatalog.TryGetIcon(tpl, out var path))
        {
            return NotFound();
        }

        FileInfo info;
        try
        {
            info = new FileInfo(path);
            if (!info.Exists)
            {
                return NotFound();
            }
        }
        catch
        {
            return NotFound();
        }

        var etag = $"\"{info.Length:x}-{info.LastWriteTimeUtc.Ticks:x}\"";
        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "private, max-age=604800";

        if (Request.Headers.IfNoneMatch.Any(v => v is not null && v.Contains(etag, StringComparison.Ordinal)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return PhysicalFile(path, "image/png");
    }

    [HttpGet("presets")]
    public IActionResult ListPresets() =>
        new JsonResult(new PresetListResponse(presets.List()), JsonOptions);

    [HttpGet("presets/{id}")]
    public IActionResult GetPreset(string id)
    {
        var def = presets.Find(id);
        if (def is null)
        {
            return NotFound(new { error = new LocalizedText($"Unknown preset '{id}'", $"Неизвестный пресет '{id}'") });
        }

        if (!presets.TryResolve(def, out var configs, out var error))
        {
            return BadRequest(new { error });
        }

        return new JsonResult(new PresetDetailResponse(def.Id, configs, def.Partial, def.ResetsData), JsonOptions);
    }

    [HttpPost("presets/{id}/apply")]
    public IActionResult ApplyPreset(string id)
    {
        var def = presets.Find(id);
        if (def is null)
        {
            return NotFound(new { error = new LocalizedText($"Unknown preset '{id}'", $"Неизвестный пресет '{id}'") });
        }

        var result = presets.Apply(def);
        var warning = result.Warning;
        var restart = result.RestartRecommended;

        // Same side effects as SaveConfig, once per touched config, even after a partial failure:
        // whatever was saved is live and must also reach the item DB.
        if (result.Applied.Contains("pool") && configManager.Mod.EnablePoolManager)
        {
            try { poolManager.Apply(); }
            catch (Exception ex) { restart = true; warning = LocalizedText.Join(warning, Technical(ex.Message)); }
        }

        if (result.Applied.Contains("levels"))
        {
            try { levelLocks.Apply(); }
            catch (Exception ex) { restart = true; warning = LocalizedText.Join(warning, Technical(ex.Message)); }
        }

        if (result.Error is not null)
        {
            return StatusCode(result.HttpStatus, new
            {
                error = result.Error,
                applied = result.Applied,
                restartRecommended = restart,
                warning,
            });
        }

        // Presets that change the price basis also clear the price cache and economy state, otherwise the
        // persisted CurrentPrice values keep pulling toward the old base. May refetch ~17 MB, so it runs
        // after the configs are saved and published; a failure is a warning, the configs stay applied.
        if (def.ResetsData)
        {
            var reset = resetService.Run(new ResetRequest(true, true));
            if (!reset.Ok)
            {
                warning = LocalizedText.Join(warning, new LocalizedText(
                    $"The preset was applied, but the data reset did not complete: {reset.Error?.En}. Use Reset in Settings.",
                    $"Пресет применён, но сброс данных не завершился: {reset.Error?.Ru}. Используйте сброс в настройках."));
            }
        }

        return Ok(new { ok = true, applied = result.Applied, restartRecommended = restart, warning });
    }

    // The detail after the colon is the server's own exception text and is not translated.
    private static LocalizedText Technical(string detail) =>
        new($"A side effect failed: {detail}", $"Побочное действие не выполнено: {detail}");

    // Live in-memory state: reading data/economy_state.json here was stale by up to
    // one tick and could catch the file mid-write.
    [HttpGet("state")]
    public IActionResult State() => new JsonResult(economyEngine.SnapshotState(), JsonOptions);

    [HttpPost("reload")]
    public IActionResult Reload()
    {
        configManager.ReloadFromDisk();

        if (configManager.Mod.EnablePoolManager)
        {
            poolManager.Apply();
        }

        levelLocks.Apply();

        return Ok(new
        {
            ok = true,
            note = "Configs reloaded. Pool and level locks re-applied. Pricing/economy may need a server restart to fully rebuild.",
        });
    }

    // Clears the live price cache and/or the economy state and re-initializes in place.
    [HttpPost("reset")]
    public IActionResult Reset([FromBody] ResetRequest? body)
    {
        if (body is null || (!body.PriceCache && !body.EconomyState))
        {
            return BadRequest(new
            {
                ok = false,
                error = new LocalizedText("Select at least one of priceCache / economyState.",
                    "Выберите хотя бы одно: priceCache / economyState."),
            });
        }

        var result = resetService.Run(body);
        return new JsonResult(result, JsonOptions) { StatusCode = result.Ok ? 200 : 500 };
    }
}
