using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerMovement _playerMovement;
    private InputRecorder _inputRecorder;

    private Vector2 moveInputBuffer;
    
    private bool jumpBuffer;
    private bool hangBuffer;

    private bool _isLookingDown;
    private bool _isLookingUp;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _inputRecorder = GetComponent<InputRecorder>();
    }
    
    private void OnMove(InputValue value)
    {
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

    private void OnReplay(InputValue value)
    {
        if (GameManager.instance.gameState == GameState.Playing)
        {
            if (value.isPressed)
            {
                GameManager.instance.Replay();
            }
        }
    }
    
    private void OnRetry(InputValue value)
    {
        if (GameManager.instance.gameState == GameState.Playing)
        {
            if (value.isPressed)
            {
                GameManager.instance.Retry();
            }
        }
    }

    private void OnCameraDown(InputValue value)
    {
        if (GameManager.instance.gameState != GameState.Playing) return;
        
        if (value.isPressed && _playerMovement.IsGroundedState && !_playerMovement.IsMoving)
        {
            _isLookingDown = true;
            _isLookingUp = false;
        }
        else
        {
            _isLookingDown = false;
        }
        ApplyCameraLook();
    }
    
    private void OnCameraUp(InputValue value)
    {
        if (GameManager.instance.gameState != GameState.Playing) return;

        if (value.isPressed && _playerMovement.IsGroundedState && !_playerMovement.IsMoving)
        {
            _isLookingUp = true;
            _isLookingDown = false;
        }
        else
        {
            _isLookingUp = false;
        }
        ApplyCameraLook();
    }

    private void ApplyCameraLook()
    {
        CameraEffect camEffect = Camera.main.GetComponent<CameraEffect>();
        if (camEffect != null)
        {
            camEffect.SetCameraLook(_isLookingUp, _isLookingDown);
        }
        _playerMovement.SetLookAnimation("isLookDown", _isLookingDown);
        _playerMovement.SetLookAnimation("isLookUp", _isLookingUp);
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

        if (GameManager.instance.gameState == GameState.Pause)
        {
            moveInputBuffer = Vector2.zero;
        }
        
        if (_isLookingDown || _isLookingUp)
        {
            if (_playerMovement.IsMoving || !_playerMovement.IsGroundedState)
            {
                _isLookingDown = false;
                _isLookingUp = false;
                ApplyCameraLook();
            }
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