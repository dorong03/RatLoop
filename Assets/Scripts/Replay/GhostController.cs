using UnityEngine;
using System.Collections.Generic;

public class GhostController : MonoBehaviour
{
    private PlayerMovement _playerMovement;
    private Queue<InputFrame> inputFrames;
    
    private bool isPlaying = false;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
    }

    public void Init(List<InputFrame> recordedData)
    {
        inputFrames = new Queue<InputFrame>(recordedData);
        isPlaying = true;
    }

    private void FixedUpdate()
    {
        if (!isPlaying)
        {
            return;
        }

        if (inputFrames.Count > 0)
        {
            InputFrame frame = inputFrames.Dequeue();

            _playerMovement.SetMoveInput(new Vector2(frame.moveX, 0));

            if (frame.isJumpPressed)
            {
                _playerMovement.TryJump();
            }

            if (frame.isHangPressed)
            {
                _playerMovement.TryHang();
            }
        }
        else
        {
            _playerMovement.SetMoveInput(Vector2.zero);
            isPlaying = false;
            
            // 제거할지 아니면 멈출지 고민해보기? ㅋㅋ
        }
    }
}
