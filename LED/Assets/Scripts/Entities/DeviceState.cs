// État logique d'un device (projecteur RGBW ou lyre).
// Aucun IP / univers / canal DMX — uniquement l'état authoring (P4).

[System.Serializable]
public struct DeviceState
{
    public byte deviceId;
    public byte pan;
    public byte panFine;
    public byte tilt;
    public byte tiltFine;
    public byte dimmer;
    public byte shutter;
    public byte colorWheel;
    public byte r;
    public byte g;
    public byte b;
    public byte w;
    public byte moveSpeed;
    public byte function;

    public const byte Center = 128;
    public const byte ShutterOpen = 40;
    public const byte DimmerFull = 255;

    public static DeviceState Blackout(byte deviceId)
    {
        return new DeviceState
        {
            deviceId = deviceId,
            pan = Center,
            panFine = 0,
            tilt = Center,
            tiltFine = 0,
            dimmer = 0,
            shutter = 0,
            colorWheel = 0,
            r = 0,
            g = 0,
            b = 0,
            w = 0,
            moveSpeed = 0,
            function = 0,
        };
    }
}
