using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Extensions;
using Estreya.BlishHUD.EventTable.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System.Diagnostics;

namespace Estreya.BlishHUD.EventTable.Controls
{
    public class ListEntry<T> : Control
    {
        private const int DRAG_DROP_WIDTH = 30;

        private bool _dragDrop = false;

        public bool DragDrop
        {
            get => this._dragDrop;
            set => this.SetProperty(ref this._dragDrop, value, true);
        }

        private bool _dragging = false;
        internal bool Dragging
        {
            get => this._dragging;
            set => this.SetProperty(ref this._dragging, value, true);
        }

        private string _text;

        public string Text
        {
            get => this._text;
            set => this.SetProperty(ref this._text, value, true);
        }

        private BitmapFont _font = GameService.Content.DefaultFont16;

        public BitmapFont Font
        {
            get => this._font;
            set => this.SetProperty(ref this._font, value, true);
        }

        private Color _textColor = Color.Black;
        public Color TextColor
        {
            get => this._textColor;
            set => this.SetProperty(ref this._textColor, value, true);
        }

        private RectangleF TextBounds => new RectangleF(0, 0, this.DragDrop ? this.Size.X - DRAG_DROP_WIDTH : this.Size.X, this.Size.Y);
        private RectangleF DragDropBounds => this.DragDrop ? new RectangleF(this.Size.X - DRAG_DROP_WIDTH, 0, DRAG_DROP_WIDTH, this.Size.Y) : RectangleF.Empty;

        public T Data { get; set; }

        public ListEntry(string title)
        {
            this.Text = title;
            //this.LeftMouseButtonPressed += this.ListEntry_LeftMouseButtonPressed;
            //this.LeftMouseButtonReleased += this.ListEntry_LeftMouseButtonReleased;
        }

        public ListEntry(string title, BitmapFont font) : this(title)
        {
            this.Font = font;
        }

        private void ListEntry_LeftMouseButtonPressed(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (this.DragDrop)
            {
                this.Dragging = true;
            }

            Debug.WriteLine($"Left MB Pressed: {this.Text}");
        }

        private void ListEntry_LeftMouseButtonReleased(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            this.Dragging = false;

            Debug.WriteLine($"Left MB Released: {this.Text}");
        }

        public void PaintDragging(Control ctrl, SpriteBatch spriteBatch)
        {
            if (this.Dragging)
            {
                if (!string.IsNullOrWhiteSpace(this.Text))
                {
            Debug.WriteLine($"Paint Drag: {this.Text}");
                    spriteBatch.DrawStringOnCtrl(ctrl, this.Text, this.Font, new RectangleF(GameService.Input.Mouse.State.X, GameService.Input.Mouse.State.Y, this.TextBounds.Width, this.TextBounds.Height), this.TextColor);
                }
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            //RectangleF textBounds = new RectangleF(bounds.X, bounds.Y, this.DragDrop ? bounds.Width - DRAG_DROP_WIDTH : bounds.Width, bounds.Height);

            if (!string.IsNullOrWhiteSpace(this.Text))
            {
                spriteBatch.DrawStringOnCtrl(this, this.Text, this.Font, this.TextBounds, this.TextColor, false, HorizontalAlignment.Center);
            }

            if (this.DragDrop)
            {
                spriteBatch.DrawOnCtrl(this, EventTableModule.ModuleInstance.ContentsManager.GetIcon(@"images\bars.png", false), this.DragDropBounds, Color.Transparent);
            }
        }
    }
}
