using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 스킬 시트 CSV 문자열을 읽어서 SkillSheetRow 리스트로 변환하는 파서.
///
/// 셀 안에 줄바꿈/콤마가 들어가 따옴표로 감싸진 경우도 처리(RFC4180 호환).
/// 시트의 A, B, E, F, G, H, I, J 열만 파싱한다.
/// (C, D - 효과 유형 컬럼, K - 스킬 설명, L - 비고 는 파싱하지 않음)
/// D열 헤더가 빈 칸이라 CsvHeaderMap을 통과하지 못하므로 빈 헤더는 임시 이름으로 채움.
/// </summary>
public class SkillSheetParser
{
    // A.ID  B.CoolDown  E.데미지 공식  F.디버프  G.디버프 시간
    // H.버프  I.버프 시간  J.타겟 수
    private const string HEADER_ID = "ID";
    private const string HEADER_COOLDOWN = "CoolDown";
    private const string HEADER_DAMAGE_FORMULA = "데미지 공식";
    private const string HEADER_DEBUFF = "디버프";
    private const string HEADER_DEBUFF_DURATION = "디버프 시간";
    private const string HEADER_BUFF = "버프";
    private const string HEADER_BUFF_DURATION = "버프 시간";
    private const string HEADER_TARGET_COUNT = "타겟 수";

    public bool TryParse(string csvText, out List<SkillSheetRow> rows)
    {
        rows = new List<SkillSheetRow>();

        if (string.IsNullOrWhiteSpace(csvText))
        {
            Debug.LogError("[SkillSheetParser] CSV 텍스트가 비어 있습니다.");
            return false;
        }

        // 따옴표 안 줄바꿈/콤마를 보호하며 논리 행 단위로 분리
        List<string> logicalLines = SplitIntoLogicalLines(csvText);

        if (logicalLines.Count < 2)
        {
            Debug.LogError($"[SkillSheetParser] 헤더 또는 데이터 행이 부족합니다. 논리 행 수: {logicalLines.Count}");
            return false;
        }

        string normalizedHeader = NormalizeBlankHeaders(logicalLines[0]);
        Debug.Log($"[SkillSheetParser] 정규화된 헤더 라인: {normalizedHeader}");

        if (!CsvHeaderMap.TryBuild(normalizedHeader, out Dictionary<string, int> headerMap))
        {
            Debug.LogError("[SkillSheetParser] 헤더 맵 생성에 실패했습니다.");
            return false;
        }

        for (int i = 1; i < logicalLines.Count; i++)
        {
            string line = logicalLines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] cells = SplitCsvLine(line);

            if (!TryParseRow(cells, headerMap, i + 1, out SkillSheetRow row))
            {
                Debug.LogError($"[SkillSheetParser] 스킬 행 파싱 실패 - CSV 라인 번호: {i + 1}, 라인 원본: {line}, 셀 개수: {cells.Length}");
                return false;
            }

            rows.Add(row);
        }

        return true;
    }

    private bool TryParseRow(
        string[] cells,
        Dictionary<string, int> headerMap,
        int csvLineNumber,
        out SkillSheetRow row)
    {
        row = null;

        if (!CsvValueParser.TryReadInt(cells, headerMap, HEADER_ID, out int id))
        { Debug.LogError($"[SkillSheetParser] {csvLineNumber}행 - ID 읽기 실패"); return false; }

        if (!CsvValueParser.TryReadFloat(cells, headerMap, HEADER_COOLDOWN, out float coolDown))
        { Debug.LogError($"[SkillSheetParser] {csvLineNumber}행 - CoolDown 읽기 실패"); return false; }

        if (!CsvValueParser.TryReadFloat(cells, headerMap, HEADER_DAMAGE_FORMULA, out float damageFormula))
        { Debug.LogError($"[SkillSheetParser] {csvLineNumber}행 - 데미지 공식 읽기 실패"); return false; }

        if (!CsvValueParser.TryReadFloat(cells, headerMap, HEADER_DEBUFF, out float debuffValue))
        { Debug.LogError($"[SkillSheetParser] {csvLineNumber}행 - 디버프 읽기 실패"); return false; }

        if (!CsvValueParser.TryReadFloat(cells, headerMap, HEADER_DEBUFF_DURATION, out float debuffDuration))
        { Debug.LogError($"[SkillSheetParser] {csvLineNumber}행 - 디버프 시간 읽기 실패"); return false; }

        if (!CsvValueParser.TryReadFloat(cells, headerMap, HEADER_BUFF, out float buffValue))
        { Debug.LogError($"[SkillSheetParser] {csvLineNumber}행 - 버프 읽기 실패"); return false; }

        if (!CsvValueParser.TryReadFloat(cells, headerMap, HEADER_BUFF_DURATION, out float buffDuration))
        { Debug.LogError($"[SkillSheetParser] {csvLineNumber}행 - 버프 시간 읽기 실패"); return false; }

        if (!CsvValueParser.TryReadInt(cells, headerMap, HEADER_TARGET_COUNT, out int targetCount))
        { Debug.LogError($"[SkillSheetParser] {csvLineNumber}행 - 타겟 수 읽기 실패"); return false; }

        row = new SkillSheetRow
        {
            Id = id,
            CoolDown = coolDown,
            DamageFormula = damageFormula,
            DebuffValue = debuffValue,
            DebuffDuration = debuffDuration,
            BuffValue = buffValue,
            BuffDuration = buffDuration,
            TargetCount = targetCount
        };

        return true;
    }

    /// <summary>
    /// 헤더 줄에서 빈 컬럼을 임시 이름(_skip0, _skip1 ...)으로 채워
    /// CsvHeaderMap의 빈/중복 헤더 검사를 통과하게 만든다.
    /// </summary>
    private static string NormalizeBlankHeaders(string headerLine)
    {
        string[] headers = headerLine.Split(',');
        int skipIndex = 0;

        for (int i = 0; i < headers.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(headers[i]))
            {
                headers[i] = $"_skip{skipIndex}";
                skipIndex++;
            }
        }

        return string.Join(",", headers);
    }

    /// <summary>
    /// CSV 텍스트를 따옴표 안 줄바꿈을 보호하며 논리 행 단위로 분리한다.
    /// "" 형태로 이스케이프된 따옴표는 한 글자 따옴표로 변환한다.
    /// </summary>
    private static List<string> SplitIntoLogicalLines(string csvText)
    {
        List<string> lines = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (c == '"')
            {
                // "" 이스케이프 처리: 따옴표 한 개로 축소
                if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                current.Append(c);
                continue;
            }

            if (!inQuotes && (c == '\n' || c == '\r'))
            {
                if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    i++;

                if (current.Length > 0)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            lines.Add(current.ToString());

        return lines;
    }

    /// <summary>
    /// CSV 한 논리 줄을 따옴표 보호하며 셀 단위로 분리한다.
    /// 셀을 감싸는 바깥쪽 따옴표는 셀 내용에 포함하지 않는다.
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        List<string> cells = new List<string>();
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        cells.Add(current.ToString());
        return cells.ToArray();
    }
}
