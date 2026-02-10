using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;
    
    // 프리팹 설정
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject ghostPrefab;
    
    // 스폰 위치 설정
    [SerializeField] private Transform spawnPoint;
    
    // 플레이어 입력받은거 리스트
    private List<List<InputFrame>> allRecordedFrames;
    // 현재 있는 고스트 리스트
    private List<ActiveGhost> activeGhostList;
    // 리플레이 횟수
    private int currentReplayIndex;
    
    private InputRecorder currentPlayerRecorder;
    private GameObject currentPlayer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            allRecordedFrames = new List<List<InputFrame>>();
            activeGhostList = new List<ActiveGhost>();
            currentReplayIndex = 0;
            SpawnPlayer();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        // 맵에 존재하는 고스트가 존재하지 않으면 Return
        if (activeGhostList.Count == 0)
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

    private void SpawnPlayer()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("spawnPoint 설정하기");
        }
        currentPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        currentPlayerRecorder = currentPlayer.GetComponent<InputRecorder>();
        currentPlayerRecorder.StartRecording();
    }

    public void StopRecordingAndReStart()
    {
        currentPlayerRecorder.StopRecording();
        List<InputFrame> inputFrames = currentPlayerRecorder.GetInputFrames();
        allRecordedFrames.Add(inputFrames);
        Destroy(currentPlayer);
        foreach (var ghost in activeGhostList)
        {
            Destroy(ghost.controller.gameObject);
        }
        activeGhostList.Clear();
        SpawnPlayer();
        CameraEffect cameraEffect = FindFirstObjectByType<CameraEffect>();
        cameraEffect.SetTarget(currentPlayer.transform);
        currentReplayIndex = 0;
        SoundManager.instance.PlaySFX(SfxType.Replay);
        foreach (var record in allRecordedFrames)
        {
            GameObject ghost = Instantiate(ghostPrefab, spawnPoint.position, Quaternion.identity);
            GhostController ghostController = ghost.GetComponent<GhostController>();
            ActiveGhost addedGhost = new ActiveGhost();
            addedGhost.controller = ghostController;
            addedGhost.recordedFrames = record;
            
            activeGhostList.Add(addedGhost);
        }
    }
}

[System.Serializable]
public class ActiveGhost
{
    public GhostController controller;
    public List<InputFrame> recordedFrames;
}
