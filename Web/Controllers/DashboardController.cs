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
    PresetService presets) : ControllerBase
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

    [HttpGet("presets")]
    public IActionResult ListPresets() =>
        new JsonResult(new PresetListResponse(presets.List()), JsonOptions);

    [HttpGet("presets/{id}")]
    public IActionResult GetPreset(string id)
    {
        var def = presets.Find(id);
        if (def is null)
        {
            return NotFound(new { error = $"Unknown preset '{id}'" });
        }

        if (!presets.TryResolve(def, out var configs, out var error))
        {
            return BadRequest(new { error });
        }

        return new JsonResult(new PresetDetailResponse(def.Id, configs), JsonOptions);
    }

    [HttpPost("presets/{id}/apply")]
    public IActionResult ApplyPreset(string id)
    {
        var def = presets.Find(id);
        if (def is null)
        {
            return NotFound(new { error = $"Unknown preset '{id}'" });
        }

        var result = presets.Apply(def);
        var warning = result.Warning;
        var restart = result.RestartRecommended;

        // Same side effects as SaveConfig, once per touched config, even after a partial failure:
        // whatever was saved is live and must also reach the item DB.
        if (result.Applied.Contains("pool") && configManager.Mod.EnablePoolManager)
        {
            try { poolManager.Apply(); }
            catch (Exception ex) { restart = true; warning = Join(warning, ex.Message); }
        }

        if (result.Applied.Contains("levels"))
        {
            try { levelLocks.Apply(); }
            catch (Exception ex) { restart = true; warning = Join(warning, ex.Message); }
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

        return Ok(new { ok = true, applied = result.Applied, restartRecommended = restart, warning });
    }

    private static string Join(string? first, string second) =>
        string.IsNullOrEmpty(first) ? second : $"{first} {second}";

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
}
