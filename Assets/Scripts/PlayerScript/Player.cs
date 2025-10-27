
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player
{
    public string name = "";
    public string skin = "";
    public string color = "";
    public Vector3 spawnPoint;
    public int point = 0;
    public int index;
}

[System.Serializable]
public class ColorAvatarMapping
{
    public string color;
    public Sprite avatarSprite;
}

[System.Serializable]
public class ColorPrefabMapping
{
    public string color;
    public GameObject prefab;
}

[System.Serializable]
public class SkinColorMapping
{
    public string skin;
    public List<ColorPrefabMapping> prefabMapping;
    public List<ColorAvatarMapping> avatarMapping;
}