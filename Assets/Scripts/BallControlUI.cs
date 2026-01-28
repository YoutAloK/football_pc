using UnityEngine;
using TMPro;

public class BallControlUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI controlsText;
    
    void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }
        
        UpdateControlsText();
    }
    
    void Update()
    {
        if (statusText != null)
        {
            BallController ball = FindObjectOfType<BallController>();
            if (ball != null && ball.isBeingDribbled && ball.dribbler == player.transform)
            {
                statusText.text = "⚽ ВЛАДЕНИЕ МЯЧОМ";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "БЕЗ МЯЧА";
                statusText.color = Color.white;
            }
        }
    }
    
    void UpdateControlsText()
    {
        if (controlsText != null)
        {
            controlsText.text = 
                "<b>ОСНОВНОЕ УПРАВЛЕНИЕ:</b>\n" +
                "WASD - Движение\n" +
                "Shift - Бег (без мяча)\n" +
                "Мышь - Камера\n" +
                "\n" +
                "<b>С МЯЧОМ:</b>\n" +
                "Space - Удар\n" +
                "Ctrl - Сильный удар\n" +
                "E - Короткий пас\n" +
                "Q - Навес\n" +
                "R - Отпустить мяч\n" +
                "\n" +
                "<b>ФИНТЫ:</b>\n" +
                "1 - Финт вправо\n" +
                "2 - Финт влево\n" +
                "3 - Толчок вперёд\n" +
                "4 - Перекат назад\n" +
                "\n" +
                "<b>ЗАЩИТА:</b>\n" +
                "T - Отбор мяча";
        }
    }
}