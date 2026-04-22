// ------------------------------------------------------------------------------
// 디버그 로그 한 건을 구조화된 데이터 형태로 담아두는 엔트리 정의 파일이다.
// 멤버별 주석은 해당 변수, 메서드, 클래스가 왜 필요한지와 호출 시 어떤 역할을 하는지를 빠르게 파악하기 위해 추가하였다.
// ------------------------------------------------------------------------------
using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 로그 심각도 레벨을 구분하기 위한 열거형이다.
/// </summary>
public enum DebugLogLevel
{
    Log,
    Warning,
    Error
}

/// <summary>
/// 한 건의 디버그 로그를 구조화해서 담는 데이터 클래스이다.
/// </summary>
public sealed class DebugEntry
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
    // context 값을 저장한다. 현재 처리 중인 컨텍스트 참조를 저장한다.
    public Object Context;
    // 오브젝트 id 값을 저장한다. 인스턴스 식별자 값을 저장한다.
    public int GameObjectId;
    // 컴포넌트 id 값을 저장한다. 인스턴스 식별자 값을 저장한다.
    public int ComponentId;
    // 색상 hex 값을 저장한다. 색상 값을 저장한다.
    public string ColorHex;
    // 호출 파일 경로 값을 저장한다. 경로나 파일 위치를 문자열로 저장한다.
    public string CallerFilePath;
    // 호출자 열 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public int CallerColumn = 1;
    // 스택 트레이스 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public string StackTrace;

    // sequence id 값을 저장한다. 인스턴스 식별자 값을 저장한다.
    public long SequenceId;
    // 프레임 개수 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
    public int FrameCount;
    // 캡처 at ISO utc 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public string CapturedAtIsoUtc;
    // 씬 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    public string SceneKey;
    // 계층 경로 값을 저장한다. 경로나 파일 위치를 문자열로 저장한다.
    public string HierarchyPath;
    // 오브젝트 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    public string GameObjectKey;
    // 컴포넌트 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    public string ComponentKey;
    // 오브젝트 이름 값을 저장한다. 표시용 이름 값을 저장한다.
    public string GameObjectName;
    // 컴포넌트 이름 값을 저장한다. 표시용 이름 값을 저장한다.
    public string ComponentName;
    // 컴포넌트 타입 이름 값을 저장한다. 표시용 이름 값을 저장한다.
    public string ComponentTypeName;

    // 반복 개수 값을 저장한다. 개수 또는 표시할 항목 수를 저장한다.
    public int RepeatCount = 1;
    // 묶기 식별 키 값을 저장한다. 오브젝트나 컴포넌트를 식별하기 위한 키 값을 저장한다.
    public string CollapseKey;
    // searchable 텍스트 값을 저장한다. 현재 검색어 상태를 저장한다. 목록 필터링이나 표시 대상 계산의 기준으로 사용한다.
    public string SearchableText;
    // 일반 텍스트 cached 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public string PlainTextCached;
    // 리치 텍스트 cached 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public string RichTextCached;
    // 요약 텍스트 cached 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public string SummaryTextCached;
    // 요약 리치 텍스트 cached 상태를 저장한다. 관련 메서드에서 기준값이나 캐시로 사용한다.
    public string SummaryRichTextCached;

    /// <summary>
    /// 일반 텍스트 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public string PlainText => string.IsNullOrEmpty(PlainTextCached) ? BuildPlainText() : PlainTextCached;
    /// <summary>
    /// 리치 텍스트 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public string RichText => string.IsNullOrEmpty(RichTextCached) ? BuildRichText() : RichTextCached;
    /// <summary>
    /// 요약 텍스트 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public string SummaryText => string.IsNullOrEmpty(SummaryTextCached) ? BuildSummaryText() : SummaryTextCached;
    /// <summary>
    /// 요약 리치 텍스트 값을 외부에 노출한다. 내부 필드나 계산 결과를 읽기 쉽게 제공하기 위한 속성이다.
    /// </summary>
    public string SummaryRichText => string.IsNullOrEmpty(SummaryRichTextCached) ? BuildSummaryRichText() : SummaryRichTextCached;

    /// <summary>
    /// derived fields와 관련된 캐시나 파생 값을 다시 계산한다. 원본 데이터가 바뀐 뒤 일관성을 맞추기 위해 사용한다.
    /// </summary>
    public void RefreshDerivedFields()
    {
        RepeatCount = Mathf.Max(1, RepeatCount);
        CollapseKey = BuildCollapseKey();
        SearchableText = BuildSearchableText();
        PlainTextCached = BuildPlainText();
        RichTextCached = BuildRichText();
        SummaryTextCached = BuildSummaryText();
        SummaryRichTextCached = BuildSummaryRichText();
    }

    /// <summary>
    /// 일반 텍스트 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildPlainText()
    {
        return $"[{Time}] [{Type}] {Message}\n출처 : [{SourceName}.{MemberName} : {LineNumber}]";
    }

    /// <summary>
    /// 리치 텍스트 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildRichText()
    {
        return $"<color={ColorHex}>[{Time}] [{Type}] {Message}</color>\n" +
               $"<color=#daa520>출처 : [{SourceName}.{MemberName} : {LineNumber}]</color>";
    }

    /// <summary>
    /// 요약 텍스트 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildSummaryText()
    {
        string sourceText = $"[{SourceName}.{MemberName} : {LineNumber}]";
        return $"[{Time}] [{Type}] {Message} | {sourceText}";
    }

    /// <summary>
    /// 요약 리치 텍스트 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildSummaryRichText()
    {
        string sourceText = $"[{SourceName}.{MemberName} : {LineNumber}]";
        return $"<color={ColorHex}>[{Time}] [{Type}] {Message}</color> " +
               $"<color=#daa520>| {sourceText}</color>";
    }

    /// <summary>
    /// searchable 텍스트 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildSearchableText()
    {
        return string.Join(" ",
            Time ?? string.Empty,
            Message ?? string.Empty,
            SourceName ?? string.Empty,
            MemberName ?? string.Empty,
            CallerFilePath ?? string.Empty,
            SceneKey ?? string.Empty,
            HierarchyPath ?? string.Empty,
            GameObjectName ?? string.Empty,
            ComponentName ?? string.Empty,
            ComponentTypeName ?? string.Empty,
            Type.ToString(),
            Level.ToString());
    }

    /// <summary>
    /// 묶기 식별 키 데이터를 조합해 새 문자열이나 키를 만든다. 동일한 규칙으로 값을 만들기 위해 사용한다.
    /// </summary>
    private string BuildCollapseKey()
    {
        return string.Join("|",
            Message ?? string.Empty,
            SourceName ?? string.Empty,
            MemberName ?? string.Empty,
            ColorHex ?? string.Empty,
            CallerFilePath ?? string.Empty,
            LineNumber.ToString(),
            CallerColumn.ToString(),
            Type.ToString(),
            Level.ToString(),
            GameObjectKey ?? GameObjectId.ToString(),
            ComponentKey ?? ComponentId.ToString());
    }
}
