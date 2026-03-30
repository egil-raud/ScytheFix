// ProgressOverlaySystem.cs
using Cairo;
using HarmonyLib;
using ScytheFix.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace ScytheFix
{
    public class ScytheFixModSystem : ModSystem
    {
        public ConfigManager<ScytheFixModConfig> Config;

        public const string patchName = "com.Egil_Raud.scythefix";

        public static ScytheFixModSystem Instance;

        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        Harmony harmony;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            this.api = api;

            harmony = new(patchName);
            harmony.PatchAll();
        }
        public override void Start(ICoreAPI api)
        {
            Instance = this;

            base.Start(api);
            api.Logger.Notification("[ScytheFix] Мод загружен!");

            Config = new ConfigManager<ScytheFixModConfig>(api, "ScytheFixModConfig", true);
        }
        public override void Dispose()
        {
            base.Dispose();
            harmony?.UnpatchAll(patchName);
        }

        // Вспомогательный метод для получения конфига
        public ScytheFixModConfig GetConfig()
        {
            return Config?.modConfig;
        }
    }
}