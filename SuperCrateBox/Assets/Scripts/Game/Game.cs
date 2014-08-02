using UnityEngine;
using System.Collections;

public class Game : Singleton<Game> 
{
	public float timescale = 1f;
	public bool multiplayer = false;
	
	public GameCamera camera_ = new GameCamera();
	public static GameCamera Camera() { return Instance.camera_; }

	public GameAudio audio_ = new GameAudio();
	public static GameAudio Audio() { return Instance.audio_; }

	public GameHUD hud = new GameHUD();
	public static GameHUD HUD() { return Instance.hud; }

	public GameProgress progress;
	public static GameProgress Progress() { return Instance.progress; }
	
	public GameStatistics statistic = new GameStatistics();
	public static GameStatistics Statistic() { return Instance.statistic; }
	
	public GameLevel level;
	public static GameLevel Level() { return Instance.level; }

	public GameMode mode;
	public static GameMode Mode() { return Instance.mode; }
	
	public GameCharacter character = new GameCharacter();
	public static GameCharacter Character() { return Instance.character; } 

	public GameWeapon weapon = new GameWeapon();
	public static GameWeapon Weapon() { return Instance.weapon; }

	public GameResult result;
	public static GameResult Result() { return Instance.result; }

	public GameCheat cheat = new GameCheat();
	public static GameCheat Cheat() { return Instance.cheat; }

	private Game() {
	}

	void Start () 
	{
		Time.timeScale = timescale;
		DontDestroyOnLoad(transform.gameObject);
		
		if (networkView == null)
			gameObject.AddComponent<NetworkView>();
		
		networkView.stateSynchronization = NetworkStateSynchronization.Off;
		networkView.observed = null;

		camera_.Start();

		statistic.Start();

		character.game = this;
		character.Start();

		if (progress == null) 
		{
			progress = gameObject.GetComponent<GameProgress>();
			if (progress == null) progress = gameObject.AddComponent<GameProgress>();
		}
		
		if (result == null) 
		{
			result = gameObject.GetComponent<GameResult>();
			if (result == null) result = gameObject.AddComponent<GameResult>();
			result.game = this;
		}

		hud.Start ();

		MasterServerManager.postBeforeDisconnected += ListenBeforeDisconnected;
	}

	public void Purge()
	{
		character.Purge ();
		hud.Purge ();
		weapon.Purge ();
		level = null;
		Destroy(mode);
	}

	void Update () 
	{
		character.Update();
		hud.Update ();
	}

	void FixedUpdate() 
	{
		character.FixedUpdate();
	}

	public static void InitLevel(LevelDef _def) 
	{
		Instance.level = new GameLevel(_def);
	}

	void OnPlayerDisconnected(NetworkPlayer player) {
		Debug.Log("Clean up after player " + player);
		Network.RemoveRPCs(player);
		Network.DestroyPlayerObjects(player);
	}

	void ListenBeforeDisconnected()
	{
		Debug.Log ("Server disconnected.");
		Network.RemoveRPCs(Network.player);
		Network.DestroyPlayerObjects(Network.player);
	}
}
