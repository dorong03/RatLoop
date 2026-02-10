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
        isPlayer = gameObject.tag.Equals("Player");
    }

    private void FixedUpdate()
    {
        // Playing 이 아닐때 ( Clear 또는 Die ) 못 움직이게 하기
        if (GameManager.instance.gameState != GameState.Playing)
        {
            _rigidbody2D.linearVelocity = new Vector2(0, _rigidbody2D.linearVelocity.y);
            return;
        }
        
        // 바닥 체크
        bool isGround = IsGrounded();
        float yVel = _rigidbody2D.linearVelocity.y;

        // 애니메이션 조건 설정
        _animator.SetBool("isRun", inputVector.x != 0 && isGround);
        _animator.SetBool("isGround", isGround);
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
        
        // 보는 방향으로 돌리기( 매달린 상태에선 고정 )
        HandleRotation();
        
        // 입력값 만큼 움직이기
        _rigidbody2D.linearVelocity = new Vector2(inputVector.x * moveSpeed, _rigidbody2D.linearVelocity.y);

        // 사운드 나오는 부분 일단 주석해놓기 
        
        // if (isGround && inputVector.x != 0)
        // {
        //     footstepTimer -= Time.deltaTime;
        //     if (footstepTimer <= 0)
        //     {
        //         if (isPlayer)
        //         {
        //             SoundManager.instance.PlaySFX(SfxType.Walk);
        //         }
        //         footstepTimer = footstepRate;
        //     }
        // }
        // else
        // {
        //     footstepTimer = 0;
        // }
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
        
        // 매달린 상태라면 취소
        if (isHanging)
        {
            CancelHanging();
        } 
        // 아니라면 Collider 가 있는지 체크하고 붙기
        else 
        {
            Collider2D hit = Physics2D.OverlapBox(hangWallCheckCollider.bounds.center, hangWallCheckCollider.size, 0, hangingWallLayer);
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