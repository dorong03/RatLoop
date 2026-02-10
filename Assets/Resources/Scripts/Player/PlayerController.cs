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
        if (GameManager.instance.gameState == GameState.Playing)
        {
            moveInputBuffer = value.Get<Vector2>();
        }
    }

    private void OnJump(InputValue value)
    {
        if (GameManager.instance.gameState == GameState.Playing)
        {
            if (value.isPressed)
            {
                jumpBuffer = true;
            }    
        }
    }

    private void OnRetry(InputValue value)
    {
        if (GameManager.instance.gameState == GameState.Playing)
        {
            if (value.isPressed)
            {
                GameManager.instance.Replay();
            }
        }
    }

    private void OnCameraDown(InputValue value)
    {
        if (GameManager.instance.gameState != GameState.Playing)
        {
            return;
        }
        var camEffect = Camera.main.GetComponent<CameraEffect>();
    
        if (camEffect != null)
        {
            camEffect.SetLookDown(value.isPressed);
        }
    }

    private void OnHang(InputValue value)
    {
        if (GameManager.instance.gameState == GameState.Playing)
        {
            if (value.isPressed)
            {
                hangBuffer = true;
            }    
        }
    }
    
    private void FixedUpdate()
    {
        if (GameManager.instance.gameState != GameState.Playing)
        {
            return;
        }
        
        // 플레이어 움직임 녹화
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
