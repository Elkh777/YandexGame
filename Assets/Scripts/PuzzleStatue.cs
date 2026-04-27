using UnityEngine;
using UnityEngine.Events;

public class PuzzleStatue : MonoBehaviour
{
    public int correctOrderIndex;
    public UnityEvent onCorrect;
    public UnityEvent onWrong;

    private static int currentStep = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null && !pc.isRangedMode)
            {
                if (correctOrderIndex == currentStep)
                {
                    currentStep++;
                    onCorrect?.Invoke();
                    if (currentStep >= 2)
                    {
                        Debug.Log("Головоломка решена! (дверь должна открыться)");
                        currentStep = 0;
                    }
                }
                else
                {
                    currentStep = 0;
                    onWrong?.Invoke();
                }
            }
        }
    }
}