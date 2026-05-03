using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkillDataSOGenerator : MonoBehaviour
{
    [Header("===입력 데이터===")]
    [Tooltip("스킬 행데이터를 보관한 SkillSheetRowProvider")]
    [SerializeField] private SkillSheetRowProvider _skillSheetRowProvider;

    [Header("===출력 폴더===")]
    [Tooltip("SkillDataSO들이 저장될 폴더\n프로젝트 창에서 폴더를 드래그해서 등록")]
    [SerializeField] private Object _outputFolder;

    [Header("===마스터 데이터베이스===")]
    [Tooltip("등록할 SkillDatabaseSO. 비워두면 마스터 등록 단계는 건너뜀.")]
    [SerializeField] private SkillDatabaseSO _skillDatabase;

    [ContextMenu("스킬 SO 생성/갱신")]
    public void GenerateOrUpdateSkillDataSO()
    {
#if UNITY_EDITOR
        if (_skillSheetRowProvider == null)
        {
            DebugTool.Error("SkillSheetRowProvider가 연결되지 않았습니다.",
                DebugType.Data, this);
            return;
        }

        if (!_skillSheetRowProvider.IsLoaded)
        {
            DebugTool.Error("SkillSheetRowProvider가 아직 로드되지 않았습니다.",
                DebugType.Data, this);
            return;
        }

        if (!TryGetOutputFolderPath(out string outputFolderPath))
        {
            return;
        }

        int createdCount = 0;
        int updatedCount = 0;
        foreach (SkillSheetRow row in _skillSheetRowProvider.Rows)
        {
            string assetPath = $"{outputFolderPath}/Skill_{row.Id}.asset";

            SkillDataSO skillData = AssetDatabase.LoadAssetAtPath<SkillDataSO>(assetPath);

            bool isCreated = false;
            if (skillData == null)
            {
                skillData = ScriptableObject.CreateInstance<SkillDataSO>();
                AssetDatabase.CreateAsset(skillData, assetPath);
                isCreated = true;
            }

            SkillDataValues values = ConvertRowToValues(row);
            skillData.OverwriteData(values);

            EditorUtility.SetDirty(skillData);

            if (isCreated)
                createdCount++;
            else
                updatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 마스터 DB 자동 등록
        int registeredCount = SyncDatabase(outputFolderPath);

        DebugTool.Log(
            $"스킬 SO 생성/갱신 완료 - 생성 : {createdCount}, 갱신 : {updatedCount}, DB 등록 : {registeredCount}",
            DebugType.Data, this);
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_outputFolder == null)
            return;

        string path = AssetDatabase.GetAssetPath(_outputFolder);
        if (!AssetDatabase.IsValidFolder(path))
        {
            DebugTool.Warnning(
                $"출력 슬롯에 등록한 자산이 폴더가 아닙니다. (경로: {path})",
                DebugType.Data, this);
            _outputFolder = null;
        }
    }

    private bool TryGetOutputFolderPath(out string outputFolderPath)
    {
        outputFolderPath = string.Empty;

        if (_outputFolder == null)
        {
            DebugTool.Error("출력 폴더가 등록되지 않았습니다.",
                DebugType.Data, this);
            return false;
        }

        string path = AssetDatabase.GetAssetPath(_outputFolder);

        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
        {
            DebugTool.Error($"출력 폴더 경로가 유효하지 않습니다. (경로: {path})",
                DebugType.Data, this);
            return false;
        }

        outputFolderPath = path;
        return true;
    }

    private SkillDataValues ConvertRowToValues(SkillSheetRow row)
    {
        return new SkillDataValues
        {
            Id = row.Id,
            CoolDown = row.CoolDown,
            DamageFormula = row.DamageFormula,
            DebuffValue = row.DebuffValue,
            DebuffDuration = row.DebuffDuration,
            BuffValue = row.BuffValue,
            BuffDuration = row.BuffDuration,
            TargetCount = row.TargetCount
        };
    }

    /// <summary>
    /// 출력 폴더의 모든 SkillDataSO를 스캔해서 SkillDatabaseSO에 등록한다.
    /// 반환값: 등록된 SO 개수. _skillDatabase가 null이면 0.
    /// </summary>
    private int SyncDatabase(string outputFolderPath)
    {
        if (_skillDatabase == null)
        {
            DebugTool.Warnning(
                "SkillDatabaseSO가 연결되지 않아 마스터 등록 단계를 건너뜁니다.",
                DebugType.Data, this);
            return 0;
        }

        string[] guids = AssetDatabase.FindAssets(
            $"t:{nameof(SkillDataSO)}",
            new[] { outputFolderPath });

        List<SkillDataSO> collected = new List<SkillDataSO>(guids.Length);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillDataSO so = AssetDatabase.LoadAssetAtPath<SkillDataSO>(path);
            if (so != null)
                collected.Add(so);
        }

        collected.Sort((a, b) => a.Id.CompareTo(b.Id));

        _skillDatabase.EditorSetAllSkills(collected);
        EditorUtility.SetDirty(_skillDatabase);
        AssetDatabase.SaveAssets();

        return collected.Count;
    }
#endif
}
