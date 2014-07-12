﻿using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour {

	#region movement
	private bool m_Floating = false;
	public bool floating { get {return m_Floating; }}
	
	private float m_JumpCooltime = 0.0f;
	public float jumpForce = 10.0f;
	public float jumpCooldown = 1.0f;
	public float moveForce = 10.0f;

	public int direction {
		get { return transform.localRotation.y > 100 ? -1 : 1; }
	}
	#endregion
	
	#region pose
	private bool m_IsStanding = true;
	public bool isStanding { get { return m_IsStanding; }}
	public bool isCrouching { get { return ! m_IsStanding; }}
	#endregion

	#region life_state
	public int hpMax = 1;
	private HasHP m_Hp;

	public float hitCooldown = 0.5f;
	public Vector2 hitForce = new Vector2(10.0f, 5.0f);
	
	public bool hitEnabled {
		get { return damageDetector.enabled; }
		set { damageDetector.enabled = value; }
	}
	
	private bool m_Dead = false;
	public bool isDead {
		get { return m_Dead; }
		private set { m_Dead = value; }
	}
	
	public Vector2 deadForce;
	public float deadDelay = 0.5f;
	#endregion

	#region weapon
	private Weapon m_Weapon;

	public Weapon weapon {
		get { return m_Weapon; }
		set {
			if (m_Weapon != null) 
			{
				Destroy(m_Weapon.gameObject);
			}

			m_Weapon = value;

			if (m_Weapon != null) {
				m_Weapon.transform.parent = weaponPivot.transform;
				m_Weapon.transform.localPosition = Vector3.zero;
				m_Weapon.transform.localEulerAngles = Vector3.zero;
				m_Weapon.owner = gameObject;
			}

			var _aimTemp = aim;
			m_Aim = -1; // invalidate aim
			aim = _aimTemp;

			if (value == null) 
			{
				m_Animator.SetTrigger("unequip");
			}
			else 
			{
				m_Animator.SetTrigger("equip_" + weapon.type);
			}

			if (networkView.isMine && (Network.peerType != NetworkPeerType.Disconnected))
			{
				weapon.networkView.viewID = Network.AllocateViewID();
				weapon.networkView.enabled = true;
				networkView.RPC("SetWeaponRPC", RPCMode.OthersBuffered, weapon.networkView.viewID, weapon.type);
			}
		}
	}

	public WeaponPivot weaponPivot;

	[RPC]
	private void SetWeaponRPC(NetworkViewID _viewID, string _weapon)
	{
		var _weaponObj = GameObject.Instantiate(Resources.Load(_weapon)) as GameObject;
		_weaponObj.networkView.viewID = _viewID;
		weapon = _weaponObj.GetComponent<Weapon>();
	}

	private float m_Aim = 90;
	public float aim { 
		get { return m_Aim; }
		set { 
			if (m_Aim == value) return;

			m_Aim = value;
			
			var _weaponAngle = weaponPivot.transform.eulerAngles;
			_weaponAngle.z = m_Aim - 90;
			weaponPivot.transform.eulerAngles = _weaponAngle;

			m_Animator.SetFloat("aim", m_Aim);
		}
	}

	public float aimSpeed = 90f;

	#endregion

	#region components
	private Animator m_Animator;
	
	// detector
	public CrateDetector crateDetector;
	public DamageDetector damageDetector;
	public LayerDetector terrainDetector;
	#endregion

	#region events
	public delegate void PostDead(Character _character);
	public event PostDead postDead;
	#endregion

	void Awake () {
		m_Hp = GetComponent<HasHP>();
		m_Hp.hp = hpMax;
		m_Hp.postDead = Die;

		// components
		m_Animator = GetComponent<Animator>();
		
		// detector
		crateDetector.doObtain = Obtain;
		damageDetector.doDamage = Hit;
		
		terrainDetector.postDetect = (Collision) =>
		{
			m_Floating = false;
			terrainDetector.gameObject.SetActive(false);
		};
	}
	
	void DestroySelf()
	{
		if (networkView.enabled)
		{
			Network.Destroy(networkView.viewID);
		}
		else 
		{
			GameObject.Destroy(gameObject);
		}
	}

	void Update() 
	{
		m_JumpCooltime -= Time.deltaTime;

		var _rotation = transform.rotation;

		/*
		if (rigidbody2D.velocity.x > 0.3f) 
		{
			_rotation.y = 0;
		}
		else if (rigidbody2D.velocity.x < -0.3f)
		{
			_rotation.y = 180;
		}

		transform.rotation = _rotation;
		*/

		m_Animator.SetFloat("speed_x", Mathf.Abs(rigidbody2D.velocity.x));
		m_Animator.SetFloat("velocity_y", rigidbody2D.velocity.y);
	}

	public void Stand()
	{
		m_IsStanding = true;
		m_Animator.SetTrigger("stand_lower");
	}

	public void Crouch()
	{
		m_IsStanding = false;
		m_Animator.SetTrigger("crouch_lower");
	}

	public bool movable { 
		get { return ! isDead; }
	}
	
	public void Move(float _direction)
	{
		rigidbody2D.AddForce(_direction * moveForce * Vector3.right);
	}
	
	public bool jumpable {
		get { return ! m_Floating && m_JumpCooltime <= 0; }
	}
	
	public void Jump()
	{
		m_Floating = true;
		m_JumpCooltime = jumpCooldown;
		rigidbody2D.velocity += new Vector2(0, jumpForce);
		terrainDetector.gameObject.SetActive(true);
	}
	
	public void ChangeAim(float _direction)
	{
		aim += _direction * aimSpeed;
	}

	public bool shootable {
		get {
			return weapon != null && weapon.IsShootable(); 
		}
	}
	
	public void Shoot() {
		weapon.Shoot();
		m_Animator.SetTrigger("shoot");
	}
	
	void EnableHit() {
		hitEnabled = true;
	}
	
	void Hit(AttackData _attackData) {
		if (! hitEnabled) return;
		hitEnabled = false;
		
		Invoke("EnableHit", hitCooldown);
		
		var _direction = Mathf.Sign(_attackData.velocity.x);
		rigidbody2D.AddForce(new Vector2(_direction * hitForce.x, hitForce.y));
		
		m_Hp.damage(_attackData);
		
		if (m_Hp > 0) {
			m_Animator.SetTrigger("Hit");
		}
	}
	
	void Die() {
		CancelInvoke("EnableHit");
		
		isDead = true;
		hitEnabled = false;
		
		var _deadForce = deadForce;
		_deadForce.x *= -direction;
		
		rigidbody2D.velocity = new Vector2(0, 0);
		rigidbody2D.AddForce(_deadForce);
		
		m_Animator.SetTrigger("Dead");
		
		Game.Statistic().death.val += 1;
		if (postDead != null) postDead(this);
		
		Invoke("DestroySelf", deadDelay);
	}
	
	void Obtain(Crate _crate) 
	{
		if (networkView.enabled && ! networkView.isMine) 
			return;
		
		if (_crate.empty) return;
		
		var _weapon = GameObject.Instantiate(Resources.Load(_crate.weapon)) as GameObject;
		weapon = _weapon.GetComponent<Weapon>();
		
		if (networkView.enabled) {
			weapon.networkView.viewID = Network.AllocateViewID();
			weapon.networkView.enabled = true;
			networkView.RPC("ObtainRPC", RPCMode.Others, weapon.networkView.viewID, _crate.weapon);
		}
	}
	
	[RPC]
	void ObtainRPC(NetworkViewID _viewID, string _weapon) 
	{
		var _theWeapon = GameObject.Instantiate(Resources.Load(_weapon)) as GameObject;
		weapon = _theWeapon.GetComponent<Weapon>();
		weapon.networkView.viewID = _viewID;
		weapon.networkView.enabled = true;
	}

	public void Unequip()
	{
		weapon = null;
	}

	/*
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {
		if (stream.isWriting) {
			int healthC = currentHealth;
			stream.Serialize(ref healthC);
		} else {
			int healthZ = 0;
			stream.Serialize(ref healthZ);
			currentHealth = healthZ;
		}
	}
	*/
}
