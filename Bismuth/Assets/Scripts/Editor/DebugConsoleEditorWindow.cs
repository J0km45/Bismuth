// ------------------------------------------------------------------------------
// 플레이 중 또는 종료 후 스냅샷을 확인하는 에디터 디버그 콘솔 창을 그리는 메인 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 에디터 디버그 콘솔 창 전체 UI와 스냅샷 표시를 담당하는 EditorWindow 클래스이다.
/// </summary>
public class DebugConsoleEditorWindow : EditorWindow
{
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
    private double _hierarchySearchApplyTime;
    // 로그 검색 적용 시간 값을 저장한다. 검색 입력을 즉시 반영하지 않고 일정 시간 뒤에 적용하기 위한 기준 시각을 저장한다.
    private double _logSearchApplyTime;

    // 자동 스크롤 값을 저장한다. 새 로그가 들어왔을 때 마지막 항목으로 자동 이동할지 결정한다.
    private bool _autoScroll = true;
    // 숨김 transform 값을 저장한다. Transform 컴포넌트를 목록에서 숨길지 결정한다.
    private bool _hideTransform = true;
    // 묶기 이전 on 선택 값을 저장한다. 다른 대상을 선택했을 때 이전에 펼친 항목을 접을지 결정한다.
    private bool _collapsePreviousOnSelection = true;
    // 묶기 로그 목록 값을 저장한다. 같은 로그를 묶어서 표시할지 결정한다.
    private bool _collapseLogs = true;
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
    // focused 스냅샷 오브젝트 식별 키 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private string _focusedSnapshotGameObjectKey = string.Empty;
    // focused 스냅샷 컴포넌트 식별 키 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
    private string _focusedSnapshotComponentKey = string.Empty;

    // 계층 검색 입력 필드 control 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private SearchField _hierarchySearchFieldControl;
    // 로그 검색 입력 필드 control 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private SearchField _logSearchFieldControl;

    // 검색 label 너비 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const float SearchLabelWidth = 48f;
    // 검색 입력 필드 fixed 너비 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const float SearchFieldFixedWidth = 160f;
    // 검색 clear 버튼 너비 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const float SearchClearButtonWidth = 56f;
    // 검색 debounce delay 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    private const double SearchDebounceDelay = 0.2d;
    // use compact 로그 행 목록 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const bool UseCompactLogRows = true;
    // compact 로그 행 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float CompactLogRowHeight = 34f;

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
    // 계층 패널 너비 pref 식별 키 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    private const string HierarchyPanelWidthPrefKey = "DebugConsoleEditorWindow.HierarchyPanelWidth";
    // 기본 로그 상세 패널 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float DefaultLogDetailPanelHeight = 220f;
    // 최소 로그 상세 패널 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float MinLogDetailPanelHeight = 140f;
    // 최대 로그 상세 패널 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const float MaxLogDetailPanelHeight = 340f;

    // 계층 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
    private float _hierarchyPanelWidth = 420f;
    // is dragging 패널 splitter 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private bool _isDraggingPanelSplitter;

    // last 로그 content 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private float _lastLogContentHeight;
    // last 로그 viewport 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private float _lastLogViewportHeight;
    // last 최대 로그 스크롤 y 값을 저장한다. 로그 패널의 스크롤 위치를 저장한다.
    private float _lastMaxLogScrollY;
    // 로그 상세 패널 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private float _logDetailPanelHeight = 220f;
    // live 로그 높이 cache 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Dictionary<string, float> _liveLogHeightCache = new();
    // 스냅샷 로그 높이 cache 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private readonly Dictionary<string, float> _snapshotLogHeightCache = new();
    // cached live 로그 groups 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private List<LiveLogGroup> _cachedLiveLogGroups = new();
    // cached live 행 heights 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private List<float> _cachedLiveRowHeights = new();
    // cached live 로그 change version 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private int _cachedLiveLogChangeVersion = -1;
    // cached live 로그 signature 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private string _cachedLiveLogSignature = string.Empty;
    // cached live 로그 너비 값을 저장한다. 인스턴스 식별자 값을 저장한다.
    private float _cachedLiveLogWidth = -1f;


    // 펼침 컴포넌트 목록 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    private readonly HashSet<int> _expandedComponents = new();
    // 펼침 하위 목록 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    private readonly HashSet<int> _expandedChildren = new();

    // selected 로그 index 값을 저장한다. 현재 선택된 로그 항목의 인덱스를 저장한다.
    private int _selectedLogIndex = -1;

    // last 스냅샷 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private DebugConsoleEditorSnapshot _lastSnapshot;
    // next 스냅샷 capture 시간 값을 저장한다. 시간 관련 값을 저장한다.
    private double _nextSnapshotCaptureTime;
    // last 캡처 매니저 change version 값을 저장한다. 중앙 DebugConsoleManager 인스턴스 참조를 저장한다.
    private int _lastCapturedManagerChangeVersion = -1;

    // 펼침 스냅샷 details 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    private readonly HashSet<string> _expandedSnapshotDetails = new();
    // 펼침 스냅샷 하위 목록 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
    private readonly HashSet<string> _expandedSnapshotChildren = new();

    // 스냅샷 directory 경로 값을 저장한다. 경로나 파일 위치를 문자열로 저장한다.
    private const string SnapshotDirectoryPath = "Library/DebugConsole";
    // 스냅샷 file 이름 값을 저장한다. 표시용 이름 값을 저장한다.
    private const string SnapshotFileName = "DebugConsoleEditorSnapshot.json";
    // current 스냅샷 version 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const int CurrentSnapshotVersion = 3;
    // 최대 스냅샷 계층 depth 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    private const int MaxSnapshotHierarchyDepth = 8;
    // 에디터 상태 pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string EditorStatePrefKey = "DebugConsoleEditorWindow.State";
    // 매니저 pref 식별 키 prefix 값을 저장한다. 중앙 DebugConsoleManager 인스턴스 참조를 저장한다.
    private const string ManagerPrefKeyPrefix = "DebugConsole.Manager";
    // 전체 활성화 pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string GlobalEnabledPrefKey = ManagerPrefKeyPrefix + ".GlobalEnabled";
    // 미러 to 유니티 pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string MirrorToUnityPrefKey = ManagerPrefKeyPrefix + ".MirrorToUnity";
    // 로그 레벨 로그 pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string LogLevelLogPrefKey = ManagerPrefKeyPrefix + ".Level.Log";
    // 로그 레벨 경고 pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string LogLevelWarningPrefKey = ManagerPrefKeyPrefix + ".Level.Warning";
    // 로그 레벨 오류 pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string LogLevelErrorPrefKey = ManagerPrefKeyPrefix + ".Level.Error";
    // 타입 pref 식별 키 prefix 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string TypePrefKeyPrefix = ManagerPrefKeyPrefix + ".Type.";
    // 오브젝트 pref 식별 키 prefix 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string GameObjectPrefKeyPrefix = ManagerPrefKeyPrefix + ".GameObject.";
    // 컴포넌트 pref 식별 키 prefix 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string ComponentPrefKeyPrefix = ManagerPrefKeyPrefix + ".Component.";
    // 오브젝트 registry pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string GameObjectRegistryPrefKey = ManagerPrefKeyPrefix + ".Registry.GameObject";
    // 컴포넌트 registry pref 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    private const string ComponentRegistryPrefKey = ManagerPrefKeyPrefix + ".Registry.Component";

    [Serializable]
    /// <summary>
    /// DebugConsoleEditorSnapshot 관련 역할을 담당하는 class이다.
    /// </summary>
    private sealed class DebugConsoleEditorSnapshot
    {
        // 스냅샷 version 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public int SnapshotVersion;
        // 씬 이름 값을 저장한다. 표시용 이름 값을 저장한다.
        public string SceneName;
        // 캡처 at 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public string CapturedAt;
        // 전체 활성화 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool GlobalEnabled;
        // 미러 to 유니티 console 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool MirrorToUnityConsole;
        // show 로그 레벨 로그 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool ShowLogLevelLog = true;
        // show 로그 레벨 경고 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool ShowLogLevelWarning = true;
        // show 로그 레벨 오류 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool ShowLogLevelError = true;
        // 타입 filters 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool[] TypeFilters;
        // roots 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public List<SnapshotGameObjectNode> Roots = new();
        // 엔트리 목록 값을 저장한다. 현재 처리하거나 렌더링할 로그 엔트리 목록을 저장한다.
        public List<SnapshotLogEntry> Entries = new();

        /// <summary>
        /// has data 여부를 계산해 반환한다. UI 표시나 분기 조건에서 바로 사용할 수 있다.
        /// </summary>
        public bool HasData => (Roots != null && Roots.Count > 0) || (Entries != null && Entries.Count > 0);
    }

    [Serializable]
    /// <summary>
    /// 스냅샷에 저장할 오브젝트 트리 노드 한 건을 표현하는 클래스이다.
    /// </summary>
    private sealed class SnapshotGameObjectNode
    {
        // 이름 값을 저장한다. 표시용 이름 값을 저장한다.
        public string Name;
        // 경로 식별 키 값을 저장한다. 경로나 파일 위치를 문자열로 저장한다.
        public string PathKey;
        // 활성화 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool Enabled;
        // 컴포넌트 목록 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public List<SnapshotComponentNode> Components = new();
        // 하위 목록 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public List<SnapshotGameObjectNode> Children = new();
    }

    [Serializable]
    /// <summary>
    /// 스냅샷에 저장할 컴포넌트 정보를 표현하는 클래스이다.
    /// </summary>
    private sealed class SnapshotComponentNode
    {
        // 이름 값을 저장한다. 표시용 이름 값을 저장한다.
        public string Name;
        // 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
        public string Key;
        // 활성화 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool Enabled;
    }

    [Serializable]
    /// <summary>
    /// SnapshotLogEntry 관련 역할을 담당하는 class이다.
    /// </summary>
    private sealed class SnapshotLogEntry
    {
        // 시간 값을 저장한다. 시간 관련 값을 저장한다.
        public string Time;
        // message 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public string Message;
        // 출처 이름 값을 저장한다. 표시용 이름 값을 저장한다.
        public string SourceName;
        // 멤버 이름 값을 저장한다. 표시용 이름 값을 저장한다.
        public string MemberName;
        // 줄 번호 number 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public int LineNumber;
        // 타입 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public DebugType Type;
        // 레벨 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public DebugLogLevel Level;
        // 오브젝트 id 값을 저장한다. 인스턴스 식별자 값을 저장한다.
        public int GameObjectId;
        // 컴포넌트 id 값을 저장한다. 인스턴스 식별자 값을 저장한다.
        public int ComponentId;
        // 오브젝트 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
        public string GameObjectKey;
        // 컴포넌트 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
        public string ComponentKey;
        // 오브젝트 이름 값을 저장한다. 표시용 이름 값을 저장한다.
        public string GameObjectName;
        // 컴포넌트 이름 값을 저장한다. 표시용 이름 값을 저장한다.
        public string ComponentName;
        // 씬 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
        public string SceneKey;
        // 계층 경로 값을 저장한다. 경로나 파일 위치를 문자열로 저장한다.
        public string HierarchyPath;
        // 컴포넌트 타입 이름 값을 저장한다. 표시용 이름 값을 저장한다.
        public string ComponentTypeName;
        // sequence id 값을 저장한다. 인스턴스 식별자 값을 저장한다.
        public long SequenceId;
        // 프레임 개수 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
        public int FrameCount;
        // 캡처 at ISO utc 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public string CapturedAtIsoUtc;
        // 색상 hex 값을 저장한다. 색상 값을 저장한다.
        public string ColorHex;
        // 호출 파일 경로 값을 저장한다. 경로나 파일 위치를 문자열로 저장한다.
        public string CallerFilePath;
        // 호출자 열 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public int CallerColumn = 1;
        // 스택 트레이스 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public string StackTrace;
        // was 표시 at capture 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public bool WasVisibleAtCapture;

        /// <summary>
        /// 리치 텍스트 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
        /// </summary>
        public string RichText =>
            $"<color={ColorHex}>[{Time}] [{Type}] {Message}</color>\n" +
            $"<color=#daa520>출처 : [{SourceName}.{MemberName} : {LineNumber}]</color>";
    }


    [Serializable]
    /// <summary>
    /// DebugConsoleEditorUiState 관련 역할을 담당하는 class이다.
    /// </summary>
    private sealed class DebugConsoleEditorUiState
    {
        // 자동 스크롤 값을 저장한다. 새 로그가 들어왔을 때 마지막 항목으로 자동 이동할지 결정한다.
        public bool AutoScroll = true;
        // 숨김 transform 값을 저장한다. Transform 컴포넌트를 목록에서 숨길지 결정한다.
        public bool HideTransform = true;
        // 묶기 이전 on 선택 값을 저장한다. 다른 대상을 선택했을 때 이전에 펼친 항목을 접을지 결정한다.
        public bool CollapsePreviousOnSelection = true;
        // 묶기 로그 목록 값을 저장한다. 같은 로그를 묶어서 표시할지 결정한다.
        public bool CollapseLogs = true;
        // show 타입 필터 패널 값을 저장한다. 타입 필터 패널 표시 여부를 저장한다.
        public bool ShowTypeFilterPanel;
        // show 로그 details 값을 저장한다. 하단 상세 패널 표시 여부를 저장한다.
        public bool ShowLogDetails = true;
        // 스택 트레이스 foldout 값을 저장한다. 스택 트레이스 영역의 접힘 상태를 저장한다.
        public bool StackTraceFoldout = true;
        // 계층 패널 너비 값을 저장한다. 패널의 현재 너비 값을 저장한다. 레이아웃 계산과 렌더링 폭 결정에 사용한다.
        public float HierarchyPanelWidth = 420f;
        // 로그 상세 패널 높이 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public float LogDetailPanelHeight = DefaultLogDetailPanelHeight;
        // 계층 스크롤 값을 저장한다. 계층 패널의 스크롤 위치를 저장한다.
        public Vector2 HierarchyScroll;
        // 로그 스크롤 값을 저장한다. 로그 패널의 스크롤 위치를 저장한다.
        public Vector2 LogScroll;
        // 타입 필터 스크롤 값을 저장한다. 타입 필터 패널의 스크롤 위치를 저장한다.
        public Vector2 TypeFilterScroll;
        // 상세 스크롤 값을 저장한다. 상세 패널의 스크롤 위치를 저장한다.
        public Vector2 DetailScroll;
        // selected 로그 index 값을 저장한다. 현재 선택된 로그 항목의 인덱스를 저장한다.
        public int SelectedLogIndex = -1;
        // focused 스냅샷 오브젝트 식별 키 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
        public string FocusedSnapshotGameObjectKey = string.Empty;
        // focused 스냅샷 컴포넌트 식별 키 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
        public string FocusedSnapshotComponentKey = string.Empty;
        // focused 오브젝트 이름 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
        public string FocusedObjectName = string.Empty;
        // focused 컴포넌트 이름 값을 저장한다. 현재 포커스된 대상의 식별 정보나 이름을 저장한다. 로그와 계층 패널을 연결하는 기준으로 사용한다.
        public string FocusedComponentName = string.Empty;
        // 펼침 스냅샷 details 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
        public List<string> ExpandedSnapshotDetails = new();
        // 펼침 스냅샷 하위 목록 값을 저장한다. 트리 항목의 펼침 상태를 저장한다.
        public List<string> ExpandedSnapshotChildren = new();
    }

    /// <summary>
    /// LiveLogGroup 관련 역할을 담당하는 class이다.
    /// </summary>
    private sealed class LiveLogGroup
    {
        // 엔트리 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public DebugEntry Entry;
        // 개수 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
        public int Count;
        // last 출처 index 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public int LastSourceIndex;
    }

    /// <summary>
    /// SnapshotLogGroup 관련 역할을 담당하는 class이다.
    /// </summary>
    private sealed class SnapshotLogGroup
    {
        // 엔트리 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public SnapshotLogEntry Entry;
        // 개수 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
        public int Count;
        // last 출처 index 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
        public int LastSourceIndex;
    }


[Serializable]
/// <summary>
/// DebugConsoleStoredBoolValue 관련 역할을 담당하는 class이다.
/// </summary>
private sealed class DebugConsoleStoredBoolValue
{
    // 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    public string Key;
    // value 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool Value;
}

[Serializable]
/// <summary>
/// DebugConsoleStoredManagerSettings 관련 역할을 담당하는 class이다.
/// </summary>
private sealed class DebugConsoleStoredManagerSettings
{
    // 전체 활성화 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool GlobalEnabled = true;
    // 미러 to 유니티 console 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool MirrorToUnityConsole;
    // show 로그 레벨 로그 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool ShowLogLevelLog = true;
    // show 로그 레벨 경고 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool ShowLogLevelWarning = true;
    // show 로그 레벨 오류 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool ShowLogLevelError = true;
    // 타입 filters 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public bool[] TypeFilters;
    // 오브젝트 필터 values 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public List<DebugConsoleStoredBoolValue> GameObjectFilterValues = new();
    // 컴포넌트 필터 values 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public List<DebugConsoleStoredBoolValue> ComponentFilterValues = new();
}

[Serializable]
/// <summary>
/// DebugConsoleEditorBackupData 관련 역할을 담당하는 class이다.
/// </summary>
private sealed class DebugConsoleEditorBackupData
{
    // 매니저 settings 값을 저장한다. 중앙 DebugConsoleManager 인스턴스 참조를 저장한다.
    public DebugConsoleStoredManagerSettings ManagerSettings = new();
    // 에디터 상태 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public DebugConsoleEditorUiState EditorState = new();
}


    [MenuItem("Tools/Debug/Runtime Debug Console Window")]
    /// <summary>
    /// 관련 작업를 연다. 외부 에셋이나 패널, 스크립트 위치로 이동시키는 데 사용한다.
    /// </summary>
    public static void Open()
    {
        DebugConsoleEditorWindow window = GetWindow<DebugConsoleEditorWindow>();
        window.titleContent = new GUIContent("Debug Console");
        window.minSize = new Vector2(1000f, 650f);
        window.Show();
    }

    /// <summary>
    /// 객체가 활성화될 때 호출되며, 이벤트 등록과 상태 복원을 수행한다.
    /// </summary>
    private void OnEnable()
    {
        EditorApplication.update += HandleEditorUpdate;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        _hierarchyPanelWidth = DebugConsolePreferenceStore.GetFloat(HierarchyPanelWidthPrefKey, _hierarchyPanelWidth);
        _hierarchySearchFieldControl ??= new SearchField();
        _logSearchFieldControl ??= new SearchField();
        _pendingHierarchySearch = _hierarchySearch;
        _pendingLogSearch = _logSearch;
        LoadEditorUiState();
        _pendingHierarchySearch = _hierarchySearch;
        _pendingLogSearch = _logSearch;
        LoadSnapshotFromDisk();
        ApplyStoredPreferencesToSnapshot(_lastSnapshot);
    }

    /// <summary>
    /// 객체가 비활성화될 때 호출되며, 등록한 이벤트나 임시 상태를 정리한다.
    /// </summary>
    private void OnDisable()
    {
        EditorApplication.update -= HandleEditorUpdate;
        EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SaveEditorUiState();
        DebugConsolePreferenceStore.SetFloat(HierarchyPanelWidthPrefKey, _hierarchyPanelWidth);
    }

    /// <summary>
    /// 에디터 update와 관련된 입력이나 이벤트를 처리한다.
    /// </summary>
    private void HandleEditorUpdate()
    {
        if (ProcessSearchDebounce())
            Repaint();

        if (EditorApplication.isPlaying)
        {
            TryCaptureLiveSnapshot(false);
            Repaint();
        }
    }

    /// <summary>
    /// play mode changed와 관련된 입력이나 이벤트를 처리한다.
    /// </summary>
    private void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _selectedLogIndex = -1;
            _hierarchyScroll = Vector2.zero;
            _logScroll = Vector2.zero;
            ClearFocus();
            _lastCapturedManagerChangeVersion = -1;
            _nextSnapshotCaptureTime = 0d;
            SaveEditorUiState();
            Repaint();
            return;
        }

        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SaveEditorUiState();
            TryCaptureLiveSnapshot(true);
            Repaint();
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            LoadEditorUiState();
            LoadSnapshotFromDisk();
            ApplyStoredPreferencesToSnapshot(_lastSnapshot);
            Repaint();
        }
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
        _expandedSnapshotDetails.Clear();
        _expandedSnapshotChildren.Clear();
        _selectedLogIndex = -1;
        _hierarchyScroll = Vector2.zero;
        ClearFocus();
        Repaint();
    }

    /// <summary>
    /// IMGUI 이벤트마다 호출되며, 현재 상태를 읽어 디버그 콘솔 UI를 그린다.
    /// </summary>
    private void OnGUI()
    {
        InitStyles();

        if (!EditorApplication.isPlaying)
        {
            DrawSnapshotOrIdleView();
            return;
        }

        DebugConsoleManager manager = DebugConsoleManager.Instance;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("DebugConsoleManager를 찾지 못했습니다. 플레이 시작 후 한 프레임 뒤에 다시 확인해보세요.", MessageType.Warning);
            return;
        }

        TryCaptureLiveSnapshot(false);
        DrawToolbar(manager);
        DrawTypeFilterPanel(manager);

        DrawResizablePanels(manager);
    }


    /// <summary>
    /// 스냅샷 or idle view 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotOrIdleView()
    {
        if (_lastSnapshot == null || !_lastSnapshot.HasData)
        {
            EditorGUILayout.HelpBox("플레이 모드에서 Runtime Debug Console 데이터를 표시합니다.", MessageType.Info);
            if (GUILayout.Button("Play"))
                EditorApplication.isPlaying = true;
            return;
        }

        ApplyStoredPreferencesToSnapshot(_lastSnapshot);

        string sceneName = string.IsNullOrWhiteSpace(_lastSnapshot.SceneName) ? "(Unknown)" : _lastSnapshot.SceneName;
        string capturedAt = string.IsNullOrWhiteSpace(_lastSnapshot.CapturedAt) ? "-" : _lastSnapshot.CapturedAt;

        EditorGUILayout.HelpBox($"마지막 플레이 스냅샷을 표시합니다. Scene : {sceneName} / Captured : {capturedAt}", MessageType.Info);

        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
        GUILayout.Label("Snapshot Mode", _titleStyle, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Clear Snapshot", GUILayout.Width(120f)))
        {
            ClearSavedSnapshot();
            return;
        }

        if (GUILayout.Button("Play", GUILayout.Width(100f)))
            EditorApplication.isPlaying = true;

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4f);

        DrawSnapshotToolbar(_lastSnapshot);
        DrawSnapshotTypeFilterPanel(_lastSnapshot);
        DrawSnapshotResizablePanels(_lastSnapshot);
    }

    /// <summary>
    /// 스냅샷 resizable panels 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotResizablePanels(DebugConsoleEditorSnapshot snapshot)
    {
        float contentWidth = Mathf.Max(620f, position.width - 24f);
        float maxHierarchyPanelWidth = Mathf.Max(MinHierarchyPanelWidth, contentWidth - MinLogPanelWidth - PanelSplitterWidth);

        if (_hierarchyPanelWidth <= 0f)
            _hierarchyPanelWidth = contentWidth * 0.42f;

        _hierarchyPanelWidth = Mathf.Clamp(_hierarchyPanelWidth, MinHierarchyPanelWidth, maxHierarchyPanelWidth);
        float logPanelWidth = Mathf.Max(MinLogPanelWidth, contentWidth - _hierarchyPanelWidth - PanelSplitterWidth);

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawSnapshotHierarchyPanel(snapshot, _hierarchyPanelWidth);
        DrawPanelSplitter(contentWidth);
        DrawSnapshotLogPanel(snapshot, logPanelWidth);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 스냅샷 계층 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotHierarchyPanel(DebugConsoleEditorSnapshot snapshot, float panelWidth)
    {
        GUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Scene Objects / Components", _titleStyle, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
        DrawHierarchySearchField(panelWidth - 20f);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4f);

        _hierarchyScroll = GUILayout.BeginScrollView(_hierarchyScroll);

        if (snapshot?.Roots != null)
        {
            for (int i = 0; i < snapshot.Roots.Count; i++)
                DrawSnapshotGameObjectNode(snapshot.Roots[i], 0, panelWidth);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(4f);
        string footerCountText = $"Count : {GetVisibleSnapshotEntryCount(snapshot)}";

        GUILayout.BeginHorizontal(_boxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(28f));
        GUILayout.Space(10f);
        GUILayout.Label(new GUIContent(footerCountText, footerCountText), _footerLeftLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(20f));
        GUILayout.Space(10f);
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 스냅샷 오브젝트 node 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotGameObjectNode(SnapshotGameObjectNode node, int depth, float panelWidth)
    {
        if (node == null)
            return;

        if (!ShouldShowSnapshotGameObject(node))
            return;

        bool hasVisibleComponents = HasVisibleSnapshotComponents(node.Components, node.Name);
        bool hasVisibleChildren = HasVisibleSnapshotChildren(node.Children);
        bool hasDetails = hasVisibleComponents || hasVisibleChildren;

        bool detailsExpanded = !string.IsNullOrWhiteSpace(node.PathKey) && _expandedSnapshotDetails.Contains(node.PathKey);
        bool childrenExpanded = !string.IsNullOrWhiteSpace(node.PathKey) && _expandedSnapshotChildren.Contains(node.PathKey);

        bool searchActive = !string.IsNullOrWhiteSpace(_hierarchySearch);
        bool forceOpenDetails = searchActive && (HasMatchingSnapshotComponent(node, _hierarchySearch) || hasVisibleChildren);
        bool forceOpenChildren = searchActive && hasVisibleChildren;

        bool isObjectFocused = IsSnapshotObjectFocused(node.PathKey);
        bool isComponentParentFocused = IsSnapshotFocusedObjectParent(node.PathKey);
        bool showDetails = hasDetails && (detailsExpanded || forceOpenDetails);
        bool canShowChildControls = hasVisibleChildren && isObjectFocused;
        bool showChildren = hasVisibleChildren && (forceOpenChildren || (childrenExpanded && canShowChildControls));

        float objectLeadingSpace = depth * 18f;
        float rowContentWidth = GetHierarchyRowContentWidth(panelWidth);

        GUILayout.BeginVertical(GetHierarchyRowStyle(isObjectFocused, isComponentParentFocused, false), GUILayout.Width(rowContentWidth));
        GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
        GUILayout.Space(objectLeadingSpace);

        bool nextObjectEnabled = GUILayout.Toggle(node.Enabled, GUIContent.none, GUILayout.Width(HierarchyToggleSize), GUILayout.Height(HierarchyRowHeight));
        if (nextObjectEnabled != node.Enabled)
            SetSnapshotGameObjectEnabled(node, nextObjectEnabled);

        GUIStyle objectStyle = GetObjectButtonStyle(node.Enabled, isObjectFocused, isComponentParentFocused);
        GUIContent objectContent = new GUIContent(GetDisplayName(node.Name), node.Name);
        float objectButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, objectLeadingSpace, true, true);
        if (GUILayout.Button(objectContent, objectStyle, GUILayout.Width(objectButtonWidth), GUILayout.Height(HierarchyRowHeight)))
            ToggleSnapshotGameObjectFocus(node);

        if (hasDetails)
        {
            string foldoutLabel = showDetails ? "▾" : "▸";
            if (GUILayout.Button(foldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleSnapshotExpandedDetails(node.PathKey);
        }
        else
        {
            GUILayout.Space(HierarchyFoldoutSize);
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        if (showDetails && hasVisibleComponents)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = node.Enabled;

            for (int i = 0; i < node.Components.Count; i++)
            {
                SnapshotComponentNode component = node.Components[i];
                if (!ShouldShowSnapshotComponent(component, node.Name))
                    continue;

                bool isComponentFocused = IsSnapshotComponentFocused(component.Key);
                float componentLeadingSpace = (depth + 1) * 18f + HierarchyToggleSize + 8f;
                rowContentWidth = GetHierarchyRowContentWidth(panelWidth);

                GUILayout.BeginVertical(GetHierarchyRowStyle(false, false, isComponentFocused), GUILayout.Width(rowContentWidth));
                GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
                GUILayout.Space(componentLeadingSpace);

                bool nextComponentEnabled = GUILayout.Toggle(component.Enabled, GUIContent.none, GUILayout.Width(HierarchyToggleSize), GUILayout.Height(HierarchyRowHeight));
                if (nextComponentEnabled != component.Enabled)
                    SetSnapshotComponentEnabled(component, nextComponentEnabled);

                GUIStyle componentStyle = GetComponentButtonStyle(node.Enabled, isComponentFocused);
                GUIContent componentContent = new GUIContent(GetDisplayName(component.Name), component.Name);
                float componentButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, componentLeadingSpace, true, true);
                if (GUILayout.Button(componentContent, componentStyle, GUILayout.Width(componentButtonWidth), GUILayout.Height(HierarchyRowHeight)))
                    ToggleSnapshotComponentFocus(node, component);

                GUILayout.Space(HierarchyFoldoutSize);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUI.enabled = previousEnabled;
        }

        if (!hasVisibleChildren)
            return;

        if (canShowChildControls)
        {
            float childLeadingSpace = (depth + 1) * 18f + HierarchyToggleSize + 8f + HierarchyToggleSize;

            GUILayout.BeginHorizontal(GUILayout.Width(rowContentWidth), GUILayout.Height(HierarchyRowHeight));
            GUILayout.Space((depth + 1) * 18f + HierarchyToggleSize + 8f);
            GUILayout.Space(HierarchyToggleSize);

            float childButtonWidth = GetHierarchyTextButtonWidth(rowContentWidth, childLeadingSpace, true, true);
            if (GUILayout.Button(new GUIContent("하위 오브젝트", "하위 오브젝트"), _linkButtonStyle, GUILayout.Width(childButtonWidth), GUILayout.Height(HierarchyRowHeight)))
                ToggleSnapshotExpandedChildren(node.PathKey);

            string childFoldoutLabel = showChildren ? "▾" : "▸";
            if (GUILayout.Button(childFoldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleSnapshotExpandedChildren(node.PathKey);

            GUILayout.EndHorizontal();
        }

        if (!showChildren)
            return;

        for (int i = 0; i < node.Children.Count; i++)
            DrawSnapshotGameObjectNode(node.Children[i], depth + 1, panelWidth);
    }


    /// <summary>
    /// 표시 스냅샷 로그 groups 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private List<SnapshotLogGroup> BuildVisibleSnapshotLogGroups(DebugConsoleEditorSnapshot snapshot)
    {
        List<SnapshotLogGroup> groups = new List<SnapshotLogGroup>();
        if (snapshot?.Entries == null)
            return groups;

        for (int i = 0; i < snapshot.Entries.Count; i++)
        {
            SnapshotLogEntry entry = snapshot.Entries[i];
            if (!ShouldDisplaySnapshotEntry(entry))
                continue;

            if (_collapseLogs && groups.Count > 0)
            {
                SnapshotLogGroup lastGroup = groups[groups.Count - 1];
                if (CanCollapseSnapshotEntries(lastGroup.Entry, entry))
                {
                    lastGroup.Entry = entry;
                    lastGroup.Count++;
                    lastGroup.LastSourceIndex = i;
                    continue;
                }
            }

            groups.Add(new SnapshotLogGroup
            {
                Entry = entry,
                Count = 1,
                LastSourceIndex = i
            });
        }

        return groups;
    }


    /// <summary>
    /// live 로그 cache signature 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildLiveLogCacheSignature(DebugConsoleManager manager)
    {
        return string.Join("|",
            manager != null ? manager.ChangeVersion : -1,
            _collapseLogs,
            UseCompactLogRows,
            _logSearch ?? string.Empty,
            _focusedGameObjectId,
            _focusedComponentId,
            _focusedSnapshotGameObjectKey ?? string.Empty,
            _focusedSnapshotComponentKey ?? string.Empty);
    }

    /// <summary>
    /// 표시 live 로그 groups and heights 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private void GetVisibleLiveLogGroupsAndHeights(DebugConsoleManager manager, float width, out List<LiveLogGroup> groups, out List<float> rowHeights)
    {
        if (manager == null)
        {
            groups = new List<LiveLogGroup>();
            rowHeights = new List<float>();
            return;
        }

        string signature = BuildLiveLogCacheSignature(manager);
        bool requiresRefresh =
            _cachedLiveLogChangeVersion != manager.ChangeVersion ||
            !string.Equals(_cachedLiveLogSignature, signature, StringComparison.Ordinal) ||
            Mathf.Abs(_cachedLiveLogWidth - width) > 0.5f;

        if (requiresRefresh)
        {
            bool canRefreshNow = Event.current == null || Event.current.type == EventType.Layout || _cachedLiveLogGroups == null || _cachedLiveRowHeights == null;
            if (canRefreshNow)
            {
                _cachedLiveLogGroups = BuildVisibleLiveLogGroups(manager);
                _cachedLiveRowHeights = BuildLiveRowHeights(_cachedLiveLogGroups, width);
                _cachedLiveLogChangeVersion = manager.ChangeVersion;
                _cachedLiveLogSignature = signature;
                _cachedLiveLogWidth = width;
            }
        }

        _cachedLiveLogGroups ??= new List<LiveLogGroup>();
        _cachedLiveRowHeights ??= new List<float>();

        groups = _cachedLiveLogGroups;
        rowHeights = _cachedLiveRowHeights;
    }

    /// <summary>
    /// 표시 live 로그 groups 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private List<LiveLogGroup> BuildVisibleLiveLogGroups(DebugConsoleManager manager)
    {
        List<LiveLogGroup> groups = new List<LiveLogGroup>();
        if (manager == null)
            return groups;

        IReadOnlyList<DebugEntry> entries = manager.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];
            if (!ShouldDisplayEntry(manager, entry))
                continue;

            int repeatCount = Mathf.Max(1, entry.RepeatCount);

            if (_collapseLogs && groups.Count > 0)
            {
                LiveLogGroup lastGroup = groups[groups.Count - 1];
                if (CanCollapseLiveEntries(lastGroup.Entry, entry))
                {
                    lastGroup.Entry = entry;
                    lastGroup.Count += repeatCount;
                    lastGroup.LastSourceIndex = i;
                    continue;
                }
            }

            groups.Add(new LiveLogGroup
            {
                Entry = entry,
                Count = repeatCount,
                LastSourceIndex = i
            });
        }

        return groups;
    }

    /// <summary>
    /// 현재 상태에서 묶기 live 엔트리 목록가 가능한지 검사한다.
    /// </summary>
    private bool CanCollapseLiveEntries(DebugEntry left, DebugEntry right)
    {
        if (left == null || right == null)
            return false;

        return string.Equals(left.CollapseKey, right.CollapseKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// 현재 상태에서 묶기 스냅샷 엔트리 목록가 가능한지 검사한다.
    /// </summary>
    private bool CanCollapseSnapshotEntries(SnapshotLogEntry left, SnapshotLogEntry right)
    {
        if (left == null || right == null)
            return false;

        return string.Equals(left.Message, right.Message, StringComparison.Ordinal) &&
               string.Equals(left.SourceName, right.SourceName, StringComparison.Ordinal) &&
               string.Equals(left.MemberName, right.MemberName, StringComparison.Ordinal) &&
               string.Equals(left.ColorHex, right.ColorHex, StringComparison.Ordinal) &&
               string.Equals(left.CallerFilePath, right.CallerFilePath, StringComparison.Ordinal) &&
               string.Equals(left.GameObjectKey, right.GameObjectKey, StringComparison.Ordinal) &&
               string.Equals(left.ComponentKey, right.ComponentKey, StringComparison.Ordinal) &&
               left.LineNumber == right.LineNumber &&
               left.CallerColumn == right.CallerColumn &&
               left.Type == right.Type &&
               left.Level == right.Level &&
               left.GameObjectId == right.GameObjectId &&
               left.ComponentId == right.ComponentId;
    }

    /// <summary>
    /// collapsed 로그 content 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private GUIContent BuildCollapsedLogContent(string richText, int repeatCount)
    {
        if (repeatCount <= 1 || string.IsNullOrWhiteSpace(richText))
            return new GUIContent(richText ?? string.Empty);

        string suffix = $" <color=#f1c232>(x{repeatCount})</color>";
        int newLineIndex = richText.IndexOf('\n');
        string collapsedText = newLineIndex >= 0
            ? richText.Insert(newLineIndex, suffix)
            : richText + suffix;

        return new GUIContent(collapsedText);
    }


/// <summary>
/// 스냅샷 로그 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawSnapshotLogPanel(DebugConsoleEditorSnapshot snapshot, float panelWidth)
{
    EditorGUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Logs", _titleStyle, GUILayout.ExpandWidth(true));
    bool nextShowLogDetails = GUILayout.Toggle(_showLogDetails, "Details", GUILayout.Width(70f));
    if (nextShowLogDetails != _showLogDetails)
    {
        _showLogDetails = nextShowLogDetails;
        SaveEditorUiState();
    }
    EditorGUILayout.EndHorizontal();
    GUILayout.Space(4f);
    EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
    DrawLogSearchField(panelWidth - 20f);
    EditorGUILayout.EndHorizontal();
    GUILayout.Space(4f);

    float width = Mathf.Max(GetLogContentWidth(panelWidth), 180f);
    float listViewportHeight = Mathf.Max(120f, position.height - (_showLogDetails ? _logDetailPanelHeight + 240f : 190f));

    List<SnapshotLogGroup> groups = BuildVisibleSnapshotLogGroups(snapshot);
    List<float> rowHeights = BuildSnapshotRowHeights(groups, width);
    CalculateVisibleRange(rowHeights, _logScroll.y, listViewportHeight, out int startIndex, out int endIndex, out float topPadding, out float visibleHeight, out float totalHeight);

    _logScroll = GUILayout.BeginScrollView(_logScroll, false, !_autoScroll, GUIStyle.none, GetLogVerticalScrollbarStyle(), GUILayout.MinHeight(listViewportHeight), GUILayout.ExpandHeight(true));

    if (topPadding > 0f)
        GUILayout.Space(topPadding);

    for (int i = startIndex; i < endIndex; i++)
    {
        SnapshotLogGroup group = groups[i];
        DrawSnapshotLogEntry(group.Entry, group.LastSourceIndex, width, group.Count);
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
        SnapshotLogEntry selectedEntry = GetSelectedSnapshotEntry(snapshot);
        DrawSnapshotLogDetailPanel(selectedEntry, panelWidth);
    }

    EditorGUILayout.EndVertical();
}

/// <summary>
/// 스냅샷 로그 엔트리 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private float DrawSnapshotLogEntry(SnapshotLogEntry entry, int sourceIndex, float width, int repeatCount)
    {
        GUIContent content = UseCompactLogRows
            ? BuildCollapsedLogContent(BuildSnapshotCompactRichText(entry), repeatCount)
            : BuildCollapsedLogContent(entry.RichText, repeatCount);
        float rowHeight = UseCompactLogRows ? CompactLogRowHeight : _richLabelStyle.CalcHeight(content, width) + 12f;

        Rect rect = GUILayoutUtility.GetRect(10f, rowHeight, GUILayout.ExpandWidth(true));

        Color previousColor = GUI.color;
        if (sourceIndex == _selectedLogIndex)
            GUI.color = new Color(0.75f, 0.85f, 1f, 1f);

        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f);
        GUI.Label(labelRect, content, _richLabelStyle);

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            rect.Contains(Event.current.mousePosition))
        {
            _selectedLogIndex = sourceIndex;
            FocusSnapshotEntry(entry);
            SaveEditorUiState();

            if (Event.current.clickCount >= 2)
                OpenEntryScript(entry);

            Event.current.Use();
        }

        return rect.height;
    }

    /// <summary>
    /// show 스냅샷 오브젝트를 수행해야 하는지 정책적으로 판단한다.
    /// </summary>
    private bool ShouldShowSnapshotGameObject(SnapshotGameObjectNode node)
    {
        if (node == null)
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        if (ContainsIgnoreCase(node.Name, _hierarchySearch))
            return true;

        if (HasMatchingSnapshotComponent(node, _hierarchySearch))
            return true;

        return HasVisibleSnapshotChildren(node.Children);
    }

    /// <summary>
    /// matching 스냅샷 컴포넌트 보유 또는 존재 여부를 검사한다.
    /// </summary>
    private bool HasMatchingSnapshotComponent(SnapshotGameObjectNode node, string keyword)
    {
        if (node?.Components == null)
            return false;

        for (int i = 0; i < node.Components.Count; i++)
        {
            SnapshotComponentNode component = node.Components[i];
            if (ShouldShowSnapshotComponent(component, node.Name) && ContainsIgnoreCase(component.Name, keyword))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 표시 스냅샷 컴포넌트 목록 보유 또는 존재 여부를 검사한다.
    /// </summary>
    private bool HasVisibleSnapshotComponents(List<SnapshotComponentNode> components, string ownerName)
    {
        if (components == null || components.Count == 0)
            return false;

        for (int i = 0; i < components.Count; i++)
        {
            if (ShouldShowSnapshotComponent(components[i], ownerName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 표시 스냅샷 하위 목록 보유 또는 존재 여부를 검사한다.
    /// </summary>
    private bool HasVisibleSnapshotChildren(List<SnapshotGameObjectNode> children)
    {
        if (children == null || children.Count == 0)
            return false;

        for (int i = 0; i < children.Count; i++)
        {
            if (ShouldShowSnapshotGameObject(children[i]))
                return true;
        }

        return false;
    }

    /// <summary>
    /// show 스냅샷 컴포넌트를 수행해야 하는지 정책적으로 판단한다.
    /// </summary>
    private bool ShouldShowSnapshotComponent(SnapshotComponentNode component, string ownerName)
    {
        if (component == null)
            return false;

        if (_hideTransform && string.Equals(component.Name, nameof(Transform), StringComparison.Ordinal))
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        if (ContainsIgnoreCase(ownerName, _hierarchySearch))
            return true;

        return ContainsIgnoreCase(component.Name, _hierarchySearch);
    }

    /// <summary>
    /// 표시 스냅샷 엔트리를 수행해야 하는지 정책적으로 판단한다.
    /// </summary>
    private bool ShouldDisplaySnapshotEntry(SnapshotLogEntry entry)
    {
        if (entry == null || _lastSnapshot == null)
            return false;

        if (!_lastSnapshot.GlobalEnabled)
            return false;

        int typeIndex = (int)entry.Type;
        if (_lastSnapshot.TypeFilters != null && typeIndex >= 0 && typeIndex < _lastSnapshot.TypeFilters.Length && !_lastSnapshot.TypeFilters[typeIndex])
            return false;

        if (!IsSnapshotLevelEnabled(_lastSnapshot, entry.Level))
            return false;

        if (!string.IsNullOrWhiteSpace(entry.GameObjectKey) && !GetStoredGameObjectEnabled(entry.GameObjectKey, true))
            return false;

        if (!string.IsNullOrWhiteSpace(entry.ComponentKey) && !GetStoredComponentEnabled(entry.ComponentKey, true))
            return false;

        if (!string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
        {
            if (!string.Equals(entry.ComponentKey, _focusedSnapshotComponentKey, StringComparison.Ordinal))
                return false;
        }
        else if (!string.IsNullOrWhiteSpace(_focusedSnapshotGameObjectKey))
        {
            if (!string.Equals(entry.GameObjectKey, _focusedSnapshotGameObjectKey, StringComparison.Ordinal))
                return false;
        }
        else if (_focusedComponentId != 0)
        {
            if (entry.ComponentId != _focusedComponentId)
                return false;
        }
        else if (_focusedGameObjectId != 0)
        {
            if (entry.GameObjectId != _focusedGameObjectId)
                return false;
        }

        if (string.IsNullOrWhiteSpace(_logSearch))
            return true;

        string searchPool = $"{entry.Message} {entry.SourceName} {entry.MemberName} {entry.Type} {entry.Time} {entry.Level}";
        return ContainsIgnoreCase(searchPool, _logSearch);
    }

    /// <summary>
    /// 표시 스냅샷 엔트리 개수 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private int GetVisibleSnapshotEntryCount(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot?.Entries == null)
            return 0;

        int count = 0;
        for (int i = 0; i < snapshot.Entries.Count; i++)
        {
            if (ShouldDisplaySnapshotEntry(snapshot.Entries[i]))
                count++;
        }

        return count;
    }

    /// <summary>
    /// capture live 스냅샷 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
    private void TryCaptureLiveSnapshot(bool force)
    {
        if (!EditorApplication.isPlaying)
            return;

        DebugConsoleManager manager = DebugConsoleManager.Instance;
        if (manager == null)
            return;

        double now = EditorApplication.timeSinceStartup;
        bool captureByInterval = now >= _nextSnapshotCaptureTime;
        bool captureByChange = manager.ChangeVersion != _lastCapturedManagerChangeVersion;

        if (!force && !captureByInterval && !captureByChange)
            return;

        _lastSnapshot = CaptureSnapshot(manager);
        SaveEditorUiState();
        SaveSnapshotToDisk(_lastSnapshot);
        _lastCapturedManagerChangeVersion = manager.ChangeVersion;
        _nextSnapshotCaptureTime = now + 0.75d;
    }

    /// <summary>
    /// 현재 상태에서 스냅샷 정보를 수집해 스냅샷이나 캐시로 만든다.
    /// </summary>
    private DebugConsoleEditorSnapshot CaptureSnapshot(DebugConsoleManager manager)
    {
        DebugConsoleEditorSnapshot snapshot = new DebugConsoleEditorSnapshot
        {
            SnapshotVersion = CurrentSnapshotVersion,
            SceneName = SceneManager.GetActiveScene().name,
            CapturedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            GlobalEnabled = manager.GlobalEnabled,
            MirrorToUnityConsole = manager.MirrorToUnityConsole,
            ShowLogLevelLog = manager.GetLevelEnabled(DebugLogLevel.Log),
            ShowLogLevelWarning = manager.GetLevelEnabled(DebugLogLevel.Warning),
            ShowLogLevelError = manager.GetLevelEnabled(DebugLogLevel.Error),
            TypeFilters = CaptureTypeFilters(manager),
            Roots = new List<SnapshotGameObjectNode>(),
            Entries = new List<SnapshotLogEntry>()
        };

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                snapshot.Roots.Add(CaptureSnapshotNode(manager, roots[i], 0));
        }

        IReadOnlyList<DebugEntry> entries = manager.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            DebugEntry entry = entries[i];
            if (entry == null)
                continue;

            CaptureSnapshotEntryTargets(entry, out string gameObjectKey, out string componentKey, out string gameObjectName, out string componentName);

            snapshot.Entries.Add(new SnapshotLogEntry
            {
                Time = entry.Time,
                Message = entry.Message,
                SourceName = entry.SourceName,
                MemberName = entry.MemberName,
                LineNumber = entry.LineNumber,
                Type = entry.Type,
                Level = entry.Level,
                GameObjectId = entry.GameObjectId,
                ComponentId = entry.ComponentId,
                GameObjectKey = gameObjectKey,
                ComponentKey = componentKey,
                GameObjectName = gameObjectName,
                ComponentName = componentName,
                SceneKey = entry.SceneKey,
                HierarchyPath = entry.HierarchyPath,
                ComponentTypeName = entry.ComponentTypeName,
                SequenceId = entry.SequenceId,
                FrameCount = entry.FrameCount,
                CapturedAtIsoUtc = entry.CapturedAtIsoUtc,
                ColorHex = entry.ColorHex,
                CallerFilePath = entry.CallerFilePath,
                CallerColumn = entry.CallerColumn,
                StackTrace = entry.StackTrace,
                WasVisibleAtCapture = ShouldDisplayEntry(manager, entry)
            });
        }

        return snapshot;
    }

    /// <summary>
    /// 현재 상태에서 스냅샷 node 정보를 수집해 스냅샷이나 캐시로 만든다.
    /// </summary>
    private SnapshotGameObjectNode CaptureSnapshotNode(DebugConsoleManager manager, GameObject go, int depth)
    {
        string objectKey = go != null ? DebugConsoleFilterKeyUtility.GetGameObjectKey(go) : string.Empty;
        SnapshotGameObjectNode node = new SnapshotGameObjectNode
        {
            Name = go != null ? go.name : "(Null)",
            PathKey = objectKey,
            Enabled = go != null && manager.GetGameObjectEnabled(go),
            Components = new List<SnapshotComponentNode>(),
            Children = new List<SnapshotGameObjectNode>()
        };

        if (go == null)
            return node;

        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            string componentName = component != null ? component.GetType().Name : "Missing Script";
            string componentKey = component != null
                ? DebugConsoleFilterKeyUtility.GetComponentKey(component)
                : $"{objectKey}|Missing Script#{i}";

            node.Components.Add(new SnapshotComponentNode
            {
                Name = componentName,
                Key = componentKey,
                Enabled = component == null || manager.GetComponentEnabled(component)
            });
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform child = go.transform.GetChild(i);
            if (child == null)
                continue;

            if (depth + 1 < MaxSnapshotHierarchyDepth)
                node.Children.Add(CaptureSnapshotNode(manager, child.gameObject, depth + 1));
        }

        return node;
    }

    /// <summary>
    /// 현재 상태에서 스냅샷 엔트리 대상 목록 정보를 수집해 스냅샷이나 캐시로 만든다.
    /// </summary>
    private void CaptureSnapshotEntryTargets(DebugEntry entry, out string gameObjectKey, out string componentKey, out string gameObjectName, out string componentName)
    {
        gameObjectKey = entry?.GameObjectKey ?? string.Empty;
        componentKey = entry?.ComponentKey ?? string.Empty;
        gameObjectName = entry?.GameObjectName ?? string.Empty;
        componentName = entry?.ComponentName ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(gameObjectKey))
            return;

        try
        {
            if (entry?.Context is GameObject go)
            {
                if (go == null)
                    return;

                gameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(go);
                gameObjectName = go.name;
                return;
            }

            if (entry?.Context is Component component)
            {
                if (component == null)
                    return;

                GameObject owner = component.gameObject;
                if (owner == null)
                    return;

                gameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(owner);
                componentKey = DebugConsoleFilterKeyUtility.GetComponentKey(component);
                gameObjectName = owner.name;
                componentName = component.GetType().Name;
            }
        }
        catch (MissingReferenceException)
        {
        }
    }

    /// <summary>
    /// 현재 상태에서 타입 filters 정보를 수집해 스냅샷이나 캐시로 만든다.
    /// </summary>
    private bool[] CaptureTypeFilters(DebugConsoleManager manager)
    {
        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        bool[] filters = new bool[types.Length];

        for (int i = 0; i < types.Length; i++)
            filters[i] = manager.GetTypeEnabled(types[i]);

        return filters;
    }

    /// <summary>
    /// 계층 경로 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        Stack<string> stack = new Stack<string>();
        Transform current = target;

        while (current != null)
        {
            stack.Push($"{current.name}[{current.GetSiblingIndex()}]");
            current = current.parent;
        }

        return string.Join("/", stack);
    }

    /// <summary>
    /// 스냅샷 file 경로 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetSnapshotFilePath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), SnapshotDirectoryPath, SnapshotFileName);
    }

    /// <summary>
    /// 스냅샷 to disk를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveSnapshotToDisk(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        try
        {
            snapshot.SnapshotVersion = CurrentSnapshotVersion;

            string filePath = GetSnapshotFilePath();
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllText(filePath, JsonUtility.ToJson(snapshot, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DebugConsoleEditorWindow snapshot save failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 스냅샷 from disk를 저장소에서 불러온다. 이전에 저장한 상태를 다시 적용하는 데 사용한다.
    /// </summary>
    private void LoadSnapshotFromDisk()
    {
        try
        {
            string filePath = GetSnapshotFilePath();
            if (!File.Exists(filePath))
            {
                _lastSnapshot = null;
                return;
            }

            string json = File.ReadAllText(filePath);
            _lastSnapshot = JsonUtility.FromJson<DebugConsoleEditorSnapshot>(json);
            if (!TryUpgradeSnapshotToCurrentVersion(_lastSnapshot))
            {
                _lastSnapshot = null;
                return;
            }

            ApplyStoredPreferencesToSnapshot(_lastSnapshot);
        }
        catch (Exception exception)
        {
            _lastSnapshot = null;
            Debug.LogWarning($"DebugConsoleEditorWindow snapshot load failed: {exception.Message}");
        }
    }


    /// <summary>
    /// upgrade 스냅샷 to current version 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
    private bool TryUpgradeSnapshotToCurrentVersion(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return false;

        if (snapshot.SnapshotVersion > CurrentSnapshotVersion)
            return false;

        NormalizeSnapshot(snapshot);
        snapshot.SnapshotVersion = CurrentSnapshotVersion;
        return true;
    }

    /// <summary>
    /// normalize 스냅샷 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void NormalizeSnapshot(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.Roots ??= new List<SnapshotGameObjectNode>();
        snapshot.Entries ??= new List<SnapshotLogEntry>();

        if (!snapshot.ShowLogLevelLog && !snapshot.ShowLogLevelWarning && !snapshot.ShowLogLevelError)
        {
            snapshot.ShowLogLevelLog = true;
            snapshot.ShowLogLevelWarning = true;
            snapshot.ShowLogLevelError = true;
        }

        for (int i = 0; i < snapshot.Roots.Count; i++)
            NormalizeSnapshotNode(snapshot.Roots[i]);

        for (int i = 0; i < snapshot.Entries.Count; i++)
        {
            SnapshotLogEntry entry = snapshot.Entries[i];
            if (entry == null)
                continue;

            entry.GameObjectKey ??= string.Empty;
            entry.ComponentKey ??= string.Empty;
            entry.GameObjectName ??= string.Empty;
            entry.ComponentName ??= string.Empty;
            entry.SceneKey ??= string.Empty;
            entry.HierarchyPath ??= string.Empty;
            entry.ComponentTypeName ??= string.Empty;
            entry.CapturedAtIsoUtc ??= string.Empty;
            entry.ColorHex ??= "#ffffff";
            entry.CallerFilePath ??= string.Empty;
            entry.SourceName ??= string.Empty;
            entry.MemberName ??= string.Empty;
            entry.Message ??= string.Empty;
            entry.Time ??= string.Empty;
        }
    }

    /// <summary>
    /// normalize 스냅샷 node 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void NormalizeSnapshotNode(SnapshotGameObjectNode node)
    {
        if (node == null)
            return;

        node.Name ??= string.Empty;
        node.PathKey ??= string.Empty;
        node.Components ??= new List<SnapshotComponentNode>();
        node.Children ??= new List<SnapshotGameObjectNode>();

        for (int i = 0; i < node.Components.Count; i++)
        {
            SnapshotComponentNode component = node.Components[i];
            if (component == null)
                continue;

            component.Name ??= string.Empty;
            component.Key ??= string.Empty;
        }

        for (int i = 0; i < node.Children.Count; i++)
            NormalizeSnapshotNode(node.Children[i]);
    }


    /// <summary>
    /// 에디터 ui 상태를 저장소에서 불러온다. 이전에 저장한 상태를 다시 적용하는 데 사용한다.
    /// </summary>
    private void LoadEditorUiState()
    {
        string raw = DebugConsolePreferenceStore.GetString(EditorStatePrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return;

        DebugConsoleEditorUiState state = JsonUtility.FromJson<DebugConsoleEditorUiState>(raw);
        if (state == null)
            return;

        _autoScroll = state.AutoScroll;
        _hideTransform = state.HideTransform;
        _collapsePreviousOnSelection = state.CollapsePreviousOnSelection;
        _collapseLogs = state.CollapseLogs;
        _showTypeFilterPanel = state.ShowTypeFilterPanel;
        _showLogDetails = state.ShowLogDetails;
        _stackTraceFoldout = state.StackTraceFoldout;
        _hierarchyPanelWidth = state.HierarchyPanelWidth > 0f ? state.HierarchyPanelWidth : _hierarchyPanelWidth;
        _logDetailPanelHeight = Mathf.Clamp(state.LogDetailPanelHeight > 0f ? state.LogDetailPanelHeight : DefaultLogDetailPanelHeight, MinLogDetailPanelHeight, MaxLogDetailPanelHeight);
        _hierarchyScroll = state.HierarchyScroll;
        _logScroll = state.LogScroll;
        _typeFilterScroll = state.TypeFilterScroll;
        _detailScroll = state.DetailScroll;
        _selectedLogIndex = state.SelectedLogIndex;
        _focusedSnapshotGameObjectKey = state.FocusedSnapshotGameObjectKey ?? string.Empty;
        _focusedSnapshotComponentKey = state.FocusedSnapshotComponentKey ?? string.Empty;
        _focusedObjectName = state.FocusedObjectName ?? string.Empty;
        _focusedComponentName = state.FocusedComponentName ?? string.Empty;

        _expandedSnapshotDetails.Clear();
        if (state.ExpandedSnapshotDetails != null)
        {
            for (int i = 0; i < state.ExpandedSnapshotDetails.Count; i++)
            {
                string key = state.ExpandedSnapshotDetails[i];
                if (!string.IsNullOrWhiteSpace(key))
                    _expandedSnapshotDetails.Add(key);
            }
        }

        _expandedSnapshotChildren.Clear();
        if (state.ExpandedSnapshotChildren != null)
        {
            for (int i = 0; i < state.ExpandedSnapshotChildren.Count; i++)
            {
                string key = state.ExpandedSnapshotChildren[i];
                if (!string.IsNullOrWhiteSpace(key))
                    _expandedSnapshotChildren.Add(key);
            }
        }
    }

    /// <summary>
    /// 에디터 ui 상태를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveEditorUiState()
    {
        DebugConsoleEditorUiState state = new DebugConsoleEditorUiState
        {
            AutoScroll = _autoScroll,
            HideTransform = _hideTransform,
            CollapsePreviousOnSelection = _collapsePreviousOnSelection,
            CollapseLogs = _collapseLogs,
            ShowTypeFilterPanel = _showTypeFilterPanel,
            ShowLogDetails = _showLogDetails,
            StackTraceFoldout = _stackTraceFoldout,
            HierarchyPanelWidth = _hierarchyPanelWidth,
            LogDetailPanelHeight = _logDetailPanelHeight,
            HierarchyScroll = _hierarchyScroll,
            LogScroll = _logScroll,
            TypeFilterScroll = _typeFilterScroll,
            DetailScroll = _detailScroll,
            SelectedLogIndex = _selectedLogIndex,
            FocusedSnapshotGameObjectKey = _focusedSnapshotGameObjectKey ?? string.Empty,
            FocusedSnapshotComponentKey = _focusedSnapshotComponentKey ?? string.Empty,
            FocusedObjectName = _focusedObjectName ?? string.Empty,
            FocusedComponentName = _focusedComponentName ?? string.Empty,
            ExpandedSnapshotDetails = new List<string>(_expandedSnapshotDetails),
            ExpandedSnapshotChildren = new List<string>(_expandedSnapshotChildren)
        };

        DebugConsolePreferenceStore.SetString(EditorStatePrefKey, JsonUtility.ToJson(state));
    }

    /// <summary>
    /// saved 스냅샷를 비우거나 초기화한다. 이전 상태를 제거하고 다음 작업을 준비하는 데 사용한다.
    /// </summary>
    private void ClearSavedSnapshot()
    {
        _lastSnapshot = null;
        _selectedLogIndex = -1;
        _hierarchyScroll = Vector2.zero;
        _logScroll = Vector2.zero;
        _expandedSnapshotDetails.Clear();
        _expandedSnapshotChildren.Clear();
        ClearFocus();
        SaveEditorUiState();

        try
        {
            string filePath = GetSnapshotFilePath();
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DebugConsoleEditorWindow snapshot delete failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 준비된 stored 환경설정 to 스냅샷 값을 실제 상태에 반영한다.
    /// </summary>
    private void ApplyStoredPreferencesToSnapshot(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.GlobalEnabled = DebugConsolePreferenceStore.GetBool(GlobalEnabledPrefKey, snapshot.GlobalEnabled);
        snapshot.MirrorToUnityConsole = DebugConsolePreferenceStore.GetBool(MirrorToUnityPrefKey, snapshot.MirrorToUnityConsole);
        snapshot.ShowLogLevelLog = DebugConsolePreferenceStore.GetBool(LogLevelLogPrefKey, snapshot.ShowLogLevelLog);
        snapshot.ShowLogLevelWarning = DebugConsolePreferenceStore.GetBool(LogLevelWarningPrefKey, snapshot.ShowLogLevelWarning);
        snapshot.ShowLogLevelError = DebugConsolePreferenceStore.GetBool(LogLevelErrorPrefKey, snapshot.ShowLogLevelError);

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        snapshot.TypeFilters ??= new bool[types.Length];
        if (snapshot.TypeFilters.Length != types.Length)
            Array.Resize(ref snapshot.TypeFilters, types.Length);

        for (int i = 0; i < types.Length; i++)
            snapshot.TypeFilters[i] = DebugConsolePreferenceStore.GetBool(GetTypePrefKey(types[i]), snapshot.TypeFilters[i]);

        if (snapshot.Roots == null)
            return;

        for (int i = 0; i < snapshot.Roots.Count; i++)
            ApplyStoredPreferencesToSnapshotNode(snapshot.Roots[i]);
    }

    /// <summary>
    /// 준비된 stored 환경설정 to 스냅샷 node 값을 실제 상태에 반영한다.
    /// </summary>
    private void ApplyStoredPreferencesToSnapshotNode(SnapshotGameObjectNode node)
    {
        if (node == null)
            return;

        if (!string.IsNullOrWhiteSpace(node.PathKey))
            node.Enabled = GetStoredGameObjectEnabled(node.PathKey, node.Enabled);

        if (node.Components != null)
        {
            for (int i = 0; i < node.Components.Count; i++)
            {
                SnapshotComponentNode component = node.Components[i];
                if (component == null || string.IsNullOrWhiteSpace(component.Key))
                    continue;

                component.Enabled = GetStoredComponentEnabled(component.Key, component.Enabled);
            }
        }

        if (node.Children == null)
            return;

        for (int i = 0; i < node.Children.Count; i++)
            ApplyStoredPreferencesToSnapshotNode(node.Children[i]);
    }

    /// <summary>
    /// 스냅샷 toolbar 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotToolbar(DebugConsoleEditorSnapshot snapshot)
    {
        float availableWidth = GetTopAreaWidth();

        int enabledCount = GetEnabledTypeCount(snapshot);
        int totalCount = Enum.GetValues(typeof(DebugType)).Length;
        string typeButtonLabel = _showTypeFilterPanel
            ? $"Type Filter ▲ ({enabledCount}/{totalCount})"
            : $"Type Filter ▼ ({enabledCount}/{totalCount})";

        if (availableWidth >= 1500f)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawSnapshotToolbarToggleGroup(snapshot);
            GUILayout.Space(8f);
            DrawSnapshotToolbarActionGroup(snapshot, typeButtonLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return;
        }

        DrawSnapshotToolbarToggleGroupWrapped(snapshot, availableWidth);
        DrawSnapshotToolbarActionGroupWrapped(snapshot, typeButtonLabel, availableWidth);
        GUILayout.Space(4f);
    }

    /// <summary>
    /// 스냅샷 타입 필터 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotTypeFilterPanel(DebugConsoleEditorSnapshot snapshot)
    {
        if (!_showTypeFilterPanel || snapshot == null)
            return;

        EditorGUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("DebugType Filter", _titleStyle);

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));

        const float minItemWidth = 120f;
        const float itemSpacing = 12f;
        float availableWidth = Mathf.Max(220f, position.width - 44f);
        int columns = Mathf.Clamp(Mathf.FloorToInt((availableWidth + itemSpacing) / (minItemWidth + itemSpacing)), 1, types.Length);
        float itemWidth = Mathf.Floor((availableWidth - itemSpacing * (columns - 1)) / columns);
        itemWidth = Mathf.Max(minItemWidth, itemWidth);

        int rows = Mathf.CeilToInt(types.Length / (float)columns);
        float viewHeight = Mathf.Min(120f, rows * 22f + Mathf.Max(0, rows - 1) * 4f + 6f);

        _typeFilterScroll = EditorGUILayout.BeginScrollView(_typeFilterScroll, GUILayout.Height(viewHeight));

        for (int row = 0; row < types.Length; row += columns)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < columns; col++)
            {
                int index = row + col;
                if (index >= types.Length)
                    break;

                DebugType type = types[index];
                bool current = GetSnapshotTypeEnabled(snapshot, type);
                bool next = GUILayout.Toggle(current, type.ToString(), GUILayout.Width(itemWidth));

                if (next != current)
                    SetSnapshotTypeEnabled(snapshot, type, next);

                if (col < columns - 1)
                    GUILayout.Space(itemSpacing);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        GUILayout.Space(4f);
    }

    /// <summary>
    /// 활성화 타입 개수 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private int GetEnabledTypeCount(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot?.TypeFilters == null)
            return 0;

        int count = 0;
        for (int i = 0; i < snapshot.TypeFilters.Length; i++)
        {
            if (snapshot.TypeFilters[i])
                count++;
        }

        return count;
    }

    /// <summary>
    /// 스냅샷 타입 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private bool GetSnapshotTypeEnabled(DebugConsoleEditorSnapshot snapshot, DebugType type)
    {
        if (snapshot?.TypeFilters == null)
            return true;

        int index = (int)type;
        if (index < 0 || index >= snapshot.TypeFilters.Length)
            return true;

        return snapshot.TypeFilters[index];
    }


    /// <summary>
    /// 스냅샷 레벨 활성화 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsSnapshotLevelEnabled(DebugConsoleEditorSnapshot snapshot, DebugLogLevel level)
    {
        if (snapshot == null)
            return true;

        return level switch
        {
            DebugLogLevel.Warning => snapshot.ShowLogLevelWarning,
            DebugLogLevel.Error => snapshot.ShowLogLevelError,
            _ => snapshot.ShowLogLevelLog
        };
    }

    /// <summary>
    /// set 스냅샷 레벨 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetSnapshotLevelEnabled(DebugConsoleEditorSnapshot snapshot, DebugLogLevel level, bool value)
    {
        if (snapshot == null)
            return;

        switch (level)
        {
            case DebugLogLevel.Warning:
                snapshot.ShowLogLevelWarning = value;
                DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, value);
                break;

            case DebugLogLevel.Error:
                snapshot.ShowLogLevelError = value;
                DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, value);
                break;

            default:
                snapshot.ShowLogLevelLog = value;
                DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, value);
                break;
        }

        SaveSnapshotIfAvailable();
    }

    /// <summary>
    /// set all 스냅샷 levels 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetAllSnapshotLevels(DebugConsoleEditorSnapshot snapshot, bool value)
    {
        if (snapshot == null)
            return;

        snapshot.ShowLogLevelLog = value;
        snapshot.ShowLogLevelWarning = value;
        snapshot.ShowLogLevelError = value;
        DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, value);
        DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, value);
        DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, value);
        SaveSnapshotIfAvailable();
    }

    /// <summary>
    /// set 스냅샷 경고 and 오류 only 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetSnapshotWarningAndErrorOnly(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.ShowLogLevelLog = false;
        snapshot.ShowLogLevelWarning = true;
        snapshot.ShowLogLevelError = true;
        DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, false);
        DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, true);
        DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, true);
        SaveSnapshotIfAvailable();
    }

    /// <summary>
    /// set 스냅샷 오류 only 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetSnapshotErrorOnly(DebugConsoleEditorSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        snapshot.ShowLogLevelLog = false;
        snapshot.ShowLogLevelWarning = false;
        snapshot.ShowLogLevelError = true;
        DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, false);
        DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, false);
        DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, true);
        SaveSnapshotIfAvailable();
    }
    /// <summary>
    /// set 스냅샷 타입 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetSnapshotTypeEnabled(DebugConsoleEditorSnapshot snapshot, DebugType type, bool value)
    {
        if (snapshot == null)
            return;

        int index = (int)type;
        if (snapshot.TypeFilters == null || index < 0)
            return;

        if (index >= snapshot.TypeFilters.Length)
            Array.Resize(ref snapshot.TypeFilters, index + 1);

        snapshot.TypeFilters[index] = value;
        DebugConsolePreferenceStore.SetBool(GetTypePrefKey(type), value);
        SaveSnapshotIfAvailable();
    }

    /// <summary>
    /// set all 스냅샷 types 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetAllSnapshotTypes(DebugConsoleEditorSnapshot snapshot, bool value)
    {
        if (snapshot == null)
            return;

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
        snapshot.TypeFilters ??= new bool[types.Length];
        if (snapshot.TypeFilters.Length != types.Length)
            Array.Resize(ref snapshot.TypeFilters, types.Length);

        for (int i = 0; i < types.Length; i++)
        {
            snapshot.TypeFilters[i] = value;
            DebugConsolePreferenceStore.SetBool(GetTypePrefKey(types[i]), value);
        }

        SaveSnapshotIfAvailable();
    }

    /// <summary>
    /// set 스냅샷 오브젝트 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetSnapshotGameObjectEnabled(SnapshotGameObjectNode node, bool value)
    {
        if (node == null)
            return;

        node.Enabled = value;
        SetStoredGameObjectEnabled(node.PathKey, value);
        SaveSnapshotIfAvailable();
    }

    /// <summary>
    /// set 스냅샷 컴포넌트 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetSnapshotComponentEnabled(SnapshotComponentNode component, bool value)
    {
        if (component == null)
            return;

        component.Enabled = value;
        SetStoredComponentEnabled(component.Key, value);
        SaveSnapshotIfAvailable();
    }

    /// <summary>
    /// 스냅샷 엔트리를 현재 포커스 대상으로 설정한다. 관련 선택 상태도 함께 갱신한다.
    /// </summary>
    private void FocusSnapshotEntry(SnapshotLogEntry entry)
    {
        if (entry == null)
            return;

        if (!string.IsNullOrWhiteSpace(entry.ComponentKey))
        {
            _focusedGameObjectId = 0;
            _focusedComponentId = 0;
            _focusedSnapshotGameObjectKey = entry.GameObjectKey ?? string.Empty;
            _focusedSnapshotComponentKey = entry.ComponentKey ?? string.Empty;
            _focusedObjectName = !string.IsNullOrWhiteSpace(entry.GameObjectName) ? entry.GameObjectName : entry.SourceName ?? string.Empty;
            _focusedComponentName = !string.IsNullOrWhiteSpace(entry.ComponentName) ? entry.ComponentName : string.Empty;
            PrepareSnapshotSelectionExpansion(_focusedSnapshotGameObjectKey, true);
            SaveEditorUiState();
            return;
        }

        if (!string.IsNullOrWhiteSpace(entry.GameObjectKey))
        {
            _focusedGameObjectId = 0;
            _focusedComponentId = 0;
            _focusedSnapshotGameObjectKey = entry.GameObjectKey ?? string.Empty;
            _focusedSnapshotComponentKey = string.Empty;
            _focusedObjectName = !string.IsNullOrWhiteSpace(entry.GameObjectName) ? entry.GameObjectName : entry.SourceName ?? string.Empty;
            _focusedComponentName = string.Empty;
            PrepareSnapshotSelectionExpansion(_focusedSnapshotGameObjectKey, true);
            SaveEditorUiState();
        }
    }

    /// <summary>
    /// 스냅샷 오브젝트 포커스 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleSnapshotGameObjectFocus(SnapshotGameObjectNode node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.PathKey))
            return;

        if (string.Equals(_focusedSnapshotGameObjectKey, node.PathKey, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
        {
            ClearFocus();
            return;
        }

        _focusedGameObjectId = 0;
        _focusedComponentId = 0;
        _focusedSnapshotGameObjectKey = node.PathKey;
        _focusedSnapshotComponentKey = string.Empty;
        _focusedObjectName = node.Name ?? string.Empty;
        _focusedComponentName = string.Empty;
        PrepareSnapshotSelectionExpansion(node.PathKey, true);
        SaveEditorUiState();
    }

    /// <summary>
    /// 스냅샷 컴포넌트 포커스 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleSnapshotComponentFocus(SnapshotGameObjectNode node, SnapshotComponentNode component)
    {
        if (node == null || component == null || string.IsNullOrWhiteSpace(component.Key))
            return;

        if (string.Equals(_focusedSnapshotComponentKey, component.Key, StringComparison.Ordinal))
        {
            ClearFocus();
            return;
        }

        _focusedGameObjectId = 0;
        _focusedComponentId = 0;
        _focusedSnapshotGameObjectKey = node.PathKey ?? string.Empty;
        _focusedSnapshotComponentKey = component.Key;
        _focusedObjectName = node.Name ?? string.Empty;
        _focusedComponentName = component.Name ?? string.Empty;
        PrepareSnapshotSelectionExpansion(_focusedSnapshotGameObjectKey, true);
        SaveEditorUiState();
    }

    /// <summary>
    /// prepare 스냅샷 선택 expansion 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void PrepareSnapshotSelectionExpansion(string objectKey, bool includeDetails)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return;

        if (_collapsePreviousOnSelection)
            PreserveSnapshotExpansionWithinTopLevelRoot(objectKey);

        ExpandSnapshotSelectionPath(objectKey, includeDetails);
    }

    /// <summary>
    /// expand 스냅샷 선택 경로 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void ExpandSnapshotSelectionPath(string objectKey, bool includeDetails)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return;

        if (includeDetails)
            _expandedSnapshotDetails.Add(objectKey);

        string current = objectKey;
        while (!string.IsNullOrWhiteSpace(current))
        {
            string parentKey = GetSnapshotParentObjectKey(current);
            if (string.IsNullOrWhiteSpace(parentKey))
                break;

            _expandedSnapshotDetails.Add(parentKey);
            _expandedSnapshotChildren.Add(parentKey);
            current = parentKey;
        }
    }

    /// <summary>
    /// preserve 스냅샷 expansion within top 레벨 root 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void PreserveSnapshotExpansionWithinTopLevelRoot(string objectKey)
    {
        string topLevelRootKey = GetSnapshotTopLevelRootKey(objectKey);
        if (string.IsNullOrWhiteSpace(topLevelRootKey))
        {
            _expandedSnapshotDetails.Clear();
            _expandedSnapshotChildren.Clear();
            return;
        }

        _expandedSnapshotDetails.RemoveWhere(key => !IsSameOrChildSnapshotObjectKey(key, topLevelRootKey));
        _expandedSnapshotChildren.RemoveWhere(key => !IsSameOrChildSnapshotObjectKey(key, topLevelRootKey));
    }

    /// <summary>
    /// same or 하위 스냅샷 오브젝트 식별 키 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsSameOrChildSnapshotObjectKey(string candidateKey, string ancestorKey)
    {
        if (string.IsNullOrWhiteSpace(candidateKey) || string.IsNullOrWhiteSpace(ancestorKey))
            return false;

        if (string.Equals(candidateKey, ancestorKey, StringComparison.Ordinal))
            return true;

        return candidateKey.StartsWith(ancestorKey + "/", StringComparison.Ordinal);
    }

    /// <summary>
    /// 스냅샷 top 레벨 root 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetSnapshotTopLevelRootKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return string.Empty;

        int separatorIndex = objectKey.IndexOf('|');
        if (separatorIndex < 0)
            return objectKey;

        string sceneKey = objectKey.Substring(0, separatorIndex);
        string hierarchyPath = objectKey.Substring(separatorIndex + 1);
        if (string.IsNullOrWhiteSpace(hierarchyPath))
            return objectKey;

        int slashIndex = hierarchyPath.IndexOf('/');
        string topLevelSegment = slashIndex >= 0 ? hierarchyPath.Substring(0, slashIndex) : hierarchyPath;
        return $"{sceneKey}|{topLevelSegment}";
    }

    /// <summary>
    /// 스냅샷 상위 오브젝트 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetSnapshotParentObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return string.Empty;

        int separatorIndex = objectKey.IndexOf('|');
        if (separatorIndex < 0)
            return string.Empty;

        string sceneKey = objectKey.Substring(0, separatorIndex);
        string hierarchyPath = objectKey.Substring(separatorIndex + 1);
        if (string.IsNullOrWhiteSpace(hierarchyPath))
            return string.Empty;

        int lastSlashIndex = hierarchyPath.LastIndexOf('/');
        if (lastSlashIndex < 0)
            return string.Empty;

        return $"{sceneKey}|{hierarchyPath.Substring(0, lastSlashIndex)}";
    }

    /// <summary>
    /// 스냅샷 오브젝트 focused 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsSnapshotObjectFocused(string objectKey)
    {
        return !string.IsNullOrWhiteSpace(objectKey) &&
               string.Equals(_focusedSnapshotGameObjectKey, objectKey, StringComparison.Ordinal) &&
               string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey);
    }

    /// <summary>
    /// 스냅샷 컴포넌트 focused 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsSnapshotComponentFocused(string componentKey)
    {
        return !string.IsNullOrWhiteSpace(componentKey) &&
               string.Equals(_focusedSnapshotComponentKey, componentKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// 스냅샷 focused 오브젝트 상위 여부를 판정한다. 조건 분기에 사용할 수 있도록 bool 값을 반환한다.
    /// </summary>
    private bool IsSnapshotFocusedObjectParent(string objectKey)
    {
        return !string.IsNullOrWhiteSpace(objectKey) &&
               !string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey) &&
               string.Equals(_focusedSnapshotGameObjectKey, objectKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// stored 오브젝트 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private bool GetStoredGameObjectEnabled(string filterKey, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return defaultValue;

        return DebugConsolePreferenceStore.GetBool(GetGameObjectPrefKey(filterKey), defaultValue);
    }

    /// <summary>
    /// stored 컴포넌트 활성화 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private bool GetStoredComponentEnabled(string filterKey, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return defaultValue;

        return DebugConsolePreferenceStore.GetBool(GetComponentPrefKey(filterKey), defaultValue);
    }

    /// <summary>
    /// set stored 오브젝트 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetStoredGameObjectEnabled(string filterKey, bool value)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return;

        string prefKey = GetGameObjectPrefKey(filterKey);
        RegisterStoredPrefKey(GameObjectRegistryPrefKey, prefKey);
        DebugConsolePreferenceStore.SetBool(prefKey, value);
    }

    /// <summary>
    /// set stored 컴포넌트 활성화 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void SetStoredComponentEnabled(string filterKey, bool value)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
            return;

        string prefKey = GetComponentPrefKey(filterKey);
        RegisterStoredPrefKey(ComponentRegistryPrefKey, prefKey);
        DebugConsolePreferenceStore.SetBool(prefKey, value);
    }

    /// <summary>
    /// register stored pref 식별 키 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void RegisterStoredPrefKey(string registryPrefKey, string prefKey)
    {
        if (string.IsNullOrWhiteSpace(registryPrefKey) || string.IsNullOrWhiteSpace(prefKey))
            return;

        HashSet<string> registry = LoadStoredRegistry(registryPrefKey);
        if (!registry.Add(prefKey))
            return;

        SaveStoredRegistry(registryPrefKey, registry);
    }

    /// <summary>
    /// stored registry를 저장소에서 불러온다. 이전에 저장한 상태를 다시 적용하는 데 사용한다.
    /// </summary>
    private HashSet<string> LoadStoredRegistry(string registryPrefKey)
    {
        HashSet<string> result = new HashSet<string>();
        string raw = DebugConsolePreferenceStore.GetString(registryPrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            result.Add(parts[i]);

        return result;
    }

    /// <summary>
    /// stored registry를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveStoredRegistry(string registryPrefKey, HashSet<string> registry)
    {
        if (registry == null || registry.Count == 0)
        {
            DebugConsolePreferenceStore.DeleteKey(registryPrefKey);
            return;
        }

        DebugConsolePreferenceStore.SetString(registryPrefKey, string.Join("\n", registry));
    }

    /// <summary>
    /// 타입 pref 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetTypePrefKey(DebugType type)
    {
        return TypePrefKeyPrefix + type;
    }

    /// <summary>
    /// 오브젝트 pref 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetGameObjectPrefKey(string filterKey)
    {
        return GameObjectPrefKeyPrefix + filterKey;
    }

    /// <summary>
    /// 컴포넌트 pref 식별 키 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetComponentPrefKey(string filterKey)
    {
        return ComponentPrefKeyPrefix + filterKey;
    }

    /// <summary>
    /// 스냅샷 if available를 저장소에 기록한다. 다음 실행에서도 같은 상태를 복원하기 위해 사용한다.
    /// </summary>
    private void SaveSnapshotIfAvailable()
    {
        SaveEditorUiState();

        if (_lastSnapshot != null)
            SaveSnapshotToDisk(_lastSnapshot);
    }

    /// <summary>
    /// 스냅샷 toolbar 토글 그룹 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotToolbarToggleGroup(DebugConsoleEditorSnapshot snapshot)
    {
        bool global = GUILayout.Toggle(snapshot.GlobalEnabled, "Global", GUILayout.Width(80f));
        if (global != snapshot.GlobalEnabled)
        {
            snapshot.GlobalEnabled = global;
            DebugConsolePreferenceStore.SetBool(GlobalEnabledPrefKey, global);
            SaveSnapshotIfAvailable();
        }

        bool mirror = GUILayout.Toggle(snapshot.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
        if (mirror != snapshot.MirrorToUnityConsole)
        {
            snapshot.MirrorToUnityConsole = mirror;
            DebugConsolePreferenceStore.SetBool(MirrorToUnityPrefKey, mirror);
            SaveSnapshotIfAvailable();
        }

        bool autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
        if (autoScroll != _autoScroll)
        {
            _autoScroll = autoScroll;
            SaveEditorUiState();
        }

        bool hideTransform = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
        if (hideTransform != _hideTransform)
        {
            _hideTransform = hideTransform;
            SaveEditorUiState();
        }

        bool collapsePrevious = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
        if (collapsePrevious != _collapsePreviousOnSelection)
        {
            _collapsePreviousOnSelection = collapsePrevious;
            SaveEditorUiState();
        }

        bool collapseLogs = GUILayout.Toggle(_collapseLogs, "Collapse Logs", GUILayout.Width(120f));
        if (collapseLogs != _collapseLogs)
        {
            _collapseLogs = collapseLogs;
            SaveEditorUiState();
        }

        bool showLogs = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Log), "Log", GUILayout.Width(70f));
        if (showLogs != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Log))
            SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Log, showLogs);

        bool showWarnings = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning), "Warn", GUILayout.Width(75f));
        if (showWarnings != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning))
            SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning, showWarnings);

        bool showErrors = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Error), "Error", GUILayout.Width(75f));
        if (showErrors != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Error))
            SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Error, showErrors);
    }

    /// <summary>
    /// 스냅샷 toolbar 토글 그룹 wrapped 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotToolbarToggleGroupWrapped(DebugConsoleEditorSnapshot snapshot, float availableWidth)
    {
        for (int index = 0; index < _toolbarToggleItems.Length;)
        {
            int endIndex = GetWrappedEndIndex(_toolbarToggleItems, index, availableWidth);
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawSnapshotToolbarToggleItems(snapshot, index, endIndex);
            EditorGUILayout.EndHorizontal();
            index = endIndex;
        }
    }

    /// <summary>
    /// 스냅샷 toolbar 토글 items 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotToolbarToggleItems(DebugConsoleEditorSnapshot snapshot, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            switch (_toolbarToggleItems[i].label)
            {
                case "Global":
                {
                    bool value = GUILayout.Toggle(snapshot.GlobalEnabled, "Global", GUILayout.Width(80f));
                    if (value != snapshot.GlobalEnabled)
                    {
                        snapshot.GlobalEnabled = value;
                        DebugConsolePreferenceStore.SetBool(GlobalEnabledPrefKey, value);
                        SaveSnapshotIfAvailable();
                    }
                    break;
                }

                case "Mirror Unity":
                {
                    bool value = GUILayout.Toggle(snapshot.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
                    if (value != snapshot.MirrorToUnityConsole)
                    {
                        snapshot.MirrorToUnityConsole = value;
                        DebugConsolePreferenceStore.SetBool(MirrorToUnityPrefKey, value);
                        SaveSnapshotIfAvailable();
                    }
                    break;
                }

                case "Auto Scroll":
                {
                    bool value = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
                    if (value != _autoScroll)
                    {
                        _autoScroll = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Hide Transform":
                {
                    bool value = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
                    if (value != _hideTransform)
                    {
                        _hideTransform = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Collapse Prev":
                {
                    bool value = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
                    if (value != _collapsePreviousOnSelection)
                    {
                        _collapsePreviousOnSelection = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Collapse Logs":
                {
                    bool value = GUILayout.Toggle(_collapseLogs, "Collapse Logs", GUILayout.Width(120f));
                    if (value != _collapseLogs)
                    {
                        _collapseLogs = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Log":
                {
                    bool value = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Log), "Log", GUILayout.Width(70f));
                    if (value != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Log))
                        SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Log, value);
                    break;
                }

                case "Warn":
                {
                    bool value = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning), "Warn", GUILayout.Width(75f));
                    if (value != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning))
                        SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Warning, value);
                    break;
                }

                case "Error":
                {
                    bool value = GUILayout.Toggle(IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Error), "Error", GUILayout.Width(75f));
                    if (value != IsSnapshotLevelEnabled(snapshot, DebugLogLevel.Error))
                        SetSnapshotLevelEnabled(snapshot, DebugLogLevel.Error, value);
                    break;
                }
            }

            if (i < endIndex - 1)
                GUILayout.Space(8f);
        }
    }

/// <summary>
/// 스냅샷 toolbar action 그룹 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawSnapshotToolbarActionGroup(DebugConsoleEditorSnapshot snapshot, string typeButtonLabel)
{
    if (GUILayout.Button(typeButtonLabel, GUILayout.Width(160f)))
    {
        _showTypeFilterPanel = !_showTypeFilterPanel;
        SaveEditorUiState();
    }

    if (GUILayout.Button("All Types On", GUILayout.Width(100f)))
        SetAllSnapshotTypes(snapshot, true);

    if (GUILayout.Button("All Types Off", GUILayout.Width(100f)))
        SetAllSnapshotTypes(snapshot, false);

    if (GUILayout.Button("All Levels", GUILayout.Width(100f)))
        SetAllSnapshotLevels(snapshot, true);

    if (GUILayout.Button("Warn+", GUILayout.Width(80f)))
        SetSnapshotWarningAndErrorOnly(snapshot);

    if (GUILayout.Button("Error Only", GUILayout.Width(100f)))
        SetSnapshotErrorOnly(snapshot);

    if (GUILayout.Button("Clear Logs", GUILayout.Width(100f)))
    {
        snapshot.Entries?.Clear();
        _selectedLogIndex = -1;
        SaveSnapshotIfAvailable();
    }

    if (GUILayout.Button("Clear Focus", GUILayout.Width(100f)))
        ClearFocus();

    if (GUILayout.Button("Reset Filters", GUILayout.Width(110f)))
        ResetStoredManagerPreferencesToDefault(snapshot);

    if (GUILayout.Button("Reset Layout", GUILayout.Width(110f)))
        ResetEditorLayoutToDefault();

    if (GUILayout.Button("Export Settings", GUILayout.Width(120f)))
        ExportEditorSettingsToFile();

    if (GUILayout.Button("Import Settings", GUILayout.Width(120f)))
        ImportEditorSettingsFromFile(snapshot);
}

/// <summary>
/// 스냅샷 toolbar action 그룹 wrapped 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawSnapshotToolbarActionGroupWrapped(DebugConsoleEditorSnapshot snapshot, string typeButtonLabel, float availableWidth)
    {
        for (int index = 0; index < _toolbarActionItems.Length;)
        {
            int endIndex = GetWrappedEndIndex(_toolbarActionItems, index, availableWidth);
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawSnapshotToolbarActionItems(snapshot, typeButtonLabel, index, endIndex);
            EditorGUILayout.EndHorizontal();
            index = endIndex;
        }
    }

    /// <summary>
    /// 스냅샷 toolbar action items 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSnapshotToolbarActionItems(DebugConsoleEditorSnapshot snapshot, string typeButtonLabel, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            string key = _toolbarActionItems[i].label;
            float width = _toolbarActionItems[i].width;

            switch (key)
            {
                case "TypeFilter":
                    if (GUILayout.Button(typeButtonLabel, GUILayout.Width(width)))
                    {
                        _showTypeFilterPanel = !_showTypeFilterPanel;
                        SaveEditorUiState();
                    }
                    break;

                case "All Types On":
                    if (GUILayout.Button("All Types On", GUILayout.Width(width)))
                        SetAllSnapshotTypes(snapshot, true);
                    break;

                case "All Types Off":
                    if (GUILayout.Button("All Types Off", GUILayout.Width(width)))
                        SetAllSnapshotTypes(snapshot, false);
                    break;

                case "All Levels":
                    if (GUILayout.Button("All Levels", GUILayout.Width(width)))
                        SetAllSnapshotLevels(snapshot, true);
                    break;

                case "Warn+":
                    if (GUILayout.Button("Warn+", GUILayout.Width(width)))
                        SetSnapshotWarningAndErrorOnly(snapshot);
                    break;

                case "Error Only":
                    if (GUILayout.Button("Error Only", GUILayout.Width(width)))
                        SetSnapshotErrorOnly(snapshot);
                    break;

                case "Clear Logs":
                    if (GUILayout.Button("Clear Logs", GUILayout.Width(width)))
                    {
                        snapshot.Entries?.Clear();
                        _selectedLogIndex = -1;
                        SaveSnapshotIfAvailable();
                    }
                    break;

                case "Clear Focus":
                    if (GUILayout.Button("Clear Focus", GUILayout.Width(width)))
                        ClearFocus();
                    break;

                case "Reset Filters":
                    if (GUILayout.Button("Reset Filters", GUILayout.Width(width)))
                        ResetStoredManagerPreferencesToDefault(null);
                    break;

                case "Reset Layout":
                    if (GUILayout.Button("Reset Layout", GUILayout.Width(width)))
                        ResetEditorLayoutToDefault();
                    break;

                case "Export Settings":
                    if (GUILayout.Button("Export Settings", GUILayout.Width(width)))
                        ExportEditorSettingsToFile();
                    break;

                case "Import Settings":
                    if (GUILayout.Button("Import Settings", GUILayout.Width(width)))
                        ImportEditorSettingsFromFile(_lastSnapshot);
                    break;
            }

            if (i < endIndex - 1)
                GUILayout.Space(8f);
        }
    }

    /// <summary>
    /// init 스타일 목록 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void InitStyles()
    {
        if (!_stylesDirty && _titleStyle != null)
            return;

        _stylesDirty = false;

        _titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            wordWrap = false,
            clipping = TextClipping.Clip
        };

        _boxStyle = new GUIStyle("box")
        {
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(8, 8, 8, 8)
        };

        _richLabelStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true,
            fontSize = 12
        };

        _dimLabelStyle = new GUIStyle(EditorStyles.label);
        _dimLabelStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);

        _searchTextFieldStyle = new GUIStyle(EditorStyles.textField)
        {
            fontSize = 12
        };

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

        _toolbarInfoLabelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
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

        _footerLeftLabelStyle = new GUIStyle(EditorStyles.label)
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
    /// toolbar 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawToolbar(DebugConsoleManager manager)
    {
        float availableWidth = GetTopAreaWidth();

        int enabledCount = GetEnabledTypeCount(manager);
        int totalCount = Enum.GetValues(typeof(DebugType)).Length;
        string typeButtonLabel = _showTypeFilterPanel
            ? $"Type Filter ▲ ({enabledCount}/{totalCount})"
            : $"Type Filter ▼ ({enabledCount}/{totalCount})";

        if (availableWidth >= 1500f)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawToolbarToggleGroup(manager);
            GUILayout.Space(8f);
            DrawToolbarActionGroup(manager, typeButtonLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return;
        }

        DrawToolbarToggleGroupWrapped(manager, availableWidth);
        DrawToolbarActionGroupWrapped(manager, typeButtonLabel, availableWidth);
        GUILayout.Space(4f);
    }

    /// <summary>
    /// 검색 bar 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawSearchBar()
    {
        float availableWidth = GetTopAreaWidth();

        if (availableWidth >= 760f)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
            float hierarchyWidth = Mathf.Clamp((availableWidth - 120f) * 0.38f, 160f, 260f);
            float logWidth = Mathf.Clamp((availableWidth - hierarchyWidth) - 24f, 220f, 320f);
            DrawHierarchySearchField(hierarchyWidth);
            GUILayout.Space(12f);
            DrawLogSearchField(logWidth);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return;
        }

        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
        DrawHierarchySearchField(availableWidth);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
        DrawLogSearchField(availableWidth);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4f);
    }

    /// <summary>
    /// 타입 필터 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawTypeFilterPanel(DebugConsoleManager manager)
    {
        if (!_showTypeFilterPanel)
            return;

        EditorGUILayout.BeginVertical(_boxStyle);
        GUILayout.Label("DebugType Filter", _titleStyle);

        DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));

        const float minItemWidth = 120f;
        const float itemSpacing = 12f;
        float availableWidth = Mathf.Max(220f, position.width - 44f);
        int columns = Mathf.Clamp(Mathf.FloorToInt((availableWidth + itemSpacing) / (minItemWidth + itemSpacing)), 1, types.Length);
        float itemWidth = Mathf.Floor((availableWidth - itemSpacing * (columns - 1)) / columns);
        itemWidth = Mathf.Max(minItemWidth, itemWidth);

        int rows = Mathf.CeilToInt(types.Length / (float)columns);
        float viewHeight = Mathf.Min(120f, rows * 22f + Mathf.Max(0, rows - 1) * 4f + 6f);

        _typeFilterScroll = EditorGUILayout.BeginScrollView(_typeFilterScroll, GUILayout.Height(viewHeight));

        for (int row = 0; row < types.Length; row += columns)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < columns; col++)
            {
                int index = row + col;
                if (index >= types.Length)
                    break;

                DebugType type = types[index];
                bool current = manager.GetTypeEnabled(type);
                bool next = GUILayout.Toggle(current, type.ToString(), GUILayout.Width(itemWidth));

                if (next != current)
                    manager.SetTypeEnabled(type, next);

                if (col < columns - 1)
                    GUILayout.Space(itemSpacing);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        GUILayout.Space(4f);
    }

    /// <summary>
    /// resizable panels 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawResizablePanels(DebugConsoleManager manager)
    {
        float contentWidth = Mathf.Max(620f, position.width - 24f);
        float maxHierarchyPanelWidth = Mathf.Max(MinHierarchyPanelWidth, contentWidth - MinLogPanelWidth - PanelSplitterWidth);

        if (_hierarchyPanelWidth <= 0f)
            _hierarchyPanelWidth = contentWidth * 0.42f;

        _hierarchyPanelWidth = Mathf.Clamp(_hierarchyPanelWidth, MinHierarchyPanelWidth, maxHierarchyPanelWidth);
        float logPanelWidth = Mathf.Max(MinLogPanelWidth, contentWidth - _hierarchyPanelWidth - PanelSplitterWidth);

        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawHierarchyPanel(manager, _hierarchyPanelWidth);
        DrawPanelSplitter(contentWidth);
        DrawLogPanel(manager, logPanelWidth);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 패널 splitter 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawPanelSplitter(float contentWidth)
    {
        Rect splitterRect = GUILayoutUtility.GetRect(PanelSplitterWidth, 10f, GUILayout.Width(PanelSplitterWidth), GUILayout.ExpandHeight(true));
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

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
            Repaint();
        }

        if (_isDraggingPanelSplitter && (current.type == EventType.MouseUp || current.rawType == EventType.MouseUp))
        {
            _isDraggingPanelSplitter = false;
            DebugConsolePreferenceStore.SetFloat(HierarchyPanelWidthPrefKey, _hierarchyPanelWidth);
            SaveEditorUiState();
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
        EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
        DrawHierarchySearchField(panelWidth - 20f);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4f);

        _hierarchyScroll = GUILayout.BeginScrollView(_hierarchyScroll);

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
            DrawGameObjectNode(manager, roots[i], 0, panelWidth);

        GUILayout.EndScrollView();

        GUILayout.Space(4f);
        string footerCountText = $"Count : {GetVisibleEntryCount(manager)}";
        GUILayout.BeginHorizontal(_boxStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(28f));
        GUILayout.Space(10f);
        GUILayout.Label(new GUIContent(footerCountText, footerCountText), _footerLeftLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(20f));
        GUILayout.Space(10f);
        GUILayout.EndHorizontal();
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
                ToggleLiveExpandedDetails(go);
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
                ToggleLiveExpandedChildren(go);

            string childFoldoutLabel = showChildren ? "▾" : "▸";
            if (GUILayout.Button(childFoldoutLabel, _foldoutButtonStyle, GUILayout.Width(HierarchyFoldoutSize), GUILayout.Height(HierarchyRowHeight)))
                ToggleLiveExpandedChildren(go);

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
    EditorGUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));
    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Logs", _titleStyle, GUILayout.ExpandWidth(true));
    bool nextShowLogDetails = GUILayout.Toggle(_showLogDetails, "Details", GUILayout.Width(70f));
    if (nextShowLogDetails != _showLogDetails)
    {
        _showLogDetails = nextShowLogDetails;
        SaveEditorUiState();
    }
    EditorGUILayout.EndHorizontal();
    GUILayout.Space(4f);
    EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
    DrawLogSearchField(panelWidth - 20f);
    EditorGUILayout.EndHorizontal();
    GUILayout.Space(4f);

    float width = Mathf.Max(GetLogContentWidth(panelWidth), 180f);
    float listViewportHeight = Mathf.Max(120f, position.height - (_showLogDetails ? _logDetailPanelHeight + 240f : 190f));

    GetVisibleLiveLogGroupsAndHeights(manager, width, out List<LiveLogGroup> groups, out List<float> rowHeights);
    CalculateVisibleRange(rowHeights, _logScroll.y, listViewportHeight, out int startIndex, out int endIndex, out float topPadding, out float visibleHeight, out float totalHeight);

    _logScroll = GUILayout.BeginScrollView(_logScroll, false, !_autoScroll, GUIStyle.none, GetLogVerticalScrollbarStyle(), GUILayout.MinHeight(listViewportHeight), GUILayout.ExpandHeight(true));

    if (topPadding > 0f)
        GUILayout.Space(topPadding);

    for (int i = startIndex; i < endIndex; i++)
    {
        LiveLogGroup group = groups[i];
        DrawLogEntry(group.Entry, group.LastSourceIndex, width, group.Count);
        GUILayout.Space(4f);
    }

    float bottomPadding = Mathf.Max(0f, totalHeight - topPadding - visibleHeight);
    if (bottomPadding > 0f)
        GUILayout.Space(bottomPadding);

    EditorGUILayout.EndScrollView();

    Rect scrollRect = GUILayoutUtility.GetLastRect();
    _lastLogViewportHeight = scrollRect.height;
    _lastLogContentHeight = totalHeight;
    _lastMaxLogScrollY = Mathf.Max(0f, _lastLogContentHeight - _lastLogViewportHeight);

    if (Event.current.type == EventType.Repaint && _autoScroll)
        _logScroll.y = _lastMaxLogScrollY + 4f;

    if (_showLogDetails)
    {
        GUILayout.Space(4f);
        DebugEntry selectedEntry = GetSelectedLiveEntry(manager);
        DrawLiveLogDetailPanel(selectedEntry, panelWidth);
    }

    EditorGUILayout.EndVertical();
}

/// <summary>
/// 로그 엔트리 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private float DrawLogEntry(DebugEntry entry, int sourceIndex, float width, int repeatCount)
    {
        GUIContent content = BuildCollapsedLogContent(UseCompactLogRows ? entry.SummaryRichText : entry.RichText, repeatCount);
        float rowHeight = UseCompactLogRows ? CompactLogRowHeight : _richLabelStyle.CalcHeight(content, width) + 12f;

        Rect rect = GUILayoutUtility.GetRect(10f, rowHeight, GUILayout.ExpandWidth(true));

        Color previousColor = GUI.color;
        if (sourceIndex == _selectedLogIndex)
            GUI.color = new Color(0.75f, 0.85f, 1f, 1f);

        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f);
        GUI.Label(labelRect, content, _richLabelStyle);

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            rect.Contains(Event.current.mousePosition))
        {
            _selectedLogIndex = sourceIndex;
            FocusEntry(entry);
            SaveEditorUiState();

            if (Event.current.clickCount >= 2)
                OpenEntryScript(entry);

            Event.current.Use();
        }

        return rect.height;
    }

    /// <summary>
    /// 스냅샷 compact 리치 텍스트 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildSnapshotCompactRichText(SnapshotLogEntry entry)
    {
        if (entry == null)
            return string.Empty;

        return $"<color={entry.ColorHex}>[{entry.Time}] [{entry.Type}] {entry.Message}</color> <color=#daa520>| [{entry.SourceName}.{entry.MemberName} : {entry.LineNumber}]</color>";
    }

/// <summary>
/// 스냅샷 행 heights 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
/// </summary>
private List<float> BuildSnapshotRowHeights(List<SnapshotLogGroup> groups, float width)
{
    List<float> heights = new List<float>(groups.Count);
    float rowHeight = UseCompactLogRows ? CompactLogRowHeight : 0f;

    for (int i = 0; i < groups.Count; i++)
    {
        if (UseCompactLogRows)
        {
            heights.Add(rowHeight);
            continue;
        }

        SnapshotLogGroup group = groups[i];
        string key = $"{group.LastSourceIndex}:{group.Count}:{Mathf.RoundToInt(width)}";
        if (!_snapshotLogHeightCache.TryGetValue(key, out float height))
        {
            GUIContent content = BuildCollapsedLogContent(group.Entry.RichText, group.Count);
            height = _richLabelStyle.CalcHeight(content, width) + 16f;
            _snapshotLogHeightCache[key] = height;
        }

        heights.Add(height);
    }

    return heights;
}

/// <summary>
/// live 행 heights 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
/// </summary>
private List<float> BuildLiveRowHeights(List<LiveLogGroup> groups, float width)
{
    List<float> heights = new List<float>(groups.Count);
    float rowHeight = UseCompactLogRows ? CompactLogRowHeight : 0f;

    for (int i = 0; i < groups.Count; i++)
    {
        if (UseCompactLogRows)
        {
            heights.Add(rowHeight);
            continue;
        }

        LiveLogGroup group = groups[i];
        long sequence = group.Entry != null ? group.Entry.SequenceId : i;
        string key = $"{sequence}:{group.Count}:{Mathf.RoundToInt(width)}";
        if (!_liveLogHeightCache.TryGetValue(key, out float height))
        {
            GUIContent content = BuildCollapsedLogContent(group.Entry.RichText, group.Count);
            height = _richLabelStyle.CalcHeight(content, width) + 16f;
            _liveLogHeightCache[key] = height;
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
            startIndex = i;
            topPadding = rowStart;
            started = true;
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
/// selected 스냅샷 엔트리 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
/// </summary>
private SnapshotLogEntry GetSelectedSnapshotEntry(DebugConsoleEditorSnapshot snapshot)
{
    if (snapshot?.Entries == null || _selectedLogIndex < 0 || _selectedLogIndex >= snapshot.Entries.Count)
        return null;

    return snapshot.Entries[_selectedLogIndex];
}

/// <summary>
/// selected live 엔트리 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
/// </summary>
private DebugEntry GetSelectedLiveEntry(DebugConsoleManager manager)
{
    if (manager == null)
        return null;

    IReadOnlyList<DebugEntry> entries = manager.Entries;
    if (_selectedLogIndex < 0 || _selectedLogIndex >= entries.Count)
        return null;

    return entries[_selectedLogIndex];
}

/// <summary>
/// 스냅샷 로그 상세 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawSnapshotLogDetailPanel(SnapshotLogEntry entry, float panelWidth)
{
    EditorGUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.Height(_logDetailPanelHeight));
    GUILayout.Label("Log Detail", _titleStyle);

    if (entry == null)
    {
        EditorGUILayout.HelpBox("로그를 선택하면 상세 정보가 표시됩니다.", MessageType.Info);
        EditorGUILayout.EndVertical();
        return;
    }

    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.Height(_logDetailPanelHeight - 28f));
    DrawDetailHeader(entry.Time, entry.Type.ToString(), entry.Level.ToString(), entry.SourceName, entry.MemberName, entry.LineNumber, entry.SceneKey, entry.HierarchyPath, entry.GameObjectName, entry.ComponentName, entry.ComponentTypeName, entry.FrameCount, entry.CapturedAtIsoUtc);
    DrawDetailBody(entry.Message, entry.CallerFilePath, entry.StackTrace);
    EditorGUILayout.EndScrollView();
    EditorGUILayout.EndVertical();
}

/// <summary>
/// live 로그 상세 패널 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawLiveLogDetailPanel(DebugEntry entry, float panelWidth)
{
    EditorGUILayout.BeginVertical(_boxStyle, GUILayout.Width(panelWidth), GUILayout.Height(_logDetailPanelHeight));
    GUILayout.Label("Log Detail", _titleStyle);

    if (entry == null)
    {
        EditorGUILayout.HelpBox("로그를 선택하면 상세 정보가 표시됩니다.", MessageType.Info);
        EditorGUILayout.EndVertical();
        return;
    }

    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.Height(_logDetailPanelHeight - 28f));
    DrawDetailHeader(entry.Time, entry.Type.ToString(), entry.Level.ToString(), entry.SourceName, entry.MemberName, entry.LineNumber, entry.SceneKey, entry.HierarchyPath, entry.GameObjectName, entry.ComponentName, entry.ComponentTypeName, entry.FrameCount, entry.CapturedAtIsoUtc);
    DrawDetailBody(entry.Message, entry.CallerFilePath, entry.StackTrace);
    EditorGUILayout.EndScrollView();
    EditorGUILayout.EndVertical();
}

/// <summary>
/// 상세 header 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawDetailHeader(string time, string type, string level, string sourceName, string memberName, int lineNumber, string sceneKey, string hierarchyPath, string gameObjectName, string componentName, string componentTypeName, int frameCount, string capturedAtIsoUtc)
{
    EditorGUILayout.LabelField("Time", string.IsNullOrWhiteSpace(time) ? "-" : time);
    EditorGUILayout.LabelField("Type", $"{type} / {level}");
    EditorGUILayout.LabelField("Source", string.IsNullOrWhiteSpace(sourceName) ? "-" : sourceName);
    EditorGUILayout.LabelField("Member", $"{memberName} : {Mathf.Max(1, lineNumber)}");
    EditorGUILayout.LabelField("Scene", string.IsNullOrWhiteSpace(sceneKey) ? "-" : sceneKey);
    EditorGUILayout.LabelField("Path", string.IsNullOrWhiteSpace(hierarchyPath) ? "-" : hierarchyPath);
    EditorGUILayout.LabelField("GameObject", string.IsNullOrWhiteSpace(gameObjectName) ? "-" : gameObjectName);
    EditorGUILayout.LabelField("Component", string.IsNullOrWhiteSpace(componentName) ? "-" : componentName);
    if (!string.IsNullOrWhiteSpace(componentTypeName))
        EditorGUILayout.LabelField("Component Type", componentTypeName);
    EditorGUILayout.LabelField("Frame", frameCount.ToString());
    EditorGUILayout.LabelField("Captured", string.IsNullOrWhiteSpace(capturedAtIsoUtc) ? "-" : capturedAtIsoUtc);
    GUILayout.Space(4f);
}

/// <summary>
/// 상세 body 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawDetailBody(string message, string callerFilePath, string stackTrace)
{
    GUILayout.Label("Message");
    bool previousEnabled = GUI.enabled;
    GUI.enabled = false;
    EditorGUILayout.TextArea(message ?? string.Empty, GUILayout.MinHeight(68f));
    GUI.enabled = previousEnabled;

    if (!string.IsNullOrWhiteSpace(callerFilePath))
        EditorGUILayout.LabelField("Caller File", callerFilePath);

    bool nextFoldout = EditorGUILayout.Foldout(_stackTraceFoldout, "Stack Trace", true);
    if (nextFoldout != _stackTraceFoldout)
    {
        _stackTraceFoldout = nextFoldout;
        SaveEditorUiState();
    }

    if (_stackTraceFoldout)
    {
        GUI.enabled = false;
        EditorGUILayout.TextArea(string.IsNullOrWhiteSpace(stackTrace) ? "(No Stack Trace)" : stackTrace, GUILayout.MinHeight(88f));
        GUI.enabled = previousEnabled;
    }
}

/// <summary>
/// 에디터 레이아웃 to 기본를 기본 상태로 되돌린다. 사용자가 변경한 임시 상태를 초기 기준값으로 복원한다.
/// </summary>
private void ResetEditorLayoutToDefault()
{
    _autoScroll = true;
    _hideTransform = true;
    _collapsePreviousOnSelection = true;
    _collapseLogs = true;
    _showTypeFilterPanel = false;
    _showLogDetails = true;
    _stackTraceFoldout = true;
    _hierarchyPanelWidth = 420f;
    _logDetailPanelHeight = DefaultLogDetailPanelHeight;
    _hierarchyScroll = Vector2.zero;
    _logScroll = Vector2.zero;
    _typeFilterScroll = Vector2.zero;
    _detailScroll = Vector2.zero;
    SaveEditorUiState();
    Repaint();
}

/// <summary>
/// stored 매니저 환경설정 to 기본를 기본 상태로 되돌린다. 사용자가 변경한 임시 상태를 초기 기준값으로 복원한다.
/// </summary>
private void ResetStoredManagerPreferencesToDefault(DebugConsoleEditorSnapshot snapshot)
{
    DebugConsolePreferenceStore.DeleteKey(GlobalEnabledPrefKey);
    DebugConsolePreferenceStore.DeleteKey(MirrorToUnityPrefKey);
    DebugConsolePreferenceStore.DeleteKey(LogLevelLogPrefKey);
    DebugConsolePreferenceStore.DeleteKey(LogLevelWarningPrefKey);
    DebugConsolePreferenceStore.DeleteKey(LogLevelErrorPrefKey);

    DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
    for (int i = 0; i < types.Length; i++)
        DebugConsolePreferenceStore.DeleteKey(GetTypePrefKey(types[i]));

    DeleteRegistryPrefValues(GameObjectRegistryPrefKey);
    DeleteRegistryPrefValues(ComponentRegistryPrefKey);

    DebugConsolePreferenceStore.DeleteKey(GameObjectRegistryPrefKey);
    DebugConsolePreferenceStore.DeleteKey(ComponentRegistryPrefKey);

    if (snapshot != null)
    {
        snapshot.GlobalEnabled = true;
        snapshot.MirrorToUnityConsole = false;
        snapshot.ShowLogLevelLog = true;
        snapshot.ShowLogLevelWarning = true;
        snapshot.ShowLogLevelError = true;

        if (snapshot.TypeFilters != null)
        {
            for (int i = 0; i < snapshot.TypeFilters.Length; i++)
                snapshot.TypeFilters[i] = true;
        }

        SaveSnapshotIfAvailable();
    }

    SaveEditorUiState();
}

/// <summary>
    /// delete registry pref values 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
private void DeleteRegistryPrefValues(string registryPrefKey)
{
    string raw = DebugConsolePreferenceStore.GetString(registryPrefKey, string.Empty);
    if (string.IsNullOrWhiteSpace(raw))
        return;

    string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < parts.Length; i++)
        DebugConsolePreferenceStore.DeleteKey(parts[i]);
}

/// <summary>
/// 현재 상태에서 stored 매니저 settings 정보를 수집해 스냅샷이나 캐시로 만든다.
/// </summary>
private DebugConsoleStoredManagerSettings CaptureStoredManagerSettings()
{
    DebugConsoleStoredManagerSettings settings = new DebugConsoleStoredManagerSettings
    {
        GlobalEnabled = DebugConsolePreferenceStore.GetBool(GlobalEnabledPrefKey, true),
        MirrorToUnityConsole = DebugConsolePreferenceStore.GetBool(MirrorToUnityPrefKey, false),
        ShowLogLevelLog = DebugConsolePreferenceStore.GetBool(LogLevelLogPrefKey, true),
        ShowLogLevelWarning = DebugConsolePreferenceStore.GetBool(LogLevelWarningPrefKey, true),
        ShowLogLevelError = DebugConsolePreferenceStore.GetBool(LogLevelErrorPrefKey, true)
    };

    DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
    settings.TypeFilters = new bool[types.Length];
    for (int i = 0; i < types.Length; i++)
        settings.TypeFilters[i] = DebugConsolePreferenceStore.GetBool(GetTypePrefKey(types[i]), true);

    settings.GameObjectFilterValues = CaptureStoredRegistryValues(GameObjectRegistryPrefKey);
    settings.ComponentFilterValues = CaptureStoredRegistryValues(ComponentRegistryPrefKey);
    return settings;
}

/// <summary>
/// 현재 상태에서 stored registry values 정보를 수집해 스냅샷이나 캐시로 만든다.
/// </summary>
private List<DebugConsoleStoredBoolValue> CaptureStoredRegistryValues(string registryPrefKey)
{
    List<DebugConsoleStoredBoolValue> values = new List<DebugConsoleStoredBoolValue>();
    string raw = DebugConsolePreferenceStore.GetString(registryPrefKey, string.Empty);
    if (string.IsNullOrWhiteSpace(raw))
        return values;

    string[] parts = raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < parts.Length; i++)
    {
        string key = parts[i];
        if (string.IsNullOrWhiteSpace(key))
            continue;

        values.Add(new DebugConsoleStoredBoolValue
        {
            Key = key,
            Value = DebugConsolePreferenceStore.GetBool(key, true)
        });
    }

    return values;
}

/// <summary>
/// 준비된 stored 매니저 settings 값을 실제 상태에 반영한다.
/// </summary>
private void ApplyStoredManagerSettings(DebugConsoleStoredManagerSettings settings, DebugConsoleEditorSnapshot snapshot)
{
    if (settings == null)
        return;

    ResetStoredManagerPreferencesToDefault(snapshot);

    DebugConsolePreferenceStore.SetBool(GlobalEnabledPrefKey, settings.GlobalEnabled);
    DebugConsolePreferenceStore.SetBool(MirrorToUnityPrefKey, settings.MirrorToUnityConsole);
    DebugConsolePreferenceStore.SetBool(LogLevelLogPrefKey, settings.ShowLogLevelLog);
    DebugConsolePreferenceStore.SetBool(LogLevelWarningPrefKey, settings.ShowLogLevelWarning);
    DebugConsolePreferenceStore.SetBool(LogLevelErrorPrefKey, settings.ShowLogLevelError);

    DebugType[] types = (DebugType[])Enum.GetValues(typeof(DebugType));
    if (settings.TypeFilters != null)
    {
        for (int i = 0; i < types.Length && i < settings.TypeFilters.Length; i++)
            DebugConsolePreferenceStore.SetBool(GetTypePrefKey(types[i]), settings.TypeFilters[i]);
    }

    ApplyStoredRegistryValues(GameObjectRegistryPrefKey, settings.GameObjectFilterValues);
    ApplyStoredRegistryValues(ComponentRegistryPrefKey, settings.ComponentFilterValues);

    if (snapshot != null)
        ApplyStoredPreferencesToSnapshot(snapshot);
}

/// <summary>
/// 준비된 stored registry values 값을 실제 상태에 반영한다.
/// </summary>
private void ApplyStoredRegistryValues(string registryPrefKey, List<DebugConsoleStoredBoolValue> values)
{
    List<string> registry = new List<string>();

    if (values != null)
    {
        for (int i = 0; i < values.Count; i++)
        {
            DebugConsoleStoredBoolValue value = values[i];
            if (value == null || string.IsNullOrWhiteSpace(value.Key))
                continue;

            registry.Add(value.Key);
            DebugConsolePreferenceStore.SetBool(value.Key, value.Value);
        }
    }

    DebugConsolePreferenceStore.SetString(registryPrefKey, string.Join("\n", registry));
}

/// <summary>
/// 에디터 settings to file를 외부 파일이나 문자열 형태로 내보낸다.
/// </summary>
private void ExportEditorSettingsToFile()
{
    string path = EditorUtility.SaveFilePanel("Export Debug Console Settings", Application.dataPath, "debug_console_settings", "json");
    if (string.IsNullOrWhiteSpace(path))
        return;

    DebugConsoleEditorBackupData data = new DebugConsoleEditorBackupData
    {
        ManagerSettings = CaptureStoredManagerSettings(),
        EditorState = BuildCurrentUiState()
    };

    File.WriteAllText(path, JsonUtility.ToJson(data, true));
}

/// <summary>
/// 외부에서 읽은 에디터 settings from file를 현재 상태에 반영한다.
/// </summary>
private void ImportEditorSettingsFromFile(DebugConsoleEditorSnapshot snapshot)
{
    string path = EditorUtility.OpenFilePanel("Import Debug Console Settings", Application.dataPath, "json");
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        return;

    DebugConsoleEditorBackupData data = JsonUtility.FromJson<DebugConsoleEditorBackupData>(File.ReadAllText(path));
    if (data == null)
        return;

    ApplyStoredManagerSettings(data.ManagerSettings, snapshot);
    ApplyEditorUiState(data.EditorState);
    SaveEditorUiState();
    Repaint();
}

/// <summary>
/// current ui 상태 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
/// </summary>
private DebugConsoleEditorUiState BuildCurrentUiState()
{
    return new DebugConsoleEditorUiState
    {
        AutoScroll = _autoScroll,
        HideTransform = _hideTransform,
        CollapsePreviousOnSelection = _collapsePreviousOnSelection,
        CollapseLogs = _collapseLogs,
        ShowTypeFilterPanel = _showTypeFilterPanel,
        ShowLogDetails = _showLogDetails,
        StackTraceFoldout = _stackTraceFoldout,
        HierarchyPanelWidth = _hierarchyPanelWidth,
        LogDetailPanelHeight = _logDetailPanelHeight,
        HierarchyScroll = _hierarchyScroll,
        LogScroll = _logScroll,
        TypeFilterScroll = _typeFilterScroll,
        DetailScroll = _detailScroll,
        SelectedLogIndex = _selectedLogIndex,
        FocusedSnapshotGameObjectKey = _focusedSnapshotGameObjectKey ?? string.Empty,
        FocusedSnapshotComponentKey = _focusedSnapshotComponentKey ?? string.Empty,
        FocusedObjectName = _focusedObjectName ?? string.Empty,
        FocusedComponentName = _focusedComponentName ?? string.Empty,
        ExpandedSnapshotDetails = new List<string>(_expandedSnapshotDetails),
        ExpandedSnapshotChildren = new List<string>(_expandedSnapshotChildren)
    };
}

/// <summary>
/// 준비된 에디터 ui 상태 값을 실제 상태에 반영한다.
/// </summary>
private void ApplyEditorUiState(DebugConsoleEditorUiState state)
{
    if (state == null)
        return;

    _autoScroll = state.AutoScroll;
    _hideTransform = state.HideTransform;
    _collapsePreviousOnSelection = state.CollapsePreviousOnSelection;
    _collapseLogs = state.CollapseLogs;
    _showTypeFilterPanel = state.ShowTypeFilterPanel;
    _showLogDetails = state.ShowLogDetails;
    _stackTraceFoldout = state.StackTraceFoldout;
    _hierarchyPanelWidth = state.HierarchyPanelWidth > 0f ? state.HierarchyPanelWidth : _hierarchyPanelWidth;
    _logDetailPanelHeight = Mathf.Clamp(state.LogDetailPanelHeight > 0f ? state.LogDetailPanelHeight : DefaultLogDetailPanelHeight, MinLogDetailPanelHeight, MaxLogDetailPanelHeight);
    _hierarchyScroll = state.HierarchyScroll;
    _logScroll = state.LogScroll;
    _typeFilterScroll = state.TypeFilterScroll;
    _detailScroll = state.DetailScroll;
    _selectedLogIndex = state.SelectedLogIndex;
    _focusedSnapshotGameObjectKey = state.FocusedSnapshotGameObjectKey ?? string.Empty;
    _focusedSnapshotComponentKey = state.FocusedSnapshotComponentKey ?? string.Empty;
    _focusedObjectName = state.FocusedObjectName ?? string.Empty;
    _focusedComponentName = state.FocusedComponentName ?? string.Empty;

    _expandedSnapshotDetails.Clear();
    if (state.ExpandedSnapshotDetails != null)
    {
        for (int i = 0; i < state.ExpandedSnapshotDetails.Count; i++)
        {
            string key = state.ExpandedSnapshotDetails[i];
            if (!string.IsNullOrWhiteSpace(key))
                _expandedSnapshotDetails.Add(key);
        }
    }

    _expandedSnapshotChildren.Clear();
    if (state.ExpandedSnapshotChildren != null)
    {
        for (int i = 0; i < state.ExpandedSnapshotChildren.Count; i++)
        {
            string key = state.ExpandedSnapshotChildren[i];
            if (!string.IsNullOrWhiteSpace(key))
                _expandedSnapshotChildren.Add(key);
        }
    }
}

    /// <summary>
    /// 엔트리 script를 연다. 외부 에셋이나 패널, 스크립트 위치로 이동시키는 데 사용한다.
    /// </summary>
    private void OpenEntryScript(DebugEntry entry)
    {
#if UNITY_EDITOR
        if (!TryGetEntryScriptLocation(entry, out MonoScript script, out int lineNumber, out int columnNumber))
            return;

        AssetDatabase.OpenAsset(script, Mathf.Max(1, lineNumber), Mathf.Max(1, columnNumber));
#endif
    }

    /// <summary>
    /// 엔트리 script를 연다. 외부 에셋이나 패널, 스크립트 위치로 이동시키는 데 사용한다.
    /// </summary>
    private void OpenEntryScript(SnapshotLogEntry entry)
    {
#if UNITY_EDITOR
        if (entry == null)
            return;

        if (!TryGetEntryScriptLocation(entry.CallerFilePath, entry.LineNumber, entry.CallerColumn, out MonoScript script, out int lineNumber, out int columnNumber))
            return;

        AssetDatabase.OpenAsset(script, Mathf.Max(1, lineNumber), Mathf.Max(1, columnNumber));
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// get 엔트리 script location 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
    private bool TryGetEntryScriptLocation(DebugEntry entry, out MonoScript script, out int lineNumber, out int columnNumber)
    {
        script = null;
        lineNumber = 1;
        columnNumber = 1;

        if (entry == null)
            return false;

        return TryGetEntryScriptLocation(entry.CallerFilePath, entry.LineNumber, entry.CallerColumn, out script, out lineNumber, out columnNumber);
    }

    /// <summary>
    /// get 엔트리 script location 처리를 시도한다. 성공 여부를 bool로 반환하고 실패 시 안전하게 빠져나간다.
    /// </summary>
    private bool TryGetEntryScriptLocation(string callerFilePath, int sourceLineNumber, int sourceColumnNumber, out MonoScript script, out int lineNumber, out int columnNumber)
    {
        script = null;
        lineNumber = Mathf.Max(1, sourceLineNumber);
        columnNumber = Mathf.Max(1, sourceColumnNumber);

        if (string.IsNullOrWhiteSpace(callerFilePath))
            return false;

        if (TryConvertCallerPathToAssetPath(callerFilePath, out string assetPath))
        {
            script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (script != null)
                return true;
        }

        return TryFindScriptByFileName(callerFilePath, out script);
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
    private bool TryFindScriptByFileName(string callerFilePath, out MonoScript script)
    {
        script = null;

        string fileName = Path.GetFileNameWithoutExtension(callerFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        string[] guids = AssetDatabase.FindAssets($"{fileName} t:MonoScript");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(Path.GetFileNameWithoutExtension(assetPath), fileName, StringComparison.Ordinal))
                continue;

            MonoScript found = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
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

        _focusedSnapshotGameObjectKey = entry.GameObjectKey ?? string.Empty;
        _focusedSnapshotComponentKey = entry.ComponentKey ?? string.Empty;
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
                    _focusedSnapshotGameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(go);
                    _focusedSnapshotComponentKey = string.Empty;
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
                    _focusedSnapshotGameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(component.gameObject);
                    _focusedSnapshotComponentKey = DebugConsoleFilterKeyUtility.GetComponentKey(component);
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

        if (targetGameObject != null)
        {
            Selection.activeGameObject = targetGameObject;
            EditorGUIUtility.PingObject(targetGameObject);
        }

        SaveEditorUiState();
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
    /// 미러 live expansion to 스냅샷 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void MirrorLiveExpansionToSnapshot(Transform target, bool includeTargetDetails)
    {
        if (target == null)
            return;

        if (includeTargetDetails)
            _expandedSnapshotDetails.Add(DebugConsoleFilterKeyUtility.GetGameObjectKey(target.gameObject));

        Transform current = target;
        while (current.parent != null)
        {
            current = current.parent;
            string key = DebugConsoleFilterKeyUtility.GetGameObjectKey(current.gameObject);
            _expandedSnapshotDetails.Add(key);
            _expandedSnapshotChildren.Add(key);
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
        _focusedSnapshotGameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(go);
        _focusedSnapshotComponentKey = string.Empty;
        _focusedObjectName = go.name;
        _focusedComponentName = string.Empty;

        PrepareSelectionExpansion(go.transform, true);
        SaveEditorUiState();
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
        _focusedSnapshotGameObjectKey = DebugConsoleFilterKeyUtility.GetGameObjectKey(component.gameObject);
        _focusedSnapshotComponentKey = DebugConsoleFilterKeyUtility.GetComponentKey(component);
        _focusedObjectName = component.gameObject.name;
        _focusedComponentName = component.GetType().Name;

        PrepareSelectionExpansion(component.transform, true);
        SaveEditorUiState();
    }

    /// <summary>
    /// prepare 선택 expansion 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private void PrepareSelectionExpansion(Transform target, bool includeDetails)
    {
        if (target == null)
            return;

        if (_collapsePreviousOnSelection)
        {
            PreserveExpansionWithinTopLevelRoot(target);
            PreserveSnapshotExpansionWithinTopLevelRoot(DebugConsoleFilterKeyUtility.GetGameObjectKey(target.gameObject));
        }

        ExpandSelectionPath(target, includeDetails);
        MirrorLiveExpansionToSnapshot(target, includeDetails);
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
    /// 포커스를 비우거나 초기화한다. 이전 상태를 제거하고 다음 작업을 준비하는 데 사용한다.
    /// </summary>
    private void ClearFocus()
    {
        _focusedGameObjectId = 0;
        _focusedComponentId = 0;
        _focusedObjectName = string.Empty;
        _focusedComponentName = string.Empty;
        _focusedSnapshotGameObjectKey = string.Empty;
        _focusedSnapshotComponentKey = string.Empty;
        SaveEditorUiState();
    }

    /// <summary>
    /// 포커스 label 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private string GetFocusLabel()
    {
        if (!string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
            return $"Focus : {_focusedObjectName}/{_focusedComponentName}";

        if (!string.IsNullOrWhiteSpace(_focusedSnapshotGameObjectKey))
            return $"Focus : {_focusedObjectName} (All Components)";

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
        if (!string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
        {
            string objectName = TrimFooterFocusSegment(_focusedObjectName);
            string componentName = TrimFooterFocusSegment(_focusedComponentName);

            if (string.Equals(_focusedObjectName, _focusedComponentName, StringComparison.Ordinal))
                return $"Focus : {objectName}";

            return $"Focus : {objectName} / {componentName}";
        }

        if (!string.IsNullOrWhiteSpace(_focusedSnapshotGameObjectKey))
            return $"Focus : {TrimFooterFocusSegment(_focusedObjectName)}";

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
        if (!string.IsNullOrWhiteSpace(_focusedSnapshotComponentKey))
            return $"({_focusedObjectName}/{_focusedComponentName})";

        if (!string.IsNullOrWhiteSpace(_focusedSnapshotGameObjectKey))
            return $"({_focusedObjectName})";

        if (_focusedComponentId != 0)
            return $"({_focusedObjectName}/{_focusedComponentName})";

        if (_focusedGameObjectId != 0)
            return $"({_focusedObjectName})";

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

        return new GUIStyle("box")
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
    /// 펼침 set 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleExpandedSet(HashSet<string> set, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (set.Contains(key))
            set.Remove(key);
        else
            set.Add(key);
    }

    /// <summary>
    /// live 펼침 details 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleLiveExpandedDetails(GameObject go)
    {
        if (go == null)
            return;

        ToggleExpandedSet(_expandedComponents, go.GetInstanceID());
        ToggleExpandedSet(_expandedSnapshotDetails, DebugConsoleFilterKeyUtility.GetGameObjectKey(go));
        SaveEditorUiState();
    }

    /// <summary>
    /// live 펼침 하위 목록 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleLiveExpandedChildren(GameObject go)
    {
        if (go == null)
            return;

        ToggleExpandedSet(_expandedChildren, go.GetInstanceID());
        ToggleExpandedSet(_expandedSnapshotChildren, DebugConsoleFilterKeyUtility.GetGameObjectKey(go));
        SaveEditorUiState();
    }

    /// <summary>
    /// 스냅샷 펼침 details 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleSnapshotExpandedDetails(string key)
    {
        ToggleExpandedSet(_expandedSnapshotDetails, key);
        SaveEditorUiState();
    }

    /// <summary>
    /// 스냅샷 펼침 하위 목록 상태를 켜고 끄는 토글 동작을 수행한다.
    /// </summary>
    private void ToggleSnapshotExpandedChildren(string key)
    {
        ToggleExpandedSet(_expandedSnapshotChildren, key);
        SaveEditorUiState();
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
        if (go == null)
            return false;

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        if (ContainsIgnoreCase(go.name, _hierarchySearch))
            return true;

        if (HasMatchingComponent(go, _hierarchySearch))
            return true;

        for (int i = 0; i < go.transform.childCount; i++)
        {
            if (ShouldShowGameObject(go.transform.GetChild(i).gameObject))
                return true;
        }

        return false;
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

        if (string.IsNullOrWhiteSpace(_hierarchySearch))
            return true;

        if (ContainsIgnoreCase(ownerName, _hierarchySearch))
            return true;

        return ContainsIgnoreCase(component.GetType().Name, _hierarchySearch);
    }


    private readonly (string label, float width)[] _toolbarToggleItems =
    {
        ("Global", 80f),
        ("Mirror Unity", 110f),
        ("Auto Scroll", 100f),
        ("Hide Transform", 110f),
        ("Collapse Prev", 110f),
        ("Collapse Logs", 110f),
        ("Log", 70f),
        ("Warn", 75f),
        ("Error", 75f),
    };

    private readonly (string label, float width)[] _toolbarActionItems =
    {
        ("TypeFilter", 150f),
        ("All Types On", 92f),
        ("All Types Off", 92f),
        ("All Levels", 92f),
        ("Warn+", 72f),
        ("Error Only", 92f),
        ("Clear Logs", 92f),
        ("Clear Focus", 92f),
        ("Reset Filters", 102f),
        ("Reset Layout", 102f),
        ("Export Settings", 112f),
        ("Import Settings", 112f),
    };

    /// <summary>
    /// toolbar 토글 그룹 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawToolbarToggleGroup(DebugConsoleManager manager)
    {
        bool global = GUILayout.Toggle(manager.GlobalEnabled, "Global", GUILayout.Width(80f));
        if (global != manager.GlobalEnabled)
            manager.GlobalEnabled = global;

        bool mirror = GUILayout.Toggle(manager.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
        if (mirror != manager.MirrorToUnityConsole)
            manager.MirrorToUnityConsole = mirror;

        bool autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
        if (autoScroll != _autoScroll)
        {
            _autoScroll = autoScroll;
            SaveEditorUiState();
        }

        bool hideTransform = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
        if (hideTransform != _hideTransform)
        {
            _hideTransform = hideTransform;
            SaveEditorUiState();
        }

        bool collapsePrevious = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
        if (collapsePrevious != _collapsePreviousOnSelection)
        {
            _collapsePreviousOnSelection = collapsePrevious;
            SaveEditorUiState();
        }

        bool collapseLogs = GUILayout.Toggle(_collapseLogs, "Collapse Logs", GUILayout.Width(120f));
        if (collapseLogs != _collapseLogs)
        {
            _collapseLogs = collapseLogs;
            SaveEditorUiState();
        }

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
    /// toolbar 토글 그룹 wrapped 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawToolbarToggleGroupWrapped(DebugConsoleManager manager, float availableWidth)
    {
        for (int index = 0; index < _toolbarToggleItems.Length;)
        {
            int endIndex = GetWrappedEndIndex(_toolbarToggleItems, index, availableWidth);
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawToolbarToggleItems(manager, index, endIndex);
            EditorGUILayout.EndHorizontal();
            index = endIndex;
        }
    }

    /// <summary>
    /// toolbar 토글 items 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawToolbarToggleItems(DebugConsoleManager manager, int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            switch (_toolbarToggleItems[i].label)
            {
                case "Global":
                {
                    bool value = GUILayout.Toggle(manager.GlobalEnabled, "Global", GUILayout.Width(80f));
                    if (value != manager.GlobalEnabled)
                        manager.GlobalEnabled = value;
                    break;
                }

                case "Mirror Unity":
                {
                    bool value = GUILayout.Toggle(manager.MirrorToUnityConsole, "Mirror Unity", GUILayout.Width(110f));
                    if (value != manager.MirrorToUnityConsole)
                        manager.MirrorToUnityConsole = value;
                    break;
                }

                case "Auto Scroll":
                {
                    bool value = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100f));
                    if (value != _autoScroll)
                    {
                        _autoScroll = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Hide Transform":
                {
                    bool value = GUILayout.Toggle(_hideTransform, "Hide Transform", GUILayout.Width(120f));
                    if (value != _hideTransform)
                    {
                        _hideTransform = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Collapse Prev":
                {
                    bool value = GUILayout.Toggle(_collapsePreviousOnSelection, "Collapse Prev", GUILayout.Width(120f));
                    if (value != _collapsePreviousOnSelection)
                    {
                        _collapsePreviousOnSelection = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Collapse Logs":
                {
                    bool value = GUILayout.Toggle(_collapseLogs, "Collapse Logs", GUILayout.Width(120f));
                    if (value != _collapseLogs)
                    {
                        _collapseLogs = value;
                        SaveEditorUiState();
                    }
                    break;
                }

                case "Log":
                {
                    bool value = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Log), "Log", GUILayout.Width(70f));
                    if (value != manager.GetLevelEnabled(DebugLogLevel.Log))
                        manager.SetLevelEnabled(DebugLogLevel.Log, value);
                    break;
                }

                case "Warn":
                {
                    bool value = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Warning), "Warn", GUILayout.Width(75f));
                    if (value != manager.GetLevelEnabled(DebugLogLevel.Warning))
                        manager.SetLevelEnabled(DebugLogLevel.Warning, value);
                    break;
                }

                case "Error":
                {
                    bool value = GUILayout.Toggle(manager.GetLevelEnabled(DebugLogLevel.Error), "Error", GUILayout.Width(75f));
                    if (value != manager.GetLevelEnabled(DebugLogLevel.Error))
                        manager.SetLevelEnabled(DebugLogLevel.Error, value);
                    break;
                }
            }

            if (i < endIndex - 1)
                GUILayout.Space(8f);
        }
    }

/// <summary>
/// toolbar action 그룹 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawToolbarActionGroup(DebugConsoleManager manager, string typeButtonLabel)
{
    if (GUILayout.Button(typeButtonLabel, GUILayout.Width(160f)))
    {
        _showTypeFilterPanel = !_showTypeFilterPanel;
        SaveEditorUiState();
    }

    if (GUILayout.Button("All Types On", GUILayout.Width(100f)))
        manager.SetAllTypes(true);

    if (GUILayout.Button("All Types Off", GUILayout.Width(100f)))
        manager.SetAllTypes(false);

    if (GUILayout.Button("All Levels", GUILayout.Width(100f)))
        manager.SetAllLevels(true);

    if (GUILayout.Button("Warn+", GUILayout.Width(80f)))
        manager.SetWarningAndErrorOnly();

    if (GUILayout.Button("Error Only", GUILayout.Width(100f)))
        manager.SetErrorOnly();

    if (GUILayout.Button("Clear Logs", GUILayout.Width(100f)))
        manager.ClearLogs();

    if (GUILayout.Button("Clear Focus", GUILayout.Width(100f)))
        ClearFocus();

    if (GUILayout.Button("Reset Filters", GUILayout.Width(110f)))
        manager.ResetAllFiltersToDefault();

    if (GUILayout.Button("Reset Layout", GUILayout.Width(110f)))
        ResetEditorLayoutToDefault();

    if (GUILayout.Button("Export Settings", GUILayout.Width(120f)))
        ExportEditorSettingsToFile();

    if (GUILayout.Button("Import Settings", GUILayout.Width(120f)))
        ImportEditorSettingsFromFile(_lastSnapshot);
}

/// <summary>
/// toolbar action 그룹 wrapped 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawToolbarActionGroupWrapped(DebugConsoleManager manager, string typeButtonLabel, float availableWidth)
    {
        for (int index = 0; index < _toolbarActionItems.Length;)
        {
            int endIndex = GetWrappedEndIndex(_toolbarActionItems, index, availableWidth);
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(24f));
            DrawToolbarActionItems(manager, typeButtonLabel, index, endIndex);
            EditorGUILayout.EndHorizontal();
            index = endIndex;
        }
    }

/// <summary>
/// toolbar action items 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
/// </summary>
private void DrawToolbarActionItems(DebugConsoleManager manager, string typeButtonLabel, int startIndex, int endIndex)
{
    for (int i = startIndex; i < endIndex; i++)
    {
        string key = _toolbarActionItems[i].label;
        float width = _toolbarActionItems[i].width;

        switch (key)
        {
            case "TypeFilter":
                if (GUILayout.Button(typeButtonLabel, GUILayout.Width(width)))
                {
                    _showTypeFilterPanel = !_showTypeFilterPanel;
                    SaveEditorUiState();
                }
                break;

            case "All Types On":
                if (GUILayout.Button("All Types On", GUILayout.Width(width)))
                    manager.SetAllTypes(true);
                break;

            case "All Types Off":
                if (GUILayout.Button("All Types Off", GUILayout.Width(width)))
                    manager.SetAllTypes(false);
                break;

            case "All Levels":
                if (GUILayout.Button("All Levels", GUILayout.Width(width)))
                    manager.SetAllLevels(true);
                break;

            case "Warn+":
                if (GUILayout.Button("Warn+", GUILayout.Width(width)))
                    manager.SetWarningAndErrorOnly();
                break;

            case "Error Only":
                if (GUILayout.Button("Error Only", GUILayout.Width(width)))
                    manager.SetErrorOnly();
                break;

            case "Clear Logs":
                if (GUILayout.Button("Clear Logs", GUILayout.Width(width)))
                    manager.ClearLogs();
                break;

            case "Clear Focus":
                if (GUILayout.Button("Clear Focus", GUILayout.Width(width)))
                    ClearFocus();
                break;

            case "Reset Filters":
                if (GUILayout.Button("Reset Filters", GUILayout.Width(width)))
                    manager.ResetAllFiltersToDefault();
                break;

            case "Reset Layout":
                if (GUILayout.Button("Reset Layout", GUILayout.Width(width)))
                    ResetEditorLayoutToDefault();
                break;

            case "Export Settings":
                if (GUILayout.Button("Export Settings", GUILayout.Width(width)))
                    ExportEditorSettingsToFile();
                break;

            case "Import Settings":
                if (GUILayout.Button("Import Settings", GUILayout.Width(width)))
                    ImportEditorSettingsFromFile(_lastSnapshot);
                break;
        }

        if (i < endIndex - 1)
            GUILayout.Space(8f);
    }
}


/// <summary>
/// wrapped end index 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
/// </summary>
private int GetWrappedEndIndex((string label, float width)[] items, int startIndex, float availableWidth)
    {
        float rowWidth = 0f;
        const float spacing = 8f;

        for (int i = startIndex; i < items.Length; i++)
        {
            float nextWidth = items[i].width + (i > startIndex ? spacing : 0f);
            if (rowWidth + nextWidth > availableWidth && i > startIndex)
                return i;

            rowWidth += nextWidth;
        }

        return items.Length;
    }

/// <summary>
/// wrapped split index 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
/// </summary>
private int GetWrappedSplitIndex((string label, float width)[] items, float availableWidth)
    {
        float rowWidth = 0f;
        const float spacing = 8f;

        for (int i = 0; i < items.Length; i++)
        {
            float nextWidth = items[i].width + (i > 0 ? spacing : 0f);
            if (rowWidth + nextWidth > availableWidth && i > 0)
                return i;

            rowWidth += nextWidth;
        }

        return items.Length;
    }

    /// <summary>
    /// toolbar info 그룹 영역을 그린다. 현재 상태와 캐시를 읽어 IMGUI 요소를 배치한다.
    /// </summary>
    private void DrawToolbarInfoGroup(DebugConsoleManager manager, bool expanded)
    {
        GUILayout.Label(GetFocusLabel(), _toolbarInfoLabelStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(expanded ? 30f : 18f));
        GUILayout.Space(8f);
        GUILayout.Label($"Count : {manager.Entries.Count}", _toolbarInfoLabelStyle, GUILayout.Width(expanded ? 120f : 110f), GUILayout.MinHeight(expanded ? 30f : 18f));
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
        Rect fieldRect = GUILayoutUtility.GetRect(fieldWidth, 20f, GUILayout.Width(fieldWidth), GUILayout.Height(20f));
        string nextSearch = (_hierarchySearchFieldControl ??= new SearchField()).OnGUI(fieldRect, _pendingHierarchySearch);
        if (!string.Equals(nextSearch, _pendingHierarchySearch, StringComparison.Ordinal))
        {
            _pendingHierarchySearch = nextSearch;
            _hierarchySearchApplyTime = EditorApplication.timeSinceStartup + SearchDebounceDelay;
        }
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
        Rect fieldRect = GUILayoutUtility.GetRect(fieldWidth, 20f, GUILayout.Width(fieldWidth), GUILayout.Height(20f));
        string nextSearch = (_logSearchFieldControl ??= new SearchField()).OnGUI(fieldRect, _pendingLogSearch);
        if (!string.Equals(nextSearch, _pendingLogSearch, StringComparison.Ordinal))
        {
            _pendingLogSearch = nextSearch;
            _logSearchApplyTime = EditorApplication.timeSinceStartup + SearchDebounceDelay;
        }

        if (GUILayout.Button("Clear", GUILayout.Width(SearchClearButtonWidth)))
        {
            _pendingLogSearch = string.Empty;
            _logSearch = string.Empty;
            _logSearchApplyTime = 0d;
            GUI.FocusControl(null);
            SaveEditorUiState();
        }
    }

    /// <summary>
    /// process 검색 debounce 처리 흐름을 수행한다. 관련 상태를 읽거나 갱신해 디버그 콘솔 동작을 이어간다.
    /// </summary>
    private bool ProcessSearchDebounce()
    {
        bool changed = false;
        double now = EditorApplication.timeSinceStartup;

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

        return changed;
    }

    /// <summary>
    /// top area 너비 값을 계산해 반환한다. 조회용 메서드이므로 호출자는 반환값을 기준으로 다음 동작을 결정한다.
    /// </summary>
    private float GetTopAreaWidth()
    {
        return Mathf.Max(320f, position.width - 32f);
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
#endif
