using UnityEngine;

public class MouseIndicator: MonoBehaviour
{
    private Vector2 startPos;
    private RectTransform rectTransform;

    private void Awake()
    {
        // 일단 상대 좌표로 위치 받아오고
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }

    // 쥐가 스테이지 버튼에 도착하면 껐다 켤다 할건데 그때 위치 초기화 시켜주죠
    private void OnEnable()
    {
        gameObject.GetComponent<RectTransform>().anchoredPosition = startPos;   
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * 2f) * 50f;
        rectTransform.anchoredPosition = startPos + Vector2.up * offset;
    }
}
