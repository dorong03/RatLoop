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
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.2f);
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.6f);
    
    [SerializeField] private Vector2 hangCheckSize = new Vector2(0.3f, 0.5f);
    [SerializeField] private Vector2 hangCheckOffset = new Vector2(0.4f, 0.5f);
    
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask hangingWallLayer;

    [SerializeField] private Collider2D hangingWallCollider;
    [SerializeField] private Rigidbody2D hangingWallRigidBody;
    
    // [Header("사운드 설정")]
    // [SerializeField] private float footstepRate = 0.3f;

    private Vector2 inputVector;
    private bool isHanging = false;
    private float footstepTimer;
    // private bool isDead = false;
    private bool isPlayer;
    // private bool isStageClear = false;
    
    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<BoxCollider2D>();
        _animator = GetComponent<Animator>();
        isPlayer = gameObject.CompareTag("Player");
    }

    private void FixedUpdate()
    {
        // Playing 이 아닐때 ( Clear 또는 Die ) 못 움직이게 하기
        if (GameManager.instance.gameState != GameState.Playing)
        {
            _rigidbody2D.linearVelocity = new Vector2(0, _rigidbody2D.linearVelocity.y);
            return;
        }
        
        bool isGrounded = IsGrounded(); 

        float yVel = _rigidbody2D.linearVelocity.y;

        _animator.SetBool("isRun", inputVector.x != 0 && isGrounded);
        _animator.SetBool("isGround", isGrounded);
        _animator.SetFloat("yVelocity", yVel);
        
        // 메달릴때 이동
        if (isHanging)
        {
            if (inputVector.x != 0)
            {
                hangingWallRigidBody.AddForce(Vector2.right * inputVector.x * poleForce);
            }
            return;
        }
        
        HandleRotation();
        
        // 이동 로직
        _rigidbody2D.linearVelocity = new Vector2(inputVector.x * moveSpeed, _rigidbody2D.linearVelocity.y);
    }
    
    public void SetMoveInput(Vector2 input)
    {
        inputVector = input;
    }

    public void TryJump()
    {
        if (GameManager.instance.gameState != GameState.Playing)
        {
            return;
        }

        // 매달린 상태에선 취소하기
        if (isHanging)
        {
            CancelHanging();
            // 플레이어면 점프 소리
            
            // if (isPlayer)
            // {
            //     SoundManager.instance.PlaySFX(SfxType.Jump);    
            // }
        }
        
        // 땅이라면 점프하기
        else if (IsGrounded())
        {
            // 플레이어면 점프 사운드
            
            // if (isPlayer)
            // {
            //     SoundManager.instance.PlaySFX(SfxType.Jump);    
            // }
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
        }
    }

    public void TryHang()
    {
        if (GameManager.instance.gameState != GameState.Playing)
        {
            return;
        }
        
        if (isHanging)
        {
            CancelHanging();
        } 
        else 
        {
            Vector2 checkPos = transform.TransformPoint(hangCheckOffset);
            
            Collider2D hit = Physics2D.OverlapBox(checkPos, hangCheckSize, 0, hangingWallLayer);
            if (hit != null)
            {
                HangingObject();
            }
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
        if (hangingWallRigidBody != null)
        {
            _rigidbody2D.linearVelocity = hangingWallRigidBody.linearVelocity * 4;
            hangingWallRigidBody = null;
        }
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
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        return Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + groundCheckOffset, groundCheckSize);
        Gizmos.color = Color.cyan;
        Vector2 hangPos = transform.TransformPoint(hangCheckOffset);
        Gizmos.DrawWireCube(hangPos, hangCheckSize);
    }
}