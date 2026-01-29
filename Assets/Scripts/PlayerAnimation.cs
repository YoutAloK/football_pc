using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;
    private PlayerController player;
    private Vector3 lastPosition;
    private float currentSpeed = 0f;
    
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        player = GetComponent<PlayerController>();
        lastPosition = transform.position;
        
        if (anim == null)
        {
            Debug.LogError("ANIMATOR NOT FOUND!");
            Debug.Log("Дочерние объекты:");
            foreach (Transform child in transform)
            {
                Debug.Log($"- {child.name}");
            }
            enabled = false;
        }
        else
        {
            Debug.Log($"Animator найден: {anim.name}");
            Debug.Log($"Controller: {anim.runtimeAnimatorController?.name ?? "NULL"}");
            
            // Тест: установи начальную скорость
            anim.SetFloat("Speed", 0f);
            Debug.Log("Начальная скорость установлена: 0");
        }
    }
    
    void Update()
    {
        if (anim == null || player == null) return;
        
        // 1. Считаем скорость
        Vector3 move = (transform.position - lastPosition) / Time.deltaTime;
        float realSpeed = move.magnitude;
        lastPosition = transform.position;
        
        // 2. Преобразуем в 0-1 для аниматора
        float targetSpeed = 0f;
        
        if (realSpeed > 0.1f)
        {
            float maxSpeed = player.hasBallControl ? player.dribbleSpeed : player.runSpeed;
            targetSpeed = realSpeed / maxSpeed;
            targetSpeed = Mathf.Clamp01(targetSpeed);
            
            // Замедление при дриблинге
            if (player.hasBallControl)
                targetSpeed *= 0.7f;
        }
        
        // 3. Плавное изменение
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        
        // 4. Передаем в аниматор
        anim.SetFloat("Speed", currentSpeed);
        
        // 5. Дебаг каждый кадр пока не починим
        Debug.Log($"Frame {Time.frameCount}: Speed={currentSpeed:F2}, Real={realSpeed:F1}");
    }
    
    public void TriggerKick()
    {
        if (anim == null)
        {
            Debug.LogError("Cannot trigger kick: Animator is null!");
            return;
        }
        
        Debug.Log("=== TRIGGER KICK CALLED ===");
        anim.SetTrigger("Kick");
        
        // Проверяем что триггер сработал
        StartCoroutine(CheckTriggerAfterFrame());
    }
    
    System.Collections.IEnumerator CheckTriggerAfterFrame()
    {
        yield return null; // Ждем один кадр
        Debug.Log($"After trigger - Speed: {anim.GetFloat("Speed"):F2}");
    }
    
    public void TriggerSkillMove(int moveIndex)
    {
        // Пока пропускаем финты
        Debug.Log($"Skill move {moveIndex} called (not implemented yet)");
    }
}