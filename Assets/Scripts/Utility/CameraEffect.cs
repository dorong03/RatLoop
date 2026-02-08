using System.Collections;
using UnityEngine;

public class CameraEffect : MonoBehaviour
{
    private Transform target;
    [SerializeField] Transform cameraSpawnPoint;

    public float smoothTime = 0.3f;
    public Vector3 offset = new Vector3(0, 0, -10f);
    
    private float defaultYOffset;
    [SerializeField] private float lookDownAmount = 4f;

    private Vector3 _velocity = Vector3.zero;

    private void Start()
    {
        defaultYOffset = offset.y;
        
        if (cameraSpawnPoint != null)
            gameObject.transform.position = cameraSpawnPoint.position;
    }
    
    public void SetLookDown(bool isLookingDown)
    {
        if (isLookingDown)
        {
            offset.y = defaultYOffset - lookDownAmount;
        }
        else
        {
            offset.y = defaultYOffset;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
            return;
        }
        
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothTime);
    }

    public void PlayPreView()
    {
        StartCoroutine(PlayStartSequence());
    }
    
    public IEnumerator PlayStartSequence()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        target = player;
        yield return new WaitForSeconds(2f);

        GameObject cheese = GameObject.FindGameObjectWithTag("Cheese");
        if (cheese != null)
        {
            target = cheese.transform;
            yield return new WaitForSeconds(2f);
        }

        target = player;
        yield return new WaitForSeconds(2f);
        GameManager.instance.PlayerAlive();
    }
}