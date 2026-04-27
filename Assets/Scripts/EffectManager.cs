using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public ParticleSystem dustParticle;
    public ParticleSystem hitParticle;

    public void PlayDust()
    {
        if (dustParticle != null) dustParticle.Play();
    }

    public void PlayHit(Vector3 position)
    {
        if (hitParticle != null)
            Instantiate(hitParticle, position, Quaternion.identity);
    }
}