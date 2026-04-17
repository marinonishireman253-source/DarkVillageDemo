using UnityEngine;

public static class UiColors
{
    public static readonly Color Charcoal = new Color32(0x0D, 0x10, 0x14, 0xFF);
    public static readonly Color Slate = new Color32(0x1E, 0x23, 0x2A, 0xFF);
    public static readonly Color Brass = new Color32(0xC8, 0xA5, 0x6E, 0xFF);
    public static readonly Color Moss = new Color32(0x51, 0x65, 0x46, 0xFF);
    public static readonly Color Ember = new Color32(0xA9, 0x4E, 0x3D, 0xFF);
    public static readonly Color TextPrimary = new Color32(0xEE, 0xE8, 0xDD, 0xFF);
    public static readonly Color TextSecondary = new Color32(0xBF, 0xC3, 0xC6, 0xFF);
    public static readonly Color DimBrass = new Color32(0x8C, 0x73, 0x50, 0xFF);
    public static readonly Color FogShadow = new Color(0f, 0f, 0f, 0.42f);
    public static readonly Color PanelOuter = new Color(0.05f, 0.06f, 0.08f, 0.9f);
    public static readonly Color PanelInner = new Color(0.12f, 0.14f, 0.17f, 0.82f);
    public static readonly Color ChoiceIdle = new Color(0.15f, 0.17f, 0.2f, 0.5f);
    public static readonly Color ChoiceSelected = new Color(0.79f, 0.67f, 0.45f, 0.38f);
}

public static class UiSpacing
{
    public const int XS = 4;
    public const int S = 8;
    public const int M = 12;
    public const int L = 16;
    public const int XL = 24;
}

public static class UiFontSize
{
    public const int Small = 11;
    public const int Caption = 12;
    public const int Body = 14;
    public const int Label = 16;
    public const int Section = 18;
    public const int Title = 20;
    public const int Button = 22;
    public const int PanelTitle = 28;
    public const int Emphasis = 30;
    public const int Hero = 40;
}
