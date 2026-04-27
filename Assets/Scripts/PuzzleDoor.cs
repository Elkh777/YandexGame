using UnityEngine;

public class PuzzleDoor : MonoBehaviour
{
    public void Open()
    {
        // Здесь можно добавить звук, анимацию
        Destroy(gameObject);
    }
}