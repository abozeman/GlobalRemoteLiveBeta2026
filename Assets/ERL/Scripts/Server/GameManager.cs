using Assets.CryptoKartz.Scripts.Managers;
using cryptokartz.Scripts.Car;
using cryptokartz.Scripts.Player;
using Fusion;
using Fusion.Sockets;
using M2MqttUnity;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace cryptokartz.Scripts.GameControllers
{

    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class GameManager : M2MqttUnityClientNetwork, INetworkRunnerCallbacks
    {

        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private NetworkObject _liveCarPrefab;
        [SerializeField] private NetworkObject _ghostCarPrefab;
        [SerializeField] private List<NetworkObject> _carPrefabs = new List<NetworkObject>();
        private readonly Dictionary<PlayerRef, NetworkObject> _playerMap = new Dictionary<PlayerRef, NetworkObject>();
        private Dictionary<PlayerRef, PlayerDataNetwork> _playerDataMap = new Dictionary<PlayerRef, PlayerDataNetwork>();
        private List<string> eventMessages = new List<string>();


        private int _playerId;
        private int _playerCount;
        private PlayerRef _player;
        private int TrackLevelId { get; set; }

        //public SessionProperty TrackId { get; private set; }


        #region Publish SessionInfo

        public IEnumerator SessionInfoPublish(SessionInfo sessionInfo)
        {
            var jsonSessionInfo = JsonConvert.SerializeObject(sessionInfo);
            client.Publish(string.Format($"ckgame.sessioninfo.{sessionInfo.Name}"), System.Text.Encoding.UTF8.GetBytes(jsonSessionInfo));
            yield return new WaitForSecondsRealtime(.033f);
        }

        public IEnumerator SessionInfoRemove(SessionInfo sessionInfo)
        {
            var jsonSessionInfo = JsonConvert.SerializeObject(sessionInfo);
            client.Publish(string.Format($"ckgame.sessioninfo.remove.{sessionInfo.Name}"), System.Text.Encoding.UTF8.GetBytes(jsonSessionInfo));
            yield return new WaitForSecondsRealtime(.033f);
        }

        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (runner.IsServer)
            {
                Log.Info($"sessionList: {sessionList}");
            }


        }


        #endregion

        #region MQTT Client

        #region Broker Settings
        /// <summary>
        /// Set ClientId.
        /// </summary>
        /// <param name="clientId">The clientId.</param>
        public void SetClientId(string clientId)
        {
            this.clientId = clientId;
        }

        /// <summary>
        /// Set the encrypted.
        /// </summary>
        /// <param name="isEncrypted">If true, is encrypted.</param>
        public void SetEncrypted(bool isEncrypted)
        {
            this.isEncrypted = isEncrypted;
        }
        #endregion

        #region Connection Methods
        protected override void OnConnecting()
        {
            base.OnConnecting();
            Debug.Log("Connecting to broker on " + brokerAddress + ":" + brokerPort.ToString() + "...\n");
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            Debug.Log("Connected to broker on " + brokerAddress + "\n");
            SubscribeTopics();
        }

        protected override void OnConnectionFailed(string errorMessage)
        {
            Debug.Log("CONNECTION FAILED! " + errorMessage);
        }

        protected override void OnDisconnected()
        {
            Debug.Log(" Disconnected.");
        }

        protected override void OnConnectionLost()
        {
            Debug.Log("GameManager CONNECTION LOST!");
        }
        #endregion

        #region Subscription/Unsubscription
        protected override void SubscribeTopics()
        {
            client.Subscribe(new string[] { string.Format("game/manager/*", clientId) }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        protected override void UnsubscribeTopics()
        {
            client.Unsubscribe(new string[] { string.Format("game/manager/{0}", clientId) });
        }

        #endregion

        #endregion

        //public virtual void Spawned()
        //{
        //    Runner.AddCallbacks(this);
        //    if (Runner.IsServer)
        //    {
        //        frontMessageQueue = messageQueue1;
        //        backMessageQueue = messageQueue2;

        //        if (autoConnect)
        //        {
        //            Connect();
        //        }

        //        Debug.Log("GameManager Spawned & Started Connecting to broker...");
        //    }

        //}
        protected override void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                string msg = System.Text.Encoding.UTF8.GetString(message);
                //string msg = "{"type": "1", "vid": "grlv0telemetry", "posX": "0.85", "posZ": "-0.018", "velX": "-0.0", "velZ": "-0.003", "rotW": "0.987", "rotX": "-0.117", "rotY": "0.014", "rotZ": "0.105", "strAngle": "0.0", "strThrottle": "0.0"}"
                //Debug.Log("msg: " + msg);
                if (topic.Contains("game/manager/livecar"))
                {
                    Debug.Log("livecar msg: " + msg);

                    //agentConfig = new AgentConfig(msg);
                    var player = PlayerRef.None;
                    CreateCarConfig carConfig = new CreateCarConfig(msg);
                    int rtLevel = int.Parse(carConfig.RacePlatformLevel);
                    TrackLevelId = rtLevel;
                    NetworkObject car = grlLiveCarSpawn(_liveCarPrefab);

                    car.gameObject.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);


                    Debug.Log($"Is Network Object LiveCar != null: {car != null}");
                    Debug.Log($"Is Network Object's GameObject LiveCar != null: {car.gameObject != null}");
                    Debug.Log($"Is Network Object's GameObject Transform LiveCar != null: {car.gameObject.transform != null}");

                    TrackDefinitionManager tdm = GetTrackDefinitionManager(rtLevel);

                    Debug.Log($"Is TrackDefinitionManager at Level {rtLevel} != null: {tdm != null}");
                    Debug.Log($"Is TrackDefinitionManager GameObject at Level {rtLevel} != null: {tdm.gameObject != null}");
                    Debug.Log($"Is TrackDefinitionManager GameObject Transform at Level {rtLevel} != null: {tdm.gameObject.transform != null}");

                    //car.GetComponent<CarPositionLiveSubscriber>().vid = 


                    try
                    {
                        car.gameObject.transform.SetParent(tdm.gameObject.transform, true);
                        Debug.Log($"LiveCar --> car.gameObject.transform.SetParent(tdm.gameObject.transform, false); (Succeeded)");

                    }
                    catch (Exception e)
                    {
                        Debug.Log($"LiveCar --> car.gameObject.transform.SetParent(tdm.gameObject.transform, false); (Failed) with Exception {e.StackTrace}");
                    }

                    //Debug.Log($"LiveCarSpawned at Level: {rtLevel}");


                }
                else if (topic.Contains("game/manager/ghostcar"))
                {
                    Debug.Log("ghostcar msg: " + msg);


                    var player = PlayerRef.None;
                    CreateCarConfig carConfig = new CreateCarConfig(msg);
                    int rtLevel = int.Parse(carConfig.RacePlatformLevel);
                    TrackLevelId = rtLevel;
                    NetworkObject car = grlGhostCarSpawn(_ghostCarPrefab);

                    //car.gameObject.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

                    Debug.Log($"Is Network Object GhostCar != null: {car != null}");
                    Debug.Log($"Is Network Object's GameObject GhostCar != null: {car.gameObject != null}");
                    Debug.Log($"Is Network Object's GameObject Transform GhostCar != null: {car.gameObject.transform != null}");

                    TrackDefinitionManager tdm = GetTrackDefinitionManager(TrackLevelId);

                    Debug.Log($"Is TrackDefinitionManager at Level {rtLevel} != null: {tdm != null}");
                    Debug.Log($"Is TrackDefinitionManager GameObject at Level {rtLevel} != null: {tdm.gameObject != null}");
                    Debug.Log($"Is TrackDefinitionManager GameObject Transform at Level {rtLevel} != null: {tdm.gameObject.transform != null}");

                    //car.GetComponent<CarPositionLiveSubscriber>().vid = $"echoghostracer{rtLevel}";


                    try
                    {
                        car.gameObject.transform.SetParent(tdm.gameObject.transform, true);
                        Debug.Log($"GhostCar --> car.gameObject.transform.SetParent(tdm.gameObject.transform, false); (Succeeded)");

                    }
                    catch (Exception e)
                    {
                        Debug.Log($"GhostCar --> car.gameObject.transform.SetParent(tdm.gameObject.transform, false); (Failed) with Exception {e.StackTrace}");
                    }



                }
                
                else if (topic.Contains("game/manager/updatetrack"))
                {
                    Debug.Log($"RaceTrackUpdated with msg: {msg}");

                    UpdateRaceTrackConfig raceTrackConfig = new UpdateRaceTrackConfig(msg);
                    int rtLevel = int.Parse(raceTrackConfig.RacePlatformLevel);
                    string rtId = raceTrackConfig.trackId;

                    GetTrackDefinitionManager(rtLevel).TrackId = rtId;
                }

                StoreMessage(msg);
            }
            catch (Exception e)
            {
                Debug.Log("GameManager Spawn EXCEPTION: " + e.Message);
            }

        }

        private TrackDefinitionManager GetTrackDefinitionManager(int level)
        {
            var tpc = GameObject.Find("TrackPlatformContainer");
            TrackDefinitionManager[] trackDefinitionManagers = tpc.GetComponentsInChildren<TrackDefinitionManager>();
            foreach (TrackDefinitionManager trackDefManager in trackDefinitionManagers)
            {
                if (trackDefManager.LevelId == level)
                {
                    return trackDefManager;
                }
            }
            return null;
        }

        private Transform GetRaceTrackShell(GameObject car)
        {
            var rts = car.transform.Find("RaceTrackShell2");
            return rts.transform;
        }

        public void LocateObject(NetworkId id)
        {
            if (Runner.TryFindObject(id, out NetworkObject foundObj))
            {
                Debug.Log($"Found object: {foundObj.name}");
            }
            else
            {
                Debug.LogWarning("Object with that ID does not exist on this client yet.");
            }
        }

        private float getVelocity(float velx, float velz)
        {
            var velocity = Math.Sqrt(Math.Pow(velx, 2) + Math.Pow(velz, 2));
            return (float)velocity;
        }

        private void StoreMessage(string eventMsg)
        {
            eventMessages.Add(eventMsg);
        }

        private void ProcessMessage(string msg)
        {
            //Debug.Log("Received: " + msg);
        }

        /// <summary>
        /// Fixed update network.
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            base.Update(); // call ProcessMqttEvents()

            /*if (CurrentGamePhase is GamePhase.InGame)
            {*/
            if (eventMessages.Count > 0)
            {
                foreach (string msg in eventMessages)
                {
                    ProcessMessage(msg);
                }
                eventMessages.Clear();
            }

            //}




        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {

            Debug.Log($"Entered OnPlayerJoined");


            if (runner.IsServer && _playerPrefab != null)
            {

                _playerId = player.PlayerId;
                _playerCount = Runner.SessionInfo.PlayerCount - 1;
                _player = player;
                //TrackId = Runner.SessionInfo.Properties["trackid"];

                if (_playerCount == 0) return;

                NetworkObject character;

                Debug.Log($"_playerId: {_playerId}");
                Debug.Log($"PlayerCount: {_playerCount}");
                //Debug.Log($"TrackId: {TrackId.PropertyValue.ToString()}");

                character = grlAvatarSpawn(_playerPrefab, player);

                _playerMap[player] = character;
                runner.SetPlayerObject(player, character);

                Log.Info($"Spawn for Player: {player}");


            }

        }

        private NetworkObject grlAvatarSpawn(NetworkObject _objPrefab, PlayerRef player)
        {
            var spawnPosition = GetRacePlatformLevelVector(4);

            return Runner.Spawn(
                _objPrefab,
                spawnPosition,
                Quaternion.identity,
                inputAuthority: player,
                InitializeAvatarBeforeSpawn
                );
        }

        private NetworkObject grlGhostCarSpawn(NetworkObject _objPrefab)
        {
            return Runner.Spawn(
                _objPrefab,
                Vector3.zero,
                Quaternion.identity,
                inputAuthority: PlayerRef.None,
                InitializeCarBeforeSpawn
                );
        }

        private NetworkObject grlLiveCarSpawn(NetworkObject _objPrefab)
        {

            return Runner.Spawn(
                _objPrefab,
                Vector3.zero,
                Quaternion.identity,
                inputAuthority: PlayerRef.None,
                InitializeCarBeforeSpawn
                );
        }

        private void InitializeCarBeforeSpawn(NetworkRunner runner, NetworkObject obj)
        {
            var objCarDataNetwork = obj.GetComponent<CarDataNetwork>();
            var datacopy = objCarDataNetwork;

            datacopy.LevelId = TrackLevelId;
            datacopy.ColorId = 1;
            objCarDataNetwork = datacopy;
        }


        private void InitializeRaceLevelsBeforeSpawn(NetworkRunner runner, NetworkObject obj)
        {

        }

        private void InitializeRaceTracksBeforeSpawn(NetworkRunner runner, NetworkObject obj)
        {
            //var objTrackGenerator = obj.GetComponentInChildren<TrackGenerator>();
            //var copy = objTrackGenerator;

            //copy.TrackId = TrackId;
            //copy.LevelId = TrackLevelId;
            //objTrackGenerator = copy;

            //Debug.Log($"TrackId set to: {objTrackGenerator.TrackId}");
            //Debug.Log($"LevelId set to: {objTrackGenerator.LevelId}");
        }

        private void InitializeAvatarBeforeSpawn(NetworkRunner runner, NetworkObject obj)
        {
            //var objPlayerData = obj.GetComponentInChildren<PlayerDataNetwork>();
            //var copy = objPlayerData;

            //copy.PlayerId = _player.PlayerId;
            //copy.PlayerTag = $"Player{_playerCount}";

            //copy.AvatarIndex = UnityEngine.Random.Range(1, 31);



            //if (_playerCount == 1)
            //{
            //    copy.PlayerTag = "Handler";
            //}
            //else
            //{
            //    copy.PlayerTag = $"Player{_playerCount}";
            //}

            //objPlayerData = copy;

            //_playerDataMap[_player] = objPlayerData;

            //Debug.Log($"PlayerId for Player: {objPlayerData.PlayerId}");
        }

        private Vector3 GetRacePlatformLevelVector(int level)
        {
            Vector3 levelVector = new Vector3(0, 1.07f, 0);
            switch (level)
            {
                case 1:
                    levelVector = new Vector3(0, 0.016f, 0); ;
                    break;
                case 2:
                    levelVector = new Vector3(0, 0.366f, 0); ;
                    break;
                case 3:
                    levelVector = new Vector3(0, 0.716f, 0); ;
                    break;
                case 4:
                    levelVector = new Vector3(0, 1.07f, 0); ;
                    break;
                default:
                    levelVector = new Vector3(0, 1.07f, 0); ;
                    break;
            }
            return levelVector;
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.TryGetPlayerObject(player, out NetworkObject character))
            {
                // Despawn Player
                runner.Despawn(character);

                // Remove player from mapping
                _playerMap.Remove(player);

                Log.Info($"Despawn for Player: {player}");
            }

            if (_playerMap.Count == 0)
            {
                Log.Info("Last player left, shutdown...");
                // Shutdown Server after the last player leaves
                //runnerServer.Shutdown();
            }
        }

        #region Unused Callbacks


        /// <summary>
        /// On user simulation message.
        /// </summary>
        /// <param name="runner">The runnerServer.</param>
        /// <param name="message">The message.</param>
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }



        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {

        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {

        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Application.Quit(0);

        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {

        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {

        }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

        }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {

        }

        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {

        }

        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {

        }

        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {

        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {

        }

        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {

        }

        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {

        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {

        }
        #endregion
    }
}