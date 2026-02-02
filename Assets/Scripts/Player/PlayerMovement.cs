using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    private Collider2D _collider2D;
    
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    
    [Header("Detection")]
    [SerializeField] private BoxCollider2D groundCheckCollider;
    [SerializeField] private BoxCollider2D hangWallCheckCollider;
    [SerializeField] private LayerMask groundLayer;
    
    private Vector2 inputVector;
    private bool isHanging = false;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<BoxCollider2D>();
    }

    private void FixedUpdate()
    {
        if (isHanging)
        {
            return;
        }
        
        HandleRotation();
        _rigidbody2D.linearVelocity = new Vector2(inputVector.x * moveSpeed, _rigidbody2D.linearVelocity.y);
    }
    
    public void SetMoveInput(Vector2 input)
    {
        inputVector = input;
    }

    public void TryJump()
    {
        Debug.Log(IsGrounded());
        if (isHanging)
        {
            CancelHanging();
        }
        
        else if (IsGrounded())
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
        }
    }

    public void TryHang()
    {
        if (isHanging)
        {
            CancelHanging();
        } 
        else if (hangWallCheckCollider.IsTouchingLayers(groundLayer))
        {
            HangingObject();
        }
    }

    private void CancelHanging()
    {
        isHanging = false;
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        transform.SetParent(null, true);
    }

    private void HangingObject()
    {
        isHanging = true;
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        SnapToWall();
    }
    
    private bool IsGrounded()
    {
        return groundCheckCollider.IsTouchingLayers(groundLayer);
    }

    private void HandleRotation()
    {
        if (inputVector.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (inputVector.x < 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }
    
    private void SnapToWall()
    {
        float direction = transform.rotation.eulerAngles.y == 180 ? -1f : 1f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * direction, 1f, groundLayer);

        if (hit.collider != null)
        {
            float playerHalfWidth = _collider2D.bounds.extents.x;
            float newX = hit.point.x - (direction * playerHalfWidth);

            transform.position = new Vector2(newX, transform.position.y);
            
            gameObject.transform.SetParent(hit.transform, true);
        }
    }
}