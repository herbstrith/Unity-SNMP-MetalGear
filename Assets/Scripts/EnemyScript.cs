using UnityEngine;
using System.Collections;

public class EnemyScript : MonoBehaviour {

    public int size;
    public int threat;
    public int attacking;

    Transform player;

    public int health;


    public int Damage;
    public bool LegHit;
    public bool headHit;
    public bool armHit;
    public bool dead = false;

	// Use this for initialization
	void Start () {
        player = GameObject.FindGameObjectWithTag("Player").transform;
	}
	
	// Update is called once per frame
	void Update () {
	    
        if(health <= 0)
        {
            player.GetComponent<MetalGear>().RemoveEnemyFromList(transform);

            //play some explosion effect
            if(!dead)
                SpecialEffectsHelper.Instance.Explosion(transform.position);

            dead = true;

            gameObject.tag = "Untagged";
        }
	}

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            attacking = 1;
            if (LegHit)
                collision.gameObject.GetComponent<MetalGear>().LegsHealth -= Damage;
            if(headHit)
                collision.gameObject.GetComponent<MetalGear>().HeadHealth -= Damage;
            if(armHit)
                collision.gameObject.GetComponent<MetalGear>().ArmHealth -= Damage;

        }

    }
}
