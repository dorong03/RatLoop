using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerMovement _playerMovement;
    private InputRecorder _inputRecorder;

    private Vector2 moveInputBuffer;
    private bool jumpBuffer;
    private bool hangBuffer;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _inputRecorder = GetComponent<InputRecorder>();
    }

    private void OnMove(InputValue value)
    {
        moveInputBuffer = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpBuffer = true;
        }
    }

    private void OnHang(InputValue value)
    {
        if (value.isPressed)
        {
            hangBuffer = true;
        }
    }

    private void OnRetry(InputValue value)
    {
        if (value.isPressed)
        {
            RecordingManager.instance.StopPlayingRecord();
        }
    }
    
    private void FixedUpdate()
    {
        InputFrame currentFrame = new InputFrame(moveInputBuffer.x, jumpBuffer, hangBuffer);

        _inputRecorder?.Record(currentFrame);
        RunInput(currentFrame);
        
        jumpBuffer = false;
        hangBuffer = false;
    }
    
    private void RunInput(InputFrame frame)
    {
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
}
