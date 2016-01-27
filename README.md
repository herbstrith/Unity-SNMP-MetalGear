
##  Project ##
This SNMP agent is inspired in the Metal Gear bipedal weapon from the Metal Gear series. 
The manager has access to  movement and position, radar and enemies detected, status on body parts, weapons information and issue attack commands on detected enemies. O gerente tem controle sobre a posição do robô no mapa, inimigos que o radar do robô detecta, status das partes do robô e informação sobre as armas.
It was done as a college class project. As I couldn't find much help on the SNMP under C# on the internet, I hope this project can help someone out there as a starting reference to his/her own project.

## Tools ##

 For the development, the Unity game engine(http://unity3d.com/) was used to make the metal gear simulation, the agent and the manager (code on C#).
 For the SNMP communication, the SNMPSHARPNET library( http://www.snmpsharpnet.com/) was used, using SNMP V1 as the standard.
 Images and 3D models have been found on the internet (Not done by me)



## The Mib ##
-1.MIB Metal Gear

--1.1 SysupTime : counter32 (read only)

--1.2 Position: OctetString (read only)

--1.3 MoveX: integer32

--1.4 MoveY: integer32

--1.5 LookAt: integer32   ---deprecated


--1.6 WeaponTable: table

----- 1.6.x.1 WeaponAmmo: counter32 (read only)

----- 1.6.x.2 WeaponDamage: integer32 (read only)


--1.7 NukeState: integer32   ---deprecated

--1.8 NukeLaunch: counter32  ---deprecated

--1.9 RadarTable: table

----- 1.9.x.1 EnemyPosition: octectString (read only)

----- 1.9.x.2 EnemySize: Counter32 (read only)

----- 1.9.x.3 EnemyThreat: Counter32 (read only)

----- 1.9.x.4 EnemyAttacking: Counter32 (read only)

--1.10 RadarState: Counter32  ---deprecated

--1.11 CameraTable: table   ---deprecated

--1.12 SelectedCamera: Counter32 

--1.13 BodyTable: table

----- 1.13.x.1 BodyPartHealth: Counter32 (read only)

----- 1.13.x.2 BodyPartArmor: Counter32 (read only)

--1.14 selectedTarget: Counter32

--1.15 selectedWeapon: Counter32

--1.16 Attacking: Counter32

--1.17 UnderAttack: Counter32 (read only)


* Deprecated nodes are features that have been dished during the development.


## How to run ##

You should be able to simply open the folder in unity as a project.

The Assets folder contains the code (Scripts folder) and other assets (models and images found in the internet) used in the project.

You will also find two folders in the compressed file (MetalGearSNMPAgent_binaries):
The folder Agent_build has the agent executable (built for windows).
The folder Manager_build has the manager executable (built for windows).

The port 161000 is used for the SNMP requisitions and the port 16009 is used to send/receive the traps.
