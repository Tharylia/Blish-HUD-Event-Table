namespace Estreya.BlishHUD.EventTable.UI.Views.Edit;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Microsoft.Xna.Framework;
using System;
using System.Threading.Tasks;

public class EditEventView : BaseView
{
    public event EventHandler<ValueEventArgs<Event>> SavePressed;
    public event EventHandler CancelPressed;

    private Event Event { get; set; }

    public EditEventView(Event ev)
    {
        this.Event = ev;
    }

    protected override void InternalBuild(Panel parent)
    {
        Rectangle bounds = parent.ContentRegion;

        FlowPanel parentPanel = new FlowPanel()
        {
            Size = bounds.Size,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            ControlPadding = new Vector2(5, 2),
            OuterControlPadding = new Vector2(20, 15),
            WidthSizingMode = SizingMode.Fill,
            Height = bounds.Height - 50,
            AutoSizePadding = new Point(0, 15),
            CanScroll = true,
            Parent = parent
        };

        // TODO: Remove when ready
        this.RenderLabel(parentPanel, "THIS IS A WIP WINDOW.\nEXPECT BUGS!", null, Color.Red);
        this.RenderEmptyLine(parentPanel);
        this.RenderEmptyLine(parentPanel);

        StandardButton saveButton;
        StandardButton cancelButton;

        this.RenderProperty(parentPanel, this.Event, ev => ev.Key, ev => false);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Name, ev => true);
        this.RenderPropertyWithChangedTypeValidation(parentPanel, this.Event, ev => ev.Offset, ev => true, (string val) =>
        {
            try
            {
                _ = TimeSpan.Parse(val);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        });
        this.RenderPropertyWithChangedTypeValidation(parentPanel, this.Event, ev => ev.Repeat, ev => true, (string val) =>
        {
            try
            {
                _ = TimeSpan.Parse(val);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        });
        this.RenderProperty(parentPanel, this.Event, ev => ev.Location, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Waypoint, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Wiki, ev => true, null, null, MathHelper.Clamp((int)GameService.Content.DefaultFont14.MeasureString(this.Event.Wiki).Width + 20, 0, parentPanel.Width));
        this.RenderPropertyWithChangedTypeValidation(parentPanel, this.Event, ev => ev.Duration, ev => true, (string val) =>
        {
            try
            {
                _ = int.Parse(val);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        });
        this.RenderProperty(parentPanel, this.Event, ev => ev.Icon, ev => true);
        this.RenderPropertyWithValidation(parentPanel, this.Event, ev => ev.BackgroundColorCode, ev => true, val =>
        {
            if (string.IsNullOrWhiteSpace(val)) return (true, null);

            try
            {
                _ = System.Drawing.ColorTranslator.FromHtml(val);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }

        });

        this.RenderProperty(parentPanel, this.Event, ev => ev.APICodeType, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.APICode, ev => true);

        FlowPanel buttonPanel = new FlowPanel
        {
            Parent = parent,
            FlowDirection = ControlFlowDirection.SingleRightToLeft,
            Location = new Point(0, parentPanel.Bottom),
            WidthSizingMode = SizingMode.Fill,
            Height = bounds.Bottom - parentPanel.Bottom
        };

        saveButton = this.RenderButton(buttonPanel, "Save", () => this.SavePressed?.Invoke(this, new ValueEventArgs<Event>(this.Event)));
        cancelButton = this.RenderButton(buttonPanel, "Cancel", () => this.CancelPressed?.Invoke(this, EventArgs.Empty));
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}
