using UnityEngine;

public sealed class DebugConsoleStyleSet
{
    public GUIStyle TitleStyle;
    public GUIStyle BoxStyle;
    public GUIStyle RichLabelStyle;
    public GUIStyle DimLabelStyle;
    public GUIStyle SearchTextFieldStyle;
    public GUIStyle LinkButtonStyle;
    public GUIStyle DisabledButtonStyle;
    public GUIStyle ObjectSelectedButtonStyle;
    public GUIStyle ComponentSelectedButtonStyle;
    public GUIStyle ParentSelectedButtonStyle;
    public GUIStyle FoldoutButtonStyle;
    public GUIStyle ToolbarButtonStyle;
    public GUIStyle ToolbarInfoLabelStyle;
    public GUIStyle ToolbarInfoRightLabelStyle;
    public GUIStyle FooterLeftLabelStyle;
    public GUIStyle FooterRightLabelStyle;
    public GUIStyle ObjectFocusedRowStyle;
    public GUIStyle ObjectParentFocusedRowStyle;
    public GUIStyle ComponentFocusedRowStyle;
    public Texture2D SolidTexture;
}

public static class DebugConsoleStyleFactory
{
    private static readonly Color SelectedObjectBg = new Color(0.98f, 0.80f, 0.18f, 1f);
    private static readonly Color SelectedComponentBg = new Color(0.84f, 0.64f, 0.14f, 1f);
    private static readonly Color SelectedParentBg = new Color(0.50f, 0.38f, 0.08f, 1f);
    private static readonly Color ObjectFocusedRowBg = new Color(0.98f, 0.80f, 0.18f, 0.32f);
    private static readonly Color ParentFocusedRowBg = new Color(0.76f, 0.58f, 0.12f, 0.22f);
    private static readonly Color ComponentFocusedRowBg = new Color(0.84f, 0.64f, 0.14f, 0.36f);
    private static readonly Color SelectedText = new Color(0.18f, 0.11f, 0.00f, 1f);
    private static readonly Color SelectedParentText = new Color(1.00f, 0.95f, 0.78f, 1f);
    private static readonly Color ToolbarInfoText = new Color(1.00f, 0.89f, 0.34f, 1f);
    private static readonly Color FooterInfoTextColor = new Color(0.96f, 0.84f, 0.22f, 1f);
    private static readonly Color NormalButtonText = new Color(0.84f, 0.96f, 0.92f, 1f);
    private static readonly Color DisabledButtonText = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color DimLabelText = new Color(0.6f, 0.6f, 0.6f);

    public static DebugConsoleStyleSet Create(
        GUIStyle labelStyle,
        GUIStyle boldLabelStyle,
        GUIStyle boxStyle,
        GUIStyle textFieldStyle,
        GUIStyle buttonStyle,
        float hierarchyRowHeight,
        float hierarchyFoldoutSize,
        Texture2D sharedTexture = null)
    {
        Texture2D solidTexture = sharedTexture != null ? sharedTexture : CreateSolidTexture();

        GUIStyle titleStyle = new GUIStyle(boldLabelStyle)
        {
            fontSize = 13,
            wordWrap = false,
            clipping = TextClipping.Clip,
            fontStyle = FontStyle.Bold
        };

        GUIStyle panelBoxStyle = new GUIStyle(boxStyle)
        {
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(8, 8, 8, 8)
        };

        GUIStyle richLabelStyle = new GUIStyle(labelStyle)
        {
            richText = true,
            wordWrap = true,
            fontSize = 12
        };

        GUIStyle dimLabelStyle = new GUIStyle(labelStyle);
        ApplyTextColor(dimLabelStyle, DimLabelText);

        GUIStyle searchFieldStyle = new GUIStyle(textFieldStyle)
        {
            fontSize = 12
        };

        GUIStyle linkButtonStyle = new GUIStyle(buttonStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 6, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fixedHeight = hierarchyRowHeight,
            fontStyle = FontStyle.Normal,
            wordWrap = false,
            clipping = TextClipping.Clip
        };
        ApplyTextColor(linkButtonStyle, NormalButtonText);

        GUIStyle disabledButtonStyle = new GUIStyle(linkButtonStyle);
        ApplyTextColor(disabledButtonStyle, DisabledButtonText);

        GUIStyle foldoutButtonStyle = new GUIStyle(buttonStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fixedWidth = hierarchyFoldoutSize,
            fixedHeight = hierarchyRowHeight,
            fontStyle = FontStyle.Bold
        };

        GUIStyle toolbarButtonStyle = new GUIStyle(buttonStyle)
        {
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle toolbarInfoLabelStyle = new GUIStyle(labelStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            richText = false,
            fontStyle = FontStyle.Bold
        };
        ApplyTextColor(toolbarInfoLabelStyle, ToolbarInfoText);

        GUIStyle toolbarInfoRightLabelStyle = new GUIStyle(toolbarInfoLabelStyle)
        {
            alignment = TextAnchor.MiddleRight
        };

        GUIStyle footerLeftLabelStyle = new GUIStyle(labelStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            richText = false,
            fontStyle = FontStyle.Bold
        };
        ApplyTextColor(footerLeftLabelStyle, FooterInfoTextColor);

        GUIStyle footerRightLabelStyle = new GUIStyle(footerLeftLabelStyle)
        {
            alignment = TextAnchor.MiddleRight
        };

        return new DebugConsoleStyleSet
        {
            TitleStyle = titleStyle,
            BoxStyle = panelBoxStyle,
            RichLabelStyle = richLabelStyle,
            DimLabelStyle = dimLabelStyle,
            SearchTextFieldStyle = searchFieldStyle,
            LinkButtonStyle = linkButtonStyle,
            DisabledButtonStyle = disabledButtonStyle,
            ObjectSelectedButtonStyle = CreateButtonStyle(buttonStyle, solidTexture, SelectedObjectBg, SelectedText, true, TextAnchor.MiddleLeft),
            ComponentSelectedButtonStyle = CreateButtonStyle(buttonStyle, solidTexture, SelectedComponentBg, SelectedText, true, TextAnchor.MiddleLeft),
            ParentSelectedButtonStyle = CreateButtonStyle(buttonStyle, solidTexture, SelectedParentBg, SelectedParentText, true, TextAnchor.MiddleLeft),
            FoldoutButtonStyle = foldoutButtonStyle,
            ToolbarButtonStyle = toolbarButtonStyle,
            ToolbarInfoLabelStyle = toolbarInfoLabelStyle,
            ToolbarInfoRightLabelStyle = toolbarInfoRightLabelStyle,
            FooterLeftLabelStyle = footerLeftLabelStyle,
            FooterRightLabelStyle = footerRightLabelStyle,
            ObjectFocusedRowStyle = CreateRowStyle(labelStyle, solidTexture, ObjectFocusedRowBg),
            ObjectParentFocusedRowStyle = CreateRowStyle(labelStyle, solidTexture, ParentFocusedRowBg),
            ComponentFocusedRowStyle = CreateRowStyle(labelStyle, solidTexture, ComponentFocusedRowBg),
            SolidTexture = solidTexture
        };
    }

    private static GUIStyle CreateRowStyle(GUIStyle labelStyle, Texture2D solidTexture, Color backgroundColor)
    {
        GUIStyle style = new GUIStyle(labelStyle)
        {
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            stretchWidth = false,
            stretchHeight = false
        };

        style.normal.background = CreateColoredBackground(solidTexture, backgroundColor);
        style.hover.background = style.normal.background;
        style.active.background = style.normal.background;
        style.focused.background = style.normal.background;
        return style;
    }

    private static GUIStyle CreateButtonStyle(GUIStyle buttonStyle, Texture2D solidTexture, Color backgroundColor, Color textColor, bool bold, TextAnchor alignment)
    {
        GUIStyle style = new GUIStyle(buttonStyle)
        {
            alignment = alignment,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            wordWrap = false,
            clipping = TextClipping.Clip,
            padding = new RectOffset(6, 6, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };

        Texture2D background = CreateColoredBackground(solidTexture, backgroundColor);
        style.normal.background = background;
        style.hover.background = background;
        style.active.background = background;
        style.focused.background = background;
        style.onNormal.background = background;
        style.onHover.background = background;
        style.onActive.background = background;
        style.onFocused.background = background;
        ApplyTextColor(style, textColor);
        return style;
    }

    private static Texture2D CreateSolidTexture()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateColoredBackground(Texture2D texture, Color color)
    {
        Texture2D coloredTexture = Object.Instantiate(texture);
        coloredTexture.SetPixel(0, 0, color);
        coloredTexture.Apply();
        return coloredTexture;
    }

    private static void ApplyTextColor(GUIStyle style, Color color)
    {
        style.normal.textColor = color;
        style.hover.textColor = color;
        style.active.textColor = color;
        style.focused.textColor = color;
        style.onNormal.textColor = color;
        style.onHover.textColor = color;
        style.onActive.textColor = color;
        style.onFocused.textColor = color;
    }
}
