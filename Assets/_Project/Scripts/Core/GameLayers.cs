using UnityEngine;

// 물리 레이어 이름. ProjectSettings > Tags and Layers 와 반드시 일치해야 한다.
public static class PhysicsLayers
{
    public const string Ground = "Ground";
    public const string Water = "Water";
    public const string Player = "Player";
    public const string Ghost = "Ghost";
    public const string GhostHead = "GhostHead";
    public const string Pole = "Pole";
    public const string Cheese = "Cheese";
    public const string Wall = "Wall";
    // 아무것과도 충돌하지 않는 장식용 레이어 (공중에 떠 있는 오브젝트, 장식 타일)
    public const string Decoration = "Decoration";

    public static int Index(string layerName) => LayerMask.NameToLayer(layerName);
}

// 소팅 레이어 이름. 아래로 갈수록 앞에 그려진다.
// ProjectSettings > Tags and Layers > Sorting Layers 의 순서와 반드시 일치해야 한다.
public static class SortingLayers
{
    public const string Background = "Background";
    public const string Decoration = "Decoration";
    public const string Ground = "Ground";
    public const string Water = "Water";
    public const string Props = "Props";
    public const string Characters = "Characters";
    public const string Effects = "Effects";
    public const string UI = "UI";

    public static readonly string[] Order =
    {
        Background, Decoration, Ground, Water, Props, Characters, Effects, UI
    };
}
