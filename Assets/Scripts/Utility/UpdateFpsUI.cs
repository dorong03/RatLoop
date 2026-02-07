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
        Application.targetFrameRate = 60;
        fpsText = transform.GetComponent<Text>();
    }
    
    void Update()
    {
        frameCount++;
        deltaTime += Time.unscaledDeltaTime;
        if (deltaTime > 1)
        {
            fps = frameCount / deltaTime;
            fpsText.text = $"fps : {Mathf.Round(fps)}";

            frameCount = 0;
            deltaTime = 0f;
        }
    }
}
