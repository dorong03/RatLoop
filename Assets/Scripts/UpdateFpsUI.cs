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
        fpsText = transform.GetComponent<Text>();
    }
    
    void Update()
    {
        Debug.Log("Test");
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
