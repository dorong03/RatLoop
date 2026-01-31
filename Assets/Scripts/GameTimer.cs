using UnityEngine;
using TMPro;
public class GameTimer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] private float currentTime;

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
        }
        else if (currentTime < 0) 
        {
            currentTime = 0;
            timerText.color = Color.blue;
            //GameOver();
        }
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int second = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00} : {1:00}", minutes, second);
    }
}