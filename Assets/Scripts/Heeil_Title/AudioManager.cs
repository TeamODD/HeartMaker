using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Assign in Inspector")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string bgmParam    = "BGMVol";
    [SerializeField] private string sfxParam = "SFXVol";
    
    public static event Action VolumesApplied; // ← 알림 이벤트
    public bool IsReady { get; private set; }   // ← 준비 플래그

    // PlayerPrefs Keys (0~1 값 저장)
    private const string KEY_MASTER = "vol.master";
    private const string KEY_BGM    = "vol.bgm";
    private const string KEY_SFX    = "vol.sfx";

    // 0~1 → dB 맵핑 한계
    private const float MIN_DB = -80f; // 무음 근사
    private const float MAX_DB = 0f;   // 기준

    // 내부 캐시 (슬라이더 기본값 용)
    [SerializeField] private float master01 = 1f;
    [SerializeField] private float bgm01    = 0.5f;
    [SerializeField] private float sfx01    = 0.5f;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();    // PlayerPrefs → 0~1
        ApplyAll();          // ← 실제 믹서에 반영
        IsReady = true;      // ← 이제 준비됨
        VolumesApplied?.Invoke(); // ← 구독자(슬라이더)에게 알림
    }

    // ====== 외부 UI에서 쓰는 Setter/Getter ======

    public void SetMaster01(float v) { master01 = Mathf.Clamp01(v); Apply(masterParam, master01); Save(KEY_MASTER, master01); }
    public void SetBGM01   (float v) { bgm01    = Mathf.Clamp01(v); Apply(bgmParam,    bgm01);    Save(KEY_BGM,    bgm01);    }
    public void SetSFX01   (float v) { sfx01    = Mathf.Clamp01(v); Apply(sfxParam,    sfx01);    Save(KEY_SFX,    sfx01);    }

    public float GetMaster01() => master01;
    public float GetBGM01()    => bgm01;
    public float GetSFX01()    => sfx01;

    // ====== 내부 구현 ======

    private void LoadVolumes()
    {
        master01 = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        bgm01    = PlayerPrefs.GetFloat(KEY_BGM,    1f);
        sfx01    = PlayerPrefs.GetFloat(KEY_SFX,    1f);
    }

    private void Save(string key, float v) => PlayerPrefs.SetFloat(key, v);

    private void ApplyAll()
    {
        Apply(masterParam, master01);
        Apply(bgmParam,    bgm01);
        Apply(sfxParam,    sfx01);
    }

    private void Apply(string param, float v01)
    {
        // 0~1 → dB (로그 곡선: 체감상 자연스러움)
        // v=0 이면 -80dB 근사치
        float db = (v01 <= 0.0001f) ? MIN_DB : Mathf.Log10(v01) * 20f;
        db = Mathf.Clamp(db, MIN_DB, MAX_DB);
        mixer.SetFloat(param, db);
    }
}
