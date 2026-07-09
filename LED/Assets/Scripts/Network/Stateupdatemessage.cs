// Assets/Scripts/Network/StateUpdateMessage.cs
//
// Structures sérialisables (JsonUtility) correspondant au format défini dans
// le contrat d'interface (section 4.2) : STATE_UPDATE + seq + liste d'entités.

using System;

[Serializable]
public class StateEntityData
{
    public int id;
    public int r;
    public int g;
    public int b;
}

[Serializable]
public class StateUpdateMessage
{
    public string type = "STATE_UPDATE";
    public int seq;
    public StateEntityData[] entities;
}