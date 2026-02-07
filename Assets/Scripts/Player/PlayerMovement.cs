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

    [SerializeField] private Collider2D hangingWallCollider;
    [SerializeField] private Rigidbody2D hangingWallRigidBody;
    
    [Header("사운드 설정")]
    [SerializeField] private float footstepRate = 0.3f;

    private Vector2 inputVector;
    private bool isHanging = false;
    private float footstepTimer;
    private bool wasGrounded;
    private bool isDead = false;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<BoxCollider2D>();
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (GameManager.instance.currentState == GameState.Die)
        {
            if (!isDead)
            {
                isDead = true;
                _animator.SetTrigger("Die");
            }
            _rigidbody2D.linearVelocity = new Vector2(0, _rigidbody2D.linearVelocity.y);
            return;
        }

        if (GameManager.instance.currentState != GameState.Playing)
        {
            _rigidbody2D.linearVelocity = new Vector2(0, _rigidbody2D.linearVelocity.y);
            return;
        }
        
        bool isGround = IsGrounded();
        float yVel = _rigidbody2D.linearVelocity.y;

        if (!wasGrounded && isGround && yVel <= 0.1f)
        {
            SoundManager.instance.PlaySFX(SfxType.Land);
        }
        wasGrounded = isGround;

        _animator.SetBool("isRun", inputVector.x != 0 && isGround);
        _animator.SetBool("isGround", isGround);
        _animator.SetFloat("yVelocity", yVel);

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

        if (isGround && inputVector.x != 0)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                SoundManager.instance.PlaySFX(SfxType.Walk);
                footstepTimer = footstepRate;
            }
        }
        else
        {
            footstepTimer = 0;
        }
    }
    
    public void SetMoveInput(Vector2 input)
    {
        inputVector = input;
    }

    public void TryJump()
    {
        if (GameManager.instance.currentState != GameState.Playing)
        {
            return;
        }

        if (isHanging)
        {
            CancelHanging();
            SoundManager.instance.PlaySFX(SfxType.Jump);
        }
        
        else if (IsGrounded())
        {
            SoundManager.instance.PlaySFX(SfxType.Jump);
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
        }
    }

    public void TryHang()
    {
        if (GameManager.instance.currentState != GameState.Playing)
        {
            return;
        }
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
        if (hangingWallCollider != null)
        {
            Physics2D.IgnoreCollision(_collider2D, hangingWallCollider, false);
            hangingWallCollider = null;
        }
        transform.SetParent(null, true);
        _rigidbody2D.linearVelocity = hangingWallRigidBody.linearVelocity * 4;
        hangingWallRigidBody = null;
    }

    private void HangingObject()
    {
        float direction = transform.rotation.eulerAngles.y == 180 ? -1f : 1f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right * direction, 1f, hangingWallLayer);

        if (hit.collider != null)
        {
            isHanging = true;
            _rigidbody2D.linearVelocity = Vector2.zero;
            _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            
            hangingWallCollider = hit.collider;
            hangingWallRigidBody = hit.collider.attachedRigidbody;
            Physics2D.IgnoreCollision(_collider2D, hangingWallCollider, true);

            transform.position = new Vector2(hit.point.x, transform.position.y);
            
            gameObject.transform.SetParent(hit.transform, true);
            transform.localRotation = Quaternion.identity;
        }
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
}