using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace UltimateFlea.LevelLocks;

[Injectable(TypePriority = OnLoadOrder.Routers)]
public class LevelLocksRouter(JsonUtil jsonUtil, LevelLockService levelLocks)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/ultimateflea/levellocks",
                (url, info, sessionID, output, cancellationToken) =>
                    new ValueTask<string>(jsonUtil.Serialize(levelLocks.GetRules()) ?? "{}")
            ),
        ]
    ) { }
