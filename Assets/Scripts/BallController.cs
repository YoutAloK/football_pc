using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Ball Settings")]
    public float maxBallSpeed = 30f;
    public float ballDrag = 2f;
    public float bounciness = 0.6f;
    
    [Header("Possession System")]
    public bool isBeingDribbled = false;
    public Transform dribbler; // Кто владеет мячом
    public float possessionStrength = 100f; // Сила владения (уменьшается при попытке отбора)
    public float possessionRecovery = 50f; // Скорость восстановления владения
    
    [Header("Dribble Settings")]
    public float dribbleMagnetStrength = 15f;
    public float dribbleDistance = 1.2f; // Расстояние мяча от игрока при дриблинге
    public float dribbleSideOffset = 0.3f; // Боковое смещение при движении
    
    private Rigidbody rb;
    private PhysicsMaterial ballPhysicsMaterial;
    private Vector3 lastDribblerVelocity;
    private float looseBallTimer = 0f; // Таймер для "свободного" мяча после потери
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 100f;
        
        // Игнорируем коллизии с игроками (они управляют мячом через скрипты)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Collider ballCollider = GetComponent<Collider>();
        
        foreach (GameObject player in players)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider != null && ballCollider != null)
            {
                Physics.IgnoreCollision(playerCollider, ballCollider, true);
            }
        }
        
        // Физический материал
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            ballPhysicsMaterial = new PhysicsMaterial("BallPhysics");
            ballPhysicsMaterial.bounciness = bounciness;
            ballPhysicsMaterial.dynamicFriction = 0.6f;
            ballPhysicsMaterial.staticFriction = 0.6f;
            ballPhysicsMaterial.frictionCombine = PhysicsMaterialCombine.Average;
            ballPhysicsMaterial.bounceCombine = PhysicsMaterialCombine.Average;
            sphereCollider.material = ballPhysicsMaterial;
        }
    }
    
    void FixedUpdate()
    {
        // Ограничение скорости
        if (rb.linearVelocity.magnitude > maxBallSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxBallSpeed;
        }
        
        // Сопротивление на земле
        if (transform.position.y <= 0.6f && rb.linearVelocity.magnitude > 0.1f && !isBeingDribbled)
        {
            rb.linearVelocity *= (1f - ballDrag * Time.fixedDeltaTime);
        }
        
        // Магнит для дриблинга
        if (isBeingDribbled && dribbler != null)
        {
            ApplyDribbleMagnet();
            
            // Восстанавливаем силу владения
            possessionStrength = Mathf.Min(100f, possessionStrength + possessionRecovery * Time.fixedDeltaTime);
        }
        
        // Таймер свободного мяча
        if (looseBallTimer > 0f)
        {
            looseBallTimer -= Time.fixedDeltaTime;
        }
    }
    
    void ApplyDribbleMagnet()
    {
        // Вычисляем скорость дриблера
        Vector3 currentVelocity = (dribbler.position - lastDribblerVelocity) / Time.fixedDeltaTime;
        lastDribblerVelocity = dribbler.position;
        
        // Целевая позиция мяча (впереди игрока с небольшим боковым смещением)
        Vector3 forwardOffset = dribbler.forward * dribbleDistance;
        Vector3 sideOffset = dribbler.right * Mathf.Sin(Time.time * 3f) * dribbleSideOffset; // Небольшое покачивание
        Vector3 targetPosition = dribbler.position + forwardOffset + sideOffset;
        targetPosition.y = 0.5f;
        
        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;
        
        // Применяем магнитную силу
        if (distance > 0.1f)
        {
            float magnetForce = distance * dribbleMagnetStrength;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, direction.normalized * magnetForce, 0.5f);
            
            // Добавляем инерцию движения игрока
            rb.linearVelocity += currentVelocity * 0.3f;
        }
        else
        {
            // Мяч близко - синхронизируем с игроком
            rb.linearVelocity = currentVelocity * 0.8f;
        }
        
        // Вращение мяча
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            Vector3 torque = Vector3.Cross(Vector3.up, rb.linearVelocity) * 2f;
            rb.AddTorque(torque);
        }
    }
    
    public void StartDribbling(Transform player)
    {
        // Проверяем таймер свободного мяча (защита от мгновенного подбора)
        if (looseBallTimer > 0f && dribbler != player)
        {
            return; // Мяч ещё "свободен", нельзя взять сразу
        }
        
        isBeingDribbled = true;
        dribbler = player;
        possessionStrength = 100f;
        lastDribblerVelocity = player.position;
        looseBallTimer = 0f;
    }
    
    public void StopDribbling()
    {
        isBeingDribbled = false;
        dribbler = null;
        possessionStrength = 100f;
        looseBallTimer = 0.5f; // Мяч свободен на 0.5 секунды
    }
    
    // Попытка отбора мяча другим игроком
    public bool TryTackle(Transform tackler, float tackleStrength)
    {
        if (!isBeingDribbled || dribbler == null || dribbler == tackler)
        {
            return false;
        }
        
        // Уменьшаем силу владения
        possessionStrength -= tackleStrength;
        
        // Если владение сломлено - отбор успешен
        if (possessionStrength <= 0f)
        {
            StopDribbling();
            
            // Мяч отскакивает в сторону
            Vector3 knockDirection = (transform.position - tackler.position).normalized;
            knockDirection.y = 0.2f;
            rb.linearVelocity = knockDirection * 5f;
            
            looseBallTimer = 0.3f; // Небольшая задержка перед подбором
            
            return true; // Отбор успешен
        }
        
        return false; // Отбор не удался
    }
    
    public void Kick(Vector3 direction, float power, bool applyLoft = false)
    {
        StopDribbling();
        
        if (applyLoft)
        {
            direction.y = Mathf.Max(direction.y, 0.5f);
        }
        
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction.normalized * power, ForceMode.Impulse);
        
        Vector3 spin = Vector3.Cross(direction, Vector3.up) * power * 0.1f;
        rb.AddTorque(spin, ForceMode.Impulse);
    }
    
    public void Pass(Vector3 direction, float power)
    {
        StopDribbling();
        
        rb.linearVelocity = Vector3.zero;
        direction.y = 0.1f;
        rb.AddForce(direction.normalized * power, ForceMode.Impulse);
    }
    
    // Финты - толчок мяча в сторону
    public void PerformSkillMove(Vector3 direction, float power)
    {
        if (!isBeingDribbled) return;
        
        // Временно отключаем дриблинг
        isBeingDribbled = false;
        
        // Толкаем мяч
        rb.linearVelocity = direction.normalized * power;
        
        // Через короткое время возобновляем дриблинг
        Invoke("ResumeAfterSkill", 0.3f);
    }
    
    void ResumeAfterSkill()
    {
        if (dribbler != null)
        {
            isBeingDribbled = true;
        }
    }
    
    public bool IsLooseBall()
    {
        return looseBallTimer > 0f;
    }
}