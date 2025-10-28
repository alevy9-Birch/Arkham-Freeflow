using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

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

    float health = 3;
    Vector2 movement;
    public float acceleration = 10f;
    public float speed = 1;
    public float sprintSpeedMult = 2;
    float currentSpeed = 1;
    public float turnSpeed = 360;
    public float cameraSensativity = 10f;
    Vector3 lookDirection = new Vector3 (0f, 0f, 1f);
    bool attacking = false;
    LayerMask enemyMask;
    RaycastHit info;
    public float[] attackDuration = new float[3];
    public float hitDistance = 1f;
    Coroutine currentAttack;

    Enemy currentTarget;
    Vector2 moveInput;

    float startAttackTime;
    float startAttackPosition;

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
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        m_attackInput = InputSystem.actions.FindAction("Attack");
        m_counterInput = InputSystem.actions.FindAction("Counter");
        m_lookInput = InputSystem.actions.FindAction("Look");
        m_moveInput = InputSystem.actions.FindAction("Move");
        m_sprintInput = InputSystem.actions.FindAction("Sprint");

    }

    private void Update()
    {
        cameraRig.position = Vector3.Lerp(cameraRig.position, transform.position, 0.5f);
        cameraRig.Rotate(transform.up, m_lookInput.ReadValue<Vector2>().x * cameraSensativity);

        moveInput = m_moveInput.ReadValue<Vector2>();
        currentSpeed = m_sprintInput.IsPressed() ? Mathf.Lerp(currentSpeed, speed * sprintSpeedMult, 20 * Time.deltaTime) : Mathf.Lerp(currentSpeed, speed, 20 * Time.deltaTime);

        if (!attacking)
        {
            Move(moveInput);
            animator.speed = currentSpeed / speed;
        }

        if (m_attackInput.WasPressedThisFrame() && currentTarget != null)
        {
            attacking = true;
            currentAttack = StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        int attackNum = Random.Range(0, 3);
        animator.SetInteger("AttackNum", attackNum);
        animator.SetTrigger("Attack");
        Vector3 startPos = transform.position;
        
        float timer = 0;
        while (timer < attackDuration[attackNum])
        {
            timer += Time.deltaTime;
            transform.forward = (currentTarget.transform.position - transform.position).normalized;
            transform.position = Vector3.Lerp(startPos, currentTarget.transform.position + (currentTarget.transform.position-transform.position).normalized * hitDistance, timer / attackDuration[attackNum]);
            yield return null;
        }

        currentTarget.Hit(1);
        attacking = false;
    }

    private void Move(float x, float y)
    {
        Vector3 targetDirection = (cameraRig.right * x + cameraRig.forward * y).normalized;
        movement = Vector2.MoveTowards(movement, new Vector2(targetDirection.x, targetDirection.z), acceleration * Time.deltaTime);

        if (Physics.SphereCast(transform.position, 3f, (targetDirection + cameraRig.forward).normalized * 0.01f, out info, 10, enemyMask))
        {
            currentTarget = info.collider.gameObject.GetComponent<Enemy>();
        }
        
        controller.Move((Vector3.right * movement.x + Vector3.forward * movement.y) * currentSpeed * Time.deltaTime);

        lookDirection = Vector3.Lerp(lookDirection, targetDirection, 0.5f * Time.deltaTime * currentSpeed);
        if (x!=0 || y!=0)
            RotateTowards(lookDirection.normalized);

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
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void Hit()
    {
        if (currentAttack != null) StopCoroutine(currentAttack);
        health -= 1;
        if (health < 1)
        {
            Die();
        }
    }

    void Die()
    {
        animator.SetTrigger("Die");
        this.enabled = false;
    }
}
