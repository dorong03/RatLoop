using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RecordingManager : MonoBehaviour
{
    public static RecordingManager instance;

    [Header("프리팹 설정")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject ghostPrefab;

    [Header("스폰 위치")] 
    public Transform spawnPoint;

    private List<List<InputFrame>> allRecordedFrames = new List<List<InputFrame>>();
    private List<ActiveGhost> activeGhostList = new List<ActiveGhost>();

    private InputRecorder currentPlayerRecorder;
    private GameObject currentPlayer;
    private int currentReplayIndex;
    
    private bool isPlaying = false;
    
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
        SpawnPlayer();
        CameraEffect effect = Camera.main.GetComponent<CameraEffect>();
        effect.PlayPreView();
    }

    private void FixedUpdate()
    {
        if (!isPlaying)
        {
            return;
        }

        for (int i = 0; i < activeGhostList.Count; i++)
        {
            ActiveGhost ghost = activeGhostList[i];

            if (currentReplayIndex < ghost.recordedFrames.Count)
            {
                ghost.controller.GhostRunInput(ghost.recordedFrames[currentReplayIndex]);
            }
            else
            {
                ghost.controller.StopGhost();
            }
        }
        currentReplayIndex++;
    }
    
    
    public void PlayerAllRecord()
    {
        CancelInvoke("PlayerAllRecord");
        
        isPlaying = true;
        currentReplayIndex = 0;
        SpawnPlayer();
        GameManager.instance.PlayerAlive();
        activeGhostList.Clear();
        
        if (!(allRecordedFrames.Count > 0))
        {
            return;
        }
        
        foreach (var record in allRecordedFrames)
        {
            // for(int i = 0 ;i < 1; i++)
            // {
            GameObject ghost = Instantiate(ghostPrefab, spawnPoint.position, Quaternion.identity);
            GhostController ghostController = ghost.GetComponent<GhostController>();
            
            ActiveGhost addedGhost = new ActiveGhost();
            addedGhost.controller = ghostController;
            addedGhost.recordedFrames = record;
            
            activeGhostList.Add(addedGhost);
            // }
        }
    }

    private void SpawnPlayer()
    {
        currentPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        currentPlayerRecorder = currentPlayer.GetComponent<InputRecorder>();
        currentPlayerRecorder.StartRecording();
    }

    public void StopPlayingRecord()
    {
        CancelInvoke("PlayerAllRecord");
        isPlaying = false;
        
        currentPlayerRecorder.StopRecording();
        List<InputFrame> inputFrames = currentPlayerRecorder.GetInputFrames();
        
        allRecordedFrames.Add(inputFrames);
        
        Destroy(currentPlayer);

        foreach (var ghost in activeGhostList)
        {
            Destroy(ghost.controller.gameObject);
        }
        activeGhostList.Clear();
        
        Invoke("PlayerAllRecord", 1f);
    }
}

[System.Serializable]
public class ActiveGhost
{
    public GhostController controller;
    public List<InputFrame> recordedFrames;
}
