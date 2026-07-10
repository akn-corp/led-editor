// Assets/Scripts/Installation/InstallationConfig.cs
//
// Structures purement données, utilisées par JsonUtility pour désérialiser
// le fichier de config d'installation. [System.Serializable] est obligatoire
// pour que Unity sache les lire depuis du JSON.

using System;

[Serializable]
public class EntityConfigData
{
    public int id;
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class InstallationConfig
{
    public EntityConfigData[] entities;
}