using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Metal gear simulator entity. The agent monitors the data from this class
/// </summary>
public class MetalGear : MonoBehaviour {

    public uint sysUpTime = 0;
    public int selectdGun = 0;
    public int selectedTarget;
    public int attacking;
    public int underAttack;

    public string location;

    public int moveToX=0;
    public int moveToY=0;

    public uint lookAt;
    //guns
    public Transform machineGun;
    public int machineGunAmmo=1000;
    public int machineGunDamage = 1;

    public Transform missileLauncher;
    public int missileLauncherAmmo=8;
    public int missileLauncherDamage = 50;

    public Transform RailGun;
    public int railGunAmmo = -1;
    public int railGunDamage = 50000;

    //cameras

    public Camera frontCamera;
    public Camera backCamera;

    public uint selectedCamera;

    //radar
    public List<Transform> targets;
    public List<int> targetSize;
    public List<int> targetThreat;
    public List<int> targetAttacking;

    public List<float> positionXEnemies;
    public List<float> positionYEnemies;
	    
    public uint radarState;

    //bodie
    public int ArmHealth;
    public int ArmArmor;
    public int LegsHealth;
    public int LegsArmor;
    public int HeadHealth;
    public int HeadArmor;

    //nuke
    public float nukeCounter;
    public uint nukeState;
    public int nukesAmmo;

    public int nukeX;
    public int nukeY;

    public float speed = 2; //movement speed

    //private
    private float _sysUpTime;
    private float _nukeCounter;

    private float _attackCooldown = 1f;

	// Use this for initialization
	void Start () {
        location = "X:" + transform.position.x.ToString() + "Y: " + transform.position.z.ToString();

	}
	
	// Update is called once per frame
	void Update () {
        _sysUpTime += Time.deltaTime;
        sysUpTime = (uint)_sysUpTime;

        location = "X:" + transform.position.x.ToString() + "Y: " + transform.position.z.ToString();
        MoveTowardsTarget();

        if(attacking == 1)
        {
            Attack();
        }
			
        if(selectedCamera == 0)
        {
            frontCamera.enabled = true;
            backCamera.enabled = false;
        }

        if (selectedCamera == 1)
        {
            frontCamera.enabled = false;
            backCamera.enabled = true;
        }
			
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        { 
            speed += 1;
        }
        if(Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        { 
            speed -= 1;
            if (speed <= 1)
                speed = 1;
        }
			
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Z))
        {
            Damage(25,  2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.X))
        {
            Damage(25, 0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.C))
        {
            underAttack = 1;
        }
	}


    /// <summary>
    /// move towards a target at a set speed.
    /// </summary>
    private void MoveTowardsTarget()
    {
        
        //the speed, in units per second, we want to move towards the target
        //float speed = 2;
        //move towards the center of the world (or where ever you like)
        Vector3 targetPosition = new Vector3(moveToX, 0, moveToY);

        if (Vector3.Distance(targetPosition, transform.position) < 5)
        {
            transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));
        }
        Vector3 currentPosition = this.transform.position;
        //first, check to see if we're close enough to the target
        if (Vector3.Distance(currentPosition, targetPosition) > .1f)
        {
            Vector3 directionOfTravel = targetPosition - currentPosition;
            //now normalize the direction, since we only want the direction information
            directionOfTravel.Normalize();
            //scale the movement on each axis by the directionOfTravel vector components

            this.transform.Translate(
                (directionOfTravel.x * speed * Time.deltaTime),
                (directionOfTravel.y * speed * Time.deltaTime),
                (directionOfTravel.z * speed * Time.deltaTime),
                Space.World);
        }
    }

    /// <summary>
    ///  bodyPart: 0 for legs, 1 for arms, 2 for head
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="bodyPart"></param>
    public void Damage(int damage, int bodyPart)
    {
        Debug.Log("received damamge");
        if (bodyPart == 0)
            LegsHealth -= damage*(1/LegsArmor);
        if (bodyPart == 1)
            ArmHealth -= damage * (1 / ArmArmor);
        if (bodyPart == 2)
            HeadHealth -= damage * (1 / HeadArmor);

        ArmHealth = (ArmHealth < 0 ) ? 0 : ArmHealth;
        LegsHealth = (LegsHealth < 0) ? 0 : LegsHealth;
        HeadHealth = (HeadHealth < 0) ? 0 : HeadHealth;

    }
		
    /// <summary>
    /// Attack the Selected target with the selected gun
    /// </summary>
    private void Attack()
    {

        if (selectedTarget > targets.Count || selectedTarget < 0)
            return;

        if (targets[selectedTarget] == null)
            return;


        if (selectdGun == 0)
        {
            if(machineGunAmmo <= 0)
            {
                attacking = 0;
                return;
            }
            targets[selectedTarget].GetComponent<EnemyScript>().health -= machineGunDamage;

            transform.LookAt(new Vector3(targets[selectedTarget].transform.position.x, transform.position.y, targets[selectedTarget].transform.position.z));

            machineGunAmmo -= 10;

            //play machine gun effect
            SpecialEffectsHelper.Instance.EnemyHit(machineGun.position);

            //play machine gun effect on target
        }

        if (selectdGun == 1)
        {
            if (missileLauncherAmmo <= 0)
            {
                attacking = 0;
                return;
            }
            transform.LookAt(targets[selectedTarget]);

            targets[selectedTarget].GetComponent<EnemyScript>().health -= missileLauncherDamage;
            missileLauncherAmmo -= 1;

            //play missile gun effect
            SpecialEffectsHelper.Instance.Explosion(missileLauncher.position);

            //play missile gun effect on target
        }

        if (selectdGun == 2)
        {
            transform.LookAt(targets[selectedTarget]);

            targets[selectedTarget].GetComponent<EnemyScript>().health -= railGunDamage;
            railGunAmmo -= 1;
            //play rail gun effect
            SpecialEffectsHelper.Instance.LightningHit(RailGun.position);

            //play rail gun effect on target
        }

        if(targets[selectedTarget].GetComponent<EnemyScript>().health <= 0)
            attacking = 0;

        
    }
		
			
    /// <summary>
    /// These 3 functions are about adding the enemies to the enemies array
    /// </summary>
    private void OnTriggerEnter(Collider collision) 
    {
        if (collision.gameObject.tag == "Enemy")
        {
            if (targets.Contains(collision.transform)) return;
            targets.Add(collision.transform);
            positionXEnemies.Add(collision.transform.position.x);
            positionYEnemies.Add(collision.transform.position.z);
            targetSize.Add(collision.transform.GetComponent<EnemyScript>().size);
            targetThreat.Add(collision.transform.GetComponent<EnemyScript>().threat);
            targetAttacking.Add(collision.transform.GetComponent<EnemyScript>().attacking);
        }
    }

    private void OnTriggerExit(Collider collision) 
    {
        if (collision.gameObject.tag == "Enemy")
        {
            int index = targets.FindIndex(d => d == collision.transform);
            if (index == -1) return;
            targets.RemoveAt(index);
            positionXEnemies.RemoveAt(index);
            positionYEnemies.RemoveAt(index);
            targetSize.RemoveAt(index);
            targetThreat.RemoveAt(index);
            targetAttacking.RemoveAt(index);
        }
    }
		
    public void RemoveEnemyFromList(Transform enemy)
    {
        int index = targets.FindIndex(d => d == enemy);
        if (index == -1) return;
        targets.RemoveAt(index);
        positionXEnemies.RemoveAt(index);
        positionYEnemies.RemoveAt(index);
        targetSize.RemoveAt(index);
        targetThreat.RemoveAt(index);
        targetAttacking.RemoveAt(index);
    }
}



/*

#MIB REFERENCE
 * .1 SysUpTime = Counter           time online
 * .2 location = Octet string       send X:--- Y:---
 * .3 MoveX = integer               point set to move
 * .4 MoveY = integer               point set to move
 * .5 LookAt = integer              rotate to x degrees
 * .6 weaponTable

 * .7 nukeState = integer           0: idle ,1:launching, -1: damaged
 * .8 nukeLaunch = Counter          # nukeOnChamber
 * .9 radarTable
 * 
 * .10 radarState = integer         0:fullfunction, 1: damaged 2: destroyed
 * .11 cameraTable
 * 
 * .12 selectedCamera = gauge       active camera id
 * .13 bodyTable
 * 
 * .14 selectedTarget = gauge               target id ( from radar table)
 * .15 selectedGun   = gauge               active gun id (from weapon table)    
 * .16 attacking= integer                   0: idle , 1 attacking 2: target destroyed
 * .17 underAttack = integer
*/


