namespace Estreya.BlishHUD.EventTable.Extensions
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using static Blish_HUD.ContentService;

    public static class ContentManagerExtensions
    {

        public static AsyncTexture2D GetRenderIcon(this ContentsManager manager, string identifier)
        {
            AsyncTexture2D icon = new AsyncTexture2D(Textures.TransparentPixel.Duplicate());
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                if (identifier.Contains("/"))
                {
                    icon = GameService.Content.GetRenderServiceTexture(identifier);
                }
                else
                {
                    icon.SwapTexture(EventTableModule.ModuleInstance.ContentsManager.GetTexture(identifier));
                }
            }

            return icon;
        }
    }
}
