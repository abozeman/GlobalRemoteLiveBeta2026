using Fusion;
using Fusion.Sockets;
using Meta.XR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace Assets.CryptoKartz.Scripts
{

    public class ERLSceneManger : NetworkBehaviour, INetworkRunnerCallbacks
    {

        public List<GameObject> sceneObjects;
        public bool isTestScene = false;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public override void Spawned()
        {
            if (!Runner.IsServer || isTestScene)
            {
                var siblingIndex = 1;
                foreach (var gameObj in sceneObjects)
                {
                    GameObject go = Instantiate(gameObj);
                    go.transform.SetSiblingIndex(siblingIndex);
                    go.name = gameObj.name.Replace("(clone)", "");
                    siblingIndex++;
                }

                OVRManager.TrackingAcquired += OnTrackingAcquired;

            }
           

        }

        void OnTrackingAcquired()
        {
            Debug.Log("OVR Camera Rig is fully initialized and tracking!");
            // Unsubscribe to prevent memory leaks
            OVRManager.TrackingAcquired -= OnTrackingAcquired;

            // Proceed with your scene logic here
            WaitForRig(OVRManager.instance.GetComponent<OVRCameraRig>().gameObject);

            StartCoroutine(SetTrackingOrigin());
            StartCoroutine(Initialize());

        }

        IEnumerator WaitForRig(GameObject rig)
        {
            OVRCameraRig cameraRig = rig.GetComponent<OVRCameraRig>();

            // Wait until the rig has initialized and the tracking system is 'live'
            while (!OVRManager.isHmdPresent)
            {
                yield return null;
            }

            Debug.Log("Hardware is ready.");
        }

        public IEnumerator Initialize()
        {
            var _erlPlatformPlacementTool = GameObject.Find("TrackPlatformPlacementTool");
            var _rayCastVisualizerNormal = GameObject.Find("_raycastVisualizationNormal");
            var _trackPlatformPlacementTarget = _erlPlatformPlacementTool.transform.Find("TrackPlatformPlacementTarget");
            var _selected = GameObject.Find("Selected");
            var _selectedScale = _selected.transform.localScale;
            _selected.transform.SetParent(_trackPlatformPlacementTarget);
            _selected.transform.localScale = _selectedScale;
            _selected.transform.localPosition = Vector3.zero;
            _rayCastVisualizerNormal.transform.SetParent(_erlPlatformPlacementTool.transform, true);
            GetComponent<EnvironmentRaycastManager>().enabled = true;
            var _erlPlatformPlacement = _erlPlatformPlacementTool.GetComponent<ERLPlatformPlacement>();
            _erlPlatformPlacement.enabled = true;
            yield return new WaitUntil(() => _erlPlatformPlacement._isReady);
        }

        private IEnumerator SetTrackingOrigin()
        {
            // Give the headset and subsystem time to fully initialize
            yield return new WaitForSeconds(1.0f);

            var inputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputSubsystems);

            foreach (var subsystem in inputSubsystems)
            {
                if (subsystem.running)
                {
                    bool success = subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                    if (success)
                    {
                        Debug.Log("Successfully set tracking origin to Floor.");
                    }
                    else
                    {
                        Debug.LogWarning("Failed to set tracking origin to Floor. Attempting Device level.");
                        subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
                    }
                }
            }
        }

        #region Uunsed INetworkRunnerCallbacks Implementation
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {

        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {

        }


        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {

        }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {

        }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

        }

        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {

        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {

        }

        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {

        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {

        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {

        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            //runner.LocalPlayer
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {

        }

        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {

        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {

        }



        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {

        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {

        }

        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {

        }

       

        #endregion
    }

}
