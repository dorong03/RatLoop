using UnityEngine;

public class parallaxSprite : MonoBehaviour
{
    // 배경 사진 넘어가는 속도
    [SerializeField, Range(-1f, 1f)] private float parallaxEffect;
    
    private Camera cam;
    private float length;
    private float startpos;

    private void Start()
    {
        cam = Camera.main;

        startpos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void LateUpdate()
    {
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        float dist = (cam.transform.position.x * parallaxEffect);

        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);
    }
}