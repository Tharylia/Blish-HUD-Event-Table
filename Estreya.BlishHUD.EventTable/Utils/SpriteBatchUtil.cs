namespace Estreya.BlishHUD.EventTable.Utils
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Controls;

    public static class SpriteBatchUtil
    {
        public static void DrawOnCtrl(this SpriteBatch spriteBatch, Blish_HUD.Controls.Control control, Texture2D texture, RectangleF destinationRectangle, Color tint)
        {
            DrawOnCtrl(spriteBatch, control, texture, destinationRectangle, tint, 0f);
        }

        public static void DrawOnCtrl(this SpriteBatch spriteBatch, Blish_HUD.Controls.Control control, Texture2D texture, RectangleF destinationRectangle, Color tint, float angle)
        {
            var rectangle = destinationRectangle.ToBounds(control.AbsoluteBounds);
            // Hacky trick to let us use RectangleF with spritebatch.
            var scale = new Vector2(rectangle.Width / texture.Width, rectangle.Height / texture.Height);

            spriteBatch.Draw(texture, rectangle.Center - rectangle.Size / 2f, null, tint, angle, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawStringOnCtrl(this SpriteBatch spriteBatch, Blish_HUD.Controls.Control ctrl, string text, BitmapFont font, RectangleF destinationRectangle, Color color, bool wrap = false, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
        {
            spriteBatch.DrawStringOnCtrl(ctrl, text, font, destinationRectangle, color, wrap, stroke: false, 1, horizontalAlignment, verticalAlignment);
        }

        public static void DrawStringOnCtrl(this SpriteBatch spriteBatch, Blish_HUD.Controls.Control ctrl, string text, BitmapFont font, RectangleF destinationRectangle, Color color, bool wrap, bool stroke, int strokeDistance = 1, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left, VerticalAlignment verticalAlignment = VerticalAlignment.Middle)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            text = wrap ? DrawUtil.WrapText(font, text, destinationRectangle.Width) : text;
            if (horizontalAlignment != 0 && (wrap || text.Contains("\n")))
            {
                using (StringReader stringReader = new StringReader(text))
                {
                    for (int i = 0; destinationRectangle.Height - i > 0; i += font.LineHeight)
                    {
                        string text2;
                        if ((text2 = stringReader.ReadLine()) == null)
                        {
                            break;
                        }

                        spriteBatch.DrawStringOnCtrl(ctrl, text2, font, destinationRectangle.Add(0, i, 0, 0), color, wrap, stroke, strokeDistance, horizontalAlignment, verticalAlignment);
                    }
                }

                return;
            }

            Vector2 vector = font.MeasureString(text);
            destinationRectangle = destinationRectangle.ToBounds(ctrl.AbsoluteBounds);
            float num = destinationRectangle.X;
            float num2 = destinationRectangle.Y;
            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    num += destinationRectangle.Width / 2 - vector.X / 2;
                    break;
                case HorizontalAlignment.Right:
                    num += destinationRectangle.Width - vector.X;
                    break;
            }

            switch (verticalAlignment)
            {
                case VerticalAlignment.Middle:
                    num2 += destinationRectangle.Height / 2 - vector.Y / 2;
                    break;
                case VerticalAlignment.Bottom:
                    num2 += destinationRectangle.Height - vector.Y;
                    break;
            }

            Vector2 vector2 = new Vector2(num, num2);
            float scale = ctrl.AbsoluteOpacity();
            if (stroke)
            {
                spriteBatch.DrawString(font, text, vector2.OffsetBy(0f, -strokeDistance), Color.Black * scale);
                spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, -strokeDistance), Color.Black * scale);
                spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, 0f), Color.Black * scale);
                spriteBatch.DrawString(font, text, vector2.OffsetBy(strokeDistance, strokeDistance), Color.Black * scale);
                spriteBatch.DrawString(font, text, vector2.OffsetBy(0f, strokeDistance), Color.Black * scale);
                spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, strokeDistance), Color.Black * scale);
                spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, 0f), Color.Black * scale);
                spriteBatch.DrawString(font, text, vector2.OffsetBy(-strokeDistance, -strokeDistance), Color.Black * scale);
            }

            spriteBatch.DrawString(font, text, vector2, color * scale);
        }

    }
}
