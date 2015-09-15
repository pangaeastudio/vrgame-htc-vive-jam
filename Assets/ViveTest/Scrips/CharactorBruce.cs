using UnityEngine;
using System.Collections;
using RootMotion.Demos;

public class CharactorBruce : MonoBehaviour 
{
	public GameObject[] weapons;

	private int lWeaponIndex = -1;
	private int rWeaponIndex = -1;

	public GameObject GetLNextWeapon()
	{
		lWeaponIndex += 1;
		if(lWeaponIndex >= weapons.Length) lWeaponIndex = 0;
		return weapons[lWeaponIndex];
	}

	public GameObject GetRNextWeapon()
	{
		rWeaponIndex += 1;
		if(rWeaponIndex >= weapons.Length) rWeaponIndex = 0;
		return weapons[rWeaponIndex];
	}

	void Update()
	{
		//gameObject.transform.position += gameObject.transform.forward * -0.01f;
	}
}