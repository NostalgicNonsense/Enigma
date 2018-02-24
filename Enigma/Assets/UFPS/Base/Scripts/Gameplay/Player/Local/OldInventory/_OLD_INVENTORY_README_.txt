This unitypackage contains the script files of the deprecated vp_SimpleInventory.
vp_SimpleInventory was deprecated in UFPS 1.4.7 but some of its files were still relied upon for powerups until UFPS 1.6.0.
The files are no longer part of the codebase or supported, and included only as a porting reference.

Make sure that your project does not depend on any of the following scripts:
- vp_SimpleInventory: This script was replaced by vp_Inventory in UFPS 1.4.7.
- vp_AmmoPickup: This script was replaced by vp_ItemPickup in UFPS 1.4.7.
- vp_WeaponPickup: This script was replaced by vp_ItemPickup in UFPS 1.4.7.
- vp_Pickup: This script was replaced by vp_Powerup in UFPS 1.7.0 (vp_Powerup only deals with powerups and not inventory items)
- vp_HealthPickup: This script is now replaced by vp_HealthPowerup which can be used in multiplayer too.
- vp_SpeedPowerup: This script is now replaced by vp_StatePowerup which supports any state (and not just the demo's 'MegaSpeed').
- vp_SlomoPickup: This script is now replaced by vp_SlomoPowerup.
