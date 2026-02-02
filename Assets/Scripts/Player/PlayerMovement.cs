using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    private Collider2D _collider2D;
    private Animator _animator;
    
    [Header("속도 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float poleForce = 3f;
    
    [Header("감지 설정")]
    [SerializeField] private BoxCollider2D groundCheckCollider;
    [SerializeField] private BoxCollider2D hangWallCheckCollider;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask hangingWallLayer;


    private Collider2D hangingWallCollider;
    private Rigidbody2D hangingWallRigidBody;
    private Vector2 inputVector;
    private bool isHanging = false;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<BoxCollider2D>();
        _animator = GetComponent<Animator>();
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
        _animator.SetBool("isJump" , _rigidbody2D.linearVelocity.y != 0f);
    }
    
    public void SetMoveInput(Vector2 input)
    {
        inputVector = input;
        _animator.SetBool("isMove", inputVector.x != 0);
    }

    public void TryJump()
    {
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
        else 
        {
            Collider2D hit = Physics2D.OverlapBox(hangWallCheckCollider.bounds.center, hangWallCheckCollider.size, 0, hangingWallLayer);
            if (hit != null)
            {
                HangingObject();
            }
        }
    }
    
    private void OnReset(InputValue value)
    {
        if (value.isPressed)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * direction, 1f, hangingWallLayer);

        if (hit.collider != null)
        {
            hangingWallCollider = hit.collider;
            hangingWallRigidBody = hit.rigidbody;
            Physics2D.IgnoreCollision(_collider2D, hangingWallCollider, true);

            transform.position = new Vector2(hit.point.x, transform.position.y);
            
            gameObject.transform.SetParent(hit.transform, true);
            transform.localRotation = Quaternion.identity;
        }
    }
}