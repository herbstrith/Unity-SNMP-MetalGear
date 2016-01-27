using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Text.RegularExpressions;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using SnmpSharpNet;
using System.Net;
using System.Net.Sockets;
using System.Threading;
public class Manager : MonoBehaviour {


	public bool CallAgent = false;
    public bool SetAgent = false;
    public string baseTreeOid = "1.3.2.5";


    public Oid baseTreeId;



    /* Agent variables*/

    public uint sysUpTime = 0;
    public int selectdGun = 0;
    public int selectedTarget;
    public int attacking;
    public int underAttack;

    public string location;

    //public int moveToX = 0;
    //public int moveToY = 0;

    public uint lookAt;
    //guns
    public Transform machineGun;
    public int machineGunAmmo = 1000;
    public int machineGunDamage = 1;

    public Transform missileLauncher;
    public int missileLauncherAmmo = 8;
    public int missileLauncherDamage = 50;

    public Transform RailGun;
    public int railGunAmmo = -1;
    public int railGunDamage = 50000;

    //cameras

    public Camera frontCamera;
    public Camera backCamera;

    public uint selectedCamera;

    //radar
    public List<Transform> targets = new List<Transform>(3);
    public List<int> targetSize = new List<int>(3);
    public List<int> targetThreat= new List<int>(3);
    public List<int> targetAttacking = new List<int>(3);


    public uint radarState;

    //body
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



    String snmpAgent = "127.0.0.1";
    String snmpCommunity = "public";


    public GameObject[] targetsImages;


    public int foundEnemy = 0;


    public int cameraValue = 0;


    public int MoveToXInput = 50;
    public int MoveToYInput = 20;

    public Image metalGearMapPosition;

    public GameObject Enemy1;
    public GameObject Enemy2;
    public GameObject Enemy3;


    public GameObject EnemyAlarmTrapLight;
    public GameObject UnderAttackAlarmTrapLight;
    public GameObject HealthAlarmTrapLight;


    public GameObject GunName1;
    public GameObject GunAmmo1;
    public GameObject GunDamage1;

    public GameObject GunName2;
    public GameObject GunAmmo2;
    public GameObject GunDamage2;

    public GameObject GunName3;
    public GameObject GunAmmo3;
    public GameObject GunDamage3;

    public Image HeadImage;
    public Image BodyImage;
    public Image LegImage;


    public Image MapUI;
    public Text XUI;
    public Text YUI;

    public Image PointerMapPosition;

    public Text inputIp;
    public Text inputPort;

    public int snmpPort = 16100;

    public GameObject Canvas;


    private float UnderAttackLightCounter = 5f;
    private float FoundEnemyLightCounter = 5f;
    private float HealthLightCounter = 5f;
    public int healthWarning = 0;

    public Text PacketPrinterSend;
    public Text PacketPrinterRcv;


    bool run_trap_thread = true;

    public void Connect()
    {
        snmpAgent = inputIp.text;

        //snmpPort = int.Parse(inputPort.text);

        if(StartUp() == 1 )
        {
            Debug.Log("Couldn't connect to agent");
            return;
        }

        //activate the ui
        for(int i =2; i < Canvas.transform.GetChildCount(); i ++)
        {
            Canvas.transform.GetChild(i).gameObject.SetActive(true);
        }

        //deactivate the login input and button
        Canvas.transform.GetChild(1).gameObject.SetActive(false);
    }

    void Awake()
    {
        Debug.Log("manager: Awake manager!");


        Thread vThread = new Thread(new ThreadStart(this.TrapThread));


        vThread.Start();
    }


	// Use this for initialization
	void Start () {

        //StartUp();
	}

    /// <summary>
    /// Kill the trap thread when quitting the app ... else the application will hang on quit
    /// </summary>
    void OnApplicationQuit()
    {
        run_trap_thread = false;
    }

	// Update is called once per frame
	void Update () {


        /* When a trap occurs */
        if(foundEnemy != 0)
        {
            //play enemy warning on the ui

            EnemyAlarmTrapLight.SetActive(true);

            FoundEnemyLightCounter -= Time.deltaTime;
            if (foundEnemy <= 0)
            {
                EnemyAlarmTrapLight.SetActive(false);
                foundEnemy = 0;
            }
            //foundEnemy = 0;
        }

        if(underAttack == 1)
        {
            //play under attack warning
            UnderAttackAlarmTrapLight.SetActive(true);

            UnderAttackLightCounter -= Time.deltaTime;
            if (UnderAttackLightCounter <= 0)
            {
                UnderAttackAlarmTrapLight.SetActive(false);

                underAttack = 0;
            }

            
        }

        if (healthWarning == 1)
        {
            //play under attack warning
            HealthAlarmTrapLight.SetActive(true);

            HealthLightCounter -= Time.deltaTime;
            if( HealthLightCounter <= 0 )
            {
                UnderAttackAlarmTrapLight.SetActive(false);

                healthWarning = 0;

            }
            
        }


		
	}



    public void MouseOnMap()
    {
        float multiplyDistanceRatio = 1f;
        float mapPosX_zero = 390f;
        float mapPosY_zero = 230f;
        var screenPoint = Input.mousePosition;
        screenPoint.z = 10.0f; //distance of the plane from the camera
        var mousePos = Camera.main.ScreenToWorldPoint(screenPoint);
       // var mousePos = Input.mousePosition;
        var imagePos = MapUI.transform.position;
        var imageWidth = MapUI.rectTransform.sizeDelta.x;
        var imageHeight = MapUI.rectTransform.sizeDelta.y;
        
        var posInImage = mousePos - imagePos;
        // -9.7, 4.8 are the 0 position in the canvas     10.731 -> 2.7
        Debug.Log("inside map click on" + screenPoint + "--------------");

        MoveToXInput = (int)((screenPoint.x - mapPosX_zero) * multiplyDistanceRatio);
        MoveToYInput = (int)((screenPoint.y - mapPosY_zero) * multiplyDistanceRatio);

        XUI.text = MoveToXInput.ToString();
        YUI.text = MoveToYInput.ToString();

        MoveToMarkerMove();
        Debug.Log("LocalCursor:" + posInImage);
    
    }



    /// <summary>
    /// Called on the agent connection to initialize the variables on the manager
    /// </summary>
    public int StartUp()
    {
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a request Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Get;
        //pdu.VbList.Add("1.3.6.1.2.1.1.1.0");

        pdu.VbList.Add(baseTreeOid + ".1");     //sysuptime 1 
        pdu.VbList.Add(baseTreeOid + ".2"); //location 2
        pdu.VbList.Add(baseTreeOid + ".3"); //movex 3
        pdu.VbList.Add(baseTreeOid + ".4"); //movey 4
        pdu.VbList.Add(baseTreeOid + ".5"); //lookat 5 

        pdu.VbList.Add(baseTreeOid + ".6.1.1");  //mg ammo 6 
        pdu.VbList.Add(baseTreeOid + ".6.1.2");  //mg dam 7 
        pdu.VbList.Add(baseTreeOid + ".6.2.1"); //missile ammo 8
        pdu.VbList.Add(baseTreeOid + ".6.2.2"); //missile dam 9
        pdu.VbList.Add(baseTreeOid + ".6.3.1"); //rail ammo 10
        pdu.VbList.Add(baseTreeOid + ".6.3.2"); //rail dam 11
        pdu.VbList.Add(baseTreeOid + ".7"); //nukestate 12
        pdu.VbList.Add(baseTreeOid + ".8"); //nukeLaunch 13
        //.9 is radar table
        pdu.VbList.Add(baseTreeOid + ".10"); //radar state 14
        //.11 is camera table ---DEPRECATED
        pdu.VbList.Add(baseTreeOid + ".12"); //camera select 16


        pdu.VbList.Add(baseTreeOid + ".13.1.1");  //head health 17
        pdu.VbList.Add(baseTreeOid + ".13.1.2");  //head armor 18
        pdu.VbList.Add(baseTreeOid + ".13.2.1");  // arm healt 13
        pdu.VbList.Add(baseTreeOid + ".13.2.2"); // arm armor 20
        pdu.VbList.Add(baseTreeOid + ".13.3.1");  //legs healt 21
        pdu.VbList.Add(baseTreeOid + ".13.3.2");  //legs armor 22

        pdu.VbList.Add(baseTreeOid + ".14");  // selec target 23
        pdu.VbList.Add(baseTreeOid + ".15");  // select gun 24
        pdu.VbList.Add(baseTreeOid + ".16");  //attack 25
        pdu.VbList.Add(baseTreeOid + ".17");  //under attack 26


        Dictionary<Oid, AsnType> result = snmp.Get(SnmpVersion.Ver1, pdu);


        if (result == null)
        {
            Debug.Log("Manager:Request failed.");
            return 1;
        }
        else
        {

            List<AsnType> list = new List<AsnType>(result.Values);

            sysUpTime = uint.Parse(list[0].ToString());

            location = list[1].ToString();  //   send X:--- Y:---
            lookAt = uint.Parse(list[4].ToString()); ;           //   rotate to x degrees


            //guns
             machineGunAmmo = int.Parse(list[5].ToString());
             machineGunDamage = int.Parse(list[6].ToString());

             GunAmmo1.GetComponent<Text>().text = machineGunAmmo.ToString();
             GunDamage1.GetComponent<Text>().text = machineGunDamage.ToString();


             missileLauncherAmmo= int.Parse(list[7].ToString());
             missileLauncherDamage = int.Parse(list[8].ToString());


             GunAmmo2.GetComponent<Text>().text = missileLauncherAmmo.ToString();
             GunDamage2.GetComponent<Text>().text = missileLauncherDamage.ToString();
             Debug.Log(list[9].ToString() + list[10].ToString());
             railGunAmmo = int.Parse(list[9].ToString());
             railGunDamage = int.Parse(list[10].ToString());


             GunAmmo3.GetComponent<Text>().text = railGunAmmo.ToString();
             GunDamage3.GetComponent<Text>().text = railGunDamage.ToString();

            //cameras

            nukeState = uint.Parse(list[11].ToString());
            nukeCounter = int.Parse(list[12].ToString());
           

    
             radarState = uint.Parse(list[13].ToString());

            selectedCamera  = uint.Parse(list[14].ToString());

            //bodie
            HeadHealth = int.Parse(list[15].ToString());
            HeadArmor = int.Parse(list[16].ToString());
            ArmHealth= int.Parse(list[17].ToString());
            ArmArmor = int.Parse(list[18].ToString());
            LegsHealth = int.Parse(list[19].ToString());
            LegsArmor = int.Parse(list[20].ToString());

            selectedTarget = int.Parse(list[21].ToString());

            selectdGun = int.Parse(list[22].ToString());
            attacking  = int.Parse(list[23].ToString());
            underAttack  = int.Parse(list[24].ToString());

            ChangeBodyColors();

            GetPosition();
        }

        return 0;
    }


    /// <summary>
    /// Ask the agent for all the health status
    /// </summary>
    public void UpdateHealth()
    {

        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a request Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Get;

        pdu.VbList.Add(baseTreeOid + ".13.1.1");  //head health 17
        pdu.VbList.Add(baseTreeOid + ".13.1.2");  //head armor 18
        pdu.VbList.Add(baseTreeOid + ".13.2.1");  // arm healt 13
        pdu.VbList.Add(baseTreeOid + ".13.2.2"); // arm armor 20
        pdu.VbList.Add(baseTreeOid + ".13.3.1");  //legs healt 21
        pdu.VbList.Add(baseTreeOid + ".13.3.2");  //legs armor 22

        PrintPacketSend(pdu);
        Dictionary<Oid, AsnType> result = snmp.Get(SnmpVersion.Ver1, pdu);


        if (result == null)
        {
            Debug.Log("Manager:Get failed.");
            return;
        }


        List<AsnType> list = new List<AsnType>(result.Values);


        //bodie
        ArmHealth = int.Parse(list[0].ToString());
        ArmArmor = int.Parse(list[1].ToString());
        LegsHealth = int.Parse(list[2].ToString());
        LegsArmor = int.Parse(list[3].ToString());
        HeadHealth = int.Parse(list[4].ToString());
        HeadArmor = int.Parse(list[5].ToString());

        ChangeBodyColors();
    }

    /// <summary>
    /// Update enemies information
    /// </summary>
    public void UpdateEnemies()
    {
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 0);
        // Create a request Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Get;

        //using a get next here would be the ideal ... but lets do the naive way for simplicity

        // asks for all the enemies around and put the values on a list, if some fail we can notice it by the list size
        pdu.VbList.Add(baseTreeOid + ".9.1.1");  
        pdu.VbList.Add(baseTreeOid + ".9.1.2");  
        pdu.VbList.Add(baseTreeOid + ".9.1.3");  
        pdu.VbList.Add(baseTreeOid + ".9.1.4");

        Dictionary<Oid, AsnType> result = new Dictionary<Oid, AsnType>();
        List<AsnType> list = new List<AsnType>(result.Values);

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result1 = snmp.Get(SnmpVersion.Ver1, pdu);

        if (result1 != null)
        {
            foreach (KeyValuePair<Oid, AsnType> entry in result1)
            {
                list.Add(entry.Value);
            }
        }


        SimpleSnmp snmp2 = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 0);
        pdu = new Pdu();
        pdu.Type = PduType.Get;

        pdu.VbList.Add(baseTreeOid + ".9.2.1");  
        pdu.VbList.Add(baseTreeOid + ".9.2.2");  
        pdu.VbList.Add(baseTreeOid + ".9.2.3");  
        pdu.VbList.Add(baseTreeOid + ".9.2.4");

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result2 = snmp2.Get(SnmpVersion.Ver1, pdu);
        if (result2 != null)
        {
            foreach (KeyValuePair<Oid, AsnType> entry in result2)
            {
                list.Add(entry.Value);
            }
        }

        SimpleSnmp snmp3 = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 0);
        pdu = new Pdu();
        pdu.Type = PduType.Get;

        pdu.VbList.Add(baseTreeOid + ".9.3.1");  
        pdu.VbList.Add(baseTreeOid + ".9.3.2");  
        pdu.VbList.Add(baseTreeOid + ".9.3.3");  
        pdu.VbList.Add(baseTreeOid + ".9.3.4");

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result3 = snmp3.Get(SnmpVersion.Ver1, pdu);

        if (result3 != null)
        {
            foreach (KeyValuePair<Oid, AsnType> entry in result3)
            {
                list.Add(entry.Value);
            }
        }


        if (result == null)
        {
            Debug.Log("Manager:Get enemies failed.");

            Enemy1.SetActive(false);
            Enemy2.SetActive(false);
            Enemy3.SetActive(false);
            return;
        }
			     
        //parse the position strings 

        Enemy1.SetActive(false);
        Enemy1.transform.GetChild(1).gameObject.SetActive(false);

        Enemy2.SetActive(false);
        Enemy2.transform.GetChild(1).gameObject.SetActive(false);

        Enemy3.SetActive(false);
        Enemy3.transform.GetChild(1).gameObject.SetActive(false);

        
		if(list.Count >= 4)
        {
            var r = new Regex(@"[0-9]+\.[0-9]+");
            var mc = r.Matches(list[0].ToString());
            var matches = new Match[mc.Count];
            mc.CopyTo(matches, 0);

            var myFloats = new float[matches.Length];

            float x = float.Parse(matches[0].Value);
            float y = float.Parse(matches[1].Value);

            Enemy1.GetComponent<Image>().rectTransform.anchoredPosition = new Vector2(x, y);

            Enemy1.GetComponent<Image>().rectTransform.localScale = new Vector2(1, 1) * int.Parse(list[1].ToString())*0.5f;

            if (int.Parse(list[3].ToString()) == 1)
            {
                Enemy1.transform.GetChild(2).gameObject.SetActive(true);
            }
            else
            {
                Enemy1.transform.GetChild(2).gameObject.SetActive(false);
            }
            
            Enemy1.SetActive(true);
        }

        if (list.Count >= 8)
        {
            var r = new Regex(@"[0-9]+\.[0-9]+");
            var mc = r.Matches(list[4].ToString());
            var matches = new Match[mc.Count];
            mc.CopyTo(matches, 0);

            var myFloats = new float[matches.Length];

            float x = float.Parse(matches[0].Value);
            float y = float.Parse(matches[1].Value);

            Enemy2.GetComponent<Image>().rectTransform.anchoredPosition = new Vector2(x, y);
            Enemy2.GetComponent<Image>().rectTransform.localScale = new Vector2(1, 1) * int.Parse(list[5].ToString()) * 0.5f;

            if (int.Parse(list[7].ToString()) == 1)
            {
                Enemy2.transform.GetChild(2).gameObject.SetActive(true);
            }
            else
            {
                Enemy2.transform.GetChild(2).gameObject.SetActive(false);
            }

            Enemy2.SetActive(true);
        }

        if (list.Count >= 12)
        {
            var r = new Regex(@"[0-9]+\.[0-9]+");
            var mc = r.Matches(list[8].ToString());
            var matches = new Match[mc.Count];
            mc.CopyTo(matches, 0);

            var myFloats = new float[matches.Length];

            float x = float.Parse(matches[0].Value);
            float y = float.Parse(matches[1].Value);

            Enemy3.GetComponent<Image>().rectTransform.anchoredPosition = new Vector2(x, y);
            Enemy3.GetComponent<Image>().rectTransform.localScale = new Vector2(1, 1) * int.Parse(list[9].ToString()) * 0.5f;


            if (int.Parse(list[11].ToString()) == 1)
            {
                Enemy3.transform.GetChild(2).gameObject.SetActive(true);
            }
            else
            {
                Enemy3.transform.GetChild(2).gameObject.SetActive(false);
            }

            Enemy3.SetActive(true);
        }

        //turn off trap light if it was on
        foundEnemy = 0;

    }


    /// <summary>
    /// Update Ammo information
    /// </summary>
    public void UpdateAmmo()
    {
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a request Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Get;

        pdu.VbList.Add(baseTreeOid + ".6.1.1");  //mg ammo 6 
        pdu.VbList.Add(baseTreeOid + ".6.2.1"); //missile ammo 8
        pdu.VbList.Add(baseTreeOid + ".6.3.1"); //rail ammo 10

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result = snmp.Get(SnmpVersion.Ver1, pdu);

        if (result == null)
        {
            Debug.Log("Manager:Set failed.");
            return;
        }


        List<AsnType> list = new List<AsnType>(result.Values);

        //guns
        machineGunAmmo = int.Parse(list[0].ToString());
        missileLauncherAmmo = int.Parse(list[1].ToString());
        railGunAmmo = int.Parse(list[2].ToString());
    }
		 
    /// <summary>
    /// Send a SetRequest to the agent to select a weapon
    /// </summary>
    /// <param name="selected"></param>
    public void SetWeapon(int selected)
    {
        String snmpAgent = "127.0.0.1";
        String snmpCommunity = "public";
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a set Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Set;

        pdu.VbList.Add(new Oid(baseTreeOid + ".15"), new Counter32((uint)selected));

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result = snmp.Set(SnmpVersion.Ver1, pdu);
        if (result == null)
        {
            Debug.Log("Manager:Set failed.");
        }
        else
        {
            foreach (KeyValuePair<Oid, AsnType> entry in result)
            {
                selectdGun = int.Parse(entry.Value.ToString());        
            }
        }
    }

    /// <summary>
    /// Send a SetRequest to the agent to select a camera
    /// </summary>
    /// <param name="selected"></param>
    public void SetCamera()
    {
        String snmpAgent = "127.0.0.1";
        String snmpCommunity = "public";
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a set Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Set;

        pdu.VbList.Add(new Oid(baseTreeOid + ".12"), new Counter32((uint)cameraValue));

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result = snmp.Set(SnmpVersion.Ver1, pdu);

        if (result == null)
        {
            Debug.Log("Manager:Set failed.");
        }
        else
        {
            foreach (KeyValuePair<Oid, AsnType> entry in result)
            {
                selectedCamera = uint.Parse(entry.Value.ToString());
		    }
        }

        if(selectedCamera == cameraValue)
        {
            cameraValue++;
            if (cameraValue > 3)
                cameraValue = 0;
        }
    }

    /// <summary>
    /// Send a SetRequest to the agent to select a target
    /// </summary>
    /// <param name="selected"></param>
    public void SetTarget(int selected)
    {
        String snmpAgent = "127.0.0.1";
        String snmpCommunity = "public";
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a set Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Set;

        pdu.VbList.Add(new Oid(baseTreeOid + ".14"), new Counter32((uint)selected));

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result = snmp.Set(SnmpVersion.Ver1, pdu);

        if (result == null)
        {
            Debug.Log("Manager:Set failed.");
        }
        else
        {
            foreach (KeyValuePair<Oid, AsnType> entry in result)
            {
                selectedTarget = int.Parse(entry.Value.ToString());
            }
        }

        if(selected == 0)
        {
            Enemy1.transform.GetChild(1).gameObject.SetActive(true);
        }
        else
        {
            Enemy1.transform.GetChild(1).gameObject.SetActive(false);
        }

        if (selected == 1)
        {
            Enemy2.transform.GetChild(1).gameObject.SetActive(true);
        }
        else
        {
            Enemy2.transform.GetChild(1).gameObject.SetActive(false);
        }

        if (selected == 2)
        {
            Enemy3.transform.GetChild(1).gameObject.SetActive(true);
        }
        else
        {
            Enemy3.transform.GetChild(1).gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Send a SetRequest to the agent to start attacking
    /// </summary>
    /// <param name="selected"></param>
    public void SetAttacking(int selected)
    {
        String snmpAgent = "127.0.0.1";
        String snmpCommunity = "public";
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a set Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Set;

        pdu.VbList.Add(new Oid(baseTreeOid + ".16"), new Counter32((uint)selected));

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result = snmp.Set(SnmpVersion.Ver1, pdu);
        
        if (result == null)
        {
            Debug.Log("Manager:Set failed.");
        }
        else
        {
            foreach (KeyValuePair<Oid, AsnType> entry in result)
            {
                attacking = int.Parse(entry.Value.ToString());
            }
        }
    }

    
    /// <summary>
    /// for every received enemy, draw them on the map
    /// </summary>
    public void InstantiateEnemyImages()
    {
        int counter = 0;
        foreach (Transform target in targets)
        {
            targetsImages[counter].GetComponent<RectTransform>().position = new Vector3(target.position.x, target.position.y, 0);
            targetsImages[counter].GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1) * targetSize[counter];
            targetsImages[counter].SetActive(true);
            if (targetAttacking[counter] == 1) 
                targetsImages[counter].transform.GetChild(0).gameObject.SetActive(true);
            else
                targetsImages[counter].transform.GetChild(0).gameObject.SetActive(false);

            if (selectedTarget == counter)
                targetsImages[counter].transform.GetChild(1).gameObject.SetActive(true);
            else
                targetsImages[counter].transform.GetChild(1).gameObject.SetActive(false);

            counter++;
        }
    }

    /// <summary>
    /// when the player clicks the button of the enemy, send a setrequest with that enemy id
    /// </summary>
    /// <param name="id"></param>
    public void SetAttackingTarget(int id)
    {
        SetAttacking(id);
    }

    /// <summary>
    /// when button is clicked send the GoTo with the data from the input fields
    /// </summary>
    public void SetGoTo()
    {
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a request Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Set;
        //pdu.VbList.Add("1.3.6.1.2.1.1.1.0");

        pdu.VbList.Add(new Oid(baseTreeOid + ".3"), new Integer32(MoveToXInput));
        pdu.VbList.Add(new Oid(baseTreeOid + ".4"), new Integer32(MoveToYInput));

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result = snmp.Set(SnmpVersion.Ver1, pdu);
        //Debug.Log(result);
        //Dictionary<Oid, AsnType> result = snmp.GetNext(SnmpVersion.Ver1, pdu);
        if (result == null)
        {
            Debug.Log("Manager:Set failed.");
        }
			
    }


    /// <summary>
    /// Send a SNMP get request for the metal gear position
    /// </summary>
    public void GetPosition()
    {
        SimpleSnmp snmp = new SimpleSnmp(snmpAgent, 16100, snmpCommunity, 2000, 2);
        // Create a request Pdu
        Pdu pdu = new Pdu();
        pdu.Type = PduType.Get;
        //pdu.VbList.Add("1.3.6.1.2.1.1.1.0");

        pdu.VbList.Add(baseTreeOid + ".2"); //location 2

        PrintPacketSend(pdu);

        Dictionary<Oid, AsnType> result = snmp.Get(SnmpVersion.Ver1, pdu);
        //Debug.Log(result);
        //Dictionary<Oid, AsnType> result = snmp.GetNext(SnmpVersion.Ver1, pdu);
        if (result == null)
        {
            Debug.Log("Manager:Request failed.");
        }
        else
        {

            List<AsnType> list = new List<AsnType>(result.Values);

            location = list[0].ToString();  //   send X:--- Y:---
            
            var r = new Regex(@"[0-9]+\.[0-9]+");
            var mc = r.Matches(location);
            var matches = new Match[mc.Count];
            mc.CopyTo(matches, 0);

            var myFloats = new float[matches.Length];

            float x = float.Parse(matches[0].Value);
            float y = float.Parse(matches[1].Value);

            metalGearMapPosition.rectTransform.anchoredPosition = new Vector2( x  , y );

        }



    }

    /// <summary>
    /// Move pointer in the map to click position
    /// </summary>
    public void MoveToMarkerMove()
    {
        PointerMapPosition.rectTransform.anchoredPosition = new Vector2(MoveToXInput, MoveToYInput);
    }

    /// <summary>
    /// Thread that will keep listening for traps
    /// </summary>
    public void TrapThread()
    {
		// Construct a socket and bind it to the trap manager port 162 

		Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 16009);
		EndPoint ep = (EndPoint)ipep;
		socket.Bind(ep);
		// Disable timeout processing. Just block until packet is received 
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
		run_trap_thread = true;
		int inlen = -1;

        while (run_trap_thread)
        {
			byte[] indata = new byte[16 * 1024];
			// 16KB receive buffer int inlen = 0;
			IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
			EndPoint inep = (EndPoint)peer;
			
		    inlen = socket.ReceiveFrom(indata, ref inep);

			if (inlen > 0) {

				// Check protocol version int 
				int ver = SnmpPacket.GetProtocolVersion(indata, inlen);
					// Parse SNMP Version 1 TRAP packet 
					SnmpV1TrapPacket pkt = new SnmpV1TrapPacket();
					pkt.decode(indata, inlen);

                    string underAttackOid = "1";
                    string foundEnemyOid = "2";
                    string headOid = "5.1";
                    string legsOid = "5.2";
                    string armsOid = "5.3";


					foreach (Vb v in pkt.Pdu.VbList) {
                        Debug.Log("for each vb");

                        switch (v.Oid.ToString())
                        {
                            case "1.3.2.5." + "1":
                                underAttack = int.Parse(v.Value.ToString());
                                break;


                            // found enemy
                            case "1.3.2.5." + "2":
                                foundEnemy = int.Parse(v.Value.ToString());
                                break;
                            // head bellow 50
                            case "1.3.2.5." + "5.1":
                                healthWarning = 1;
                                HeadHealth = int.Parse(v.Value.ToString());
                                break;
                            // head bellow 25
                            case "1.3.2.5." + "5.2":
                                healthWarning = 1;
                                LegsHealth = int.Parse(v.Value.ToString());
                                break;
                            // head destroyed
                            case "1.3.2.5." + "5.3":
                                healthWarning = 1;
                                ArmHealth = int.Parse(v.Value.ToString());
                                break;

                            
                        }
                          

					}
				
			} else {
				if (inlen == 0)
					Console.WriteLine("Zero length packet received.");
			}
		}
        Debug.Log(" trap thread EXITT");

		
    }

    /// <summary>
    /// change the GUI body image color
    /// </summary>
    public void ChangeBodyColors()
    {
        float redMod= HeadHealth < 0 ? 0f : 5f;
        float blueMod = HeadHealth < 80 ? 0.8f : 1.0f;
        float greenMod = HeadHealth < 33 ? 0.6f : 1.7f;
        HeadImage.color = new Color((HeadHealth / 100f)*redMod, (HeadHealth /100f)*greenMod,  (HeadHealth /100f)*blueMod,255);

        redMod = ArmHealth < 0 ? 0f : 5f;
        blueMod = ArmHealth < 80 ? 0.8f : 1.0f; 
        greenMod = ArmHealth < 33 ? 0.6f : 1.7f;

        BodyImage.color = new Color((ArmHealth / 100f) * redMod, (ArmHealth / 100f) * greenMod, (ArmHealth / 100f) * blueMod, 255);

        redMod = LegsHealth < 0 ? 0f : 5f;
        blueMod = LegsHealth < 80 ? 0.8f : 1.0f; 
        greenMod = LegsHealth < 33 ? 0.6f : 1.7f;
        
        LegImage.color = new Color((LegsHealth / 100f) * redMod, (LegsHealth / 100f) * greenMod, (LegsHealth / 100f) * blueMod, 255);

    }

    /// <summary>
    /// Prints the packets
    /// </summary>
    /// <param name="result"></param>
    public void PrintPacketSend(Pdu result)
    {
        // ErrorStatus other then 0 is an error returned by 
        // the Agent - see SnmpConstants for error definitions
	
        if (result.Type == PduType.Set)
        {
            PacketPrinterSend.text = "Set ";
        }
        if (result.Type == PduType.Get)
        {
            PacketPrinterSend.text = "Get ";
        }
        if (result.ErrorStatus != 0)
        {
            // agent reported an error with the request
            PacketPrinterSend.text = "Error: status " + result.ErrorStatus + " Index " + result.ErrorIndex;
        }
        else
        {
            // Reply variables are returned in the same order as they were added
            //  to the VbList
            foreach (Vb VarBind in result.VbList)
            {
                PacketPrinterSend.text = "Request OID: " + VarBind.Oid.ToString() + " Type " + SnmpConstants.GetTypeName(VarBind.Value.Type) + " Value " + VarBind.Value.ToString();
            }        
        }
    }

    /// <summary>
    /// Prints the packets
    /// </summary>
    /// <param name="result"></param>
    public void PrintPacketRecv(SnmpV1Packet result)
    {
        // ErrorStatus other then 0 is an error returned by 
        // the Agent - see SnmpConstants for error definitions

        if (result.Pdu.Type == PduType.Set)
        {
            PacketPrinterRcv.text = "Set ";
        }
        if (result.Pdu.Type == PduType.Get)
        {
            PacketPrinterRcv.text = "Get ";
        }
        if (result.Pdu.ErrorStatus != 0)
        {
            // agent reported an error with the request
            PacketPrinterRcv.text = "Error: status " + result.Pdu.ErrorStatus + " Index " + result.Pdu.ErrorIndex;
        }
        else
        {
            // Reply variables are returned in the same order as they were added
            //  to the VbList
            foreach (Vb VarBind in result.Pdu.VbList)
            {
                PacketPrinterRcv.text = "Request OID: " + VarBind.Oid.ToString() + " Type " + SnmpConstants.GetTypeName(VarBind.Value.Type) + " Value " + VarBind.Value.ToString();
            }
        }
    }

}
