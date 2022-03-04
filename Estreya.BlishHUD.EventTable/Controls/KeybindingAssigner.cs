namespace Estreya.BlishHUD.EventTable.Controls
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class KeybindingAssigner  : LabelBase
    {
        private const int UNIVERSAL_PADDING = 2;

        private int _nameWidth = 183;

        private KeyBinding _keyBinding;

        private Rectangle _nameRegion;

        private Rectangle _hotkeyRegion;

        private bool _overHotkey;

        public int NameWidth
        {
            get
            {
                return _nameWidth;
            }
            set
            {
                SetProperty(ref _nameWidth, value, invalidateLayout: true, "NameWidth");
            }
        }

        public string KeyBindingName
        {
            get
            {
                return _text;
            }
            set
            {
                SetProperty(ref _text, value, invalidateLayout: false, "KeyBindingName");
            }
        }

        public KeyBinding KeyBinding
        {
            get
            {
                return _keyBinding;
            }
            set
            {
                if (SetProperty(ref _keyBinding, value, invalidateLayout: false, "KeyBinding"))
                {
                    base.Enabled = (_keyBinding != null);
                }
            }
        }
        public bool WithName { get; }

        public event EventHandler<EventArgs> BindingChanged;

        protected void OnBindingChanged(EventArgs e)
        {
            this.BindingChanged?.Invoke(this, e);
        }

        public KeybindingAssigner(KeyBinding keyBinding, bool withName)
        {
            KeyBinding = (keyBinding ?? new KeyBinding());
            this.WithName = withName;
            _font = Control.Content.DefaultFont14;
            _showShadow = true;
            _cacheLabel = false;
            base.Size = new Point(340, 16);
        }

        public KeybindingAssigner(bool withName)
            : this(null, withName)
        {
        }

        protected override void OnClick(MouseEventArgs e)
        {
            if (_overHotkey && e.IsDoubleClick)
            {
                SetupNewAssignmentWindow();
            }

            base.OnClick(e);
        }

        protected override void OnMouseMoved(MouseEventArgs e)
        {
            _overHotkey = (base.RelativeMousePosition.X >= _hotkeyRegion.Left);
            base.OnMouseMoved(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e)
        {
            _overHotkey = false;
            base.OnMouseLeft(e);
        }

        public override void RecalculateLayout()
        {
            _nameRegion = new Rectangle(0, 0, _nameWidth, _size.Y);
            _hotkeyRegion = new Rectangle(WithName ? _nameWidth + 2 : 0, 0, _size.X - (WithName ? _nameWidth - 2 : 0), _size.Y);
        }

        private void SetupNewAssignmentWindow()
        {
            KeybindingAssignmentWindow newHkAssign = new KeybindingAssignmentWindow(_text, _keyBinding.ModifierKeys, _keyBinding.PrimaryKey)
            {
                Parent = Control.Graphics.SpriteScreen
            };
            newHkAssign.AssignmentAccepted += delegate
            {
                _keyBinding.ModifierKeys = newHkAssign.ModifierKeys;
                _keyBinding.PrimaryKey = newHkAssign.PrimaryKey;
                OnBindingChanged(EventArgs.Empty);
            };
            newHkAssign.Show();
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (WithName)
            {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, _nameRegion, Color.White * 0.15f);
                DrawText(spriteBatch, _nameRegion);
            }

            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, _hotkeyRegion, Color.White * ((_enabled && _overHotkey) ? 0.2f : 0.15f));
            if (_enabled)
            {
                spriteBatch.DrawStringOnCtrl(this, _keyBinding.GetBindingDisplayText(), Control.Content.DefaultFont14, _hotkeyRegion.OffsetBy(1, 1), Color.Black, wrap: false, HorizontalAlignment.Center);
                spriteBatch.DrawStringOnCtrl(this, _keyBinding.GetBindingDisplayText(), Control.Content.DefaultFont14, _hotkeyRegion, Color.White, wrap: false, HorizontalAlignment.Center);
            }
        }
    }
}
