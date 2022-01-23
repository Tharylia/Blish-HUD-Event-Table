namespace Estreya.BlishHUD.EventTable.Helpers
{
    using Microsoft.Xna.Framework;
    using MonoGame.Extended;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class MathHelper
    {
        public static float CalculeAngle(Point start, Point arrival)
        {
            var radian =  (float)Math.Atan2(arrival.Y - start.Y, arrival.X - start.X);

            return  radian;
        }

        public static float CalculeAngle(Point2 start, Point2 arrival)
        {
            var radian = (float)Math.Atan2(arrival.Y - start.Y, arrival.X - start.X);

            return radian;
        }
        public static double CalculeDistance(Point start, Point arrival)
        {
            var deltaX = Math.Pow(arrival.X - start.X, 2);
            var deltaY = Math.Pow(arrival.Y - start.Y, 2);

            var distance = Math.Sqrt(deltaY + deltaX);

            return distance;
        }

        public static float CalculeDistance(Point2 start, Point2 arrival)
        {
            var deltaX = Math.Pow(arrival.X - start.X, 2);
            var deltaY = Math.Pow(arrival.Y - start.Y, 2);

            var distance = (float)Math.Sqrt(deltaY + deltaX);

            return distance;
        }
    }
}
