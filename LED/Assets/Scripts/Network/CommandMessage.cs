// Structure du canal Commandes (TCP), conforme au contrat d'interface
// (section 3.1). "payload" reste une chaîne JSON brute pour rester simple :
// JsonUtility ne gère pas bien les objets génériques/polymorphes.

using System;

[Serializable]
public class CommandMessage
{
    public string type;
    public string id;
    public long timestamp;
    public int version = 1;
    public string payload = "{}";
}