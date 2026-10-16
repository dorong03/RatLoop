using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIAnimation : MonoBehaviour
{
    [SerializeField] private List<Sprite> frames;
    [SerializeField] private Image image;
    [SerializeField] private float frameRate = 0.3f;
    [SerializeField] private GameObject backgroundPanel;

    private Coroutine animationCoroutine;
    private Color color;

    private void Start()
    {
        color = image.color;
        color.a = 0f;
        image.color = color;

        if (backgroundPanel != null)
            backgroundPanel.SetActive(false);
    }

    public void PlayAnimation(string nextSceneName)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimationCoroutine(nextSceneName));
    }

    private IEnumerator AnimationCoroutine(string nextSceneName)
    {
        if (frames == null || frames.Count == 0)
        {
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        color.a = 1f;
        image.color = color;

        if (backgroundPanel != null)
            backgroundPanel.SetActive(true);

        for (int i = 0; i < frames.Count; i++)
        {
            image.sprite = frames[i];

            if (i < frames.Count - 1)
                yield return new WaitForSeconds(frameRate);
        }

        SceneManager.LoadScene(nextSceneName);
    }
}