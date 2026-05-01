using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Assets.CryptoKartz.Scripts
{
    public class ERLTrackVisualsManager : NetworkBehaviour, INetworkRunnerCallbacks, ITrackDefinitionManager
    {

        public GameObject TrackExterior;
        public GameObject TrackInterior;
        public GameObject TrackStartline;
        public GameObject TrackSurface;

        private GameObject m_TrackSurface;
        private GameObject m_TrackExterior;
        private GameObject m_TrackInterior;
        private GameObject m_TrackStartline;

        private bool trackIsRendered { get; set; } = false;
        private LineRenderer m_exteriorLineRenderer;
        private LineRenderer m_interiorLineRenderer;
        private LineRenderer m_startLineRenderer;
        private TrackDefinition m_trackDefinition;

        private int ExteriorCoordinatesLength;
        private List<Vector2> ExteriorCoordinates = new();
        private int InteriorCoordinatesLength;
        private List<Vector2> InteriorCoordinates = new();
        private int StartlineCoordinatesLength;
        private List<Vector2> StartlineCoordinates = new();


        public float renderDelay { get; private set; } = .1f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public override void Spawned()
        {


            if (!Runner.IsServer)
            {

                var test = transform.GetComponent<TrackDefinitionManager>();
                transform.GetComponent<TrackDefinitionManager>().RegisterTrackDefinitionReadyListener(this);

                m_TrackSurface = Instantiate(TrackSurface, transform);
                m_TrackExterior = Instantiate(TrackExterior, transform);
                m_TrackInterior = Instantiate(TrackInterior, transform);
                m_TrackStartline = Instantiate(TrackStartline, transform);

                ExteriorCoordinatesLength = transform.GetComponent<TrackDefinitionManager>().ExteriorCoordinatesLength;
                transform.GetComponent<TrackDefinitionManager>().ExteriorCoordinates.CopyTo(ExteriorCoordinates);
                InteriorCoordinatesLength = transform.GetComponent<TrackDefinitionManager>().InteriorCoordinatesLength;
                transform.GetComponent<TrackDefinitionManager>().InteriorCoordinates.CopyTo(InteriorCoordinates);
                StartlineCoordinatesLength = transform.GetComponent<TrackDefinitionManager>().StartlineCoordinatesLength;
                transform.GetComponent<TrackDefinitionManager>().StartlineCoordinates.CopyTo(StartlineCoordinates);


                if (ExteriorCoordinatesLength > 0
                   && InteriorCoordinatesLength > 0
                   && StartlineCoordinatesLength > 0
                   && ExteriorCoordinates[0] != Vector2.zero
                   && InteriorCoordinates[0] != Vector2.zero
                   && StartlineCoordinates[0] != Vector2.zero)
                {

                    m_trackDefinition = GetTrackDefinition(ExteriorCoordinates, InteriorCoordinates, StartlineCoordinates, ExteriorCoordinatesLength, InteriorCoordinatesLength, StartlineCoordinatesLength);
                }

            }


        }


        void ITrackDefinitionManager.OnTrackDefinitionReady(int trackLevelId)
        {
            trackIsRendered = false;
        }

        public TrackDefinition GetTrackDefinition(List<Vector2> _exteriorCoordinates, List<Vector2> _interiorCoordinates, List<Vector2> _startlineCoordinates, int _exteriorCoordinatesLength, int _interiorCoordinatesLength, int _startlineCoordinatesLength)
        {
            TrackDefinition trackDefinition = new TrackDefinition();
            trackDefinition.geometry = new Geometry();
            trackDefinition.properties = new Properties();

            var elrpc = _exteriorCoordinatesLength;
            var ilrpc = _interiorCoordinatesLength;
            var slrpc = _startlineCoordinatesLength;

            trackDefinition.geometry.coordinates = new float[3][][] {
            new float[elrpc][],
            new float[ilrpc][],
            new float[slrpc][],
        };


            try
            {
                if (_exteriorCoordinatesLength > 0)
                {
                    for (var i = 0; i < _exteriorCoordinatesLength; i++)
                    {
                        trackDefinition.geometry.coordinates[0][i] = new float[2];
                        trackDefinition.geometry.coordinates[0][i][0] = _exteriorCoordinates[i].x;
                        trackDefinition.geometry.coordinates[0][i][1] = _exteriorCoordinates[i].y;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }

            if (_interiorCoordinatesLength > 0)
            {
                for (var i = 0; i < _interiorCoordinatesLength; i++)
                {
                    trackDefinition.geometry.coordinates[1][i] = new float[2];
                    trackDefinition.geometry.coordinates[1][i][0] = _interiorCoordinates[i].x;
                    trackDefinition.geometry.coordinates[1][i][1] = _interiorCoordinates[i].y;
                }
            }

            if (_startlineCoordinatesLength > 0)
            {
                for (var i = 0; i < _startlineCoordinatesLength; i++)
                {
                    trackDefinition.geometry.coordinates[2][i] = new float[2];
                    trackDefinition.geometry.coordinates[2][i][0] = _startlineCoordinates[i].x;
                    trackDefinition.geometry.coordinates[2][i][1] = _startlineCoordinates[i].y;
                }
            }

            return trackDefinition;


        }




        public override void Render()
        {


            if (!trackIsRendered)
            {
                StartCoroutine(RenderTrack(m_trackDefinition, transform.localScale));
            }
        }

        private IEnumerator RenderTrack(TrackDefinition trackDefinition, Vector3 scale)
        {
            ExteriorCoordinatesLength = transform.GetComponent<TrackDefinitionManager>().ExteriorCoordinatesLength;
            transform.GetComponent<TrackDefinitionManager>().ExteriorCoordinates.CopyTo(ExteriorCoordinates);
            InteriorCoordinatesLength = transform.GetComponent<TrackDefinitionManager>().InteriorCoordinatesLength;
            transform.GetComponent<TrackDefinitionManager>().InteriorCoordinates.CopyTo(InteriorCoordinates);
            StartlineCoordinatesLength = transform.GetComponent<TrackDefinitionManager>().StartlineCoordinatesLength;
            transform.GetComponent<TrackDefinitionManager>().StartlineCoordinates.CopyTo(StartlineCoordinates);

            if (ExteriorCoordinatesLength > 0
                   && InteriorCoordinatesLength > 0
                   && StartlineCoordinatesLength > 0
                   && ExteriorCoordinates[0] != Vector2.zero
                   && InteriorCoordinates[0] != Vector2.zero
                   && StartlineCoordinates[0] != Vector2.zero)
            {

                m_trackDefinition = GetTrackDefinition(ExteriorCoordinates, InteriorCoordinates, StartlineCoordinates, ExteriorCoordinatesLength, InteriorCoordinatesLength, StartlineCoordinatesLength);
            }

            while (m_TrackExterior == null || m_TrackExterior.GetComponent<LineRenderer>() == null)
            {
                yield return null; // Wait for the next frame
            }

            while (m_TrackInterior == null || m_TrackInterior.GetComponent<LineRenderer>() == null)
            {
                yield return null; // Wait for the next frame
            }

            while (m_TrackStartline == null || m_TrackStartline.GetComponent<LineRenderer>() == null)
            {
                yield return null; // Wait for the next frame
            }

            m_TrackExterior.GetComponent<LineRenderer>().positionCount = 0;


            scale = new Vector3(1f, 1f, 1f);
            m_exteriorLineRenderer = m_TrackExterior.GetComponent<LineRenderer>();
            m_exteriorLineRenderer.startWidth = .05f;
            m_exteriorLineRenderer.endWidth = .05f;

            m_exteriorLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            m_exteriorLineRenderer.positionCount = trackDefinition.geometry.coordinates[0].Length;

            for (var i = 0; i < trackDefinition.geometry.coordinates[0].Length; i++)
            {
                var point = trackDefinition.geometry.coordinates[0][i];
                if (point != null) m_exteriorLineRenderer.SetPosition(i, new Vector3(point[0] * scale.x, (point[1]) * scale.y, 0f));
                new WaitForSeconds(renderDelay);
            }

            m_interiorLineRenderer = m_TrackInterior.GetComponent<LineRenderer>();
            m_interiorLineRenderer.startWidth = .05f;
            m_interiorLineRenderer.endWidth = .05f;

            m_interiorLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            m_interiorLineRenderer.positionCount = trackDefinition.geometry.coordinates[1].Length;

            for (var i = 0; i < trackDefinition.geometry.coordinates[1].Length; i++)
            {
                var point = trackDefinition.geometry.coordinates[1][i];
                if (point != null)
                {
                    m_interiorLineRenderer.SetPosition(i, new Vector3(point[0] * scale.x, point[1] * scale.y, 0f));
                    new WaitForSeconds(renderDelay);
                }
            }

            if (trackDefinition.geometry.coordinates.Length < 3)
            {
                m_TrackStartline.SetActive(false);
                yield return null;
            }

            try
            {
                m_TrackStartline.SetActive(true);
                m_startLineRenderer = m_TrackStartline.GetComponent<LineRenderer>();
                m_startLineRenderer.startWidth = .05f;
                m_startLineRenderer.endWidth = .05f;

                m_startLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                m_startLineRenderer.positionCount = trackDefinition.geometry.coordinates[2].Length;

                for (var i = 0; i < trackDefinition.geometry.coordinates[2].Length; i++)
                {
                    var point = trackDefinition.geometry.coordinates[2][i];
                    if (point != null) m_startLineRenderer.SetPosition(i, new Vector3(point[0] * scale.x, point[1] * scale.y, 0f));
                    new WaitForSeconds(renderDelay);

                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }


            yield return trackIsRendered = m_exteriorLineRenderer.positionCount == ExteriorCoordinatesLength && m_interiorLineRenderer.positionCount == InteriorCoordinatesLength && m_startLineRenderer.positionCount == StartlineCoordinatesLength;

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
