namespace Estreya.BlishHUD.EventTable.State;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class IconState : ManagedState
{
    private static readonly Logger Logger = Logger.GetLogger<IconState>();
    private const string FOLDER_NAME = "images";

    private readonly ContentsManager _contentsManager;

    private readonly AsyncLock _textureLock = new AsyncLock();
    private readonly Dictionary<string, Texture2D> _loadedTextures = new Dictionary<string, Texture2D>();

    private string _basePath;
    private string _path;

    private string Path
    {
        get
        {
            if (this._path == null)
            {
                this._path = System.IO.Path.Combine(this._basePath, FOLDER_NAME);
            }

            return this._path;
        }
    }

    public IconState(ContentsManager contentsManager, string basePath) : base(60000)
    {
        this._contentsManager = contentsManager;
        this._basePath = basePath;
    }

    public override async Task Reload()
    {
        await this.LoadImages();
    }

    protected override Task Initialize()
    {
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        using (this._textureLock.Lock())
        {
            foreach (KeyValuePair<string, Texture2D> texture in this._loadedTextures)
            {
                texture.Value.Dispose();
            }

            this._loadedTextures.Clear();
        }
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
    }

    protected override async Task Load()
    {
        await this.LoadImages();
    }

    protected override async Task Save()
    {
        if (!Directory.Exists(this.Path))
        {
            Directory.CreateDirectory(this.Path);
        }

        using (await this._textureLock.LockAsync())
        {
            string[] currentLoadedTexturesArr = new string[this._loadedTextures.Keys.Count];

            this._loadedTextures.Keys.CopyTo(currentLoadedTexturesArr, 0);

            List<string> currentLoadedTextures = new List<string>(currentLoadedTexturesArr);

            string[] filePaths = this.GetFiles();

            foreach (string filePath in filePaths)
            {
                string sanitizedFileName = SanitizeFileName(System.IO.Path.GetFileNameWithoutExtension(filePath));
                if (currentLoadedTextures.Contains(sanitizedFileName))
                {
                    _ = currentLoadedTextures.Remove(sanitizedFileName);
                }
            }

            foreach (string newTextureIdentifier in currentLoadedTextures)
            {
                try
                {
                    string fileName = System.IO.Path.ChangeExtension(System.IO.Path.Combine(this.Path, newTextureIdentifier), "png");
                    using FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                    Texture2D newTexture = this._loadedTextures[newTextureIdentifier];
                    if (newTexture == ContentService.Textures.Error)
                    {
                        Logger.Warn("Texture \"{0}\" is erroneous. Skipping saving.", newTextureIdentifier);
                        continue;
                    }

                    newTexture.SaveAsPng(fileStream, newTexture.Width, newTexture.Height);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed saving texture \"{1}\": {0}", newTextureIdentifier);
                }
            }
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(fileName, invalidRegStr, "_");
    }

    private async Task LoadImages()
    {
        using (await this._textureLock.LockAsync())
        {
            this._loadedTextures.Clear();

            if (!Directory.Exists(this.Path))
            {
                return;
            }

            string[] filePaths = this.GetFiles();

            foreach (string filePath in filePaths)
            {
                try
                {
                    using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    Texture2D texture = TextureUtil.FromStreamPremultiplied(fileStream);
                    if (texture == null)
                    {
                        continue;
                    }

                    string fileName = SanitizeFileName(System.IO.Path.GetFileNameWithoutExtension(filePath));
                    this._loadedTextures.Add(fileName, texture);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed preloading texture \"{1}\": {0}", filePath);
                }
            }
        }
    }

    private string[] GetFiles()
    {
        string[] filePaths = Directory.GetFiles(this.Path, "*.png");
        return filePaths;
    }

    public bool HasIcon(string identifier)
    {
        string sanitizedIdentifier = SanitizeFileName(identifier);

        return this._loadedTextures.ContainsKey(sanitizedIdentifier);
    }

    public Texture2D GetIcon(string identifier, bool checkRenderAPI = true)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        string sanitizedIdentifier = SanitizeFileName(System.IO.Path.ChangeExtension(identifier, null));

        using (this._textureLock.Lock())
        {
            if (this._loadedTextures.ContainsKey(sanitizedIdentifier))
            {
                return this._loadedTextures[sanitizedIdentifier];
            }

            Texture2D icon = null;
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                if (checkRenderAPI && identifier.Contains("/"))
                {
                    try
                    {
                        AsyncTexture2D asyncTexture = GameService.Content.GetRenderServiceTexture(identifier);
                        asyncTexture.TextureSwapped += (s, e) =>
                        {
                            using (this._textureLock.Lock())
                            {
                                this._loadedTextures[sanitizedIdentifier] = e.NewValue;
                            }

                            Logger.Debug("Async texture \"{0}\" was swapped in cache.", identifier);
                        };

                        icon = asyncTexture;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Could not load icon from render api: {ex.Message}");
                    }
                }
                else
                {
                    // Load from module ref folder.
                    Texture2D texture = this._contentsManager.GetTexture(identifier);
                    if (texture == ContentService.Textures.Error)
                    {
                        // Load from base ref folder.
                        texture = GameService.Content.GetTexture(identifier);
                    }

                    icon = texture;
                }
            }

            this._loadedTextures.Add(sanitizedIdentifier, icon);

            return icon;
        }
    }

    public Task<Texture2D> GetIconAsync(string identifier, bool checkRenderAPI = true)
    {
        return Task.Run(() =>
        {
            return this.GetIcon(identifier, checkRenderAPI);
        });
    }
}
