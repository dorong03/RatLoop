using UnityEngine;
using System.Collections.Generic;

public class GhostController : MonoBehaviour
{
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
    }

    public void GhostRunInput(InputFrame inputFrame)
    {
        _playerMovement.SetMoveInput(new Vector2(inputFrame.moveX, 0));
        if (inputFrame.isJumpPressed)
        {
            _playerMovement.TryJump();
        }

        if (inputFrame.isHangPressed)
        {
            _playerMovement.TryHang();
        }
    }

    public void StopGhost()
    {
        GhostRunInput(new InputFrame(0f, false, false));
    }
}


