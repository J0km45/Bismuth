// ------------------------------------------------------------------------------
// 게임 실행 중 F1로 여는 런타임 디버그 콘솔 창을 그리는 메인 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 런타임 디버그 콘솔의 전체 UI와 입력 처리를 담당하는 MonoBehaviour 클래스이다.
/// </summary>
public class RuntimeDebugConsoleWindow : MonoBehaviour
{
    /// <summary>
    /// 로그 원본 인덱스를 유지한 채 런타임 표시 대상 로그를 묶어두는 보조 클래스이다.
    /// </summary>
    private sealed class VisibleRuntimeLogEntry
    {
        // 엔트리 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public DebugEntry Entry;
        // 출처 index 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public int SourceIndex;
    }
    // 토글 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    // 표시 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    [SerializeField] private bool _visible = false;
    // 창 영역 값을 저장한다. 런타임 창의 위치와 크기를 저장한다.
    [SerializeField] private Rect _windowRect = new Rect(20f, 20f, 1450f, 850f);
    // 자동 스크롤 값을 저장한다. 새 로그가 들어왔을 때 마지막 항목으로 자동 이동할지 결정한다.
    [SerializeField] private bool _autoScroll = true;
    // 숨김 transform 값을 저장한다. Transform 컴포넌트를 목록에서 숨길지 결정한다.
    [SerializeField] private bool _hideTransform = true;
    // 묶기 이전 on 선택 값을 저장한다. 다른 대상을 선택했을 때 이전에 펼친 항목을 접을지 결정한다.
    [SerializeField] private bool _collapsePreviousOnSelection = true;

    // 계층 스크롤 값을 저장한다. 계층 패널의 스크롤 위치를 저장한다.
    private Vector2 _hierarchyScroll;
    // 로그 스크롤 값을 저장한다. 로그 패널의 스크롤 위치를 저장한다.
    private Vector2 _logScroll;
    // 타입 필터 스크롤 값을 저장한다. 타입 필터 패널의 스크롤 위치를 저장한다.
    private Vector2 _typeFilterScroll;
    // 상세 스크롤 값을 저장한다. 상세 패널의 스크롤 위치를 저장한다.
    private Vector2 _detailScroll;

    // 계층 검색 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private string _hierarchySearch = string.Empty;
    // 로그 검색 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private string _logSearch = string.Empty;
    // 대기 계층 검색 값을 저장한다. 지금 즉시 적용하지 않고 다음 단계에서 반영할 임시 상태를 저장한다.
    private string _pendingHierarchySearch = string.Empty;
    // 대기 로그 검색 값을 저장한다. 지금 즉시 적용하지 않고 다음 단계에서 반영할 임시 상태를 저장한다.
    private string _pendingLogSearch = string.Empty;
    // 계층 검색 적용 시간 값을 저장한다. 검색 입력을 즉시 반영하지 않고 일정 시간 뒤에 적용하기 위한 기준 시각을 저장한다.
    private float _hierarchySearchApplyTime;
    // 로그 검색 적용 시간 값을 저장한다. 검색 입력을 즉시 반영하지 않고 일정 시간 뒤에 적용하기 위한 기준 시각을 저장한다.
    private float _logSearchApplyTime;

    // 계층 검색 control 이름 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const string HierarchySearchControlName = "DebugConsole_HierarchySearch";
    // 로그 검색 control 이름 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const string LogSearchControlName = "DebugConsole_LogSearch";
    // 이전 IME composition mode 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private IMECompositionMode _previousImeCompositionMode = IMECompositionMode.Auto;
    // IME composition 캡처 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private bool _imeCompositionCaptured;
    // 검색 입력 필드 focused this 프레임 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private bool _searchFieldFocusedThisFrame;
    // last focused 검색 입력 필드 영역 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private Rect _lastFocusedSearchFieldRect;

    /// <summary>
    /// SearchFieldFocus 값을 구분하기 위한 열거형이다.
    /// </summary>
    private enum SearchFieldFocus
    {
        None,
        Hierarchy,
        Log
    }

    // active 검색 입력 필드 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private SearchFieldFocus _activeSearchField = SearchFieldFocus.None;
    // 검색 입력 필드 content 스타일 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private GUIStyle _searchFieldContentStyle;
    // close 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _closeButtonStyle;

    // event system 활성화 상태 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Dictionary<EventSystem, bool> _eventSystemEnabledState = new Dictionary<EventSystem, bool>();

    // 검색 오버레이 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private RuntimeDebugConsoleSearchOverlay _searchOverlay;

    // 검색 label 너비 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const float SearchLabelWidth = 48f;
    // 검색 입력 필드 fixed 너비 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const float SearchFieldFixedWidth = 160f;
    // 검색 clear 버튼 너비 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const float SearchClearButtonWidth = 56f;
    // 검색 debounce delay 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const float SearchDebounceDelay = 0.2f;
    // use compact 로그 행 목록 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const bool UseCompactLogRows = true;
    // compact 로그 행 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float CompactLogRowHeight = 34f;
    // 계층 검색 screen 영역 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private Rect _hierarchySearchScreenRect;
    // 로그 검색 screen 영역 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private Rect _logSearchScreenRect;

    // show 타입 필터 패널 값을 저장한다. 타입 필터 패널 표시 여부를 저장한다.
    private bool _showTypeFilterPanel;
    // show 로그 details 값을 저장한다. 하단 상세 패널 표시 여부를 저장한다.
    private bool _showLogDetails = true;
    // 스택 트레이스 foldout 값을 저장한다. 스택 트레이스 영역의 접힘 상태를 저장한다.
    private bool _stackTraceFoldout = true;

    // focused 오브젝트 id 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private int _focusedGameObjectId;
    // focused 컴포넌트 id 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private int _focusedComponentId;
    // focused 오브젝트 이름 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private string _focusedObjectName = string.Empty;
    // focused 컴포넌트 이름 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private string _focusedComponentName = string.Empty;

    // 제목 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _titleStyle;
    // box 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _boxStyle;
    // 리치 label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _richLabelStyle;
    // dim label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _dimLabelStyle;
    // 검색 텍스트 입력 필드 스타일 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private GUIStyle _searchTextFieldStyle;
    // link 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _linkButtonStyle;
    // disabled 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _disabledButtonStyle;
    // 오브젝트 selected 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _objectSelectedButtonStyle;
    // 컴포넌트 selected 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _componentSelectedButtonStyle;
    // 상위 selected 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _parentSelectedButtonStyle;
    // foldout 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _foldoutButtonStyle;
    // toolbar 버튼 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _toolbarButtonStyle;
    // toolbar info label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _toolbarInfoLabelStyle;
    // toolbar info right label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _toolbarInfoRightLabelStyle;
    // 하단 left label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _footerLeftLabelStyle;
    // 하단 right label 스타일 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private GUIStyle _footerRightLabelStyle;
    // 오브젝트 focused 행 스타일 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private GUIStyle _objectFocusedRowStyle;
    // 오브젝트 상위 focused 행 스타일 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private GUIStyle _objectParentFocusedRowStyle;
    // 컴포넌트 focused 행 스타일 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private GUIStyle _componentFocusedRowStyle;
    // solid texture 값을 저장한다. 인스턴스 식별자 값을 저장한다.
    private Texture2D _solidTexture;
    // 스타일 목록 dirty 값을 저장한다. 이 영역을 그릴 때 사용할 GUIStyle 참조를 저장한다.
    private bool _stylesDirty = true;

    // selected 오브젝트 bg 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Color _selectedObjectBg = new Color(0.98f, 0.80f, 0.18f, 1f);
    // selected 컴포넌트 bg 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Color _selectedComponentBg = new Color(0.84f, 0.64f, 0.14f, 1f);
    // selected 상위 bg 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Color _selectedParentBg = new Color(0.50f, 0.38f, 0.08f, 1f);
    // 오브젝트 focused 행 bg 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private readonly Color _objectFocusedRowBg = new Color(0.98f, 0.80f, 0.18f, 0.32f);
    // 상위 focused 행 bg 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private readonly Color _parentFocusedRowBg = new Color(0.76f, 0.58f, 0.12f, 0.22f);
    // 컴포넌트 focused 행 bg 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private readonly Color _componentFocusedRowBg = new Color(0.84f, 0.64f, 0.14f, 0.36f);
    // selected 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Color _selectedText = new Color(0.18f, 0.11f, 0.00f, 1f);
    // selected 상위 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Color _selectedParentText = new Color(1.00f, 0.95f, 0.78f, 1f);
    // toolbar info 텍스트 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Color _toolbarInfoText = new Color(1.00f, 0.89f, 0.34f, 1f);
    // 하단 info 텍스트 색상 값을 저장한다. 색상 값을 저장한다.
    private readonly Color _footerInfoTextColor = new Color(0.96f, 0.84f, 0.22f, 1f);

    // 최대 표시 이름 length 값을 저장한다. 표시용 이름 값을 저장한다.
    private const int MaxDisplayNameLength = 15;
    // 하단 포커스 구간 최대 length 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const int FooterFocusSegmentMaxLength = 16;
    // 계층 행 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float HierarchyRowHeight = 22f;
    // 계층 토글 크기 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float HierarchyToggleSize = 18f;
    // 계층 foldout 크기 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float HierarchyFoldoutSize = 18f;
    // 패널 splitter 너비 값을 저장한다. 인스턴스 식별자 값을 저장한다.
    private const float PanelSplitterWidth = 6f;
    // 최대 계층 indent 패널티 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float MaxHierarchyIndentPenalty = 24f;
    // 최소 계층 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    private const float MinHierarchyPanelWidth = 220f;
    // 최소 로그 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    private const float MinLogPanelWidth = 220f;
    // 계층 행 content right 예약 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float HierarchyRowContentRightReserve = 18f;

    // 창 영역 pref 식별 키 값을 저장한다. 런타임 창의 위치와 크기를 저장한다.
    private const string WindowRectPrefKey = "RuntimeDebugConsoleWindow.WindowRect";
    // 계층 패널 너비 pref 식별 키 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    private const string HierarchyPanelWidthPrefKey = "RuntimeDebugConsoleWindow.HierarchyPanelWidth";

    // 계층 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    [SerializeField] private float _hierarchyPanelWidth = 480f;
    // is dragging 패널 splitter 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private bool _isDraggingPanelSplitter;

    // last 로그 content 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private float _lastLogContentHeight;
    // last 로그 viewport 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private float _lastLogViewportHeight;
    // cached 표시 엔트리 목록 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    private List<VisibleRuntimeLogEntry> _cachedVisibleEntries = new List<VisibleRuntimeLogEntry>();
    // cached 표시 행 heights 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private List<float> _cachedVisibleRowHeights = new List<float>();
    // cached 표시 엔트리 목록 change version 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    private int _cachedVisibleEntriesChangeVersion = -1;
    // cached 표시 엔트리 목록 signature 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    private string _cachedVisibleEntriesSignature = string.Empty;
    // cached 표시 엔트리 목록 너비 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
    private float _cachedVisibleEntriesWidth = -1f;

    // last 최대 로그 스크롤 y 값을 저장한다. 로그 패널의 스크롤 위치를 저장한다.
    private float _lastMaxLogScrollY;
    // 로그 상세 패널 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private float _logDetailPanelHeight = 220f;
    // 행 높이 cache 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Dictionary<string, float> _rowHeightCache = new();

    // 펼침 컴포넌트 목록 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    private readonly HashSet<int> _expandedComponents = new();
    // 펼침 하위 목록 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    private readonly HashSet<int> _expandedChildren = new();

    // selected 로그 index 값을 저장한다. 현재 선택된 로그 항목의 인덱스를 저장한다.
    private int _selectedLogIndex = -1;

    /// <summary>
    /// 객체가 활성화될 때 호출되며, 이벤트 등록과 상태 복원을 수행한다.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        _stylesDirty = true;
        _titleStyle = null;
        _windowRect = DebugConsolePreferenceStore.GetRect(WindowRectPrefKey, _windowRect);
        _hierarchyPanelWidth = DebugConsolePreferenceStore.GetFloat(HierarchyPanelWidthPrefKey, _hierarchyPanelWidth);
        _showLogDetails = DebugConsolePreferenceStore.GetBool(WindowRectPrefKey + ".ShowLogDetails", _showLogDetails);
        _stackTraceFoldout = DebugConsolePreferenceStore.GetBool(WindowRectPrefKey + ".StackTraceFoldout", _stackTraceFoldout);
        _logDetailPanelHeight = DebugConsolePreferenceStore.GetFloat(WindowRectPrefKey + ".LogDetailHeight", _logDetailPanelHeight);
        _pendingHierarchySearch = _hierarchySearch;
        _pendingLogSearch = _logSearch;
        EnsureSearchOverlay();
        ApplyUiInputBlockState();
    }

    /// <summary>
    /// 객체가 비활성화될 때 호출되며, 등록한 이벤트나 임시 상태를 정리한다.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SaveLayoutPreferences();
        RestoreEventSystems();

        if (_searchOverlay != null)
            _searchOverlay.SetVisible(false);
    }

    /// <summary>
    /// 씬 loaded와 관련된 입력이나 이벤트를 처리한다.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _stylesDirty = true;
        _titleStyle = null;
        _expandedComponents.Clear();
        _expandedChildren.Clear();
        _selectedLogIndex = -1;
        _hierarchyScroll = Vector2.zero;
        _activeSearchField = SearchFieldFocus.None;
        ClearFocus();

        if (_searchOverlay != null)
            _searchOverlay.SetVisible(false);

        ApplyUiInputBlockState();
    }

    /// <summary>
    /// 매 프레임 호출되며 입력과 시간 기반 상태를 갱신한다.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
            SetConsoleVisible(!_visible);

        ProcessSearchDebounce();
    }

    /// <summary>
    /// set console 표시 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetConsoleVisible(bool visible)
    {
        _visible = visible;
        ApplyUiInputBlockState();

        if (!_visible)
        {
            SaveLayoutPreferences();

            if (_searchOverlay != null)
                _searchOverlay.SetVisible(false);
        }
    }

    /// <summary>
    /// 준비된 ui 입력 block 상태 값을 실제 상태에 반영한다.
    /// </summary>
    private void ApplyUiInputBlockState()
    {
        if (_visible)
            DisableSceneEventSystems();
        else
            RestoreEventSystems();
    }

    /// <summary>
    /// disable 씬 event systems 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void DisableSceneEventSystems()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem eventSystem = eventSystems[i];
            if (eventSystem == null)
                continue;

            if (!_eventSystemEnabledState.ContainsKey(eventSystem))
                _eventSystemEnabledState[eventSystem] = eventSystem.enabled;

            eventSystem.enabled = false;
        }
    }

    /// <summary>
    /// restore event systems 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void RestoreEventSystems()
    {
        List<EventSystem> keys = new List<EventSystem>(_eventSystemEnabledState.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            EventSystem eventSystem = keys[i];
            if (eventSystem == null)
                continue;

            eventSystem.enabled = _eventSystemEnabledState[eventSystem];
        }

        _eventSystemEnabledState.Clear();
    }

    /// <summary>
    /// IMGUI 이벤트마다 호출되며, 현재 상태를 읽어 디버그 콘솔 UI를 그린다.
    /// </summary>
    private void OnGUI()
    {
        EnsureSearchOverlay();

        if (!_visible)
        {
            if (_searchOverlay != null)
                _searchOverlay.SetVisible(false);

            return;
        }

        if (_searchOverlay != null)
        {
            string nextHierarchySearch = _searchOverlay.HierarchyText;
            string nextLogSearch = _searchOverlay.LogText;

            if (!string.Equals(nextHierarchySearch, _pendingHierarchySearch, StringComparison.Ordinal))
            {
                _pendingHierarchySearch = nextHierarchySearch;
                _hierarchySearchApplyTime = Time.unscaledTime + SearchDebounceDelay;
            }

            if (!string.Equals(nextLogSearch, _pendingLogSearch, StringComparison.Ordinal))
            {
                _pendingLogSearch = nextLogSearch;
                _logSearchApplyTime = Time.unscaledTime + SearchDebounceDelay;
            }
        }
        _hierarchySearchScreenRect = Rect.zero;
        _logSearchScreenRect = Rect.zero;

        InitStyles();
        Rect previousWindowRect = _windowRect;
        _windowRect = GUI.Window(91357, _windowRect, DrawWindow, "Runtime Debug Console");

        if (previousWindowRect != _windowRect)
            SaveLayoutPreferences();

        UpdateSearchOverlayLayout();
    }

    

    

    /// <summary>
    /// 검색 오버레이가 준비되어 있는지 확인하고, 없으면 생성하거나 복구한다.
    /// </summary>
    private void EnsureSearchOverlay()
    {
        if (_searchOverlay != null)
            return;

        _searchOverlay = GetComponentInChildren<RuntimeDebugConsoleSearchOverlay>(true);

        if (_searchOverlay == null)
        {
            GameObject overlayObject = new GameObject("RuntimeDebugConsoleSearchOverlay");
            overlayObject.transform.SetParent(transform, false);
            _searchOverlay = overlayObject.AddComponent<RuntimeDebugConsoleSearchOverlay>();
        }

        _searchOverlay.Initialize();
    }

    /// <summary>
    /// update 검색 오버레이 레이아웃 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void UpdateSearchOverlayLayout()
    {
        if (_searchOverlay != null)
            _searchOverlay.SetVisible(false);
    }

    /// <summary>
    /// to screen 영역 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private Rect ToScreenRect(Rect guiRect)
    {
        Vector2 topLeft = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.xMin, guiRect.yMin));
        return new Rect(topLeft.x, topLeft.y, guiRect.width, guiRect.height);
    }

    /// <summary>
    /// init 스타일 목록 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void InitStyles()
    {
        if (!_stylesDirty && _titleStyle != null)
            return;

        _stylesDirty = false;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13,
            wordWrap = false,
            clipping = TextClipping.Clip
        };

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(8, 8, 8, 8)
        };

        _richLabelStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            wordWrap = true,
            fontSize = 12
        };

        _dimLabelStyle = new GUIStyle(GUI.skin.label);
        _dimLabelStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);

        _searchTextFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 12
        };
        _searchTextFieldStyle.normal.textColor = Color.white;
        _searchTextFieldStyle.focused.textColor = Color.white;
        _searchTextFieldStyle.hover.textColor = Color.white;
        _searchTextFieldStyle.active.textColor = Color.white;

        _linkButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 6, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fixedHeight = HierarchyRowHeight,
            fontStyle = FontStyle.Normal,
            wordWrap = false,
            clipping = TextClipping.Clip
        };
        Color normalButtonText = new Color(0.84f, 0.96f, 0.92f, 1f);
        _linkButtonStyle.normal.textColor = normalButtonText;
        _linkButtonStyle.hover.textColor = normalButtonText;
        _linkButtonStyle.active.textColor = normalButtonText;
        _linkButtonStyle.focused.textColor = normalButtonText;
        _linkButtonStyle.onNormal.textColor = normalButtonText;
        _linkButtonStyle.onHover.textColor = normalButtonText;
        _linkButtonStyle.onActive.textColor = normalButtonText;
        _linkButtonStyle.onFocused.textColor = normalButtonText;

        _disabledButtonStyle = new GUIStyle(_linkButtonStyle);
        _disabledButtonStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
        _disabledButtonStyle.hover.textColor = _disabledButtonStyle.normal.textColor;
        _disabledButtonStyle.active.textColor = _disabledButtonStyle.normal.textColor;

        _foldoutButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fixedWidth = HierarchyFoldoutSize,
            fixedHeight = HierarchyRowHeight,
            fontStyle = FontStyle.Bold
        };

        if (_solidTexture == null)
        {
            _solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _solidTexture.SetPixel(0, 0, Color.white);
            _solidTexture.Apply();
        }

        _objectFocusedRowStyle = CreateRowStyle(_objectFocusedRowBg);
        _objectParentFocusedRowStyle = CreateRowStyle(_parentFocusedRowBg);
        _componentFocusedRowStyle = CreateRowStyle(_componentFocusedRowBg);

        _objectSelectedButtonStyle = CreateButtonStyle(_selectedObjectBg, _selectedText, true, TextAnchor.MiddleLeft);
        _parentSelectedButtonStyle = CreateButtonStyle(_selectedParentBg, _selectedParentText, true, TextAnchor.MiddleLeft);
        _componentSelectedButtonStyle = CreateButtonStyle(_selectedComponentBg, _selectedText, true, TextAnchor.MiddleLeft);

        _toolbarButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter
        };

        _toolbarInfoLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            richText = false,
            fontStyle = FontStyle.Bold
        };
        _toolbarInfoLabelStyle.normal.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.hover.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.active.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.focused.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.onNormal.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.onHover.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.onActive.textColor = _toolbarInfoText;
        _toolbarInfoLabelStyle.onFocused.textColor = _toolbarInfoText;

        _toolbarInfoRightLabelStyle = new GUIStyle(_toolbarInfoLabelStyle)
        {
            alignment = TextAnchor.MiddleRight
        };

        _footerLeftLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            richText = false,
            fontStyle = FontStyle.Bold
        };
        ApplyLabelTextColor(_footerLeftLabelStyle, _footerInfoTextColor);

        _footerRightLabelStyle = new GUIStyle(_footerLeftLabelStyle)
        {
            alignment = TextAnchor.MiddleRight
        };
    }

    /// <summary>
    /// 준비된 label 텍스트 색상 값을 실제 상태에 반영한다.
    /// </summary>
    private void ApplyLabelTextColor(GUIStyle style, Color color)
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

    /// <summary>
    /// 창 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawWindow(int windowId)
    {
        Rect closeButtonRect = new Rect(_windowRect.width - 30f, 4f, 22f, 18f);
        GUIStyle closeButtonStyle = _closeButtonStyle ?? GUI.skin.button;
        if (GUI.Button(closeButtonRect, "X", closeButtonStyle))
        {
            SetConsoleVisible(false);
            GUIUtility.ExitGUI();
        }

        DebugConsoleManager manager = DebugConsoleManager.Instance;
        if (manager == null)
        {
            GUILayout.Label("DebugConsoleManager가 없습니다.");
            GUI.DragWindow(new Rect(0, 0, Mathf.Max(0f, _windowRect.width - 36f), 20f));
            return;
        }

        DrawToolbar(manager);
        DrawTypeFilterPanel(manager);

        if (_searchOverlay != null)
            _searchOverlay.SetVisible(false);

        DrawResizablePanels(manager);

        GUI.DragWindow(new Rect(0, 0, Mathf.Max(0f, _windowRect.width - 36f), 24f));
    }

    /// <summary>
    /// toolbar 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawToolbar(DebugConsoleManager manager)
    {
        float availableWidth = GetTopAreaWidth();
        int layoutLevel = GetTopLayoutLevel(availableWidth);

        int enabledCount = GetEnabledTypeCount(manager);
        int totalCount = Enum.GetValues(typeof(DebugType)).Length;
        string typeButtonLabel = _showTypeFilterPanel
            ? $"Type Filter ▲ ({enabledCount}/{totalCount})"
            : $"Type Filter ▼ ({enabledCount}/{totalCount})";

        if (layoutLevel == 1)
        {
            GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
            DrawToolbarToggleGroup(manager);
            GUILayout.Space(8f);
            DrawToolbarActionGroup(manager, typeButtonLabel);
            GUILayout.EndHorizontal();
            return;
        }

        GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
        DrawToolbarToggleGroup(manager);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUILayout.MinHeight(28f));
        DrawToolbarActionGroup(manager, typeButtonLabel);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 검색 bar 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSearchBar()
    {
        float availableWidth = GetTopAreaWidth();

        if (availableWidth >= 760f)
        {
            GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
            float hierarchyWidth = Mathf.Clamp((availableWidth - 120f) * 0.38f, 160f, 260f);
            float logWidth = Mathf.Clamp((availableWidth - hierarchyWidth) - 24f, 220f, 320f);
            DrawHierarchySearchField(hierarchyWidth);
            GUILayout.Space(12f);
            DrawLogSearchField(logWidth);
            GUILayout.EndHorizontal();
            return;
        }

        GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
        DrawHierarchySearchField(availableWidth);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
        DrawLogSearchField(availableWidth);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 타입 필터 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawTypeFilterPanel(DebugConsoleManager manager)
    {
        if (!_showTypeFilterPanel)
            return;

        GUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("DebugType Filter", _titleStyle);

        _typeFilterScroll = GUILayout.BeginScrollView(_typeFilterScroll, GUILayout.Height(88f));

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        const int columns = 4;

        for (int row = 0; row < types.Length; row += columns)
        {
            GUILayout.BeginHorizontal();

            for (int col = 0; col < columns; col++)
            {
                int index = row + col;
                if (index >= types.Length)
                {
                    GUILayout.FlexibleSpace();
                    continue;
                }

                DebugType type = types[index];
                bool current = manager.GetTypeEnabled(type);
                bool next = GUILayout.Toggle(current, type.ToString(), GUILayout.Width(140f));

                if (next != current)
                    manager.SetTypeEnabled(type, next);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    /// <summary>
    /// resizable panels 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawResizablePanels(DebugConsoleManager manager)
    {
        float contentWidth = Mathf.Max(620f, _windowRect.width - 24f);
        float maxHierarchyPanelWidth = Mathf.Max(MinHierarchyPanelWidth, contentWidth - MinLogPanelWidth - PanelSplitterWidth);

        if (_hierarchyPanelWidth <= 0f)
            _hierarchyPanelWidth = contentWidth * 0.42f;

        _hierarchyPanelWidth = Mathf.Clamp(_hierarchyPanelWidth, MinHierarchyPanelWidth, maxHierarchyPanelWidth);
        float logPanelWidth = Mathf.Max(MinLogPanelWidth, contentWidth - _hierarchyPanelWidth - PanelSplitterWidth);

        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawHierarchyPanel(manager, _hierarchyPanelWidth);
        DrawPanelSplitter(contentWidth);
        DrawLogPanel(manager, logPanelWidth);
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 패널 splitter 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawPanelSplitter(float contentWidth)
    {
        Rect splitterRect = GUILayoutUtility.GetRect(PanelSplitterWidth, 10f, GUILayout.Width(PanelSplitterWidth), GUILayout.ExpandHeight(true));
        Event current = Event.current;
        bool hovered = splitterRect.Contains(current.mousePosition);

        if (current.type == EventType.MouseDown && current.button == 0 && hovered)
        {
            _isDraggingPanelSplitter = true;
            current.Use();
        }

        if (_isDraggingPanelSplitter && current.type == EventType.MouseDrag)
        {
            float maxHierarchyPanelWidth = Mathf.Max(MinHierarchyPanelWidth, contentWidth - MinLogPanelWidth - PanelSplitterWidth);
            _hierarchyPanelWidth = Mathf.Clamp(_hierarchyPanelWidth + current.delta.x, MinHierarchyPanelWidth, maxHierarchyPanelWidth);
            current.Use();
        }

        if (_isDraggingPanelSplitter && (current.type == EventType.MouseUp || current.rawType == EventType.MouseUp))
        {
            _isDraggingPanelSplitter = false;
            SaveLayoutPreferences();
            current.Use();
        }

        Color previousColor = GUI.color;
        if (_isDraggingPanelSplitter)
            GUI.color = new Color(1.00f, 0.86f, 0.32f, 0.95f);
        else if (hovered)
            GUI.color = new Color(0.95f, 0.92f, 0.72f, 0.55f);
        else
            GUI.color = new Color(0.80f, 0.80f, 0.80f, 0.20f);

        GUI.Box(splitterRect, GUIContent.none);
        GUI.color = previousColor;
    }

    /// <summary>
    /// 계층 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawHierarchyPanel(DebugConsoleManager manager, float panelWidth)
    {
        GUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Scene Objects / Components", _titleStyle, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
        DrawHierarchySearchField(panelWidth - 20f);
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);

        _hierarchyScroll = GUILayout.BeginScrollView(_hierarchyScroll);

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
            DrawGameObjectNode(manager, roots[i], 0, panelWidth);

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 계층 행 content 너비 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private float GetHierarchyRowContentWidth(float panelWidth)
    {
        float width = panelWidth;
        width -= _boxStyle.padding.left + _boxStyle.padding.right;
        width -= HierarchyRowContentRightReserve;
        return Mathf.Max(140f, width);
    }

    /// <summary>
    /// 계층 텍스트 버튼 너비 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private float GetHierarchyTextButtonWidth(float rowContentWidth, float leadingSpace, bool reserveToggle, bool reserveFoldout)
    {
        float width = rowContentWidth;
        width -= Mathf.Min(leadingSpace, MaxHierarchyIndentPenalty);

        if (reserveToggle)
            width -= HierarchyToggleSize + 4f;

        if (reserveFoldout)
            width -= HierarchyFoldoutSize + 4f;

        return Mathf.Max(92f, width);
    }

    /// <summary>
    /// 로그 content 너비 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private float GetLogContentWidth(float panelWidth)
    {
        float scrollbarReserve = _autoScroll ? 34f : 58f;
        return Mathf.Max(140f, panelWidth - _boxStyle.padding.left - _boxStyle.padding.right - scrollbarReserve);
    }

    /// <summary>
    /// 로그 vertical scrollbar 스타일 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private GUIStyle GetLogVerticalScrollbarStyle()
    {
        return _autoScroll ? GUIStyle.none : GUI.skin.verticalScrollbar;
    }

    /// <summary>
    /// 오브젝트 node 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawGameObjectNode(DebugConsoleManager manager, GameObject go, int depth, float panelWidth)
    {
        if (go == null)
            return;

        if (!ShouldShowGameObject(go))
            return;

        int id = go.GetInstanceID();
        bool objectEnabled = manager.GetGameObjectEnabled(go);

        Component[] components = go.GetComponents<Component>();
        bool hasVisibleComponents = HasVisibleComponents(components);
        bool hasVisibleChildren = HasVisibleChildren(go);
        bool hasDetails = hasVisibleComponents || hasVisibleChildren;

        bool detailsExpanded = _expandedComponents.Contains(id);
        bool childrenExpanded = _expandedChildren.Contains(id);

        bool searchActive = !string.IsNullOrWhiteSpace(_hierarchySearch);
        bool forceOpenDetails = searchActive && (HasMatchingComponent(go, _hierarchySearch) || HasVisibleChildren(go));
        bool forceOpenChildren = searchActive && HasVisibleChildren(go);

        bool isObjectFocused = IsObjectFocused(id);
        bool isComponentParentFocused = IsFocusedObjectParent(id);
        bool showDetails = hasDetails && (detailsExpanded || forceOpenDetails);
        bool showChildren = hasVisibleChildren && (childrenExpanded || forceOpenChildren);

        float objectLeadingSpace = depth * 18f;
        float rowContentWidth = GetHierarchyRowContentWidth(panelWidth);

        GUILayout.BeginVertical(GetHierarchyRowStyle(isObjectFocused, isComponentParentFocused, false), GUILayout.Width(rowContentWidth));
        GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
        GUILayout.Space(objectLeadingSpace);

        bool nextObjectEnabled = GUILayout.Toggle(objectEnabled, GUIContent.none, GUILayout.Width(HierarchyToggleSize), GUILayout.Height(HierarchyRowHeight));
        if (nextObjectEnabled != objectEnabled)
            manager.SetGameObjectEnabled(go, nextObjectEnabled);

        GUIStyle objectStyle = GetObjectButtonStyle(objectEnabled, isObjectFocused, isComponentParentFocused);
        GUIContent objectContent = new GUIContent(GetDisplayName(go.name), go.name);
        float objectButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, objectLeadingSpace, true, true);
        if (GUILayout.Button(objectContent, objectStyle, GUILayout.Width(objectButtonWidth), GUILayout.Height(HierarchyRowHeight)))
            ToggleGameObjectFocus(go);

        if (hasDetails)
        {
            string foldoutLabel = showDetails ? "▾" : "▸";
            if (GUILayout.Button(foldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleExpandedSet(_expandedComponents, id);
        }
        else
        {
            GUILayout.Space(HierarchyFoldoutSize);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (!showDetails)
            return;

        if (hasVisibleComponents)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = objectEnabled;

            float componentLeadingSpace = (depth + 1) * 18f + HierarchyToggleSize + 8f;

            foreach (Component component in components)
            {
                if (!ShouldShowComponent(component, go.name))
                    continue;

                bool isComponentFocused = IsComponentFocused(component.GetInstanceID());

                GUILayout.BeginVertical(GetHierarchyRowStyle(false, false, isComponentFocused), GUILayout.Width(rowContentWidth));
                GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
                GUILayout.Space(componentLeadingSpace);

                bool componentEnabled = manager.GetComponentEnabled(component);
                bool nextComponentEnabled = GUILayout.Toggle(componentEnabled, GUIContent.none, GUILayout.Width(HierarchyToggleSize), GUILayout.Height(HierarchyRowHeight));
                if (nextComponentEnabled != componentEnabled)
                    manager.SetComponentEnabled(component, nextComponentEnabled);

                GUIStyle componentStyle = GetComponentButtonStyle(objectEnabled, isComponentFocused);
                GUIContent componentContent = new GUIContent(GetDisplayName(component.GetType().Name), component.GetType().Name);
                float componentButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, componentLeadingSpace, true, true);
                if (GUILayout.Button(componentContent, componentStyle, GUILayout.Width(componentButtonWidth), GUILayout.Height(HierarchyRowHeight)))
                    ToggleComponentFocus(component);

                GUILayout.Space(HierarchyFoldoutSize);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUI.enabled = previousEnabled;
        }

        if (hasVisibleChildren)
        {
            float childLeadingSpace = (depth + 1) * 18f + HierarchyToggleSize + 8f + HierarchyToggleSize;

            GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
            GUILayout.Space((depth + 1) * 18f + HierarchyToggleSize + 8f);
            GUILayout.Space(HierarchyToggleSize);

            float childButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, childLeadingSpace, true, true);
            if (GUILayout.Button(new GUIContent("하위 오브젝트", "하위 오브젝트"), _linkButtonStyle, GUILayout.Width(childButtonWidth), GUILayout.Height(HierarchyRowHeight)))
                ToggleExpandedSet(_expandedChildren, id);

            string childFoldoutLabel = showChildren ? "▾" : "▸";
            if (GUILayout.Button(childFoldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleExpandedSet(_expandedChildren, id);

            GUILayout.EndHorizontal();

            if (showChildren)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                    DrawGameObjectNode(manager, go.transform.GetChild(i).gameObject, depth + 1, panelWidth);
            }
        }
    }


/// <summary>
/// 로그 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawLogPanel(DebugConsoleManager manager, float panelWidth)
{
    GUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
    GUILayout.BeginHorizontal();
    GUILayout.Label("Logs", _titleStyle, GUILayout.ExpandWidth(true));
    bool nextShowLogDetails = GUILayout.Toggle(_showLogDetails, "Details", GUILayout.Width(72f));
    if (nextShowLogDetails != _showLogDetails)
    {
        _showLogDetails = nextShowLogDetails;
        SaveLayoutPreferences();
    }
    GUILayout.EndHorizontal();
    GUILayout.Space(4f);
    GUILayout.BeginHorizontal(GUILayout.MinHeight(26f));
    DrawLogSearchField(panelWidth - 20f);
    GUILayout.EndHorizontal();
    GUILayout.Space(4f);

    float logContentWidth = GetLogContentWidth(panelWidth);
    float listViewportHeight = Mathf.Max(120f, _windowRect.height - (_showLogDetails ? _logDetailPanelHeight + 210f : 170f));

    GetVisibleEntriesAndHeights(manager, logContentWidth, out List<VisibleRuntimeLogEntry> visibleEntries, out List<float> rowHeights);
    CalculateVisibleRange(rowHeights, _logScroll.y, listViewportHeight, out int startIndex, out int endIndex, out float topPadding, out float visibleHeight, out float totalHeight);

    _logScroll = GUILayout.BeginScrollView(_logScroll, false, !_autoScroll, GUIStyle.none, GetLogVerticalScrollbarStyle(), GUILayout.MinHeight(listViewportHeight), GUILayout.ExpandHeight(true));

    if (topPadding > 0f)
        GUILayout.Space(topPadding);

    for (int i = startIndex; i < endIndex; i++)
    {
        VisibleRuntimeLogEntry visibleEntry = visibleEntries[i];
        DrawLogEntry(visibleEntry.Entry, visibleEntry.SourceIndex, logContentWidth);
        GUILayout.Space(4f);
    }

    float bottomPadding = Mathf.Max(0f, totalHeight - topPadding - visibleHeight);
    if (bottomPadding > 0f)
        GUILayout.Space(bottomPadding);

    GUILayout.EndScrollView();

    Rect scrollRect = GUILayoutUtility.GetLastRect();
    _lastLogViewportHeight = scrollRect.height;
    _lastLogContentHeight = totalHeight;
    _lastMaxLogScrollY = Mathf.Max(0f, _lastLogContentHeight - _lastLogViewportHeight);

    if (Event.current.type == EventType.Repaint && _autoScroll)
    {
        Vector2 nextScroll = _logScroll;
        nextScroll.y = _lastMaxLogScrollY + 4f;
        _logScroll = nextScroll;
    }

    if (_showLogDetails)
    {
        GUILayout.Space(4f);
        DrawLiveLogDetailPanel(GetSelectedEntry(manager), panelWidth);
    }

    GUILayout.EndVertical();
}

/// <summary>
/// 로그 엔트리 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private float DrawLogEntry(DebugEntry entry, int index, float contentWidth)
    {
        string displayText = UseCompactLogRows ? entry.SummaryRichText : entry.RichText;
        int repeatCount = entry != null ? Mathf.Max(1, entry.RepeatCount) : 1;
        GUIContent content = BuildCollapsedLogContent(displayText, repeatCount);
        float estimatedWidth = Mathf.Max(140f, contentWidth);
        float rowHeight = UseCompactLogRows ? CompactLogRowHeight : _richLabelStyle.CalcHeight(content, estimatedWidth) + 14f;

        Rect rect = GUILayoutUtility.GetRect(0f, rowHeight, GUILayout.ExpandWidth(true));

        Color previousColor = GUI.color;
        if (index == _selectedLogIndex)
            GUI.color = new Color(0.75f, 0.85f, 1f, 1f);

        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, Mathf.Max(0f, rect.width - 12f), rect.height - 12f);
        GUI.Label(labelRect, content, _richLabelStyle);

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            rect.Contains(Event.current.mousePosition))
        {
            _selectedLogIndex = index;
            FocusEntry(entry);

            if (Event.current.clickCount >= 2)
                OpenEntryScript(entry);

            Event.current.Use();
        }

        return rect.height;
    }



/// <summary>
/// 표시 엔트리 목록 signature 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
/// </summary>
private string BuildVisibleEntriesSignature(DebugConsoleManager manager)
{
    return string.Join("|",
        manager != null ? manager.ChangeVersion : -1,
        UseCompactLogRows,
        _logSearch ?? string.Empty,
        _focusedGameObjectId,
        _focusedComponentId);
}

/// <summary>
/// 표시 엔트리 목록 and heights 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
/// </summary>
private void GetVisibleEntriesAndHeights(DebugConsoleManager manager, float width, out List<VisibleRuntimeLogEntry> visibleEntries, out List<float> rowHeights)
{
    if (manager == null)
    {
        visibleEntries = new List<VisibleRuntimeLogEntry>();
        rowHeights = new List<float>();
        return;
    }

    string signature = BuildVisibleEntriesSignature(manager);
    bool requiresRefresh =
        _cachedVisibleEntriesChangeVersion != manager.ChangeVersion ||
        !string.Equals(_cachedVisibleEntriesSignature, signature, StringComparison.Ordinal) ||
        Mathf.Abs(_cachedVisibleEntriesWidth - width) > 0.5f;

    if (requiresRefresh)
    {
        bool canRefreshNow = Event.current == null || Event.current.type == EventType.Layout || _cachedVisibleEntries == null || _cachedVisibleRowHeights == null;
        if (canRefreshNow)
        {
            _cachedVisibleEntries = BuildVisibleEntries(manager);
            _cachedVisibleRowHeights = BuildRowHeights(_cachedVisibleEntries, width);
            _cachedVisibleEntriesChangeVersion = manager.ChangeVersion;
            _cachedVisibleEntriesSignature = signature;
            _cachedVisibleEntriesWidth = width;
        }
    }

    _cachedVisibleEntries ??= new List<VisibleRuntimeLogEntry>();
    _cachedVisibleRowHeights ??= new List<float>();

    visibleEntries = _cachedVisibleEntries;
    rowHeights = _cachedVisibleRowHeights;
}

/// <summary>
/// 표시 엔트리 목록 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
/// </summary>
private List<VisibleRuntimeLogEntry> BuildVisibleEntries(DebugConsoleManager manager)
{
    List<VisibleRuntimeLogEntry> result = new List<VisibleRuntimeLogEntry>();
    if (manager == null)
        return result;

    IReadOnlyList<DebugEntry> entries = manager.Entries;
    for (int i = 0; i < entries.Count; i++)
    {
        DebugEntry entry = entries[i];
        if (!ShouldDisplayEntry(manager, entry))
            continue;

        result.Add(new VisibleRuntimeLogEntry
        {
            Entry = entry,
            SourceIndex = i
        });
    }

    return result;
}

/// <summary>
/// 행 heights 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
/// </summary>
private List<float> BuildRowHeights(List<VisibleRuntimeLogEntry> entries, float width)
{
    List<float> heights = new List<float>(entries.Count);
    float rowHeight = UseCompactLogRows ? CompactLogRowHeight : 0f;

    for (int i = 0; i < entries.Count; i++)
    {
        if (UseCompactLogRows)
        {
            heights.Add(rowHeight);
            continue;
        }

        DebugEntry entry = entries[i].Entry;
        long sequence = entry != null ? entry.SequenceId : i;
        string key = $"{sequence}:{Mathf.RoundToInt(width)}";
        if (!_rowHeightCache.TryGetValue(key, out float height))
        {
            GUIContent content = new GUIContent(entry != null ? entry.RichText : string.Empty);
            height = _richLabelStyle.CalcHeight(content, Mathf.Max(140f, width)) + 18f;
            _rowHeightCache[key] = height;
        }

        heights.Add(height);
    }

    return heights;
}

/// <summary>
    /// calculate 표시 range 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
private void CalculateVisibleRange(List<float> rowHeights, float scrollY, float viewportHeight, out int startIndex, out int endIndex, out float topPadding, out float visibleHeight, out float totalHeight)
{
    startIndex = 0;
    endIndex = rowHeights != null ? rowHeights.Count : 0;
    topPadding = 0f;
    visibleHeight = 0f;
    totalHeight = 0f;

    if (rowHeights == null || rowHeights.Count == 0)
        return;

    const float overscan = 240f;
    float minY = Mathf.Max(0f, scrollY - overscan);
    float maxY = scrollY + Mathf.Max(0f, viewportHeight) + overscan;
    float cumulative = 0f;
    bool started = false;

    for (int i = 0; i < rowHeights.Count; i++)
    {
        float rowHeight = rowHeights[i];
        float rowStart = cumulative;
        float rowEnd = cumulative + rowHeight;
        totalHeight = rowEnd;

        if (!started && rowEnd >= minY)
        {
            started = true;
            startIndex = i;
            topPadding = rowStart;
        }

        if (started)
        {
            visibleHeight += rowHeight;
            endIndex = i + 1;
            if (rowStart > maxY)
                break;
        }

        cumulative = rowEnd;
    }

    if (!started)
    {
        startIndex = 0;
        endIndex = rowHeights.Count;
        topPadding = 0f;
        visibleHeight = totalHeight;
    }
}

/// <summary>
/// selected 엔트리 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
/// </summary>
private DebugEntry GetSelectedEntry(DebugConsoleManager manager)
{
    if (manager == null)
        return null;

    IReadOnlyList<DebugEntry> entries = manager.Entries;
    if (_selectedLogIndex < 0 || _selectedLogIndex >= entries.Count)
        return null;

    return entries[_selectedLogIndex];
}

/// <summary>
/// live 로그 상세 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawLiveLogDetailPanel(DebugEntry entry, float panelWidth)
{
    GUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.Height(_logDetailPanelHeight));
    GUILayout.Label("Log Detail", _titleStyle);

    if (entry == null)
    {
        GUILayout.Label("로그를 선택하면 상세 정보가 표시됩니다.", _dimLabelStyle);
        GUILayout.EndVertical();
        return;
    }

    _detailScroll = GUILayout.BeginScrollView(_detailScroll, GUILayout.Height(_logDetailPanelHeight - 28f));
    GUILayout.Label($"Time : {entry.Time}", _dimLabelStyle);
    GUILayout.Label($"Type : {entry.Type} / {entry.Level}", _dimLabelStyle);
    GUILayout.Label($"Source : {entry.SourceName}", _dimLabelStyle);
    GUILayout.Label($"Member : {entry.MemberName} : {Mathf.Max(1, entry.LineNumber)}", _dimLabelStyle);
    GUILayout.Label($"Scene : {(string.IsNullOrWhiteSpace(entry.SceneKey) ? "-" : entry.SceneKey)}", _dimLabelStyle);
    GUILayout.Label($"Path : {(string.IsNullOrWhiteSpace(entry.HierarchyPath) ? "-" : entry.HierarchyPath)}", _dimLabelStyle);
    GUILayout.Label($"GameObject : {(string.IsNullOrWhiteSpace(entry.GameObjectName) ? "-" : entry.GameObjectName)}", _dimLabelStyle);
    GUILayout.Label($"Component : {(string.IsNullOrWhiteSpace(entry.ComponentName) ? "-" : entry.ComponentName)}", _dimLabelStyle);
    GUILayout.Label($"Frame : {entry.FrameCount}", _dimLabelStyle);
    GUILayout.Space(4f);

    GUILayout.Label("Message", _dimLabelStyle);
    GUI.enabled = false;
    GUILayout.TextArea(entry.Message ?? string.Empty, GUILayout.MinHeight(68f));
    GUI.enabled = true;

    if (!string.IsNullOrWhiteSpace(entry.CallerFilePath))
        GUILayout.Label($"Caller File : {entry.CallerFilePath}", _dimLabelStyle);

    bool nextFoldout = GUILayout.Toggle(_stackTraceFoldout, "Stack Trace", GUI.skin.button, GUILayout.Height(24f));
    if (nextFoldout != _stackTraceFoldout)
    {
        _stackTraceFoldout = nextFoldout;
        SaveLayoutPreferences();
    }

    if (_stackTraceFoldout)
    {
        GUI.enabled = false;
        GUILayout.TextArea(string.IsNullOrWhiteSpace(entry.StackTrace) ? "(No Stack Trace)" : entry.StackTrace, GUILayout.MinHeight(96f));
        GUI.enabled = true;
    }

    GUILayout.EndScrollView();
    GUILayout.EndVertical();
}

/// <summary>
/// 런타임 레이아웃 to 기본를 기본 상태로 되돌린다. 사용자가 변경한 임시 상태를 초기 기준값으로 복원한다.
/// </summary>
private void ResetRuntimeLayoutToDefault()
{
    _hierarchyPanelWidth = 420f;
    _logDetailPanelHeight = 220f;
    _hierarchyScroll = Vector2.zero;
    _logScroll = Vector2.zero;
    _typeFilterScroll = Vector2.zero;
    _detailScroll = Vector2.zero;
    _showTypeFilterPanel = false;
    _showLogDetails = true;
    _stackTraceFoldout = true;
    SaveLayoutPreferences();
}

    /// <summary>
    /// 엔트리 script를 연다. 외부 에셋이나 패널, 스크립트 위치로 이동시키는 데 사용한다.
    /// </summary>
    private void OpenEntryScript(DebugEntry entry)
    {
#if UNITY_EDITOR
        if (!TryGetEntryScriptLocation(entry, out UnityEditor.MonoScript script, out int lineNumber, out int columnNumber))
            return;

        UnityEditor.AssetDatabase.OpenAsset(script, Mathf.Max(1, lineNumber), Mathf.Max(1, columnNumber));
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// get 엔트리 script location 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
    private bool TryGetEntryScriptLocation(DebugEntry entry, out UnityEditor.MonoScript script, out int lineNumber, out int columnNumber)
    {
        script = null;
        lineNumber = 1;
        columnNumber = 1;

        if (entry == null || string.IsNullOrWhiteSpace(entry.CallerFilePath))
            return false;

        lineNumber = Mathf.Max(1, entry.LineNumber);
        columnNumber = Mathf.Max(1, entry.CallerColumn);

        if (TryConvertCallerPathToAssetPath(entry.CallerFilePath, out string assetPath))
        {
            script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(assetPath);
            if (script != null)
                return true;
        }

        return TryFindScriptByFileName(entry.CallerFilePath, out script);
    }

    /// <summary>
    /// convert 호출자 경로 to 에셋 경로 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
    private bool TryConvertCallerPathToAssetPath(string callerFilePath, out string assetPath)
    {
        assetPath = string.Empty;

        if (string.IsNullOrWhiteSpace(callerFilePath))
            return false;

        string normalizedPath = callerFilePath.Replace('\\', '/');

        int assetsIndex = normalizedPath.LastIndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
        if (assetsIndex >= 0)
        {
            assetPath = normalizedPath.Substring(assetsIndex + 1);
            return true;
        }

        int packagesIndex = normalizedPath.LastIndexOf("/Packages/", StringComparison.OrdinalIgnoreCase);
        if (packagesIndex >= 0)
        {
            assetPath = normalizedPath.Substring(packagesIndex + 1);
            return true;
        }

        string projectAssetsPath = Application.dataPath.Replace('\\', '/');
        if (normalizedPath.StartsWith(projectAssetsPath, StringComparison.OrdinalIgnoreCase))
        {
            assetPath = "Assets" + normalizedPath.Substring(projectAssetsPath.Length);
            return true;
        }

        return false;
    }

    /// <summary>
    /// find script by file 이름 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
    private bool TryFindScriptByFileName(string callerFilePath, out UnityEditor.MonoScript script)
    {
        script = null;

        string fileName = Path.GetFileNameWithoutExtension(callerFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        string[] guids = UnityEditor.AssetDatabase.FindAssets($"{fileName} t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(Path.GetFileNameWithoutExtension(assetPath), fileName, StringComparison.Ordinal))
                continue;

            UnityEditor.MonoScript found = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(assetPath);
            if (found == null)
                continue;

            script = found;
            return true;
        }

        return false;
    }
#endif

    /// <summary>
    /// 엔트리를 현재 포커스 대상으로 설정한다. 관련 선택 상태도 함께 갱신한다.
    /// </summary>
    private void FocusEntry(DebugEntry entry)
    {
        if (entry == null)
            return;

        _focusedObjectName = entry.GameObjectName ?? string.Empty;
        _focusedComponentName = entry.ComponentName ?? string.Empty;

        GameObject targetGameObject = null;

        try
        {
            if (entry.Context is GameObject go)
            {
                if (go != null)
                {
                    targetGameObject = go;
                    _focusedGameObjectId = go.GetInstanceID();
                    _focusedComponentId = 0;
                    _focusedObjectName = go.name;
                    _focusedComponentName = string.Empty;
                    PrepareSelectionExpansion(go.transform, true);
                }
            }
            else if (entry.Context is Component component)
            {
                if (component != null && component.gameObject != null)
                {
                    targetGameObject = component.gameObject;
                    _focusedGameObjectId = component.gameObject.GetInstanceID();
                    _focusedComponentId = component.GetInstanceID();
                    _focusedObjectName = component.gameObject.name;
                    _focusedComponentName = component.GetType().Name;
                    PrepareSelectionExpansion(component.transform, true);
                }
            }
        }
        catch (MissingReferenceException)
        {
            targetGameObject = null;
        }

        if (targetGameObject == null)
            return;

#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = targetGameObject;
        UnityEditor.EditorGUIUtility.PingObject(targetGameObject);
#endif
    }

    /// <summary>
    /// expand 선택 경로 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void ExpandSelectionPath(Transform target, bool includeTargetDetails)
    {
        Transform current = target;

        if (current == null)
            return;

        if (includeTargetDetails)
            _expandedComponents.Add(current.gameObject.GetInstanceID());

        while (current.parent != null)
        {
            Transform parent = current.parent;
            int parentId = parent.gameObject.GetInstanceID();
            _expandedComponents.Add(parentId);
            _expandedChildren.Add(parentId);
            current = parent;
        }
    }

    /// <summary>
    /// 표시 엔트리를 수행해야 하는지 정책적으로 판단한다.
    /// </summary>
    private bool ShouldDisplayEntry(DebugConsoleManager manager, DebugEntry entry)
    {
        if (entry == null)
            return false;

        if (!manager.IsAllowed(entry.Type, entry.GameObjectId, entry.ComponentId))
            return false;

        if (!manager.GetLevelEnabled(entry.Level))
            return false;

        if (_focusedComponentId != 0)
        {
            if (entry.ComponentId != _focusedComponentId)
                return false;
        }
        else if (_focusedGameObjectId != 0)
        {
            if (entry.GameObjectId != _focusedGameObjectId)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 오브젝트 포커스 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleGameObjectFocus(GameObject go)
    {
        if (go == null)
            return;

        int id = go.GetInstanceID();

        if (_focusedGameObjectId == id && _focusedComponentId == 0)
        {
            ClearFocus();
            return;
        }

        _focusedGameObjectId = id;
        _focusedComponentId = 0;
        _focusedObjectName = go.name;
        _focusedComponentName = string.Empty;

        PrepareSelectionExpansion(go.transform, true);
    }

    /// <summary>
    /// 컴포넌트 포커스 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleComponentFocus(Component component)
    {
        if (component == null)
            return;

        int componentId = component.GetInstanceID();

        if (_focusedComponentId == componentId)
        {
            ClearFocus();
            return;
        }

        _focusedGameObjectId = component.gameObject.GetInstanceID();
        _focusedComponentId = componentId;
        _focusedObjectName = component.gameObject.name;
        _focusedComponentName = component.GetType().Name;

        PrepareSelectionExpansion(component.transform, true);
    }

    /// <summary>
    /// prepare 선택 expansion 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void PrepareSelectionExpansion(Transform target, bool includeDetails)
    {
        if (target == null)
            return;

        if (_collapsePreviousOnSelection)
            PreserveExpansionWithinTopLevelRoot(target);

        ExpandSelectionPath(target, includeDetails);
    }

    /// <summary>
    /// preserve expansion within top 레벨 root 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void PreserveExpansionWithinTopLevelRoot(Transform target)
    {
        Transform topLevelRoot = GetTopLevelRoot(target);

        if (topLevelRoot == null)
        {
            _expandedComponents.Clear();
            _expandedChildren.Clear();
            return;
        }

        HashSet<int> allowedIds = new HashSet<int>();
        CollectSubtreeIds(topLevelRoot, allowedIds);

        _expandedComponents.RemoveWhere(id => !allowedIds.Contains(id));
        _expandedChildren.RemoveWhere(id => !allowedIds.Contains(id));
    }

    /// <summary>
    /// top 레벨 root 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private Transform GetTopLevelRoot(Transform target)
    {
        if (target == null)
            return null;

        Transform current = target;
        while (current.parent != null)
            current = current.parent;

        return current;
    }

    /// <summary>
    /// 조건에 맞는 subtree ids 항목을 모아 반환한다.
    /// </summary>
    private void CollectSubtreeIds(Transform node, HashSet<int> ids)
    {
        if (node == null || ids == null)
            return;

        ids.Add(node.gameObject.GetInstanceID());

        for (int i = 0; i < node.childCount; i++)
            CollectSubtreeIds(node.GetChild(i), ids);
    }


    /// <summary>
    /// 레이아웃 환경설정를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveLayoutPreferences()
    {
        DebugConsolePreferenceStore.SetRect(WindowRectPrefKey, _windowRect);
        DebugConsolePreferenceStore.SetFloat(HierarchyPanelWidthPrefKey, _hierarchyPanelWidth);
        DebugConsolePreferenceStore.SetBool(WindowRectPrefKey + ".ShowLogDetails", _showLogDetails);
        DebugConsolePreferenceStore.SetBool(WindowRectPrefKey + ".StackTraceFoldout", _stackTraceFoldout);
        DebugConsolePreferenceStore.SetFloat(WindowRectPrefKey + ".LogDetailHeight", _logDetailPanelHeight);
    }

    /// <summary>
    /// 포커스를 비우거나 초기화한다. 이전 상태를 제거하고 다음 작업을 준비하는 데 사용한다.
    /// </summary>
    private void ClearFocus()
    {
        _focusedGameObjectId = 0;
        _focusedComponentId = 0;
        _focusedObjectName = string.Empty;
        _focusedComponentName = string.Empty;
    }

    /// <summary>
    /// 포커스 label 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetFocusLabel()
    {
        if (_focusedComponentId != 0)
            return $"Focus : {_focusedObjectName}/{_focusedComponentName}";

        if (_focusedGameObjectId != 0)
            return $"Focus : {_focusedObjectName} (All Components)";

        return "Focus : All";
    }

    /// <summary>
    /// 하단 포커스 label 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetFooterFocusLabel()
    {
        if (_focusedComponentId != 0)
        {
            string objectName = TrimFooterFocusSegment(_focusedObjectName);
            string componentName = TrimFooterFocusSegment(_focusedComponentName);

            if (string.Equals(_focusedObjectName, _focusedComponentName, StringComparison.Ordinal))
                return $"Focus : {objectName}";

            return $"Focus : {objectName} / {componentName}";
        }

        if (_focusedGameObjectId != 0)
            return $"Focus : {TrimFooterFocusSegment(_focusedObjectName)}";

        return "Focus : All";
    }

    /// <summary>
    /// trim 하단 포커스 구간 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private string TrimFooterFocusSegment(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length > FooterFocusSegmentMaxLength
            ? value.Substring(0, FooterFocusSegmentMaxLength) + "..."
            : value;
    }

    /// <summary>
    /// 포커스 suffix 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetFocusSuffix()
    {
        if (_focusedComponentId != 0)
        {
            string objectName = TrimFooterFocusSegment(_focusedObjectName);
            string componentName = TrimFooterFocusSegment(_focusedComponentName);
            return $"({objectName}/{componentName})";
        }

        if (_focusedGameObjectId != 0)
            return $"({TrimFooterFocusSegment(_focusedObjectName)})";

        return string.Empty;
    }

    /// <summary>
    /// 행 스타일 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
    private GUIStyle CreateRowStyle(Color backgroundColor)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, backgroundColor);
        texture.Apply();

        return new GUIStyle(GUI.skin.box)
        {
            normal = { background = texture },
            border = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 1, 1),
            padding = new RectOffset(3, 3, 1, 1),
            alignment = TextAnchor.MiddleLeft
        };
    }

    /// <summary>
    /// 버튼 스타일 인스턴스나 데이터를 생성한다. 필요한 기본값을 함께 채워 즉시 사용할 수 있게 만든다.
    /// </summary>
    private GUIStyle CreateButtonStyle(Color backgroundColor, Color textColor, bool bold, TextAnchor alignment)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, backgroundColor);
        texture.Apply();

        GUIStyle style = new GUIStyle(_linkButtonStyle)
        {
            alignment = alignment,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            fixedHeight = HierarchyRowHeight
        };

        style.normal.background = texture;
        style.hover.background = texture;
        style.active.background = texture;
        style.focused.background = texture;
        style.onNormal.background = texture;
        style.onHover.background = texture;
        style.onActive.background = texture;
        style.onFocused.background = texture;
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.active.textColor = textColor;
        style.focused.textColor = textColor;
        style.onNormal.textColor = textColor;
        style.onHover.textColor = textColor;
        style.onActive.textColor = textColor;
        style.onFocused.textColor = textColor;
        return style;
    }

    /// <summary>
    /// 계층 행 스타일 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private GUIStyle GetHierarchyRowStyle(bool isObjectFocused, bool isComponentParentFocused, bool isComponentFocused)
    {
        if (isComponentFocused)
            return _componentFocusedRowStyle;

        if (isObjectFocused)
            return _objectFocusedRowStyle;

        if (isComponentParentFocused)
            return _objectParentFocusedRowStyle;

        return GUIStyle.none;
    }

    /// <summary>
    /// focused 오브젝트 상위 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsFocusedObjectParent(int gameObjectId)
    {
        return _focusedGameObjectId == gameObjectId && _focusedComponentId != 0;
    }

    /// <summary>
    /// 오브젝트 focused 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsObjectFocused(int gameObjectId)
    {
        return _focusedGameObjectId == gameObjectId && _focusedComponentId == 0;
    }

    /// <summary>
    /// 컴포넌트 focused 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsComponentFocused(int componentId)
    {
        return _focusedComponentId == componentId;
    }

    /// <summary>
    /// 오브젝트 버튼 스타일 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private GUIStyle GetObjectButtonStyle(bool objectEnabled, bool isObjectFocused, bool isComponentParentFocused)
    {
        if (isObjectFocused)
            return _objectSelectedButtonStyle;

        if (isComponentParentFocused)
            return _parentSelectedButtonStyle;

        return objectEnabled ? _linkButtonStyle : _disabledButtonStyle;
    }

    /// <summary>
    /// 컴포넌트 버튼 스타일 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private GUIStyle GetComponentButtonStyle(bool objectEnabled, bool isComponentFocused)
    {
        if (isComponentFocused)
            return _componentSelectedButtonStyle;

        return objectEnabled ? _linkButtonStyle : _disabledButtonStyle;
    }

    /// <summary>
    /// 펼침 set 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleExpandedSet(HashSet<int> set, int id)
    {
        if (set.Contains(id))
            set.Remove(id);
        else
            set.Add(id);
    }

    /// <summary>
    /// 활성화 타입 개수 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private int GetEnabledTypeCount(DebugConsoleManager manager)
    {
        int count = 0;
        foreach (DebugType type in Enum.GetValues(typeof(DebugType)))
        {
            if (manager.GetTypeEnabled(type))
                count++;
        }

        return count;
    }

    /// <summary>
    /// show 오브젝트를 수행해야 하는지 정책적으로 판단한다.
    /// </summary>
    private bool ShouldShowGameObject(GameObject go)
    {
        return go != null;
    }

    /// <summary>
    /// 표시 하위 목록 보유 또는 존재 여부를 검사한다.
    /// </summary>
    private bool HasVisibleChildren(GameObject go)
    {
        for (int i = 0; i < go.transform.childCount; i++)
        {
            if (ShouldShowGameObject(go.transform.GetChild(i).gameObject))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 표시 컴포넌트 목록 보유 또는 존재 여부를 검사한다.
    /// </summary>
    private bool HasVisibleComponents(Component[] components)
    {
        if (components == null || components.Length == 0)
            return false;

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// matching 컴포넌트 보유 또는 존재 여부를 검사한다.
    /// </summary>
    private bool HasMatchingComponent(GameObject go, string query)
    {
        Component[] components = go.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            if (_hideTransform && component is Transform)
                continue;

            if (ContainsIgnoreCase(component.GetType().Name, query))
                return true;
        }

        return false;
    }

    /// <summary>
    /// show 컴포넌트를 수행해야 하는지 정책적으로 판단한다.
    /// </summary>
    private bool ShouldShowComponent(Component component, string ownerName)
    {
        if (component == null)
            return false;

        if (_hideTransform && component is Transform)
            return false;

        return true;
    }


    /// <summary>
    /// toolbar 토글 그룹 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawToolbarToggleGroup(DebugConsoleManager manager)
    {
        bool global = GUILayout.Toggle(manager.GlobalEnabled, "Global", GUILayout.Width(72f));
        if (global != manager.GlobalEnabled)
            manager.GlobalEnabled = global;

        bool mirror = GUILayout.Toggle(manager.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
        if (mirror != manager.MirrorToUnityConsole)
            manager.MirrorToUnityConsole = mirror;

        bool autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(92f));
        if (autoScroll != _autoScroll)
            _autoScroll = autoScroll;

        bool hideTransform = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(110f));
        if (hideTransform != _hideTransform)
            _hideTransform = hideTransform;

        bool collapsePrevious = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(110f));
        if (collapsePrevious != _collapsePreviousOnSelection)
            _collapsePreviousOnSelection = collapsePrevious;

        bool showLogs = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Log), "Log", GUILayout.Width(70f));
        if (showLogs != manager.GetLevelEnabled(DebugLogLevel.Log))
            manager.SetLevelEnabled(DebugLogLevel.Log, showLogs);

        bool showWarnings = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Warning), "Warn", GUILayout.Width(75f));
        if (showWarnings != manager.GetLevelEnabled(DebugLogLevel.Warning))
            manager.SetLevelEnabled(DebugLogLevel.Warning, showWarnings);

        bool showErrors = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Error), "Error", GUILayout.Width(75f));
        if (showErrors != manager.GetLevelEnabled(DebugLogLevel.Error))
            manager.SetLevelEnabled(DebugLogLevel.Error, showErrors);
    }

/// <summary>
/// toolbar action 그룹 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawToolbarActionGroup(DebugConsoleManager manager, string typeButtonLabel)
{
    if (GUILayout.Button(typeButtonLabel, _toolbarButtonStyle, GUILayout.Width(150f)))
        _showTypeFilterPanel = !_showTypeFilterPanel;

    if (GUILayout.Button("All Types On", _toolbarButtonStyle, GUILayout.Width(92f)))
        manager.SetAllTypes(true);

    if (GUILayout.Button("All Types Off", _toolbarButtonStyle, GUILayout.Width(92f)))
        manager.SetAllTypes(false);

    if (GUILayout.Button("All Levels", _toolbarButtonStyle, GUILayout.Width(92f)))
        manager.SetAllLevels(true);

    if (GUILayout.Button("Warn+", _toolbarButtonStyle, GUILayout.Width(72f)))
        manager.SetWarningAndErrorOnly();

    if (GUILayout.Button("Error Only", _toolbarButtonStyle, GUILayout.Width(92f)))
        manager.SetErrorOnly();

    if (GUILayout.Button("Clear Logs", _toolbarButtonStyle, GUILayout.Width(92f)))
        manager.ClearLogs();

    if (GUILayout.Button("Clear Focus", _toolbarButtonStyle, GUILayout.Width(92f)))
        ClearFocus();

    if (GUILayout.Button("Reset Filters", _toolbarButtonStyle, GUILayout.Width(110f)))
        manager.ResetAllFiltersToDefault();

    if (GUILayout.Button("Reset Layout", _toolbarButtonStyle, GUILayout.Width(110f)))
        ResetRuntimeLayoutToDefault();
}

/// <summary>
/// toolbar info 그룹 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawToolbarInfoGroup(DebugConsoleManager manager, bool expanded)
    {
        GUILayout.Label(GetFocusLabel(), _toolbarInfoLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(expanded ? 34f : 18f));
        GUILayout.Space(8f);
        GUILayout.Label($"Count : {manager.Entries.Count}", _toolbarInfoLabelStyle, GUILayout.Width(expanded ? 120f : 110f), GUILayout.MinHeight(expanded ? 34f : 18f));
    }

    /// <summary>
    /// process 검색 debounce 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void ProcessSearchDebounce()
    {
        bool changed = false;
        float now = Time.unscaledTime;

        if (!string.Equals(_hierarchySearch, _pendingHierarchySearch, StringComparison.Ordinal) &&
            now >= _hierarchySearchApplyTime)
        {
            _hierarchySearch = _pendingHierarchySearch;
            _hierarchyScroll = Vector2.zero;
            changed = true;
        }

        if (!string.Equals(_logSearch, _pendingLogSearch, StringComparison.Ordinal) &&
            now >= _logSearchApplyTime)
        {
            _logSearch = _pendingLogSearch;
            _logScroll = Vector2.zero;
            changed = true;
        }

        if (changed)
        {
            _cachedVisibleEntriesChangeVersion = -1;
        }
    }

    /// <summary>
    /// collapsed 로그 content 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private GUIContent BuildCollapsedLogContent(string richText, int repeatCount)
    {
        if (repeatCount <= 1 || string.IsNullOrWhiteSpace(richText))
            return new GUIContent(richText ?? string.Empty);

        string suffix = $" <color=#f1c232>(x{repeatCount})</color>";
        return new GUIContent(richText + suffix);
    }

    /// <summary>
    /// 계층 검색 입력 필드 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawHierarchySearchField()
    {
        DrawHierarchySearchField(SearchLabelWidth + SearchFieldFixedWidth + 12f);
    }

    /// <summary>
    /// 계층 검색 입력 필드 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawHierarchySearchField(float availableWidth)
    {
        GUILayout.Label("Search", GUILayout.Width(SearchLabelWidth));

        float fieldWidth = GetSearchFieldWidth(availableWidth, false);
        Rect fieldRect = GUILayoutUtility.GetRect(fieldWidth, 24f, GUILayout.Width(fieldWidth), GUILayout.Height(24f));
        GUI.Box(fieldRect, GUIContent.none, _searchTextFieldStyle);
        _hierarchySearchScreenRect = ToScreenRect(fieldRect);

        Event current = Event.current;
        if (current.type == EventType.MouseDown && fieldRect.Contains(current.mousePosition))
        {
            Rect screenRect = ToScreenRect(fieldRect);
            _hierarchySearchScreenRect = screenRect;
            _activeSearchField = SearchFieldFocus.Hierarchy;

            if (_searchOverlay != null)
            {
                _searchOverlay.SetVisible(true);
                _searchOverlay.SetHierarchyRect(screenRect);
                _searchOverlay.SetTexts(_pendingHierarchySearch, _pendingLogSearch);
                _searchOverlay.FocusHierarchy();
            }

            current.Use();
        }

        if (_searchOverlay != null && _searchOverlay.IsHierarchyFocused)
            _searchFieldFocusedThisFrame = true;
    }

    /// <summary>
    /// 로그 검색 입력 필드 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawLogSearchField()
    {
        DrawLogSearchField(SearchLabelWidth + SearchFieldFixedWidth + SearchClearButtonWidth + 20f);
    }

    /// <summary>
    /// 로그 검색 입력 필드 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawLogSearchField(float availableWidth)
    {
        GUILayout.Label("Search", GUILayout.Width(SearchLabelWidth));

        float fieldWidth = GetSearchFieldWidth(availableWidth, true);
        Rect fieldRect = GUILayoutUtility.GetRect(fieldWidth, 24f, GUILayout.Width(fieldWidth), GUILayout.Height(24f));
        GUI.Box(fieldRect, GUIContent.none, _searchTextFieldStyle);
        _logSearchScreenRect = ToScreenRect(fieldRect);

        Event current = Event.current;
        if (current.type == EventType.MouseDown && fieldRect.Contains(current.mousePosition))
        {
            Rect screenRect = ToScreenRect(fieldRect);
            _logSearchScreenRect = screenRect;
            _activeSearchField = SearchFieldFocus.Log;

            if (_searchOverlay != null)
            {
                _searchOverlay.SetVisible(true);
                _searchOverlay.SetLogRect(screenRect);
                _searchOverlay.SetTexts(_pendingHierarchySearch, _pendingLogSearch);
                _searchOverlay.FocusLog();
            }

            current.Use();
        }

        if (_searchOverlay != null && _searchOverlay.IsLogFocused)
            _searchFieldFocusedThisFrame = true;

        if (GUILayout.Button("Clear", GUILayout.Width(SearchClearButtonWidth)))
        {
            _pendingLogSearch = string.Empty;
            _logSearch = string.Empty;
            _logSearchApplyTime = 0f;
            _activeSearchField = SearchFieldFocus.None;

            if (_searchOverlay != null)
            {
                _searchOverlay.SetTexts(_pendingHierarchySearch, _pendingLogSearch);
                _searchOverlay.SetVisible(false);
            }
        }
    }

    /// <summary>
    /// top area 너비 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private float GetTopAreaWidth()
    {
        return Mathf.Max(320f, _windowRect.width - 36f);
    }

    /// <summary>
    /// top 레이아웃 레벨 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private int GetTopLayoutLevel(float availableWidth)
    {
        if (availableWidth >= 1500f)
            return 1;

        if (availableWidth >= 980f)
            return 2;

        return 3;
    }

    /// <summary>
    /// 검색 입력 필드 너비 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private float GetSearchFieldWidth(float availableWidth, bool hasClearButton)
    {
        float reserveWidth = SearchLabelWidth + 10f + (hasClearButton ? SearchClearButtonWidth + 8f : 0f);
        float fieldWidth = availableWidth - reserveWidth;
        return Mathf.Clamp(fieldWidth, 96f, SearchFieldFixedWidth);
    }

    /// <summary>
    /// 표시 엔트리 개수 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private int GetVisibleEntryCount(DebugConsoleManager manager)
    {
        int count = 0;
        IReadOnlyList<DebugEntry> entries = manager.Entries;

        for (int i = 0; i < entries.Count; i++)
        {
            if (ShouldDisplayEntry(manager, entries[i]))
                count++;
        }

        return count;
    }

    /// <summary>
    /// update 계층 버튼 widths 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void UpdateHierarchyButtonWidths()
    {
    }

    /// <summary>
    /// 조건에 맞는 계층 버튼 widths 항목을 모아 반환한다.
    /// </summary>
    private void CollectHierarchyButtonWidths(GameObject go, ref float maxNameWidth)
    {
        if (go == null || !ShouldShowGameObject(go))
            return;

        maxNameWidth = Mathf.Max(maxNameWidth, _linkButtonStyle.CalcSize(new GUIContent($"▶ {GetDisplayName(go.name)}")).x);

        Component[] components = go.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (!ShouldShowComponent(component, go.name))
                continue;

            maxNameWidth = Mathf.Max(maxNameWidth, _linkButtonStyle.CalcSize(new GUIContent($"▶ {GetDisplayName(component.GetType().Name)}")).x);
        }

        for (int i = 0; i < go.transform.childCount; i++)
            CollectHierarchyButtonWidths(go.transform.GetChild(i).gameObject, ref maxNameWidth);
    }

    /// <summary>
    /// 표시 이름 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetDisplayName(string source)
    {
        if (string.IsNullOrEmpty(source))
            return "(Null)";

        return source.Length > MaxDisplayNameLength ? source.Substring(0, MaxDisplayNameLength) + "..." : source;
    }

    /// <summary>
    /// near bottom 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsNearBottom(float maxScrollY)
    {
        if (maxScrollY <= 0f)
            return true;

        float remaining = maxScrollY - _logScroll.y;
        return remaining <= Mathf.Max(maxScrollY * 0.05f, 32f);
    }

    /// <summary>
    /// contains ignore case 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private bool ContainsIgnoreCase(string source, string keyword)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(keyword))
            return false;

        return source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
