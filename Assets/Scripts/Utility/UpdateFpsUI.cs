using UnityEngine;
using UnityEngine.UI;

public class UpdateFpsUI : MonoBehaviour
{
    private Text fpsText;

    private float frameCount = 0;
    private float deltaTime = 0f;
    private float fps = 0f;

    void Start()
    {
        Application.targetFrameRate = 70;
        fpsText = transform.GetComponent<Text>();
    }
    
    void Update()
    {

    }
}
