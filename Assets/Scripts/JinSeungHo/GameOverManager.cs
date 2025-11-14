using System.Collections;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    // 게임이 끝났을 때 요정이 던지는 것을 비활성화 하기 위해 가져옴
    public GameObject gameLogicManager;

    // 왼쪽 오른쪽의 버블 차이를 계산할 목적으로 가져옴
    public GameObject leftBoxArea;
    public GameObject rightBoxArea;
    // 시소가 기울어 지는 것을 멈추기 위해 가져옴
    public GameObject seesaw;
    // 화면 클릭 감지 버튼 비활성화
    public GameObject clickPanel;


    public float maxDiff = 10;
    public bool isGameOver = false;

    // Update is called once per frame
    void Update()
    {
        int leftBubble = leftBoxArea.GetComponent<CountInsideBox>().currentObjCount;
        int rightBubble = rightBoxArea.GetComponent<CountInsideBox>().currentObjCount;

        int diff = Mathf.Abs(leftBubble - rightBubble);

        // 게임 오버 조건 충족시 게임 오버
        // 여기에 게임 오버시 실행할 것들 작성
        if (diff >= maxDiff || isGameOver)
        {
            isGameOver = true;
            seesaw.GetComponent<SeesawLean>().enabled = false;  // 시소의 코드에 의한 기울임을 멈춤
            seesaw.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;   // 시소가 물리적으로 정지함
            this.GetComponent<ScatterBubble>().ScatteringBubble();    // 버블의 고정이 풀리고 위로 쓸려감
            clickPanel.SetActive(false);

            Debug.Log("게임 오버");

            // UI 호출 및 게임 종료
            GetComponent<UIManager>().setUI();

            // 요정이 버블을 던지는 코드 비활성화
            gameLogicManager.GetComponent<GameManager>().enabled = false;
            // 게임 오버가 되면 모든게 끝나므로 코드 비활성화
            this.enabled = false;
        }
    }
}
