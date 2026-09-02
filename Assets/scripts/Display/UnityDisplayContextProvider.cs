using System;
using UnityEngine;

// The single place in the runtime that reads Unity's global display state.
// After M2 no presentation rule may call Screen.* or Application.platform
// directly; rules receive the DisplayContext this provider captures.
//
// Works under the Editor's Device Simulator as well, because the simulator
// overrides Screen.* and Application.platform at the same API surface.
public sealed class UnityDisplayContextProvider : IDisplayContextProvider
{
    public DisplayContext GetCurrent()
    {
        int width  = Screen.width;
        int height = Screen.height;

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException(
                $"[UnityDisplayContextProvider] Screen reports {width}x{height}. " +
                "Capture the display context after the screen is initialized.");

        var resolution = new Vector2Int(width, height);
        Rect safeArea  = ClampToResolution(Screen.safeArea, resolution);

        return new DisplayContext(resolution, safeArea, MapPlatform(Application.platform));
    }

    // Screen.safeArea can lag Screen.width/height by a frame during a resize or
    // orientation change. Clamp here, at the boundary, so DisplayContext itself
    // can stay strict and never carry a rect outside the resolution.
    public static Rect ClampToResolution(Rect rect, Vector2Int resolution)
    {
        if (!IsFinite(rect.x) || !IsFinite(rect.y) || !IsFinite(rect.width) || !IsFinite(rect.height))
            return new Rect(0, 0, resolution.x, resolution.y);

        float xMin = Mathf.Clamp(rect.xMin, 0f, resolution.x);
        float yMin = Mathf.Clamp(rect.yMin, 0f, resolution.y);
        float xMax = Mathf.Clamp(rect.xMax, xMin, resolution.x);
        float yMax = Mathf.Clamp(rect.yMax, yMin, resolution.y);

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

    // RuntimePlatform -> DisplayPlatform. Anything not listed is Unknown on
    // purpose; add a mapping only when a rule actually needs it.
    public static DisplayPlatform MapPlatform(RuntimePlatform platform)
    {
        switch (platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:
                return DisplayPlatform.Desktop;

            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
                return DisplayPlatform.Mobile;

            case RuntimePlatform.PS4:
            case RuntimePlatform.PS5:
            case RuntimePlatform.XboxOne:
            case RuntimePlatform.GameCoreXboxOne:
            case RuntimePlatform.GameCoreXboxSeries:
            case RuntimePlatform.Switch:
                return DisplayPlatform.Console;

            default:
                return DisplayPlatform.Unknown;
        }
    }
}
