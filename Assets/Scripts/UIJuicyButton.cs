using UnityEngine;
using UnityEngine.EventSystems;

public class UIJuicyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = 0.92f;
    public float animationSpeed = 18f;

    private Vector3 targetScale = Vector3.one;

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = Vector3.one * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }
}
