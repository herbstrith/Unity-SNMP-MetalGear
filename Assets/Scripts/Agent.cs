using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using SnmpSharpNet;
using System.Net;
using System.Net.Sockets;
using System.Threading;


public class Agent : MonoBehaviour {

	float counter=0;
	private Socket mSock = null;

	Thread thread;
	Mutex mainLoop;

    public String baseTreeOid= "1.3.2.5";
    public Oid baseTreeId;

    /* Agent variables*/

    public uint SysUpTime = 0 ;          //time online
    public string location = "X:0 Y:0" ;  //   send X:--- Y:---
    public int MoveX = 0;            //  point set to move
    public int MoveY = 0;           //    point set to move
    public int LookAt = 0;           //   rotate to x degrees
    //* .6 weaponTable

    public int nukeState = 0;          // 0: idle ,1:launching, -1: damaged
    public int nukeLaunch = 0;       //   # nukeOnChamber
    //* .9 radarTable
     
    public int radarState = 0;      //   0:fullfunction, 1: damaged 2: destroyed
    //* .11 cameraTable
  
    public int selectedCamera = 0;     //  active camera id
    //* .13 bodyTable
  
    public int selectedTarget = 0;             //  target id ( from radar table)
    public int selectedGun = 0;               //active gun id (from weapon table)
    public int attacking= 0;                 //  0: idle , 1 attacking 2: target destroyed

    public bool underAttack = false;

    SnmpPacket responsePacket;
    /* End agent variables */

    public MetalGear metalGear;

    private bool enemiesAround = false;

    private bool head50 = false;
    private bool head25 = false;
    private bool head0 = false;
    private bool body50 = false;
    private bool body25 = false;
    private bool body0 = false;
    private bool legs50 = false;
    private bool legs25 = false;
    private bool legs0 = false;

    public Text textInstance;

    private string textTemp="";
    private bool callPrint = false;

    private bool foundEnemy = false;

    private float positionXEnemy1;
    private float positionYEnemy1;
    private float positionXEnemy2;
    private float positionYEnemy2;
    private float positionXEnemy3;
    private float positionYEnemy3;

	void Awake() {

		Thread vThread = new Thread(new ThreadStart(this.ListenerThread));

		vThread.Start();
	}

	// Use this for initialization
	void Start () {
		Debug.Log ("Agent: started start");


		Debug.Log ("Agent: started end");
	}



	// Update is called once per frame
	void Update () {

        /*
		 We call the traps in the update function as it has to be constantly monitored 
		 (it would also be good to have another thread just for sending them)
        */

        // under attack ----------------------------------------------------------------------------
        if (metalGear.underAttack != 0 && !underAttack)
        {
            //send trap under attack
            underAttack = true;
            sendTrap(0);     
        }

        if(metalGear.underAttack == 0 && underAttack)
        {
            //send trap not under attack anymore
            sendTrap(1);
            underAttack = false;
        }

        //found enemy ----------------------------------------------------------------------------
        if( !enemiesAround && metalGear.targets.Count > 0)
        {
            //send trap enemies around
            sendTrap(2);
            enemiesAround = true;
            foundEnemy = true;
        }

        //health traps ----------------------------------------------------------------------------

        if( metalGear.HeadHealth < 50 && !head50)
        {
            //send trap head health
            sendTrap(3);
            head50 = true;
        }

        if (metalGear.LegsHealth < 50 && !legs50)
        {
            //send trap head health
            sendTrap(5);
            legs50 = true;
        }

        if (metalGear.HeadHealth < 25 && !head25)
        {
            //send trap head health
            sendTrap(4);
            head25 = true;
        }

        if (metalGear.LegsHealth < 25 && !legs25)
        {
            //send trap head health
            sendTrap(6);
            legs25 = true;
        }

        if (callPrint)
        {
            //PrintPacket();
            callPrint = false;
        }

	}


    /// <summary>
    /// Thread Functions which does all the SNMP Agent job
    /// </summary>
	public void ListenerThread() {
		mSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		mSock.ReceiveTimeout = 2000;
		IPEndPoint vEndPoint = new IPEndPoint(IPAddress.Any, 16100);
		mSock.Bind(vEndPoint);
		
		Debug.Log ("Agent: thread started");

		byte[] vBuff = new byte[4096];
		int vLen = 0;
		
		while (true) {
			if (this.mSock.Available > 0) {
				EndPoint vSender = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
				vLen = mSock.ReceiveFrom(vBuff, ref vSender);
				//Debug.Log ("Agent: Data received (bytes): " + vLen);


                SnmpPacket vPacket = new SnmpV1Packet("" + "public");

				vPacket.decode(vBuff, vLen);
				//Debug.Log("Agent: PDU decoded: " + vPacket.Pdu.VbCount);
				Oid vOid = null;

                responsePacket = new SnmpV1Packet("" + "public");
                responsePacket.Pdu.ErrorStatus = 0; // no error

				if (vPacket.Pdu != null && vPacket.Pdu.VbList != null) {
					foreach (Vb vVb in vPacket.Pdu.VbList) {
						Debug.Log(vVb.ToString());
						vOid = vVb.Oid;
                        if (vPacket.Pdu.Type == PduType.Set)
                            ProcessSetRequest(vOid,vVb);

                        if (vPacket.Pdu.Type == PduType.GetNext){
                            GetNext(vOid);
                            break;
                        }
                        ProcessGetRequest(vOid);
					}
				}

                //Debug.Log(vOid.ToString());
                responsePacket.Pdu.Type = PduType.Response;
                responsePacket.Pdu.RequestId = vPacket.Pdu.RequestId;

                byte[] vOutBuff = responsePacket.encode();
                mSock.SendTo(vOutBuff, vSender);

                callPrint = true;

			}
			Thread.Sleep(1000);
		}
	}




    /// <summary>
    /// Process a received SNMP GetRequest
    /// </summary>
    /// <param name="Ooid"></param>
    public void ProcessGetRequest(Oid Ooid)
    {

        switch (Ooid.ToString())
        {
            case "1.3.2.5.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32(metalGear.sysUpTime));
                Debug.Log("SysUp request");
                break;
            case "1.3.2.5.2":
                responsePacket.Pdu.VbList.Add(Ooid, new OctetString(metalGear.location));
                Debug.Log("location request");
                break;
            case "1.3.2.5.3":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.moveToX));
                Debug.Log("moveX request");
                break;
            case "1.3.2.5.4":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.moveToY));
                Debug.Log("moveY request");
                break;
            case "1.3.2.5.5":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.lookAt));
                Debug.Log("LookAt request");
                break;

            // -------------------------------------------- Weapon Table --------------------//
            #region weapon table
            case "1.3.2.5.6":
                Debug.Log("weapontable request");
                break;

            case "1.3.2.5.6.1":
                Debug.Log("MG request");
                break;
            case "1.3.2.5.6.1.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.machineGunAmmo));
                Debug.Log("MG  Ammo request");
                break;
            case "1.3.2.5.6.1.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.machineGunDamage));
                Debug.Log("MG  Damage request");
                break;


            case "1.3.2.5.6.2":
                Debug.Log("Missiles request");
                break;
            case "1.3.2.5.6.2.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.missileLauncherAmmo));
                Debug.Log("Missiles  Ammo request");
                break;
            case "1.3.2.5.6.2.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.missileLauncherDamage));
                Debug.Log("Missiles  Damage request" );
                break;


            case "1.3.2.5.6.3":
                Debug.Log("Railgun request");
                break;
            case "1.3.2.5.6.3.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.railGunAmmo));
                Debug.Log("Railgun  Ammo request");
                break;
            case "1.3.2.5.6.3.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.railGunDamage));
                Debug.Log("Railgun  Damage request");
                break;
            #endregion

            // -------------------------------------------- Weapon Table END --------------------//

            case "1.3.2.5.7":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.nukeState));
                Debug.Log("nukeState request");
                break;

            case "1.3.2.5.8":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.nukeCounter));
                Debug.Log("nukeLaunch request");
                break;

            // -------------------------------------------- enemies Table --------------------//
            #region enemies table
            case "1.3.2.5.9":
                Debug.Log("radarTable request");
                break;

            // --------------------------------------------   --------------------//

            case "1.3.2.5.9.1":
                Debug.Log("radarTable enemy request");      
                break;

            case "1.3.2.5.9.1.1":
                Debug.Log("HEERE enemy 1");
                if (metalGear.targetSize.Count <= 0)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                responsePacket.Pdu.VbList.Add(Ooid, new OctetString("X:" + metalGear.positionXEnemies[0].ToString() + "Y:" + metalGear.positionYEnemies[0].ToString()));
                Debug.Log("radarTable enemy positionrequest");
                break;
            case "1.3.2.5.9.1.2":
                if (metalGear.targetSize.Count <= 0)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetSize[0]));
                Debug.Log("radarTable enemy size request");
                break;
            case "1.3.2.5.9.1.3":
                if (metalGear.targetSize.Count <= 0)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetThreat[0]));
                Debug.Log("radarTable enemy threat request");
                break;
            case "1.3.2.5.9.1.4":
                 if (metalGear.targetSize.Count <= 0)
                 {
                     Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                 }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetAttacking[0]));
                Debug.Log("radarTable enemy attack request");
                break;

             // --------------------------------------------   --------------------//

            case "1.3.2.5.9.2":
                Debug.Log("radarTable enemy request");      
                break;

            case "1.3.2.5.9.2.1":
                Debug.Log("HEERE enemy 2");

                if (metalGear.targetSize.Count <= 1)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                responsePacket.Pdu.VbList.Add(Ooid, new OctetString("X:" + metalGear.positionXEnemies[1].ToString() + "Y:" + metalGear.positionYEnemies[1].ToString()));
                Debug.Log("radarTable enemy positionrequest");
                break;
            case "1.3.2.5.9.2.2":
                if (metalGear.targetSize.Count <= 1)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetSize[1]));
                Debug.Log("radarTable enemy size request");
                break;
            case "1.3.2.5.9.2.3":
                if (metalGear.targetSize.Count <= 1)
                {

                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetThreat[1]));
                Debug.Log("radarTable enemy threat request");
                break;
            case "1.3.2.5.9.2.4":
                if (metalGear.targetSize.Count <= 1)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetAttacking[1]));
                Debug.Log("radarTable enemy attack request");
                break;

             // --------------------------------------------   --------------------//

            case "1.3.2.5.9.3":
                Debug.Log("radarTable enemy request");      
                break;

            case "1.3.2.5.9.3.1":
                Debug.Log("HEERE enemy 3");

                if (metalGear.targetSize.Count <= 2)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                Debug.Log("HEERE enemy 3 after if");

                responsePacket.Pdu.VbList.Add(Ooid, new OctetString("X:" + metalGear.positionXEnemies[2].ToString() + "Y:" + metalGear.positionYEnemies[2].ToString()));
                Debug.Log("radarTable enemy positionrequest");
                break;
            case "1.3.2.5.9.3.2":
                if (metalGear.targetSize.Count <= 2){
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                } //radar position doesnt exist
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetSize[2]));
                Debug.Log("radarTable enemy size request");
                break;
            case "1.3.2.5.9.3.3":
                if (metalGear.targetSize.Count <= 2) {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetThreat[2]));
                Debug.Log("radarTable enemy threat request");
                break;
            case "1.3.2.5.9.3.4":
                if (metalGear.targetSize.Count <= 2)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32( (uint)metalGear.targetAttacking[2]));
                Debug.Log("radarTable enemy attack request");
                break;

            #endregion
            // -------------------------------------------- enemies Table  end--------------------//

            case "1.3.2.5.10":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.radarState));
                Debug.Log("radarState request");
                break;
            case "1.3.2.5.11":
                Debug.Log("cameraTable request");
                break;
            case "1.3.2.5.12":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.selectedCamera));
                Debug.Log("selectedCamera request");
                break;

            // -------------------------------------------- Body Table --------------------//
            #region Body table
            case "1.3.2.5.13":  
                Debug.Log("bodyTable request");
                break;

            case "1.3.2.5.13.1":
                Debug.Log("head request");
                break;
            case "1.3.2.5.13.1.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.HeadHealth));
                Debug.Log("head  health request");
                break;
            case "1.3.2.5.13.1.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.HeadArmor));
                Debug.Log("head  armor request");
                break;


            case "1.3.2.5.13.2":
                Debug.Log("arm request");
                break;
            case "1.3.2.5.13.2.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.ArmHealth));
                Debug.Log("arm  health request");
                break;
            case "1.3.2.5.13.2.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.ArmArmor));
                Debug.Log("arm  armor request");
                break;


            case "1.3.2.5.13.3":
                Debug.Log("leg request");
                break;
            case "1.3.2.5.13.3.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.LegsHealth));
                Debug.Log("leg  health request");
                break;
            case "1.3.2.5.13.3.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.LegsArmor));
                Debug.Log("leg  armor request");
                break;

            #endregion
            // -------------------------------------------- Body Table end --------------------//

            case "1.3.2.5.14":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.selectedTarget));
                Debug.Log("selectedTarget request");
                break;
            case "1.3.2.5.15":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.selectdGun));
                Debug.Log("selectedGun request");
                break;
            case "1.3.2.5.16":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.attacking));
                Debug.Log("attacking request");
                break;
            case "1.3.2.5.17":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.underAttack));
                Debug.Log("attacking request");
                break;
            default:
                Debug.Log("Wroing OID");
                responsePacket.Pdu.ErrorStatus = 1; //  error
                break;
        }

        Debug.Log("out of the cases");

    }


    /// <summary>
    /// Process a received SNMP SetRequest
    /// </summary>
    /// <param name="Ooid"></param>
    /// <param name="valuePair"></param>
    public void ProcessSetRequest(Oid Ooid, Vb valuePair)
    {

        switch (Ooid.ToString())
        {
            case "1.3.2.5.1":
                Debug.Log("SysUp set request -- error read only");
                break;
            case "1.3.2.5.2":
                Debug.Log("location set request -- error read only");
                break;
            case "1.3.2.5.3":
                metalGear.moveToX = int.Parse(valuePair.Value.ToString());
                Debug.Log("moveX set request");
                break;
            case "1.3.2.5.4":
                metalGear.moveToY = int.Parse(valuePair.Value.ToString());
                Debug.Log("moveY set request");
                break;
            case "1.3.2.5.5":
                metalGear.lookAt = (uint)int.Parse(valuePair.Value.ToString());
                Debug.Log("LookAt set request");
                break;
            /*case "1.3.2.5.6":
                Debug.Log("weapontable request");
                break;
                */
            case "1.3.2.5.7":
                Debug.Log("nukeState set request --read only");
                break;

            case "1.3.2.5.8":
                Debug.Log("nukeLaunch set request --read only");
                break;
            case "1.3.2.5.9":
                Debug.Log("radarTable request");
                break;
            case "1.3.2.5.10":
                Debug.Log("radarState set request --read only");
                break;
            case "1.3.2.5.11":
                Debug.Log("cameraTable request");
                break;
            case "1.3.2.5.12":
                metalGear.selectedCamera = (uint)int.Parse(valuePair.Value.ToString());
                Debug.Log("selectedCamera request");
                break;
            case "1.3.2.5.13":
                Debug.Log("bodyTable request");
                break;
            case "1.3.2.5.14":
                metalGear.selectedTarget = int.Parse(valuePair.Value.ToString());
                Debug.Log("selectedTarget set request");
                break;
            case "1.3.2.5.15":
                metalGear.selectdGun = int.Parse(valuePair.Value.ToString());
                Debug.Log("selectedGun set request");
                break;
            case "1.3.2.5.16":
                metalGear.attacking = int.Parse(valuePair.Value.ToString());
                Debug.Log("attacking request");
                break;
            case "1.3.2.5.17":
                Debug.Log("under attacking set request error - read only");
                break;
            default:
                Debug.Log("Wroing OID");
                responsePacket.Pdu.ErrorStatus = 1; // no error
                break;
        }

    }


    /// <summary>
	/// Traps are only sent when using an agent and manager in the same system (localhost)
    ///  under attack 0
    ///  not under attack anymore 1
    ///  found enemy 2
    ///  head bellow 50 3
    ///  head bellow 25 4
    ///  head bellow 0 5
    ///  legs bellow 50 6
    ///  legs bellow 25 7
    ///  legs bellow 0 8
    /// </summary>
    /// <param name="trapCase"></param>
    public void sendTrap(int trapCase) {

        string underAttackOid = "1";
        string foundEnemyOid = "2";
        string headOid = "5.1";
        string legsOid = "5.2";
        string armsOid = "5.3";

        TrapAgent agent = new TrapAgent();

        // Variable Binding collection to send with the trap
        VbCollection col = new VbCollection();
        
        switch (trapCase)
        {
            //under attack
            case 0:
                col.Add(new Oid("1.3.2.5." + underAttackOid), new Counter32((uint)metalGear.underAttack));
                break;

            //not under attack anymore
            case 1:
                col.Add(new Oid("1.3.2.5." + underAttackOid), new Counter32((uint)metalGear.underAttack));
                break;
            
            // found enemy
            case 2:
                col.Add(new Oid("1.3.2.5." + foundEnemyOid), new Counter32(1));
                break;
            // head bellow 50
            case 3:
                col.Add(new Oid("1.3.2.5." + headOid), new Counter32((uint)metalGear.HeadHealth));
                break;
            // head bellow 25
            case 4:
                col.Add(new Oid("1.3.2.5." + headOid), new Counter32((uint)metalGear.HeadHealth));
                break;
            // head destroyed
            case 5:
                col.Add(new Oid("1.3.2.5." + headOid), new Counter32((uint)metalGear.HeadHealth));
                break;

            // legs bellow 50
            case 6:
                col.Add(new Oid("1.3.2.5." + legsOid), new Counter32((uint)metalGear.LegsHealth));
                break;
            // legs bellow 25
            case 7:
                col.Add(new Oid("1.3.2.5." + legsOid), new Counter32((uint)metalGear.LegsHealth));
                break;
            // legs destroyed
            case 8:
                col.Add(new Oid("1.3.2.5." + legsOid), new Counter32((uint)metalGear.LegsHealth));
                break;
         
        }
        Debug.Log("sending trap");
        // Send the trap to the localhost port 162
        agent.SendV1Trap(new IpAddress("127.0.0.1"), 16009, "public",
                         new Oid("1.3.2.5.0"), new IpAddress("127.0.0.1"),
                         SnmpConstants.LinkUp, 0, 13432, col);


    }

    /// <summary>
    /// Print a packet in the screen text object
    /// </summary>
    /// <param name="packet"></param>
    public void PrintPacket()
    {
        if (textInstance.text.Length > 200)
        {
            textInstance.text = " ";
        }
        textInstance.text += textTemp;

    }


    public void GetNext(Oid Ooid)
    {
        //we return the next so we "translate" the mib returns from the Get method
        switch (Ooid.ToString())
        {
            case "1.3.2.5":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32(metalGear.sysUpTime));
                Debug.Log("SysUp request");
                break;
            case "1.3.2.5.1":
                responsePacket.Pdu.VbList.Add(Ooid, new OctetString(metalGear.location));
                Debug.Log("location request");
                break;
            case "1.3.2.5.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.moveToX));
                Debug.Log("moveX request");
                break;
            case "1.3.2.5.3":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.moveToY));
                Debug.Log("moveY request");
                break;
            case "1.3.2.5.4":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.lookAt));
                Debug.Log("LookAt request");
                break;

            // -------------------------------------------- Weapon Table --------------------//
            #region weapon table
            case "1.3.2.5.6":                
            case "1.3.2.5.6.1":               
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.machineGunAmmo));
                Debug.Log("MG  Ammo request");
                break;
            case "1.3.2.5.6.1.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.machineGunDamage));
                Debug.Log("MG  Damage request");
                break;


            case "1.3.2.5.6.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.missileLauncherAmmo));
                Debug.Log("Missiles  Ammo request");
                break;
            case "1.3.2.5.6.2.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.missileLauncherDamage));
                Debug.Log("Missiles  Damage request");
                break;


            case "1.3.2.5.6.3":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.railGunAmmo));
                Debug.Log("Railgun  Ammo request");
                break;
            case "1.3.2.5.6.3.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Integer32(metalGear.railGunDamage));
                Debug.Log("Railgun  Damage request");
                break;
            #endregion

            // -------------------------------------------- Weapon Table END --------------------//

            case "1.3.2.5.6.3.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.nukeState));
                Debug.Log("nukeState request");
                break;

            case "1.3.2.5.7":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.nukeCounter));
                Debug.Log("nukeLaunch request");
                break;

            // -------------------------------------------- enemies Table --------------------//
            #region enemies table
            case "1.3.2.5.8":
            case "1.3.2.5.9":
            // --------------------------------------------   --------------------//
            case "1.3.2.5.9.1":

                Debug.Log("HEERE enemy 1");
                //in here we have to return the next node of the mib if it isnt outside of the mib root
                if (metalGear.targetSize.Count <= 0)
                {
                    if(Ooid.ToString().CompareTo("1.3.2.5.9.1") == 1){
                        Debug.Log("Wroing OID");
                        responsePacket.Pdu.ErrorStatus = 1; //  error
                        break;
                    }
                    responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.radarState));
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new OctetString("X:" + metalGear.positionXEnemies[0].ToString() + "Y:" + metalGear.positionYEnemies[0].ToString()));
                Debug.Log("radarTable enemy positionrequest");
                break;
            case "1.3.2.5.9.1.1":
                if (metalGear.targetSize.Count <= 0)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetSize[0]));
                Debug.Log("radarTable enemy size request");
                break;
            case "1.3.2.5.9.1.2":
                if (metalGear.targetSize.Count <= 0)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;

                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetThreat[0]));
                Debug.Log("radarTable enemy threat request");
                break;
            case "1.3.2.5.9.1.3":
                if (metalGear.targetSize.Count <= 0)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetAttacking[0]));
                Debug.Log("radarTable enemy attack request");
                break;

            // --------------------------------------------   --------------------//

            case "1.3.2.5.9.2":
                Debug.Log("HEERE enemy 2");

                if (metalGear.targetSize.Count <= 1)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new OctetString("X:" + metalGear.positionXEnemies[1].ToString() + "Y:" + metalGear.positionYEnemies[1].ToString()));
                Debug.Log("radarTable enemy positionrequest");
                break;
            case "1.3.2.5.9.2.1":
                if (metalGear.targetSize.Count <= 1)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetSize[1]));
                Debug.Log("radarTable enemy size request");
                break;
            case "1.3.2.5.9.2.2":
                if (metalGear.targetSize.Count <= 1)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetThreat[1]));
                Debug.Log("radarTable enemy threat request");
                break;
            case "1.3.2.5.9.2.3":
                if (metalGear.targetSize.Count <= 1)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetAttacking[1]));
                Debug.Log("radarTable enemy attack request");
                break;

            // --------------------------------------------   --------------------//

            case "1.3.2.5.9.3":
                Debug.Log("HEERE enemy 3");
                if (metalGear.targetSize.Count <= 2)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                Debug.Log("HEERE enemy 3 after if");

                responsePacket.Pdu.VbList.Add(Ooid, new OctetString("X:" + metalGear.positionXEnemies[2].ToString() + "Y:" + metalGear.positionYEnemies[2].ToString()));
                Debug.Log("radarTable enemy positionrequest");
                break;
            case "1.3.2.5.9.3.1":
                if (metalGear.targetSize.Count <= 2)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                } //radar position doesnt exist
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetSize[2]));
                Debug.Log("radarTable enemy size request");
                break;
            case "1.3.2.5.9.3.2":
                if (metalGear.targetSize.Count <= 2)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetThreat[2]));
                Debug.Log("radarTable enemy threat request");
                break;
            case "1.3.2.5.9.3.3":
                if (metalGear.targetSize.Count <= 2)
                {
                    Debug.Log("Wroing OID");
                    responsePacket.Pdu.ErrorStatus = 1; //  error
                    break;
                }
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.targetAttacking[2]));
                Debug.Log("radarTable enemy attack request");
                break;

            #endregion
            // -------------------------------------------- enemies Table  end--------------------//

            case "1.3.2.5.9.3.4":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.radarState));
                Debug.Log("radarState request");
                break;
            case "1.3.2.5.10":
                Debug.Log("cameraTable request");
                break;
            case "1.3.2.5.11":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.selectedCamera));
                Debug.Log("selectedCamera request");
                break;

            // -------------------------------------------- Body Table --------------------//
            #region Body table
            case "1.3.2.5.12":
            case "1.3.2.5.13":
            case "1.3.2.5.13.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.HeadHealth));
                Debug.Log("head  health request");
                break;
            case "1.3.2.5.13.1.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.HeadArmor));
                Debug.Log("head  armor request");
                break;
            case "1.3.2.5.13.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.ArmHealth));
                Debug.Log("arm  health request");
                break;
            case "1.3.2.5.13.2.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.ArmArmor));
                Debug.Log("arm  armor request");
                break;
            case "1.3.2.5.13.3":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.LegsHealth));
                Debug.Log("leg  health request");
                break;
            case "1.3.2.5.13.3.1":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.LegsArmor));
                Debug.Log("leg  armor request");
                break;

            #endregion
            // -------------------------------------------- Body Table end --------------------//

            case "1.3.2.5.13.3.2":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.selectedTarget));
                Debug.Log("selectedTarget request");
                break;
            case "1.3.2.5.14":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.selectdGun));
                Debug.Log("selectedGun request");
                break;
            case "1.3.2.5.15":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.attacking));
                Debug.Log("attacking request");
                break;
            case "1.3.2.5.16":
                responsePacket.Pdu.VbList.Add(Ooid, new Counter32((uint)metalGear.underAttack));
                Debug.Log("Under Attack request");
                break;
            default:
                Debug.Log("Out of bounds OID");
                responsePacket.Pdu.ErrorStatus = 1; //  error
                break;
        }
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
 * .17 underAttack

*/