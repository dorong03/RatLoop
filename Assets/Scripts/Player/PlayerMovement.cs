using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    private Collider2D _collider2D;
    
    [Header("속도 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float poleForce = 3f;
    
    [Header("감지 설정")]
    [SerializeField] private BoxCollider2D groundCheckCollider;
    [SerializeField] private BoxCollider2D hangWallCheckCollider;
    [SerializeField] private LayerMask groundLayer;


    private Collider2D hangingWallCollider;
    private Rigidbody2D hangingWallRigidBody;
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
            if (inputVector.x != 0)
            {
                hangingWallRigidBody.AddForce(Vector2.right * inputVector.x * poleForce);
            }
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
        hangingWallRigidBody = null;
        if (hangingWallCollider != null)
        {
            Physics2D.IgnoreCollision(_collider2D, hangingWallCollider, false);
            hangingWallCollider = null;
        }
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
            hangingWallCollider = hit.collider;
            hangingWallRigidBody = hit.rigidbody;
            Physics2D.IgnoreCollision(_collider2D, hangingWallCollider, true);
            
            float playerHalfWidth = _collider2D.bounds.extents.x;
            float newX = hit.point.x - (direction * playerHalfWidth);

            transform.position = new Vector2(newX, transform.position.y);
            
            gameObject.transform.SetParent(hit.transform, true);
            transform.localRotation = Quaternion.identity;
        }
    }
}