// Assets/Scripts/Network/DeviceState.cs
//
// Etat d'un appareil d'eclairage : le projecteur RGBW central (deviceId 0)
// ou l'une des quatre lyres (deviceId 1 a 4).
//
// Structure de donnees pure, sur le modele de ColorState : aucune logique,
// aucune reference reseau. Les champs correspondent exactement au bloc de
// 16 octets decrit dans Docs/protocole-state.md.

public class DeviceState
{
    public byte DeviceId;

    public byte Pan;
    public byte PanFine;
    public byte Tilt;
    public byte TiltFine;

    public byte Dimmer;
    public byte Shutter;
    public byte ColorWheel;

    public byte R;
    public byte G;
    public byte B;
    public byte W;

    public byte MoveSpeed;
    public byte Function;

    public DeviceState(byte deviceId)
    {
        DeviceId = deviceId;

        // Au repos : appareil pointe droit devant, eteint, obturateur ouvert.
        Pan = 128;
        Tilt = 128;
        Shutter = 255;
        Dimmer = 0;
    }
}
