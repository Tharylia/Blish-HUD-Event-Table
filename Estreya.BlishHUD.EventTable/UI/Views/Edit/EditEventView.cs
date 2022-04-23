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
    public event EventHandler<EventArgs> CancelPressed;

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
            HeightSizingMode = SizingMode.Fill,
            AutoSizePadding = new Point(0, 15),
            Parent = parent
        };

        this.RenderProperty(parentPanel, this.Event, ev => ev.Key, ev => false);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Name, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Offset, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Repeat, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Location, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Waypoint, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Wiki, ev => true, null, (int)GameService.Content.DefaultFont14.MeasureString(this.Event.Wiki).Width + 20);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Duration, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.Icon, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.BackgroundColorCode, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.APICodeType, ev => true);
        this.RenderProperty(parentPanel, this.Event, ev => ev.APICode, ev => true);

    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}
