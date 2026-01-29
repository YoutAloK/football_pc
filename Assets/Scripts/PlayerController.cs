using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float dribbleSpeed = 3.5f;
    public float rotationSpeed = 10f;
    public float gravity = 20f;
    
    [Header("Ball Interaction")]
    public float kickPower = 15f;
    public float strongKickPower = 25f;
    public float passPower = 10f;
    public float chipPower = 12f;
    public float detectionRange = 2.5f;
    public float dribbleRange = 1.5f;
    
    [Header("Skill Moves")]
    public float skillMoveSpeed = 8f; // Скорость финта
    public float skillMoveCooldown = 1f; // Перезарядка финтов
    private float lastSkillMoveTime = 0f;
    
    [Header("Tackle")]
    public float tackleRange = 2f; // Радиус отбора
    public float tackleStrength = 60f; // Сила отбора
    public float tackleCooldown = 1.5f; // Перезарядка отбора
    private float lastTackleTime = 0f;
    
    [Header("Animation Timing")]
    public float kickDelay = 0.3f; // Задержка удара после начала анимации
    public float passDelay = 0.2f; // Задержка паса
    public float chipDelay = 0.3f; // Задержка навеса
    
    [Header("References")]
    public Transform cameraTransform;
    public Transform ballIndicator;
    public Transform dribbleAnchor; // Точка привязки мяча для анимированной модели
    
    [Header("Ball Control")]
    private BallController currentBall;
    public bool hasBallControl = false;
    
    private CharacterController characterController;
    private Vector3 moveDirection;
    private float currentSpeed;
    private bool isRunning;
    private float verticalVelocity = 0f;
    private PlayerAnimation playerAnimation;
    private bool isPerformingAction = false; // Флаг выполнения действия
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerAnimation = GetComponent<PlayerAnimation>();
        
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
            else
            {
                Camera[] cameras = FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                {
                    cameraTransform = cameras[0].transform;
                }
            }
        }
        
        Vector3 startPos = transform.position;
        startPos.y = 1f;
        transform.position = startPos;
    }
    
    void Update()
    {
        if (cameraTransform == null) return;
        
        // Если выполняем действие (удар, пас) - пропускаем ввод
        if (isPerformingAction) return;
        
        CheckForBall();
        HandleMovement();
        HandleRotation();
        HandleBallControl();
        HandleSkillMoves();
        HandleTackle();
    }
    
    void CheckForBall()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        BallController nearestBall = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider hit in hits)
        {
            BallController ball = hit.GetComponent<BallController>();
            if (ball != null)
            {
                float distance = Vector3.Distance(transform.position, ball.transform.position);
                if (distance < nearestDistance)
                {
                    nearestBall = ball;
                    nearestDistance = distance;
                }
            }
        }
        
        currentBall = nearestBall;
        
        // Автоматический подбор мяча
        if (currentBall != null && nearestDistance < dribbleRange)
        {
            if (!currentBall.isBeingDribbled)
            {
                StartDribbling();
            }
            else if (currentBall.dribbler == transform)
            {
                hasBallControl = true;
            }
        }
        else if (currentBall != null && nearestDistance > dribbleRange && currentBall.dribbler == transform)
        {
            StopDribbling();
        }
        else if (currentBall == null && hasBallControl)
        {
            StopDribbling();
        }
    }
    
    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        isRunning = Input.GetKey(KeyCode.LeftShift) && !hasBallControl;
        
        if (hasBallControl)
        {
            currentSpeed = dribbleSpeed;
        }
        else
        {
            currentSpeed = isRunning ? runSpeed : walkSpeed;
        }
        
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        moveDirection = (forward * vertical + right * horizontal).normalized;
        
        if (characterController.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        
        Vector3 moveVector = moveDirection * currentSpeed;
        moveVector.y = verticalVelocity;
        
        characterController.Move(moveVector * Time.deltaTime);
    }
    
    void HandleRotation()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            Vector3 targetDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void HandleBallControl()
    {
        if (currentBall == null || !hasBallControl) return;
        
        // ПРОБЕЛ - Удар
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartAction("shoot");
        }
        
        // ЛЕВЫЙ CTRL - Сильный удар
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            StartAction("power");
        }
        
        // E - Короткий пас
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartAction("pass");
        }
        
        // Q - Навес
        if (Input.GetKeyDown(KeyCode.Q))
        {
            StartAction("chip");
        }
        
        // R - Отпустить мяч
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopDribbling();
        }
    }
    
    void StartAction(string actionType)
    {
        if (isPerformingAction) return;
        
        isPerformingAction = true;
        
        switch (actionType)
        {
            case "shoot":
                PerformShoot();
                break;
            case "power":
                PerformPowerShot();
                break;
            case "pass":
                PerformPass();
                break;
            case "chip":
                PerformChip();
                break;
        }
    }
    
    void EndAction()
    {
        isPerformingAction = false;
        Debug.Log("Действие завершено");
    }
    
    void PerformShoot()
    {
        if (playerAnimation != null)
            playerAnimation.TriggerKick();
        
        // Запускаем удар через задержку (когда нога касается мяча в анимации)
        Invoke("ExecuteShoot", kickDelay);
        Invoke("EndAction", kickDelay + 0.1f); // Даем немного времени после удара
    }
    
    void ExecuteShoot()
    {
        if (currentBall != null)
        {
            Vector3 shootDirection = transform.forward;
            shootDirection.y = 0.3f;
            currentBall.Kick(shootDirection, kickPower, false);
            StopDribbling();
            Debug.Log("Удар выполнен!");
        }
    }
    
    void PerformPowerShot()
    {
        if (playerAnimation != null)
            playerAnimation.TriggerKick();
        
        Invoke("ExecutePowerShot", kickDelay);
        Invoke("EndAction", kickDelay + 0.1f);
    }
    
    void ExecutePowerShot()
    {
        if (currentBall != null)
        {
            Vector3 shootDirection = transform.forward;
            shootDirection.y = 0.4f;
            currentBall.Kick(shootDirection, strongKickPower, false);
            StopDribbling();
            Debug.Log("Сильный удар выполнен!");
        }
    }
    
    void PerformPass()
    {
        if (playerAnimation != null)
            playerAnimation.TriggerKick();
        
        Invoke("ExecutePass", passDelay);
        Invoke("EndAction", passDelay + 0.1f);
    }
    
    void ExecutePass()
    {
        if (currentBall != null)
        {
            Vector3 passDirection = transform.forward;
            currentBall.Pass(passDirection, passPower);
            StopDribbling();
            Debug.Log("Пас выполнен!");
        }
    }
    
    void PerformChip()
    {
        if (playerAnimation != null)
            playerAnimation.TriggerKick();
        
        Invoke("ExecuteChip", chipDelay);
        Invoke("EndAction", chipDelay + 0.1f);
    }
    
    void ExecuteChip()
    {
        if (currentBall != null)
        {
            Vector3 chipDirection = transform.forward;
            chipDirection.y = 0.6f;
            currentBall.Kick(chipDirection, chipPower, true);
            StopDribbling();
            Debug.Log("Навес выполнен!");
        }
    }
    
    void HandleSkillMoves()
    {
        if (!hasBallControl || currentBall == null) return;
        
        if (Time.time - lastSkillMoveTime < skillMoveCooldown) return;
        
        Vector3 skillDirection = Vector3.zero;
        bool skillPressed = false;
        int skillIndex = -1;
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            skillDirection = transform.right;
            skillPressed = true;
            skillIndex = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            skillDirection = -transform.right;
            skillPressed = true;
            skillIndex = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            skillDirection = transform.forward * 1.5f;
            skillPressed = true;
            skillIndex = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            skillDirection = -transform.forward * 0.8f;
            skillPressed = true;
            skillIndex = 3;
        }
        
        if (skillPressed)
        {
            skillDirection.y = 0f;
            currentBall.PerformSkillMove(skillDirection, skillMoveSpeed);
            
            if (playerAnimation != null)
            {
                playerAnimation.TriggerSkillMove(skillIndex);
            }
            
            lastSkillMoveTime = Time.time;
            Debug.Log($"Финт {skillIndex + 1} выполнен!");
        }
    }
    
    void HandleTackle()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (Time.time - lastTackleTime < tackleCooldown)
            {
                Debug.Log("Отбор на перезарядке!");
                return;
            }
            
            Collider[] hits = Physics.OverlapSphere(transform.position, tackleRange);
            
            foreach (Collider hit in hits)
            {
                BallController ball = hit.GetComponent<BallController>();
                if (ball != null && ball.isBeingDribbled && ball.dribbler != transform)
                {
                    bool success = ball.TryTackle(transform, tackleStrength);
                    
                    if (success)
                    {
                        Debug.Log("ОТБОР УСПЕШЕН!");
                        Invoke("TryPickupAfterTackle", 0.4f);
                    }
                    else
                    {
                        Debug.Log("Отбор не удался!");
                    }
                    
                    lastTackleTime = Time.time;
                    break;
                }
            }
        }
    }
    
    void TryPickupAfterTackle()
    {
        CheckForBall();
    }
    
    void StartDribbling()
    {
        if (currentBall.IsLooseBall())
        {
            return;
        }
        
        hasBallControl = true;
        currentBall.StartDribbling(transform);
        Debug.Log("Подобрал мяч!");
    }
    
    void StopDribbling()
    {
        hasBallControl = false;
        if (currentBall != null)
        {
            currentBall.StopDribbling();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, dribbleRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tackleRange);
        
        if (dribbleAnchor != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(dribbleAnchor.position, 0.1f);
        }
    }
}