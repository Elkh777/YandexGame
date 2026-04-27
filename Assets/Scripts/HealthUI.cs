using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public Image[] hearts;

    public void UpdateHealth(int current, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < current) hearts[i].enabled = true;
            else hearts[i].enabled = false;
        }
    }
}