using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    
    [Header("움직임 속도")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    
    [Header("점프 및 잡기 판정")]
    [SerializeField] private BoxCollider2D groundCheckCollider;
    [SerializeField] private LayerMask groundLayer;

    private Vector2 inputVector;

    private bool isGrounded;

    private void Awake()
    {
        _rigidbody2D = gameObject.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        _rigidbody2D.linearVelocity = new Vector2(inputVector.x * moveSpeed, _rigidbody2D.linearVelocity.y);
    }
    
    private void OnMove(InputValue value)
    {
        inputVector = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed && IsGrounded())
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
        }
    }

    private bool IsGrounded()
    {
        isGrounded = groundCheckCollider.IsTouchingLayers(groundLayer);
        return isGrounded;
    }
}
