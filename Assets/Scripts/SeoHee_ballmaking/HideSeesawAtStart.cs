using UnityEngine;

public class HideSeesawExceptBar : MonoBehaviour
{
    [Header("보여줄 바(SeesawBar) 오브젝트")]
    public GameObject seesawBar;             // 보여야 할 바 오브젝트 참조
    [Header("숨기고 싶은 시소 몸통 또는 기타 오브젝트")]
    public GameObject[] hideObjects;         // 숨기고자 하는 다른 오브젝트들 참조

    void Awake()
    {
        //// 바가 지정되어 있으면 활성 상태 유지
        //if (seesawBar != null)
        //{
        //    seesawBar.SetActive(true);
        //}
        //else
        //{
        //    Debug.LogWarning("seesawBar가 할당되지 않았습니다.");
        //}

        // 숨기고 싶은 오브젝트들은 비활성화 => 숨기고 싶은 오브젝트는 메쉬 렌더러 비활성화
        foreach (var obj in hideObjects)
        {
            if (obj != null)
            {
                obj.GetComponent<SpriteRenderer>().enabled = false;
            }
        }
    }
}
