// ProgressOverlaySystem.cs
using Vintagestory.API.Common;

namespace ProgressOverlay
{
    public class ProgressOverlaySystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.Logger.Notification("[ScytheFix] Мод загружен!");
        }
    }
}