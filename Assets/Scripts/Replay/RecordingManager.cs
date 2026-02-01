using System.Collections.Generic;
using UnityEngine;

public class RecordingManager : MonoBehaviour
{
    public static RecordingManager instance;

    [Header("프리팹 설정")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject ghostPrefab;

    [Header("스폰 위치")] 
    public Transform spawnPoint;

    private List<List<InputFrame>> allRecordedFrames = new List<List<InputFrame>>();

    private InputRecorder currentPlayerRecorder;
    private GameObject currentPlayer;
    private List<GameObject> ghostList = new List<GameObject>();
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayerAllRecord();
    }
    
    public void PlayerAllRecord()
    {
        currentPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        currentPlayerRecorder = currentPlayer.GetComponent<InputRecorder>();
        currentPlayerRecorder.StartRecording();
        
        if (!(allRecordedFrames.Count > 0))
        {
            return;
        }

        foreach (var record in allRecordedFrames)
        {
            GameObject ghost = Instantiate(ghostPrefab, spawnPoint.position, Quaternion.identity);
            ghostList.Add(ghost);
            ghost.GetComponent<GhostController>().Init(record);    
        }
    }

    public void StopPlayingRecord()
    {
        currentPlayerRecorder.StopRecording();
        List<InputFrame> inputFrames = currentPlayerRecorder.GetInputFrames();
        allRecordedFrames.Add(inputFrames);
        
        Destroy(currentPlayer);

        foreach (GameObject ghost in ghostList)
        {
            Destroy(ghost);
        }
        
        Invoke("PlayerAllRecord", 1f);
    }
}
