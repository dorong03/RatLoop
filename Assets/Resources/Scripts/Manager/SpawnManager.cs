using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private Transform spawnPoint;

    private List<List<InputFrame>> allRecordedFrames;
    private List<ActiveGhost> activeGhostList;
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
            Debug.LogError("spawnPoint is null");
            return;
        }

        currentPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        
        AlignToGround(currentPlayer);

        currentPlayerRecorder = currentPlayer.GetComponent<InputRecorder>();
        currentPlayerRecorder.StartRecording();
    }

    private void AlignToGround(GameObject player)
    {
        Rigidbody2D _rigidbody = player.GetComponent<Rigidbody2D>();
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();

        BoxCollider2D col = player.GetComponent<BoxCollider2D>();
        float halfHeight = 0f;

        if (col != null)
        {
            halfHeight = (col.size.y * player.transform.localScale.y) * 0.5f;
            halfHeight -= col.offset.y;
        }
        
        RaycastHit2D hit = Physics2D.Raycast(player.transform.position, Vector2.down, 10f, LayerMask.GetMask("Ground"));

        if (hit.collider != null)
        {
            Vector3 finalPos = new Vector3(hit.point.x, hit.point.y + halfHeight, player.transform.position.z);
            player.transform.position = finalPos;
        }
    }

    public void StopRecordingAndReStart()
    {
        currentPlayerRecorder.StopRecording();
        List<InputFrame> inputFrames = currentPlayerRecorder.GetInputFrames();
        allRecordedFrames.Add(inputFrames);

        foreach (var ghost in activeGhostList)
        {
            Destroy(ghost.controller.gameObject);
        }
        activeGhostList.Clear();

        currentPlayer.GetComponent<InputRecorder>().StartRecording();

        CameraEffect cameraEffect = FindFirstObjectByType<CameraEffect>();
        if (cameraEffect != null)
        {
            cameraEffect.SetTarget(currentPlayer.transform);
        }
        
        currentReplayIndex = 0;
        SoundManager.instance.PlaySFX(SfxType.Replay);

        for (int i = 0; i < 1; i++)
        {
            foreach (var record in allRecordedFrames)
            {
                GameObject ghost = Instantiate(ghostPrefab, spawnPoint.position, Quaternion.identity);
                GhostController ghostController = ghost.GetComponent<GhostController>();
                ActiveGhost addedGhost = new ActiveGhost
                {
                    controller = ghostController,
                    recordedFrames = record
                };

                activeGhostList.Add(addedGhost);
            }
        }
    }
}

[System.Serializable]
public class ActiveGhost
{
    public GhostController controller;
    public List<InputFrame> recordedFrames;
}