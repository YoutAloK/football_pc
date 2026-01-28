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
    
    [Header("References")]
    public Transform cameraTransform;
    public Transform ballIndicator;
    
    [Header("Ball Control")]
    private BallController currentBall;
    private bool hasBallControl = false;
    
    private CharacterController characterController;
    private Vector3 moveDirection;
    private float currentSpeed;
    private bool isRunning;
    private float verticalVelocity = 0f;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
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
            // Проверяем: мяч свободен или принадлежит другому игроку
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
            ShootBall();
        }
        
        // ЛЕВЫЙ CTRL - Сильный удар
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            PowerShot();
        }
        
        // E - Короткий пас
        if (Input.GetKeyDown(KeyCode.E))
        {
            PassBall();
        }
        
        // Q - Навес
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ChipBall();
        }
        
        // R - Отпустить мяч
        if (Input.GetKeyDown(KeyCode.R))
        {
            StopDribbling();
        }
    }
    
    void HandleSkillMoves()
    {
        if (!hasBallControl || currentBall == null) return;
        
        // Проверяем кулдаун
        if (Time.time - lastSkillMoveTime < skillMoveCooldown) return;
        
        // Цифры 1-4 для разных финтов
        Vector3 skillDirection = Vector3.zero;
        bool skillPressed = false;
        
        // 1 - Финт вправо
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            skillDirection = transform.right;
            skillPressed = true;
            Debug.Log("Финт ВПРАВО!");
        }
        // 2 - Финт влево
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            skillDirection = -transform.right;
            skillPressed = true;
            Debug.Log("Финт ВЛЕВО!");
        }
        // 3 - Финт вперёд (толчок мяча)
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            skillDirection = transform.forward * 1.5f;
            skillPressed = true;
            Debug.Log("Толчок ВПЕРЁД!");
        }
        // 4 - Финт назад (перекат)
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            skillDirection = -transform.forward * 0.8f;
            skillPressed = true;
            Debug.Log("Перекат НАЗАД!");
        }
        
        if (skillPressed)
        {
            skillDirection.y = 0f;
            currentBall.PerformSkillMove(skillDirection, skillMoveSpeed);
            lastSkillMoveTime = Time.time;
        }
    }
    
    void HandleTackle()
    {
        // T - Отбор мяча
        if (Input.GetKeyDown(KeyCode.T))
        {
            // Проверяем кулдаун
            if (Time.time - lastTackleTime < tackleCooldown)
            {
                Debug.Log("Отбор на перезарядке!");
                return;
            }
            
            // Ищем мяч рядом
            Collider[] hits = Physics.OverlapSphere(transform.position, tackleRange);
            
            foreach (Collider hit in hits)
            {
                BallController ball = hit.GetComponent<BallController>();
                if (ball != null && ball.isBeingDribbled && ball.dribbler != transform)
                {
                    // Попытка отбора
                    bool success = ball.TryTackle(transform, tackleStrength);
                    
                    if (success)
                    {
                        Debug.Log("ОТБОР УСПЕШЕН!");
                        // Через короткое время пытаемся подобрать мяч
                        Invoke("TryPickupAfterTackle", 0.4f);
                    }
                    else
                    {
                        Debug.Log("Отбор не удался, игрок удержал мяч!");
                    }
                    
                    lastTackleTime = Time.time;
                    break;
                }
            }
        }
    }
    
    void TryPickupAfterTackle()
    {
        // Пытаемся подобрать мяч после отбора
        CheckForBall();
    }
    
    void StartDribbling()
    {
        if (currentBall.IsLooseBall())
        {
            return; // Мяч ещё свободен после отбора/удара
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
    
    void ShootBall()
    {
        Vector3 shootDirection = transform.forward;
        shootDirection.y = 0.3f;
        currentBall.Kick(shootDirection, kickPower, false);
        StopDribbling();
    }
    
    void PowerShot()
    {
        Vector3 shootDirection = transform.forward;
        shootDirection.y = 0.4f;
        currentBall.Kick(shootDirection, strongKickPower, false);
        StopDribbling();
    }
    
    void PassBall()
    {
        Vector3 passDirection = transform.forward;
        currentBall.Pass(passDirection, passPower);
        StopDribbling();
    }
    
    void ChipBall()
    {
        Vector3 chipDirection = transform.forward;
        chipDirection.y = 0.6f;
        currentBall.Kick(chipDirection, chipPower, true);
        StopDribbling();
    }
    
    void OnDrawGizmosSelected()
    {
        // Зона обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Зона дриблинга
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, dribbleRange);
        
        // Зона отбора
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tackleRange);
    }
}