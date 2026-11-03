using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoreFunMultiplay
{
    public class MultiplayManager : MonoBehaviour
    {
        public static MultiplayManager Instance { get; private set; }

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
        }

        private readonly Dictionary<string, PlayerMotionData> _currentSongPlayers = new Dictionary<string, PlayerMotionData>();
        private readonly Dictionary<string, PlayerMotionData> _lastCompletedPlayers = new Dictionary<string, PlayerMotionData>();

        public bool IsInSong { get; private set; } = false;
        public float LocalPlayerAccuracyPercent { get; set; } = -1f;
        public float LastCompletedLocalAccuracyPercent { get; private set; } = -1f;
        public string LocalPlayerName { get; set; } = "You";

        private float _updateInterval = 0.1f;
        private float _timer = 0f;
        private float _roomCheckTimer = 0f;
        private string _lastAnnouncedRoomCode = "";

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
            IsInSong = true;
            _timer = 0f;
            ResultsUIHost.Instance.MarkSongStarted();

            if (MultiplayerPlusBridge.IsLocalPlayerRoomHost())
            {
                ResultsUIHost.Instance.UpdateTabContents();
                ResultsUIHost.Instance.PublishCurrentStateToCloud(false);
            }
            else
            {
                string roomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
                RoomSyncManager.TriggerParticipantFetchWithRetries(roomCode);
            }

            Plugin.Log.Info("MultiplayManager: Song tracking started (Multiplayer+ Room).");
        }

        /// <summary>
        /// ホストが曲を最後までプレイし終えた時に呼ばれ、今回の曲データを確定する
        /// </summary>
        public void CompleteSong()
        {
            IsInSong = false;
            _lastCompletedPlayers.Clear();
            foreach (var kvp in _currentSongPlayers)
            {
                _lastCompletedPlayers[kvp.Key] = kvp.Value;
            }
            LastCompletedLocalAccuracyPercent = LocalPlayerAccuracyPercent;
            Plugin.Log.Info($"MultiplayManager: Song completed to end. Confirmed players: {_lastCompletedPlayers.Count}");
        }

        /// <summary>
        /// 曲が途中で終了（中断・Quit・途中失敗）した時に呼ばれ、今回の途中データを破棄する
        /// </summary>
        public void AbortSong()
        {
            IsInSong = false;
            _currentSongPlayers.Clear();
            LocalPlayerAccuracyPercent = -1f;
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
                bool inRoom = MultiplayerPlusBridge.IsInMultiplayerPlusRoom();
                if (!inRoom)
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
                    string roomCode = MultiplayerPlusBridge.GetCurrentRoomCode();
                    if (!string.IsNullOrEmpty(roomCode))
                    {
                        if (_lastAnnouncedRoomCode != roomCode)
                        {
                            _lastAnnouncedRoomCode = roomCode;
                            ResultsUIHost.Instance.UpdateHeaderText();
                            if (!MultiplayerPlusBridge.IsLocalPlayerRoomHost())
                            {
                                RoomSyncManager.PollLatestRoomSnapshotAsync(roomCode);
                            }
                        }
                    }
                }
            }

            if (!IsInSong) return;

            _timer += Time.deltaTime;
            if (_timer >= _updateInterval)
            {
                _timer = 0f;
                UpdateTracking();
            }
        }

        private void UpdateTracking()
        {
            var keys = _currentSongPlayers.Keys.ToList();
            foreach (var key in keys)
            {
                var pd = _currentSongPlayers[key];

                if (pd.Head == null || pd.LeftHand == null || pd.RightHand == null) continue;

                Vector3 headPos = pd.Head.position;
                Vector3 leftPos = pd.LeftHand.position;
                Vector3 rightPos = pd.RightHand.position;

                if (!pd.HasInitialPos)
                {
                    pd.LastHeadPos = headPos;
                    pd.LastLeftPos = leftPos;
                    pd.LastRightPos = rightPos;
                    pd.HasInitialPos = true;
                    continue;
                }

                float distHead = Vector3.Distance(headPos, pd.LastHeadPos);
                float distL = Vector3.Distance(leftPos, pd.LastLeftPos);
                float distR = Vector3.Distance(rightPos, pd.LastRightPos);

                // Ignore teleport spikes (e.g. avatar spawn jump > 5m in 0.1s)
                if (distHead < 5f && distL < 5f && distR < 5f)
                {
                    pd.TotalDistance += (distHead + distL + distR);
                    pd.HeadDistance += distHead;
                }

                pd.LastHeadPos = headPos;
                pd.LastLeftPos = leftPos;
                pd.LastRightPos = rightPos;
            }
        }

        public void RegisterPlayer(string name, Transform head, Transform left, Transform right)
        {
            if (!IsInSong || string.IsNullOrEmpty(name)) return;
            if (head == null || left == null || right == null) return;

            if (!_currentSongPlayers.TryGetValue(name, out var existing))
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
                    HeadDistance = 0f
                };
                Plugin.Log.Info("Started tracking player: " + name);
            }
            else
            {
                existing.Head = head;
                existing.LeftHand = left;
                existing.RightHand = right;
            }
        }

        public Dictionary<string, PlayerMotionData> GetTrackedPlayers()
        {
            return IsInSong ? _currentSongPlayers : _lastCompletedPlayers;
        }

        public void ClearData()
        {
            _currentSongPlayers.Clear();
            _lastCompletedPlayers.Clear();
        }
    }
}
