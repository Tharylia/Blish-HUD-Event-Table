﻿namespace Estreya.BlishHUD.EventTable.Utils;

using Blish_HUD;
using Blish_HUD.Controls.Intern;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public static class MapNavigationUtil
{
    private static readonly Logger Logger = Logger.GetLogger(typeof(MapNavigationUtil));
    public static int MouseMoveAndClickDelay { get; set; } = 50;
    public static int KeyboardPressDelay { get; set; } = 20;

    private static double GetDistance(double x1, double y1, double x2, double y2)
    {
        return GetDistance(x2 - x1, y2 - y1);
    }

    private static double GetDistance(double offsetX, double offsetY)
    {
        return Math.Sqrt(Math.Pow(offsetX, 2) + Math.Pow(offsetY, 2));
    }

    private static async Task WaitForTick(int ticks = 1)
    {
        int tick = GameService.Gw2Mumble.Tick;
        while (GameService.Gw2Mumble.Tick - tick < ticks * 2)
        {
            await Task.Delay(10);
        }
    }

    public static async Task<bool> OpenFullscreenMap()
    {
        if (!IsInGame())
        {
            Logger.Debug("Not in game");
            return false;
        }

        if (GameService.Gw2Mumble.UI.IsMapOpen)
        {
            return true;
        }

        // Consider pressing the open map icon in the UI.
        Keyboard.Stroke(Blish_HUD.Controls.Extern.VirtualKeyShort.KEY_M);

        await Task.Delay(500);

        return GameService.Gw2Mumble.UI.IsMapOpen;
    }

    public static async Task<bool> CloseFullscreenMap()
    {
        if (!IsInGame())
        {
            Logger.Debug("Not in game");
            return false;
        }

        if (!GameService.Gw2Mumble.UI.IsMapOpen)
        {
            return true;
        }

        Keyboard.Press(Blish_HUD.Controls.Extern.VirtualKeyShort.ESCAPE);

        await Task.Delay(500);

        return !GameService.Gw2Mumble.UI.IsMapOpen;
    }

    private static bool IsInGame()
    {
        return GameService.GameIntegration.Gw2Instance.IsInGame;
    }

    private static async Task<bool> Zoom(double requiredZoomLevel, int steps)
    {
        if (!IsInGame())
        {
            Logger.Debug("Not in game");
            return false;
        }

        int maxTries = 10;
        int remainingTries = maxTries;
        double startZoom = GetMapScale();

        bool isZoomIn = steps > 0;

        while (isZoomIn ? startZoom > requiredZoomLevel : startZoom < requiredZoomLevel)
        {
            await WaitForTick(2);
            if (!GameService.Gw2Mumble.UI.IsMapOpen)
            {
                Logger.Debug("User closed map.");
                return false;
            }

            Mouse.RotateWheel(steps);
            Mouse.RotateWheel(steps);
            Mouse.RotateWheel(steps);
            Mouse.RotateWheel(steps);
            await WaitForTick();

            double zoomAterScroll = GetMapScale();

            Logger.Debug($"Scrolled from {startZoom} to {zoomAterScroll}");

            if (startZoom == zoomAterScroll)
            {
                remainingTries--;

                if (remainingTries <= 0)
                {
                    return false;
                }
            }
            else
            {
                remainingTries = maxTries;
            }

            startZoom = zoomAterScroll;
        }

        return true;
    }

    public static Task<bool> ZoomOut(double requiredZoomLevel)
    {
        return Zoom(requiredZoomLevel, -int.MaxValue);
    }

    public static Task<bool> ZoomIn(double requiredZoomLevel)
    {
        return Zoom(requiredZoomLevel, int.MaxValue);
    }

    private static double GetMapScale()
    {
        return GameService.Gw2Mumble.UI.MapScale * GameService.Graphics.UIScaleMultiplier;
    }

    private static async Task<bool> MoveMap(double x, double y, double targetDistance)
    {
        while (true)
        {
            if (!IsInGame())
            {
                Logger.Debug("Not in game");
                return false;
            }

            await WaitForTick(2);
            if (!GameService.Gw2Mumble.UI.IsMapOpen)
            {
                Logger.Debug("User closed map.");
                return false;
            }

            Gw2Sharp.Models.Coordinates2 mapPos = GameService.Gw2Mumble.UI.MapCenter;

            double offsetX = mapPos.X - x;
            double offsetY = mapPos.Y - y;


            Logger.Debug($"Distance remaining: {GetDistance(mapPos.X, mapPos.Y, x, y)}");
            Logger.Debug($"Map Position: {GameService.Gw2Mumble.UI.MapPosition.X}, {GameService.Gw2Mumble.UI.MapPosition.Y}");

            double distance = Math.Sqrt(Math.Pow(offsetX, 2) + Math.Pow(offsetY, 2));
            if (distance < targetDistance)
            {
                break;
            }
            //Logger.Debug($"Distance remaining: {offsetX}, {offsetY}");

            Mouse.SetPosition(GameService.Graphics.WindowWidth / 2, GameService.Graphics.WindowHeight / 2);

            System.Drawing.Point startPos = Mouse.GetPosition();
            Mouse.Press(MouseButton.RIGHT);
            Mouse.SetPosition(startPos.X + (int)MathHelper.Clamp((float)offsetX / (float)(GetMapScale() * 0.9d), -100000, 100000),
                              startPos.Y + (int)MathHelper.Clamp((float)offsetY / (float)(GetMapScale() * 0.9d), -100000, 100000));

            await WaitForTick();
            startPos = Mouse.GetPosition();
            Mouse.SetPosition(startPos.X + (int)MathHelper.Clamp((float)offsetX / (float)(GetMapScale() * 0.9d), -100000, 100000),
                              startPos.Y + (int)MathHelper.Clamp((float)offsetY / (float)(GetMapScale() * 0.9d), -100000, 100000));

            Mouse.Release(MouseButton.RIGHT);

            await Task.Delay(MouseMoveAndClickDelay);
        }

        return true;
    }

    public static async Task<bool> ChangeMapLayer(ChangeMapLayerDirection direction)
    {
        if (!IsInGame())
        {
            Logger.Debug("Not in game");
            return false;
        }

        Keyboard.Press(Blish_HUD.Controls.Extern.VirtualKeyShort.SHIFT);
        await Task.Delay(KeyboardPressDelay);
        Mouse.RotateWheel(int.MaxValue * (direction == ChangeMapLayerDirection.Up ? 1 : -1));
        await Task.Delay(KeyboardPressDelay);
        Keyboard.Release(Blish_HUD.Controls.Extern.VirtualKeyShort.SHIFT);

        return true;
    }

    public static Task<bool> NavigateToPosition(ContinentFloorRegionMapPoi poi)
    {
        return NavigateToPosition(poi, false);
    }

    public static Task<bool> NavigateToPosition(ContinentFloorRegionMapPoi poi, bool directTeleport)
    {
        return NavigateToPosition(poi.Coord.X, poi.Coord.Y, poi.Type == PoiType.Waypoint, directTeleport);
    }

    public static Task<bool> NavigateToPosition(double x, double y)
    {

        return NavigateToPosition(x, y, false, false);
    }

    public static async Task<bool> NavigateToPosition(double x, double y, bool isWaypoint, bool directTeleport)
    {
        try
        {
            if (!IsInGame())
            {
                Logger.Debug("Not in game");
                return false;
            }

            Controls.ScreenNotification.ShowNotification(new string[] { "DO NOT MOVE THE CURSOR!", "Close map to cancel." }, Blish_HUD.Controls.ScreenNotification.NotificationType.Warning, duration: 7);

            if (!await OpenFullscreenMap())
            {
                Logger.Debug("Could not open map.");
            }

            await WaitForTick();

            Gw2Sharp.Models.Coordinates2 mapPos = GameService.Gw2Mumble.UI.MapCenter;

            Mouse.SetPosition(GameService.Graphics.WindowWidth / 2, GameService.Graphics.WindowHeight / 2);

            if (GameService.Gw2Mumble.CurrentMap.Id == 1206) // Mistlock Santuary
            {
                if (!await ChangeMapLayer(ChangeMapLayerDirection.Down))
                {
                    Logger.Debug("Changing map layer failed.");
                    return false;
                }
            }

            if (!await ZoomOut(6))
            {
                Logger.Debug($"Zooming out did not work.");
                return false;
            }

            double totalDist = GetDistance(mapPos.X, mapPos.Y, x, y) / (GetMapScale() * 0.9d);

            Logger.Debug($"Distance: {totalDist}");

            if (!await MoveMap(x, y, 50))
            {
                Logger.Debug($"Moving the map did not work.");
                return false;
            }

            await WaitForTick();

            int finalMouseX = GameService.Graphics.WindowWidth / 2;
            int finalMouseY = GameService.Graphics.WindowHeight / 2;

            Logger.Debug($"Set mouse on waypoint: x = {finalMouseX}, y = {finalMouseY}");

            Mouse.SetPosition(finalMouseX, finalMouseY, true);

            if (!await ZoomIn(2))
            {
                Logger.Debug($"Zooming in did not work.");
                return false;
            }

            if (!await MoveMap(x, y, 5))
            {
                Logger.Debug($"Moving the map did not work.");
                return false;
            }

            if (isWaypoint)
            {
                Logger.Debug($"Set mouse on waypoint: x = {finalMouseX}, y = {finalMouseY}");

                Mouse.SetPosition(finalMouseX, finalMouseY, true);

                await Task.Delay(MouseMoveAndClickDelay);

                Mouse.Click(MouseButton.LEFT);

                await Task.Delay(MouseMoveAndClickDelay);

                finalMouseX -= 50;
                finalMouseY += 10;
                Logger.Debug($"Set mouse on waypoint yes button: x = {finalMouseX}, y = {finalMouseY}");
                Mouse.SetPosition(finalMouseX, finalMouseY, true);

                if (directTeleport)
                {
                    await Task.Delay(250); // Wait for teleport window to open.
                    Mouse.Click(MouseButton.LEFT);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Navigation to position failed:");
            return false;
        }
    }

    public enum ChangeMapLayerDirection
    {
        Up,
        Down
    }
}