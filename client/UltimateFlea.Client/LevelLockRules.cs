using System.Collections.Generic;
using Newtonsoft.Json;

namespace UltimateFlea.Client
{
    // Mirrors server LevelLocks/LevelLocksRules.cs.
    public class LevelLockRules
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("lockBuying")]
        public bool LockBuying { get; set; }

        [JsonProperty("lockSelling")]
        public bool LockSelling { get; set; }

        [JsonProperty("levels")]
        public Dictionary<string, int> Levels { get; set; } = new Dictionary<string, int>();

        [JsonProperty("categories")]
        public Dictionary<string, int> Categories { get; set; } = new Dictionary<string, int>();
    }
}
