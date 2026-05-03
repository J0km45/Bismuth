// ------------------------------------------------------------------------------
// 디버그 콘솔에서 사용할 GUIStyle을 일관된 테마로 생성하는 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using UnityEngine;

/// <summary>
/// DebugConsoleStyleSet 관련 역할을 담당하는 class이다.
/// </summary>
public sealed class DebugConsoleStyleSet
{
    // 제목 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle TitleStyle;
    // box 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle BoxStyle;
    // 리치 label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle RichLabelStyle;
    // dim label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle DimLabelStyle;
    // 검색 텍스트 입력 필드 스타일 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    public GUIStyle SearchTextFieldStyle;
    // link 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle LinkButtonStyle;
    // disabled 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle DisabledButtonStyle;
    // 오브젝트 selected 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle ObjectSelectedButtonStyle;
    // 컴포넌트 selected 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle ComponentSelectedButtonStyle;
    // 상위 selected 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle ParentSelectedButtonStyle;
    // foldout 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle FoldoutButtonStyle;
    // toolbar 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle ToolbarButtonStyle;
    // toolbar info label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle ToolbarInfoLabelStyle;
    // toolbar info right label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle ToolbarInfoRightLabelStyle;
    // 하단 left label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle FooterLeftLabelStyle;
    // 하단 right label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    public GUIStyle FooterRightLabelStyle;
    // 오브젝트 focused 행 스타일 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    public GUIStyle ObjectFocusedRowStyle;
    // 오브젝트 상위 focused 행 스타일 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    public GUIStyle ObjectParentFocusedRowStyle;
    // 컴포넌트 focused 행 스타일 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    public GUIStyle ComponentFocusedRowStyle;
    // solid texture 값을 저장한다. 인스턴스 식별자 값을 저장한다.
    public Texture2D SolidTexture;
}

/// <summary>
/// 디버그 콘솔 전용 GUI 스타일을 생성하는 정적 팩토리 클래스이다.
/// </summary>
public static class DebugConsoleStyleFactory
{
    // selected 오브젝트 bg 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color SelectedObjectBg = new Color(0.98f, 0.80f, 0.18f, 1f);
    // selected 컴포넌트 bg 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color SelectedComponentBg = new Color(0.84f, 0.64f, 0.14f, 1f);
    // selected 상위 bg 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color SelectedParentBg = new Color(0.50f, 0.38f, 0.08f, 1f);
    // 오브젝트 focused 행 bg 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private static readonly Color ObjectFocusedRowBg = new Color(0.98f, 0.80f, 0.18f, 0.32f);
    // 상위 focused 행 bg 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private static readonly Color ParentFocusedRowBg = new Color(0.76f, 0.58f, 0.12f, 0.22f);
    // 컴포넌트 focused 행 bg 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private static readonly Color ComponentFocusedRowBg = new Color(0.84f, 0.64f, 0.14f, 0.36f);
    // selected 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color SelectedText = new Color(0.18f, 0.11f, 0.00f, 1f);
    // selected 상위 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color SelectedParentText = new Color(1.00f, 0.95f, 0.78f, 1f);
    // toolbar info 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color ToolbarInfoText = new Color(1.00f, 0.89f, 0.34f, 1f);
    // 하단 info 텍스트 색상 값을 저장한다. 색상 값을 저장한다.
    private static readonly Color FooterInfoTextColor = new Color(0.96f, 0.84f, 0.22f, 1f);
    // normal 버튼 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color NormalButtonText = new Color(0.84f, 0.96f, 0.92f, 1f);
    // disabled 버튼 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color DisabledButtonText = new Color(0.55f, 0.55f, 0.55f);
    // dim label 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private static readonly Color DimLabelText = new Color(0.6f, 0.6f, 0.6f);

    /// <summary>
    /// 관련 작업 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
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

    /// <summary>
    /// 행 스타일 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
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

    /// <summary>
    /// 버튼 스타일 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
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

    /// <summary>
    /// solid texture 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
    private static Texture2D CreateSolidTexture()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// colored background 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
    private static Texture2D CreateColoredBackground(Texture2D texture, Color color)
    {
        Texture2D coloredTexture = Object.Instantiate(texture);
        coloredTexture.SetPixel(0, 0, color);
        coloredTexture.Apply();
        return coloredTexture;
    }

    /// <summary>
    /// 준비된 텍스트 색상 값을 실제 상태에 반영한다.
    /// </summary>
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
