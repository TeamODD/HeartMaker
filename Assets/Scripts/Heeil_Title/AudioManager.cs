using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("=== Mixer & Params ===")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string bgmParam = "BGMVol";
    [SerializeField] private string sfxParam = "SFXVol";

    public static event Action VolumesApplied;
    public bool IsReady { get; private set; }
    public AudioMixer Mixer => mixer;
    public AudioMixerGroup SfxGroup { get; private set; }

    const string KEY_MASTER = "vol.master";
    const string KEY_BGM = "vol.bgm";
    const string KEY_SFX = "vol.sfx";
    const float MIN_DB = -80f, MAX_DB = 0f;

    float master01, bgm01, sfx01;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (mixer == null)
        {
            Debug.LogWarning("🚫 AudioMixer가 연결되지 않았습니다.");
        }
        else
        {
            var groups = mixer.FindMatchingGroups("SFX");
            if (groups != null && groups.Length > 0)
            {
                SfxGroup = groups[0];
                Debug.Log("✅ SFX 그룹 자동 연결 완료");
            }
            else
            {
                Debug.LogWarning("🚫 'SFX' 그룹을 Mixer에서 찾을 수 없습니다.");
            }
        }

        LoadVolumes();
        ApplyAll();
        StartCoroutine(LateBoot());
        IsReady = true;
    }

    IEnumerator LateBoot()
    {
        yield return null;
        ApplyAll();
        VolumesApplied?.Invoke();
    }

    public void SetMixer(AudioMixer m)
    {
        mixer = m;
        var groups = mixer.FindMatchingGroups("SFX");
        if (groups != null && groups.Length > 0)
        {
            SfxGroup = groups[0];
            Debug.Log("✅ SFX 그룹 수동 연결 완료");
        }
        else
        {
            Debug.LogWarning("🚫 'SFX' 그룹을 Mixer에서 찾을 수 없습니다.");
        }

        ApplyAll();
    }

    public void SetMaster01(float v)
    {
        master01 = Mathf.Clamp01(v);
        Apply(masterParam, master01);
        PlayerPrefs.SetFloat(KEY_MASTER, master01);
    }

    public void SetBGM01(float v)
    {
        bgm01 = Mathf.Clamp01(v);
        Apply(bgmParam, bgm01);
        PlayerPrefs.SetFloat(KEY_BGM, bgm01);
    }

    public void SetSFX01(float v)
    {
        sfx01 = Mathf.Clamp01(v);
        Apply(sfxParam, sfx01);
        PlayerPrefs.SetFloat(KEY_SFX, sfx01);
    }

    public float GetMaster01() => master01;
    public float GetBGM01() => bgm01;
    public float GetSFX01() => sfx01;

    void LoadVolumes()
    {
        master01 = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        bgm01 = PlayerPrefs.GetFloat(KEY_BGM, 1f);
        sfx01 = PlayerPrefs.GetFloat(KEY_SFX, 1f);
    }

    void ApplyAll()
    {
        if (mixer == null) return;
        Apply(masterParam, master01);
        Apply(bgmParam, bgm01);
        Apply(sfxParam, sfx01);
    }

    void Apply(string param, float v01)
    {
        if (mixer == null)
        {
            Debug.LogWarning($"🚫 Mixer가 설정되지 않아 '{param}' 적용 실패");
            return;
        }

        float db = (v01 <= 0.0001f) ? MIN_DB : Mathf.Log10(v01) * 20f;
        bool success = mixer.SetFloat(param, Mathf.Clamp(db, MIN_DB, MAX_DB));

        if (!success)
        {
            Debug.LogWarning($"⚠️ Mixer 파라미터 '{param}' 적용 실패");
        }
        else
        {
            Debug.Log($"🔊 {param} 볼륨 적용: {v01 * 100f:F0}% ({Mathf.Clamp(db, MIN_DB, MAX_DB)} dB)");
        }
    }
}