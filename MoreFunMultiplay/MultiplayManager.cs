using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreFunMultiplay;

public class MultiplayManager : MonoBehaviour
{
	public class PlayerMotionData
	{
		public string PlayerName;

		public Transform Head;

		public Transform LeftHand;

		public Transform RightHand;

		public Vector3 LastHeadPos;

		public Vector3 LastLeftPos;

		public Vector3 LastRightPos;

		public bool HasInitialPos;

		public float TotalDistance;

		public float HeadDistance;

		public float LeftHandDistance;

		public float RightHandDistance;
	}

	private readonly Dictionary<string, PlayerMotionData> _currentSongPlayers = new Dictionary<string, PlayerMotionData>();

	private readonly Dictionary<string, PlayerMotionData> _lastCompletedPlayers = new Dictionary<string, PlayerMotionData>();

	private float _updateInterval = 0.1f;

	private float _timer;

	private float _roomCheckTimer;

	private string _lastAnnouncedRoomCode = "";

	public static MultiplayManager Instance { get; private set; }

	public bool IsInSong { get; private set; }

	public float LocalPlayerAccuracyPercent { get; set; } = -1f;


	public float LastCompletedLocalAccuracyPercent { get; private set; } = -1f;


	public int LocalPlayerMissCount { get; set; }

	public int LastCompletedLocalMissCount { get; private set; }

	public int LocalPlayerHitCount { get; set; }

	public int LastCompletedLocalHitCount { get; private set; }

	public bool LocalPlayerFailed { get; set; }

	public bool LastCompletedLocalFailed { get; private set; }

	public string LocalPlayerName { get; set; } = "You";


	private void Awake()
	{
		Instance = this;
		Plugin.Log.Info("MultiplayManager Awake");
	}

	public void StartSong()
	{
		if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
		{
			IsInSong = false;
			return;
		}
		_currentSongPlayers.Clear();
		LocalPlayerAccuracyPercent = -1f;
		LocalPlayerMissCount = 0;
		LocalPlayerHitCount = 0;
		LocalPlayerFailed = false;
		IsInSong = true;
		_timer = 0f;
		ResultsUIHost.Instance.MarkSongStarted();
		if (MultiplayerPlusBridge.IsLocalPlayerRoomHost())
		{
			ResultsUIHost.Instance.UpdateTabContents();
			ResultsUIHost.Instance.PublishCurrentStateToCloud(isSongEndUpdate: false);
		}
		else
		{
			RoomSyncManager.TriggerParticipantFetchWithRetries(MultiplayerPlusBridge.GetCurrentRoomCode());
		}
		Plugin.Log.Info("MultiplayManager: Song tracking started (Multiplayer+ Room).");
	}

	public void CompleteSong()
	{
		IsInSong = false;
		_lastCompletedPlayers.Clear();
		foreach (KeyValuePair<string, PlayerMotionData> currentSongPlayer in _currentSongPlayers)
		{
			_lastCompletedPlayers[currentSongPlayer.Key] = currentSongPlayer.Value;
		}
		LastCompletedLocalAccuracyPercent = LocalPlayerAccuracyPercent;
		LastCompletedLocalMissCount = LocalPlayerMissCount;
		LastCompletedLocalHitCount = LocalPlayerHitCount;
		LastCompletedLocalFailed = LocalPlayerFailed;
		Plugin.Log.Info($"MultiplayManager: Song completed to end. Confirmed players: {_lastCompletedPlayers.Count}");
	}

	public void AbortSong()
	{
		IsInSong = false;
		_currentSongPlayers.Clear();
		LocalPlayerAccuracyPercent = -1f;
		LocalPlayerMissCount = 0;
		LocalPlayerHitCount = 0;
		LocalPlayerFailed = false;
		Plugin.Log.Info("MultiplayManager: Song ended midway (aborted/failed). Discarded current song data without awarding points.");
	}

	public void StopSong()
	{
		IsInSong = false;
	}

	private void Update()
	{
		RoomSyncManager.ProcessMainThreadQueue();
		_roomCheckTimer += Time.deltaTime;
		if (_roomCheckTimer >= 0.5f)
		{
			_roomCheckTimer = 0f;
			if (!MultiplayerPlusBridge.IsInMultiplayerPlusRoom())
			{
				_lastAnnouncedRoomCode = "";
				RoomSyncManager.ResetAppliedState();
				if (IsInSong)
				{
					IsInSong = false;
					_currentSongPlayers.Clear();
				}
				if (ResultsUIHost.Instance.IsFloatingScreenVisible)
				{
					ResultsUIHost.Instance.HideFloatingScreen();
				}
			}
			else if (!IsInSong)
			{
				string currentRoomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
				if (!string.IsNullOrEmpty(currentRoomCode) && _lastAnnouncedRoomCode != currentRoomCode)
				{
					_lastAnnouncedRoomCode = currentRoomCode;
					ResultsUIHost.Instance.UpdateHeaderText();
					if (!MultiplayerPlusBridge.IsLocalPlayerRoomHost())
					{
						RoomSyncManager.PollLatestRoomSnapshotAsync(currentRoomCode);
					}
				}
			}
		}
		if (IsInSong)
		{
			_timer += Time.deltaTime;
			if (_timer >= _updateInterval)
			{
				_timer = 0f;
				UpdateTracking();
			}
		}
	}

	private void UpdateTracking()
	{
		foreach (string item in _currentSongPlayers.Keys.ToList())
		{
			PlayerMotionData playerMotionData = _currentSongPlayers[item];
			if (playerMotionData.Head == null || playerMotionData.LeftHand == null || playerMotionData.RightHand == null)
			{
				continue;
			}
			Vector3 position = playerMotionData.Head.position;
			Vector3 position2 = playerMotionData.LeftHand.position;
			Vector3 position3 = playerMotionData.RightHand.position;
			if (!playerMotionData.HasInitialPos)
			{
				playerMotionData.LastHeadPos = position;
				playerMotionData.LastLeftPos = position2;
				playerMotionData.LastRightPos = position3;
				playerMotionData.HasInitialPos = true;
				continue;
			}
			float num = Vector3.Distance(position, playerMotionData.LastHeadPos);
			float num2 = Vector3.Distance(position2, playerMotionData.LastLeftPos);
			float num3 = Vector3.Distance(position3, playerMotionData.LastRightPos);
			if (num < 5f && num2 < 5f && num3 < 5f)
			{
				playerMotionData.TotalDistance += num + num2 + num3;
				playerMotionData.HeadDistance += num;
				playerMotionData.LeftHandDistance += num2;
				playerMotionData.RightHandDistance += num3;
			}
			playerMotionData.LastHeadPos = position;
			playerMotionData.LastLeftPos = position2;
			playerMotionData.LastRightPos = position3;
		}
	}

	public void RegisterPlayer(string name, Transform head, Transform left, Transform right)
	{
		if (IsInSong && !string.IsNullOrEmpty(name) && !(head == null) && !(left == null) && !(right == null))
		{
			if (!_currentSongPlayers.TryGetValue(name, out var value))
			{
				_currentSongPlayers[name] = new PlayerMotionData
				{
					PlayerName = name,
					Head = head,
					LeftHand = left,
					RightHand = right,
					LastHeadPos = head.position,
					LastLeftPos = left.position,
					LastRightPos = right.position,
					HasInitialPos = true,
					TotalDistance = 0f,
					HeadDistance = 0f,
					LeftHandDistance = 0f,
					RightHandDistance = 0f
				};
				Plugin.Log.Info("Started tracking player: " + name);
			}
			else
			{
				value.Head = head;
				value.LeftHand = left;
				value.RightHand = right;
			}
		}
	}

	public Dictionary<string, PlayerMotionData> GetTrackedPlayers()
	{
		if (!IsInSong)
		{
			return _lastCompletedPlayers;
		}
		return _currentSongPlayers;
	}

	public void ClearData()
	{
		_currentSongPlayers.Clear();
		_lastCompletedPlayers.Clear();
	}
}
