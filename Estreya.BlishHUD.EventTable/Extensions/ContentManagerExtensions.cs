namespace Estreya.BlishHUD.EventTable.Extensions
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using static Blish_HUD.ContentService;

    public static class ContentManagerExtensions
    {
        private static Dictionary<string, AsyncTexture2D> IconCache { get; set; } = new Dictionary<string, AsyncTexture2D>();

        public static AsyncTexture2D GetRenderIcon(this ContentsManager manager, string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return null;
            }

            lock (IconCache)
            {
                if (IconCache.ContainsKey(identifier)) return IconCache[identifier];

                AsyncTexture2D icon = null;// new AsyncTexture2D(Textures.TransparentPixel.Duplicate());
                if (!string.IsNullOrWhiteSpace(identifier))
                {
                    if (identifier.Contains("/"))
                    {
                        icon = GameService.Content.GetRenderServiceTexture(identifier);
                    }
                    else
                    {
                        Texture2D texture = EventTableModule.ModuleInstance.ContentsManager.GetTexture(identifier);
                        if (texture == ContentService.Textures.Error)
                        {
                            texture = GameService.Content.GetTexture(identifier);
                        }

                        icon = new AsyncTexture2D(texture);
                        //icon.SwapTexture(texture);
                    }
                }

                IconCache.Add(identifier, icon);

                return icon;
            }
        }
    }
}
