using UnityEngine;

public class Goal : MonoBehaviour
{
    [Header("Goal Settings")]
    public string teamName = "Team A"; // Название команды (чьи это ворота)
    public AudioClip goalSound; // Звук гола (опционально)
    
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Проверяем, мяч ли это
        if (other.GetComponent<BallController>() != null)
        {
            OnGoalScored();
        }
    }
    
    void OnGoalScored()
    {
        Debug.Log("ГОЛ! В ворота " + teamName);
        
        // Воспроизводим звук если есть
        if (audioSource != null && goalSound != null)
        {
            audioSource.PlayOneShot(goalSound);
        }
        
        // Здесь можно добавить счётчик голов, эффекты и т.д.
    }
}