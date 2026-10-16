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

    private readonly List<List<InputFrame>> allRecordedFrames = new List<List<InputFrame>>();
    private readonly List<ActiveGhost> activeGhostList = new List<ActiveGhost>();
    private int currentReplayIndex;

    private InputRecorder currentPlayerRecorder;
    private GameObject currentPlayer;

    private void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // LevelLoader 가 레벨을 생성한 직후 호출한다. (이 컴포넌트의 Awake 보다 먼저 불릴 수 있다)
    public void Initialize(Transform levelSpawnPoint)
    {
        instance = this;
        spawnPoint = levelSpawnPoint;
        allRecordedFrames.Clear();
        activeGhostList.Clear();
        currentReplayIndex = 0;
        SpawnPlayer();
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

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            Destroy(player);
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
        
        SpawnPlayer();

        CameraEffect cameraEffect = FindFirstObjectByType<CameraEffect>();
        if (cameraEffect != null)
        {
            cameraEffect.SetTarget(currentPlayer.transform);
        }
        
        currentReplayIndex = 0;
        SoundManager.instance.PlaySFX(SfxType.Replay);

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

[System.Serializable]
public class ActiveGhost
{
    public GhostController controller;
    public List<InputFrame> recordedFrames;
}