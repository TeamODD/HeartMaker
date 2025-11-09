// BGMKeepAliveChild.cs
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMKeepAliveChild : MonoBehaviour
{
    private static BGMKeepAliveChild instance;
    private AudioSource src;

    void Awake()
    {
        // 같은 스크립트가 또 생기면 자신을 정리 (중복 방지)
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;

        // AudioManager 밑으로 귀속(있다면), 부모가 DontDestroyOnLoad이므로 함께 유지됨
        if (AudioManager.I != null)
        {
            transform.SetParent(AudioManager.I.transform, false);
        }
        else
        {
            // 혹시 AudioManager가 없을 드문 경우 대비
            DontDestroyOnLoad(gameObject);
        }

        src = GetComponent<AudioSource>();
        src.loop = true;

        // 첫 씬에서 한 번만 재생
        if (!src.isPlaying && src.clip != null)
            src.Play();
    }
}
