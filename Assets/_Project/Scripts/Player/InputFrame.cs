using UnityEngine;

[System.Serializable]
public struct InputFrame
{
    public float moveX;
    public bool isJumpPressed;
    public bool isHangPressed;

    public InputFrame(float moveX, bool isJumpPressed, bool isHangPressed)
    {
        this.moveX = moveX;
        this.isJumpPressed = isJumpPressed;
        this.isHangPressed = isHangPressed;
    }
}