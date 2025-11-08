// VolumeSlider.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    public enum Channel { Master, BGM, SFX }
    [SerializeField] private Channel channel = Channel.Master;
    [SerializeField] private Slider slider;

    private void Reset()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (!slider) slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnSliderChanged);

        // AudioManager 준비될 때까지 기다렸다가 초기값 "적용"까지 수행
        StartCoroutine(InitAndApplyOnce());

        // AudioManager가 나중에 재적용(씬 전환 등)하면 현재 값으로 다시 반영
        AudioManager.VolumesApplied += OnVolumesApplied;
    }

    private void OnDisable()
    {
        slider?.onValueChanged.RemoveListener(OnSliderChanged);
        AudioManager.VolumesApplied -= OnVolumesApplied;
    }

    private IEnumerator InitAndApplyOnce()
    {
        // 최대 2초 정도 대기 (필요 시 늘리거나 WaitUntil 사용)
        float t = 0f;
        while (AudioManager.I == null || !AudioManager.I.IsReady)
        {
            t += Time.unscaledDeltaTime;
            if (t > 2f) break; // 안전 탈출
            yield return null;
        }

        float v = ReadCurrent();
        // 표시값만 먼저 맞추고…
        slider.SetValueWithoutNotify(v);
        // 실제 믹서에 즉시 반영
        Apply(v);
    }

    private void OnVolumesApplied()
    {
        // 매니저가 재적용을 수행하면, 내 현재 슬라이더 값으로 다시 한번 강제 반영
        Apply(slider ? slider.value : 1f);
    }

    private void OnSliderChanged(float v) => Apply(v);

    private float ReadCurrent()
    {
        if (AudioManager.I == null) return 1f;
        switch (channel)
        {
            case Channel.BGM: return AudioManager.I.GetBGM01();
            case Channel.SFX: return AudioManager.I.GetSFX01();
            default:          return AudioManager.I.GetMaster01();
        }
    }

    private void Apply(float v)
    {
        if (AudioManager.I == null) return;
        switch (channel)
        {
            case Channel.BGM: AudioManager.I.SetBGM01(v);   break;
            case Channel.SFX: AudioManager.I.SetSFX01(v);   break;
            default:          AudioManager.I.SetMaster01(v);break;
        }
    }
}
