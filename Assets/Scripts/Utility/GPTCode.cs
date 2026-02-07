using UnityEngine;

public class GPTCode : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject cam; // 메인 카메라
    [SerializeField, Range(0f, 1f)] private float parallaxEffect; // 1에 가까울수록 같이 움직임(먼 배경), 0은 고정

    private float length; // 이미지 가로 길이
    private float startpos; // 시작 위치

    private void Start()
    {
        // 카메라가 없으면 자동으로 찾기
        if (cam == null) cam = Camera.main.gameObject;

        startpos = transform.position.x;
        // 스프라이트의 가로 길이를 구함 (무한 스크롤용)
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void LateUpdate()
    {
        // 카메라가 움직인 거리만큼 배경도 이동해야 함
        // temp: 배경이 카메라를 따라가지 않고 얼마나 '뒤쳐졌는지' 계산 (무한 스크롤 위치 재계산용)
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        
        // dist: 실제 배경이 이동해야 할 거리 (시차 적용)
        float dist = (cam.transform.position.x * parallaxEffect);

        // 배경 위치 이동 (Y축은 유지)
        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

        // --- [무한 스크롤 로직] ---
        // 배경이 카메라보다 너무 뒤쳐지거나 앞서가면, 위치를 뚝 떼어서 옆으로 붙임
        if (temp > startpos + length) startpos += length;
        else if (temp < startpos - length) startpos -= length;
    }
}