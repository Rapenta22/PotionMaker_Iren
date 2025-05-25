using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    private static string targetScene;
    private static Action onAfterSceneLoad;
    private static bool playIntro;

    void Awake()
    {
        Debug.Log("[SceneLoader] Awake() 실행 - 중복 구독 방지 및 초기화");
        // 중복 구독 방지
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        DontDestroyOnLoad(gameObject);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneLoader] 씬 로드됨: {scene.name}");

        if (GManager.Instance != null)
        {
            Debug.Log("[SceneLoader] GManager.Instance.AutoReferenceSceneObjects() 호출");
            GManager.Instance.AutoReferenceSceneObjects();
        }
        else
        {
            Debug.LogWarning("[SceneLoader] GManager.Instance가 null입니다!");
        }

        StartCoroutine(DelayedStart(scene));
    }

    private IEnumerator DelayedStart(Scene scene)
    {
        Debug.Log("[SceneLoader] DelayedStart() - 한 프레임 대기");
        yield return null;  // 씬 오브젝트가 모두 로드되고 활성화될 때까지 한 프레임 대기

        if (scene.name == "LoadingScene")
        {
            Debug.Log("[SceneLoader] LoadingScene에서 StartLoading 호출");
            GManager.Instance.IsLoadingManager.StartLoading(targetScene, playIntro);
        }
        else if (scene.name == "MainGame")
        {
            Debug.Log("[SceneLoader] MainGame에 진입");

            // 맵 찾기
            GameObject map = GameObject.Find("MapM0_CityHall"); // 씬에 맞게 이름 조정
            if (map != null)
            {
                Debug.Log("[SceneLoader] 맵 오브젝트 찾음");
                BoxCollider2D mapCollider = map.GetComponent<BoxCollider2D>();
                if (mapCollider != null && GManager.Instance.IsCameraBase != null)
                {
                    Debug.Log("[SceneLoader] 맵 콜라이더와 카메라베이스 찾음");
                    var bounds = mapCollider.bounds;
                    GManager.Instance.IsCameraBase.SetCameraBounds(bounds.min, bounds.max);
                    Debug.Log($"[SceneLoader] Camera bounds set: min={bounds.min}, max={bounds.max}");
                }
                else
                {
                    Debug.LogWarning("[SceneLoader] 맵 콜라이더 또는 카메라 베이스를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[SceneLoader] 맵 오브젝트를 찾을 수 없습니다.");
            }

            // 캐릭터 찾기 및 세팅
            var character = GameObject.Find("Character");
            if (character != null)
            {
                Debug.Log("[SceneLoader] 캐릭터 찾음");
                GManager.Instance.Setting(character);
                Debug.Log("[SceneLoader] GManager.Setting 호출 완료");
            }
            else
            {
                Debug.LogWarning("[SceneLoader] 캐릭터 오브젝트를 찾을 수 없습니다.");
            }

            // onAfterSceneLoad 액션이 있으면 호출
            if (onAfterSceneLoad != null)
            {
                Debug.Log("[SceneLoader] onAfterSceneLoad 콜백 호출");
            }
            onAfterSceneLoad?.Invoke();
            onAfterSceneLoad = null;
        }
        else
        {
            Debug.Log($"[SceneLoader] {scene.name} 씬에는 특별한 처리가 없습니다.");
            onAfterSceneLoad?.Invoke();
            onAfterSceneLoad = null;
        }
    }

    public static void LoadScene(string sceneName, bool isPlayIntro, Action afterLoad = null)
    {
        Debug.Log($"[SceneLoader] LoadScene 호출! targetScene={sceneName}, playIntro={isPlayIntro}");
        targetScene = sceneName;
        playIntro = isPlayIntro;
        onAfterSceneLoad = afterLoad;

        Debug.Log("[SceneLoader] FadeInOut을 통한 로딩씬으로 전환 시작");
        GManager.Instance.StartCoroutine(GManager.Instance.IsFadeInOut.LoadSceneWithFade("LoadingScene"));
    }
}
