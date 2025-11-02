using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public InputActionAsset InputActions;

    private InputAction m_moveInput;
    private InputAction m_lookInput;
    private InputAction m_attackInput;
    private InputAction m_counterInput;
    private InputAction m_sprintInput;

    CharacterController controller;
    public Transform cameraRig;
    Animator animator;

    public float health = 3;
    Vector2 movement;
    public float acceleration = 10f;
    public float speed = 1;
    public float sprintSpeedMult = 2;
    float currentSpeed = 1;
    public float turnSpeed = 360;
    public float cameraSensativity = 10f;
    Vector3 lookDirection = new Vector3 (0f, 0f, 1f);
    bool attacking = false;
    public LayerMask enemyMask;
    RaycastHit info;
    public float[] attackDuration;
    public float hitDistance = 1f;
    Coroutine currentAttack;
    bool countering = false;
    bool multiCounter = false;
    bool criticalStrike = false;
    public int combo = 0;
    int speedCombo;
    public float criticalWindow = 2f;
    public float comboTime;
    public float comboResetTime = 10f;
    public TextMeshProUGUI comboMeter;
    float y;

    public Enemy currentTarget;
    Vector2 moveInput;

    float startAttackTime;
    float startAttackPosition;
    private int attackRange = 8;
    int lastAttack = -1;

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    void Awake()
    {
        Time.timeScale = 1f;
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        m_attackInput = InputSystem.actions.FindAction("Attack");
        m_counterInput = InputSystem.actions.FindAction("Counter");
        m_lookInput = InputSystem.actions.FindAction("Look");
        m_moveInput = InputSystem.actions.FindAction("Move");
        m_sprintInput = InputSystem.actions.FindAction("Sprint");

        Cursor.lockState = CursorLockMode.Locked;
    }
    
    private void Update()
    {
        speedCombo = Mathf.Clamp(combo, 0, 40);
        if (combo == 0)
            comboMeter.text = "";
        else
            comboMeter.text = "x" + combo.ToString();
        
        cameraRig.position = Vector3.Lerp(cameraRig.position, transform.position, 0.5f);
        cameraRig.Rotate(Vector3.up, m_lookInput.ReadValue<Vector2>().x * cameraSensativity);

        moveInput = m_moveInput.ReadValue<Vector2>();
        currentSpeed = m_sprintInput.IsPressed() ? Mathf.Lerp(currentSpeed, speed * sprintSpeedMult, 20 * Time.deltaTime) : Mathf.Lerp(currentSpeed, speed, 20 * Time.deltaTime);


        if (countering && m_counterInput.WasPressedThisFrame())
        {
            if (EnemyAI.Instance.counterable)
            {
                EnemyAI.Instance.Counter();

                if (!multiCounter)
                {
                    StopCoroutine(currentAttack);
                    currentAttack = StartCoroutine(Counter(true));
                }
            }
        }
        if (!attacking && !countering)
        {
            Move(moveInput);
            animator.speed = currentSpeed / speed;

            if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) > attackRange + 2)
            {
                currentTarget = null;
            }
            if (m_attackInput.WasPressedThisFrame() && SetCurrentTarget())
            {
                currentAttack = StartCoroutine(Attack());
            }
            else if (m_counterInput.WasPressedThisFrame())
            {
                if (EnemyAI.Instance.counterable)
                {
                    EnemyAI.Instance.Counter();
                    currentAttack = StartCoroutine(Counter(false));
                }
                else
                {
                    animator.SetTrigger("Block");
                    Stun();
                }
            }
        }
        
        RotateTowards(lookDirection.normalized);

        if (Time.time > comboTime) combo = 0;
    }

    IEnumerator Attack()
    {
        attacking = true;
        int attackNum = Random.Range(0, 3);
        if (attackNum == lastAttack) { attackNum++; attackNum %= 3; }
        lastAttack = attackNum;
        animator.SetInteger("AttackNum", attackNum);
        animator.SetTrigger("Attack");
        animator.speed = 1 / (1.2f - speedCombo * 0.012f);
        Vector3 startPos = transform.position;
        
        float timer = 0f;
        while (timer < attackDuration[attackNum] * (1.2f - speedCombo * 0.012f) && currentTarget != null)
        {
            timer += Time.deltaTime;
            lookDirection = (currentTarget.transform.position - transform.position).normalized;
            transform.position = Vector3.Lerp(startPos, currentTarget.transform.position - (transform.forward * hitDistance), timer / (attackDuration[attackNum] * (1.2f - speedCombo * 0.012f)));
            yield return null;
        }
    }

    IEnumerator Counter(bool multi)
    {
        multiCounter = multi;
        countering = true;
        int attackNum = multi ? 4 : 3;
        animator.SetInteger("AttackNum", attackNum);
        animator.SetTrigger("Attack");
        animator.speed = 2;
        currentTarget = EnemyAI.Instance.attackers[0];
        Vector3 startPos = transform.position;

        float timer = 0f;
        while (timer < attackDuration[attackNum] && currentTarget != null)
        {
            timer += Time.deltaTime;
            lookDirection = (currentTarget.transform.position - transform.position).normalized;
            transform.position = Vector3.Lerp(startPos, currentTarget.transform.position - (transform.forward * hitDistance), timer / attackDuration[attackNum]);
            yield return null;
        }
        countering = false;
        currentTarget = null;
        animator.speed = 1;
    }

    private void Move(float x, float y)
    {
        Vector3 inputDirection = (cameraRig.right * x + cameraRig.forward * y).normalized;

        if (x != 0 || y != 0) lookDirection = inputDirection;

        movement = Vector2.MoveTowards(movement, new Vector2(inputDirection.x, inputDirection.z), acceleration * Time.deltaTime);
        if (Physics.SphereCast(transform.position, 3f, inputDirection, out info, attackRange, enemyMask))
        {
            currentTarget = info.collider.gameObject.GetComponent<Enemy>();
        }
        
        controller.Move((Vector3.right * movement.x + Vector3.forward * movement.y) * currentSpeed * Time.deltaTime);

        animator.SetFloat("MoveX", Vector3.Dot(controller.velocity, transform.right));
        animator.SetFloat("MoveY", Vector3.Dot(controller.velocity, transform.forward));
    }

    private void Move(Vector2 input)
    {
        Move(input.x, input.y);
    }

    void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smoothly rotate toward target
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime * currentSpeed);
    }

    public void IsHit(int dmg)
    {
        if (currentAttack != null) StopCoroutine(currentAttack);
        health -= dmg;
        animator.SetTrigger("Hit");
        combo = 0;
        if (health <= 0)
        {
            Die();
        }
        else
        {
            Stun();
        }
    }

    void Stun()
    {
        enabled = false;
        StartCoroutine(Recover());
    }

    IEnumerator Recover()
    {
        yield return new WaitForSeconds(0.4f);
        enabled = true;
        currentAttack = null;
        attacking = false;
        countering = false;
        animator.speed = 1;
    }

    void Hit()
    {
        if (currentTarget == null)
        {
            attacking = false;
            return;
        }
        if (criticalStrike)
        {
            currentTarget.IsHit(3);
            Combo(3);
        }
        else
        {
            currentTarget.IsHit(1);
            Combo(1);
        }
        if (currentTarget.health <= 0) currentTarget = null;
        criticalStrike = false;
        StartCoroutine(CriticalWindow());
        attacking = false;
    }

    IEnumerator CriticalWindow()
    {
        float endTime = Time.time + criticalWindow;
        while (Time.time < endTime)
        {
            Debug.Log("Can Crit");
            if (m_attackInput.WasPressedThisFrame() && SetCurrentTarget())
            {
                criticalStrike = true;
            }
            yield return null;
        }
    }

    public void Combo(int plus)
    {
        comboTime = Time.time + comboResetTime;
        combo += plus;
    }

    private bool SetCurrentTarget()
    {
        moveInput = m_moveInput.ReadValue<Vector2>();
        if (moveInput == Vector2.zero) moveInput = Vector2.up;

        Vector3 inputDirection = (cameraRig.right * moveInput.x + cameraRig.forward * moveInput.y).normalized;

        if (Physics.SphereCast(transform.position, 3f, inputDirection, out info, attackRange, enemyMask))
        {
            currentTarget = info.collider.gameObject.GetComponent<Enemy>();
        }
        else return false;
        
        if (Physics.Raycast(transform.position, inputDirection, out info, attackRange, enemyMask))
        {
            currentTarget = info.collider.gameObject.GetComponent<Enemy>();
        }
        return true;
    }

    void Die()
    {
        animator.SetTrigger("Die");
        this.enabled = false;
        StartCoroutine(ReloadScene());
    }

    public void FreezeAnimation()
    {
        animator.speed = 0;
    }

    IEnumerator ReloadScene()
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
