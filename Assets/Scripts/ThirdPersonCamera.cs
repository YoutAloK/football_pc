using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target; // Игрок
    public float distance = 5f; // Расстояние от игрока
    public float height = 2f; // Высота камеры
    public float smoothSpeed = 10f; // Плавность следования
    public float rotationSmoothSpeed = 5f; // Плавность вращения
    public float mouseSensitivity = 2f; // Чувствительность мыши
    
    [Header("Rotation Limits")]
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;
    
    private float currentRotationX = 0f;
    private float currentRotationY = 20f; // Начальный угол камеры
    private Vector3 currentVelocity; // Для SmoothDamp
    
    void Start()
    {
        // Скрываем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Если цель не назначена, ищем игрока
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleCursorControl();
        HandleRotation();
        HandlePosition();
    }
    
    void HandleCursorControl()
    {
        // Разблокировать курсор при нажатии Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Заблокировать курсор при клике
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    void HandleRotation()
    {
        // Получаем ввод мыши только если курсор заблокирован
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            currentRotationX += mouseX;
            currentRotationY -= mouseY;
            
            // Ограничиваем вертикальный угол
            currentRotationY = Mathf.Clamp(currentRotationY, minVerticalAngle, maxVerticalAngle);
        }
    }
    
    void HandlePosition()
    {
        // Вычисляем желаемую позицию камеры
        Quaternion rotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 desiredPosition = target.position + rotation * offset;
        
        // Плавно перемещаем камеру используя SmoothDamp для лучшей плавности
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);
        
        // Точка, на которую смотрит камера (немного выше центра игрока)
        Vector3 lookAtPoint = target.position + Vector3.up * (height * 0.5f);
        
        // Плавно поворачиваем камеру
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}