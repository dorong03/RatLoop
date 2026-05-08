using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    private Animator _animator;

    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minMoveTime = 1f;
    [SerializeField] private float maxMoveTime = 3f;

    [Header("대기 설정")]
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;

    [Header("수면 설정")]
    [SerializeField] private float sleepTime = 5f;
    [SerializeField] private float minSleepTime = 5f;
    [SerializeField] private float maxSleepTime = 10f;

    [Header("수면 이모지 설정")]
    [SerializeField] private GameObject sleepEmojiPrefab;
    [SerializeField] private float sleepEmojiSpawnRate = 3f;

    [Header("점프 설정")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float minJumpInterval = 1f;
    [SerializeField] private float maxJumpInterval = 4f;

    [Header("감지 설정")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.2f);
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.6f);
    [SerializeField] private LayerMask groundLayer;

    [Header("벽 감지 설정")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private Vector2 wallCheckOffset = new Vector2(0f, 0f);

    private float _currentMoveX = 0f;
    private bool _doJump = false;
    private bool _isSleeping = false;
    private bool _wasWalled = false;

    private enum AIPattern { MoveRight, MoveLeft, Idle, Sleep }

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        StartCoroutine(PatternLoop());
        StartCoroutine(RandomJumpLoop());
    }

    private void FixedUpdate()
    {
        bool isGrounded = IsGrounded();

        if (IsWalled())
        {
            if (!_wasWalled)
            {
                _currentMoveX = -_currentMoveX;
                _wasWalled = true;
            }
        }
        else
        {
            _wasWalled = false;
        }

        _rigidbody2D.linearVelocity = new Vector2(_currentMoveX * moveSpeed, _rigidbody2D.linearVelocity.y);

        if (_doJump && isGrounded)
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
            _doJump = false;
        }

        if (_currentMoveX > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (_currentMoveX < 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        _animator.SetBool("isGround", isGrounded);
        _animator.SetFloat("yVelocity", _rigidbody2D.linearVelocity.y);
        _animator.SetBool("isRun", _currentMoveX != 0 && isGrounded);
    }

    private bool IsWalled()
    {
        if (_currentMoveX == 0)
        {
            return false;
        }

        Vector2 origin = (Vector2)transform.position + wallCheckOffset;
        Vector2 direction = _currentMoveX > 0 ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    IEnumerator PatternLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            AIPattern pattern = PickRandomPattern();

            switch (pattern)
            {
                case AIPattern.MoveRight:
                    yield return StartCoroutine(DoMove(1f));
                    break;
                case AIPattern.MoveLeft:
                    yield return StartCoroutine(DoMove(-1f));
                    break;
                case AIPattern.Idle:
                    yield return StartCoroutine(DoIdle());
                    break;
                case AIPattern.Sleep:
                    yield return StartCoroutine(DoSleep());
                    break;
            }
        }
    }

    AIPattern PickRandomPattern()
    {
        float[] weights = { 35f, 35f, 20f, 10f };
        float total = 0f;
        foreach (var w in weights) total += w;

        float rand = Random.Range(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (rand < cumulative)
            {
                return (AIPattern)i;
            }
        }

        return AIPattern.MoveRight;
    }

    IEnumerator DoMove(float direction)
    {
        _isSleeping = false;
        _currentMoveX = direction;
        yield return new WaitForSeconds(Random.Range(minMoveTime, maxMoveTime));
        _currentMoveX = 0f;
    }

    IEnumerator DoIdle()
    {
        _isSleeping = false;
        _currentMoveX = 0f;
        yield return new WaitForSeconds(Random.Range(minIdleTime, maxIdleTime));
    }

    IEnumerator DoSleep()
    {
        _currentMoveX = 0f;
        _isSleeping = true;
        yield return new WaitForSeconds(sleepTime);
 
        _animator.SetBool("isSleeping", true);

        float elapsed = 0f;
        float duration = Random.Range(minSleepTime, maxSleepTime);
        while (elapsed < duration)
        {
            if (sleepEmojiPrefab != null)
            {
                Instantiate(sleepEmojiPrefab,
                    transform.position + (transform.up * 0.1f) + (transform.right * 0.6f),
                    Quaternion.identity);
            }

            yield return new WaitForSeconds(sleepEmojiSpawnRate); 
            elapsed += sleepEmojiSpawnRate;                      
        }

        _isSleeping = false;
        _animator.SetBool("isSleeping", false);
    }

    IEnumerator RandomJumpLoop()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            float waitTime = Random.Range(minJumpInterval, maxJumpInterval);
            yield return new WaitForSeconds(waitTime);

            if (!_isSleeping)
            {
                _doJump = true;
            }
        }
    }

    private bool IsGrounded()
    {
        if (_rigidbody2D.linearVelocity.y > 0.05f)
        {
            return false;
        }

        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        return Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
    }
}