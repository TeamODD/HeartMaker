// AudioManager.cs (추가/수정본)
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager2 : MonoBehaviour
{
    public static AudioManager2 I { get; private set; }

    [Header("=== Mixer & Params ===")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string bgmParam    = "BGMVol";
    [SerializeField] private string sfxParam    = "SFXVol";

    [Header("=== Mixer Groups (선택) ===")]
    [SerializeField] private AudioMixerGroup bgmGroup; // BGM 출력 그룹(없으면 null 가능)
    public AudioMixerGroup SfxGroup { get; private set; }

    public static event Action VolumesApplied;
    public bool IsReady { get; private set; }
    public AudioMixer Mixer => mixer;

    const string KEY_MASTER = "vol.master";
    const string KEY_BGM    = "vol.bgm";
    const string KEY_SFX    = "vol.sfx";
    const float MIN_DB = -80f, MAX_DB = 0f;

    float master01, bgm01, sfx01;

    // ==== BGM 전용 ====
    private AudioSource bgmA, bgmB;     // 크로스페이드용 2트랙
    private AudioSource bgmActive;      // 현재 재생 중 소스
    private Coroutine bgmRoutine;
    [SerializeField] private float defaultBGMCrossFade = 0.5f;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
        InitBgmSources();   // BGM 소스 준비

        ApplyAll();
        StartCoroutine(LateBoot()); // 초기화 순서 이슈 대비
        IsReady = true;
    }

    IEnumerator LateBoot()
    {
        yield return null;
        ApplyAll();
        VolumesApplied?.Invoke();
    }

    void InitBgmSources()
    {
        // 자식 오브젝트 두 개 만들고 오디오소스 2개 준비(크로스페이드)
        bgmA = CreateBgmSource("BGM_A");
        bgmB = CreateBgmSource("BGM_B");
        bgmActive = bgmA;
    }

    AudioSource CreateBgmSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.outputAudioMixerGroup = bgmGroup; // null이면 유니티 기본출력 사용
        return src;
    }

    // 외부에서 mixer를 주입할 때 호출(있으면 그룹도 찾아줌)
    public void SetMixer(AudioMixer m)
    {
        mixer = m;

        // SFX 그룹 찾기(있으면)
        var sfxGroups = mixer.FindMatchingGroups("SFX");
        if (sfxGroups != null && sfxGroups.Length > 0) SfxGroup = sfxGroups[0];

        // BGM 그룹 자동 연결 시도(미리 인스펙터에서 지정해도 됨)
        if (bgmGroup == null)
        {
            var bgmGroups = mixer.FindMatchingGroups("BGM");
            if (bgmGroups != null && bgmGroups.Length > 0) bgmGroup = bgmGroups[0];
            if (bgmA) bgmA.outputAudioMixerGroup = bgmGroup;
            if (bgmB) bgmB.outputAudioMixerGroup = bgmGroup;
        }

        ApplyAll();
    }

    public void SetMaster01(float v){ master01 = Mathf.Clamp01(v); Apply(masterParam, master01); PlayerPrefs.SetFloat(KEY_MASTER, master01); }
    public void SetBGM01   (float v){ bgm01    = Mathf.Clamp01(v); Apply(bgmParam,    bgm01);    PlayerPrefs.SetFloat(KEY_BGM,    bgm01);    SyncBgmVolumes(); }
    public void SetSFX01   (float v){ sfx01    = Mathf.Clamp01(v); Apply(sfxParam,    sfx01);    PlayerPrefs.SetFloat(KEY_SFX,    sfx01);    }

    public float GetMaster01()=> master01;
    public float GetBGM01()   => bgm01;
    public float GetSFX01()   => sfx01;

    void LoadVolumes()
    {
        master01 = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        bgm01    = PlayerPrefs.GetFloat(KEY_BGM,    1f);
        sfx01    = PlayerPrefs.GetFloat(KEY_SFX,    1f);
    }

    void ApplyAll()
    {
        if (mixer == null) return;
        Apply(masterParam, master01);
        Apply(bgmParam,    bgm01);
        Apply(sfxParam,    sfx01);
        SyncBgmVolumes();
    }

    void Apply(string param, float v01)
    {
        if (mixer == null) return;
        float db = (v01 <= 0.0001f) ? MIN_DB : Mathf.Log10(v01) * 20f;
        mixer.SetFloat(param, Mathf.Clamp(db, MIN_DB, MAX_DB));
    }

    void SyncBgmVolumes()
    {
        // 믹서에 맡겨도 되지만, 크로스페이드 시 상대 볼륨 보정을 위해 소스 볼륨도 1.0 기준으로 맞춰둠
        if (bgmA) bgmA.volume = 1f;
        if (bgmB) bgmB.volume = 1f;
    }

    // ======== BGM API ========

    /// <summary>
    /// 같은 클립이면 기본적으로 재시작하지 않음(=끊김 없음). 바꾸고 싶으면 restartIfSame = true.
    /// </summary>
    public void PlayBGM(AudioClip clip, float fade = -1f, bool restartIfSame = false)
    {
        if (clip == null) return;
        float useFade = (fade < 0f) ? defaultBGMCrossFade : fade;

        // 이미 같은 곡 재생 중 + 재시작 원치 않으면 아무것도 하지 않음
        if (bgmActive != null && bgmActive.clip == clip && bgmActive.isPlaying && !restartIfSame)
            return;

        if (bgmRoutine != null) StopCoroutine(bgmRoutine);
        bgmRoutine = StartCoroutine(CoCrossFadeTo(clip, useFade));
    }

    public void StopBGM(float fade = 0.3f)
    {
        if (bgmRoutine != null) StopCoroutine(bgmRoutine);
        bgmRoutine = StartCoroutine(CoStopBGM(fade));
    }

    public void PauseBGM()  { if (bgmActive) bgmActive.Pause();  }
    public void UnpauseBGM(){ if (bgmActive) bgmActive.UnPause(); }

    public bool IsPlayingBGM(AudioClip clip)
    {
        return bgmActive && bgmActive.clip == clip && bgmActive.isPlaying;
    }

    IEnumerator CoCrossFadeTo(AudioClip nextClip, float fade)
    {
        var from = bgmActive;
        var to   = (bgmActive == bgmA) ? bgmB : bgmA;

        to.clip = nextClip;
        to.time = 0f;           // 이어듣기를 원하면 여기서 time을 설정
        to.volume = 0f;
        to.Play();

        if (fade <= 0f)
        {
            // 즉시 전환
            if (from) from.Stop();
            to.volume = 1f;
            bgmActive = to;
            yield break;
        }

        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime; // 씬 전환/일시정지 영향 최소화
            float k = Mathf.Clamp01(t / fade);
            if (from) from.volume = 1f - k;
            to.volume = k;
            yield return null;
        }

        if (from) { from.Stop(); from.volume = 1f; }
        to.volume = 1f;
        bgmActive = to;
    }

    IEnumerator CoStopBGM(float fade)
    {
        var from = bgmActive;
        if (from == null || !from.isPlaying) yield break;

        if (fade <= 0f)
        {
            from.Stop();
            yield break;
        }

        float start = from.volume;
        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fade);
            from.volume = Mathf.Lerp(start, 0f, k);
            yield return null;
        }
        from.Stop();
        from.volume = 1f;
    }
}
