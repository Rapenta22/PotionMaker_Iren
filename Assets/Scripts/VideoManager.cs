using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    [Header("영상 출력용 캔버스")]
    [SerializeField] private GameObject m_videoCanvas;
    [Header("비디오 플레이어")]
    [SerializeField] private VideoPlayer m_videoPlayer;

    private bool m_isVideoPlaying = false;

    void Awake()
    {
        m_videoPlayer.loopPointReached += OnVideoEnd;
        m_videoCanvas.SetActive(false);
    }

    public IEnumerator PlayVideoRoutine(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[VideoManager] 재생할 비디오가 없습니다!");
            yield break;
        }

        // 영상 시작 전: FadeOut-In
        yield return StartCoroutine(GManager.Instance.IsFadeInOut.FadeOut());

        m_videoPlayer.clip = clip;
        m_videoCanvas.SetActive(true);
        m_videoPlayer.Play();
        m_isVideoPlaying = true;

        yield return StartCoroutine(GManager.Instance.IsFadeInOut.FadeIn());

        // 영상 끝까지 대기
        bool videoEnded = false;
        m_videoPlayer.loopPointReached += (vp) => videoEnded = true;
        while (!videoEnded)
            yield return null;

        // 영상 끝나면 FadeOut (영상 닫기)
        yield return StartCoroutine(GManager.Instance.IsFadeInOut.FadeOut());
        m_videoCanvas.SetActive(false);
        m_isVideoPlaying = false;
        yield return StartCoroutine(GManager.Instance.IsFadeInOut.FadeIn());

    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        // 코루틴에서 처리 중
    }
}
