using UnityEngine;

public class CameraBase : MonoBehaviour
{
    [Header("카메라 이동 제한 범위")]
    public Vector2 minPos;
    public Vector2 maxPos;

    private Transform target;
    private Camera cam;

    private void Start()
    {
        target = GManager.Instance.IsUserTrans;
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null && GManager.Instance.IsUserTrans != null)
        {
            target = GManager.Instance.IsUserTrans;
            Debug.Log("[CameraBase] target을 재설정: " + target.name);
        }

        if (!GManager.Instance.IsSettingFlag) return;
        if (target == null || cam == null) return;
        Vector3 targetPos = target.position;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        // 맵 사이즈
        float mapWidth = maxPos.x - minPos.x;
        float mapHeight = maxPos.y - minPos.y;

        float clampedX, clampedY;

        // 넓은 맵일 경우 (맵이 카메라보다 큼)
        if (mapWidth > camWidth * 2)
        {
            clampedX = Mathf.Clamp(targetPos.x, minPos.x + camWidth, maxPos.x - camWidth);
        }
        else
        {
            // 맵이 좁으면 중앙 고정
            clampedX = (minPos.x + maxPos.x) / 2f;
        }

        if (mapHeight > camHeight * 2)
        {
            clampedY = Mathf.Clamp(targetPos.y, minPos.y + camHeight, maxPos.y - camHeight);
        }
        else
        {
            clampedY = (minPos.y + maxPos.y) / 2f;
        }

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    public void SetCameraBounds(Vector2 min, Vector2 max)
    {
        minPos = min;
        maxPos = max;
    }
}