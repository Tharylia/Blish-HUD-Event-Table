namespace Estreya.BlishHUD.EventTable.Input
{
    using Blish_HUD.Input;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MouseEventArgs
    {
        public Point Position { get; private set; }
        
        public bool DoubleClick { get; private set; }

        public MouseEventType EventType { get; private set; }

        public MouseEventArgs(Point position, bool doubleClick, MouseEventType type)
        {
            this.Position = position;
            this.DoubleClick = doubleClick;
            this.EventType = type;
        }
    }
}
