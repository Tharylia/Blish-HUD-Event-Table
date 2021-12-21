namespace Estreya.BlishHUD.EventTable.Helpers
{
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class MathHelper
    {
        public static double CalculeAngle(Point start, Point arrival)
        {
            var radian =  (float)Math.Atan2(arrival.Y - start.Y, arrival.X - start.X);

            return  radian;
        }
        public static double CalculeDistance(Point start, Point arrival)
        {
            var deltaX = Math.Pow(arrival.X - start.X, 2);
            var deltaY = Math.Pow(arrival.Y - start.Y, 2);

            var distance = Math.Sqrt(deltaY + deltaX);

            return distance;
        }
    }
}
