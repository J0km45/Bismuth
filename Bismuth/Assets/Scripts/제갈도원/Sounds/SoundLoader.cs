using System;
using System.Collections.Generic;
using UnityEngine;


public static class SoundLoader
{
    private static Dictionary<string, AudioClip> _byKey;
    private static bool _built;

    // .wav 랑 공백 처리 먼저
    public static string NormalizeKey(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        string name = fileName.Trim();
        int lastDot = name.LastIndexOf('.'); // .wav 빼기
        if (lastDot > 0)
            return name.Substring(0, lastDot);

        return name;
    }

    // Resources -> Sounds -> {이름} 안 클립 전부, 파일명이면 해당 클립 하나만 배열로 
    // 시트 이름으로 공격 사운드 찾기
    // 먼저 Sounds -> 이름 폴더를 찾고,
    // 없으면 같은 이름의 단일 클립을 찾음
    public static AudioClip[] AttackGroup(string rawFromSheet)
    {
        string key = NormalizeKey(rawFromSheet);

        AudioClip[] clipsInFolder = Resources.LoadAll<AudioClip>($"Audio/Units/{key}");
        if (clipsInFolder.Length > 0)
            return clipsInFolder;

        EnsureBuilt();

        if (_byKey.TryGetValue(key, out AudioClip clip))
            return new[] { clip };

        return null;
    }

    // 사운드 캐시가 아직 만들어지지 않았다면 한 번만 생성
    private static void EnsureBuilt()
    {
        if (_built)
            return;
        
        _built = true;

        // 사운드 이름을 key 로, AudioClip 을 value 로 저장할 딕셔너리 생성
        // 대소문자는 구분하지 않고 비교
        _byKey = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

       
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio/Units");

        // 불러온 클립이 없으면 종료
        if (clips == null)
            return;

        // 로드한 모든 클립을 순회하면서 딕셔너리에 저장
        foreach (AudioClip audioClip in clips)
        {
            if (audioClip == null)
                continue;

            // 클립 이름을 검색용 key 로 정리
            string key = NormalizeKey(audioClip.name);

            if (string.IsNullOrEmpty(key))
                continue;

            if (_byKey.ContainsKey(key))
                continue;

            // 같으면 등록
            _byKey[key] = audioClip;
        }
    }
}
