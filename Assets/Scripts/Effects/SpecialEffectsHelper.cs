using UnityEngine;

/// <summary>
/// Creating instance of particles from code with no effort
/// </summary>
public class SpecialEffectsHelper : MonoBehaviour
{
    /// <summary>
    /// Singleton
    /// </summary>
    public static SpecialEffectsHelper Instance;

    public ParticleSystem EnemyHitEffect;
    public ParticleSystem GroundHitEffect;

    public ParticleSystem smokeEffect;
    public ParticleSystem fireEffect;
    public ParticleSystem lightningEffect;

    //public AudioClip ExplosionSound;
    //public AudioSource audioSource;
    void Awake()
    {
        // Register the singleton
        if (Instance != null)
        {
            Debug.LogError("Multiple instances of SpecialEffectsHelper!");
        }

        Instance = this;
    }

    /// <summary>
    /// Create an explosion at the given location
    /// </summary>
    /// <param name="position"></param>
    public void EnemyHit(Vector3 position)
    {      
        instantiate(EnemyHitEffect, position);
    }

    
    public void GroundHit(Vector3 position)
    {
        
        instantiate(GroundHitEffect, position);
    }

    public void LightningHit(Vector3 position)
    {
        instantiate(lightningEffect, position);
    }

    public void Explosion(Vector3 position)
    {
        instantiate(smokeEffect, position);
        //AudioSource.PlayClipAtPoint(ExplosionSound, position,.3f);

        instantiate(fireEffect, position);
    }
    /// <summary>
    /// Instantiate a Particle system from prefab
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private ParticleSystem instantiate(ParticleSystem prefab, Vector3 position)
    {
        ParticleSystem newParticleSystem = Instantiate(
          prefab,
          position,
          Quaternion.identity
        ) as ParticleSystem;

        // Make sure it will be destroyed
        Destroy(
          newParticleSystem.gameObject,
          newParticleSystem.startLifetime
        );

        return newParticleSystem;
    }
}