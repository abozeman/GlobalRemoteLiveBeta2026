// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.CryptoKartz.Scripts
{
    public class ERLPlatformPlacement : NetworkBehaviour
    {
        [SerializeField] private EnvironmentRaycastManager _raycastManager;
        private Transform _centerEyeAnchor;
        private Transform _raycastAnchor;
        private Transform _raycastVisualizationNormal;
        private GameObject _platformSelected;

        [SerializeField] private OVRInput.RawButton _grabButton = OVRInput.RawButton.RIndexTrigger | OVRInput.RawButton.RHandTrigger;
        [SerializeField] private OVRInput.RawAxis2D _scaleAxis = OVRInput.RawAxis2D.RThumbstick;
        [SerializeField] private OVRInput.RawAxis2D _moveAxis = OVRInput.RawAxis2D.RThumbstick;
        [SerializeField] private Transform _trackPlatform;
        [SerializeField] private Vector2 _trackPlatformAspectRatio = new Vector2(0.823f, 0.823f);
        [SerializeField] private LineRenderer _raycastVisualizationLine;

        private readonly RollingAverage _rollingAverageFilter = new RollingAverage();
        private Pose? _targetPose;
        private Vector3 _positionVelocity;
        private float _rotationVelocity;
        private bool _isGrabbing;
        private float _distanceFromController;
        private Pose? _environmentPose;
        private EnvironmentRaycastHitStatus _currentEnvHitStatus;
        private OVRSpatialAnchor _spatialAnchor;
        private bool _isRestoringAnchorTracking;

        public bool _isReady { get; private set; } = false;

        public override void Spawned()
        {
            
        }

        public void OnEnable()
        {
            StartCoroutine(Initialize());
        }


        private IEnumerator Initialize()
        {
            // 1. Wait for OVR Rig to exist
            while (GameObject.Find("OVRCameraRig") == null)
            {
                yield return null; // Wait for the next frame
            }

            // 2. Wait for MRUK to load the room
            while (MRUK.Instance == null || !MRUK.Instance.IsInitialized)
            {
                yield return null;
            }
            
            Debug.Log("Placement Tool: Systems detected. Starting Update.");

            // Adjust panel size based on aspect ratio
            var panelScale = _trackPlatform.localScale;
            panelScale.x = panelScale.x / _trackPlatformAspectRatio.x;
            panelScale.y = panelScale.y / _trackPlatformAspectRatio.y;
            _trackPlatform.localScale = panelScale;

            GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
            _centerEyeAnchor = ovrCameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
            Fusion.Assert.Always(_centerEyeAnchor, "Could not find CenterEyeAnchor in OVR Camera Rig");
            _raycastAnchor = ovrCameraRig.transform.Find("TrackingSpace/RightHandAnchor");
            Fusion.Assert.Always(_raycastAnchor, "Could not find RightHandAnchor in OVR Camera Rig");
            _raycastVisualizationNormal = GameObject.Find("_raycastVisualizationNormal").transform;
            _platformSelected = GameObject.Find("Selected");


            // Place the panel in front of the user
            var position = _centerEyeAnchor.position + _centerEyeAnchor.forward;
            var forward = Vector3.ProjectOnPlane(_centerEyeAnchor.position - position, Vector3.up).normalized;
            _trackPlatform.position = position;
            _trackPlatform.rotation = Quaternion.LookRotation(forward);

            // Create the OVRSpatialAnchor and make it a parent of the panel.
            // This will prevent the panel front drifting after headset lock/unlock.
            var parent = new GameObject(nameof(OVRSpatialAnchor));
            CreateSpatialAnchorAndSave(parent.transform);
            _isReady = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _isGrabbing = false;
                _targetPose = null;
            }
        }

        private async void CreateSpatialAnchorAndSave(Transform target)
        {
            Fusion.Assert.Always(target.GetComponent<OVRSpatialAnchor>());
            _spatialAnchor = target.gameObject.AddComponent<OVRSpatialAnchor>();
            target.SetPositionAndRotation(_trackPlatform.position, _trackPlatform.rotation);
            var scale = _trackPlatform.localScale;
            _trackPlatform.SetParent(target);
            _trackPlatform.localScale = scale;
            _trackPlatform.SetLocalPositionAndRotation(default, Quaternion.identity);


            // Wait for localization because SaveAnchorAsync() requires the anchor to be localized first.
            while (true)
            {
                if (_spatialAnchor == null)
                {
                    // Spatial Anchor destroys itself when creation fails.
                    return;
                }
                if (_spatialAnchor.Localized)
                {
                    break;
                }
                await Task.Yield();
            }

            // Save the anchor.
            var saveAnchorResult = await _spatialAnchor.SaveAnchorAsync();
            if (!saveAnchorResult.Success)
            {
                Debug.LogError($"SaveAnchorAsync() failed {saveAnchorResult}");
            }
        }

        private void Update()
        {
            if(!_isReady)
            {
                return;
            }

            if (!Application.isFocused && !Application.isEditor)
            {
                return;
            }

            VisualizeRaycast();
            if (_isGrabbing)
            {
                UpdateTargetPose();
                if (OVRInput.GetUp(_grabButton))
                {
                    _platformSelected.SetActive(false);
                    _isGrabbing = false;
                    _environmentPose = null;
                    if (!_isRestoringAnchorTracking)
                    {
                        // If the existing OVRSpatialAnchor if further than 3 meters away from the current panel position, delete it and create a new one:
                        // https://developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-best-practices#tips-for-using-spatial-anchors
                        if (_trackPlatform.localPosition.magnitude > 3f || _spatialAnchor == null || !_spatialAnchor.Localized)
                        {
                            if (_spatialAnchor != null)
                            {
                                _spatialAnchor.EraseAnchorAsync().ContinueWith(static result =>
                                {
                                    if (!result.Success)
                                    {
                                        Debug.LogError($"EraseAnchorAsync() failed {result}");
                                    }
                                });
                                DestroyImmediate(_spatialAnchor);
                            }
                            CreateSpatialAnchorAndSave(_trackPlatform.parent);
                        }
                    }
                }
            }
            else
            {
                // Animate scale with right thumbstick
                const float scaleSpeed = 1.5f;
                var panelScale = _trackPlatform.localScale.x;
                panelScale *= 1f + OVRInput.Get(_scaleAxis).y * scaleSpeed * Time.deltaTime;
                panelScale = Mathf.Clamp(panelScale, 0.1f, 1f);
                _trackPlatform.localScale = new Vector3(panelScale, panelScale, panelScale);

                // Detect grab gesture and update grab indicator
                bool didHitPanel = Physics.Raycast(GetRaycastRay(), out var hit) && hit.transform == _trackPlatform;
                _platformSelected.SetActive(didHitPanel);
                if (didHitPanel && OVRInput.GetDown(_grabButton))
                {
                    _isGrabbing = true;
                    _distanceFromController = Vector3.Distance(_raycastAnchor.position, _trackPlatform.position);
                }
            }
            AnimatePanelPose();
            UpdateSpatialAnchorTrackingState();
        }

        private Ray GetRaycastRay()
        {
            return new Ray(_raycastAnchor.position + _raycastAnchor.forward * 0.1f, _raycastAnchor.forward);
        }

        private void UpdateTargetPose()
        {
            // Animate manual placement position with right thumbstick
            const float moveSpeed = 2.5f;
            _distanceFromController += OVRInput.Get(_moveAxis).y * moveSpeed * Time.deltaTime;
            _distanceFromController = Mathf.Clamp(_distanceFromController, 0.3f, float.MaxValue);

            // Try place the panel onto environment
            var newEnvPose = TryGetEnvironmentPose();
            if (newEnvPose.HasValue)
            {
                _environmentPose = newEnvPose.Value;
            }
            else if (_currentEnvHitStatus == EnvironmentRaycastHitStatus.HitPointOutsideOfCameraFrustum)
            {
                _environmentPose = null;
            }
            var manualPlacementPosition = _raycastAnchor.position + _raycastAnchor.forward * _distanceFromController;
            var panelForward = Vector3.ProjectOnPlane(_centerEyeAnchor.position - manualPlacementPosition, Vector3.up).normalized;
            var manualPlacementPose = new Pose(manualPlacementPosition, Quaternion.LookRotation(panelForward));
            // If environment pose is available and the panel is closer to it than to the user, place the panel onto environment to create a magnetism effect
            bool chooseEnvPose = _environmentPose.HasValue && Vector3.Distance(manualPlacementPose.position, _environmentPose.Value.position) / Vector3.Distance(manualPlacementPose.position, _centerEyeAnchor.position) < 0.5;
            _targetPose = chooseEnvPose ? _environmentPose.Value : manualPlacementPose;
        }

        private Pose? TryGetEnvironmentPose()
        {
            var ray = GetRaycastRay();
            if (!_raycastManager.Raycast(ray, out var hit) || hit.normalConfidence < 0.5f)
            {
                return null;
            }
            bool isCeiling = Vector3.Dot(hit.normal, Vector3.down) > 0.7f;
            if (isCeiling)
            {
                return null;
            }
            const float sizeTolerance = 0.2f;
            var panelSize = new Vector3(_trackPlatform.localScale.x, _trackPlatform.localScale.y, 0f) * (1f - sizeTolerance);
            bool isVerticalSurface = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.3f;
            if (isVerticalSurface)
            {
                // If the surface is vertical, stick the panel to the surface
                if (_raycastManager.PlaceBox(ray, panelSize, Vector3.up, out var result))
                {
                    // Apply the rolling average filter to smooth the normal
                    var smoothedNormal = _rollingAverageFilter.UpdateRollingAverage(result.normal);
                    return new Pose(result.point, Quaternion.LookRotation(smoothedNormal, Vector3.up));
                }
            }
            else
            {
                // Position the panel upright and check collisions with environment
                var position = hit.point + Vector3.up * _trackPlatform.localScale.y * 0.5f;
                var halfExtents = panelSize * 0.5f;
                var forward = Vector3.ProjectOnPlane(_centerEyeAnchor.position - position, Vector3.up).normalized;
                var orientation = Quaternion.LookRotation(forward, Vector3.up);
                const float collisionCheckOffset = 0.1f;
                if (!_raycastManager.CheckBox(position + Vector3.up * collisionCheckOffset, halfExtents, orientation))
                {
                    return new Pose(position, orientation);
                }
            }
            return null;
        }

        private void AnimatePanelPose()
        {
            if (!_targetPose.HasValue)
            {
                return;
            }

            const float smoothTime = 0.13f;
            _trackPlatform.position = Vector3.SmoothDamp(_trackPlatform.position, _targetPose.Value.position, ref _positionVelocity, smoothTime);

            float angle = Quaternion.Angle(_trackPlatform.rotation, _targetPose.Value.rotation);
            if (angle > 0f)
            {
                float dampedAngle = Mathf.SmoothDampAngle(angle, 0f, ref _rotationVelocity, smoothTime);
                float t = 1f - dampedAngle / angle;
                _trackPlatform.rotation = Quaternion.SlerpUnclamped(_trackPlatform.rotation, _targetPose.Value.rotation, t);
            }
        }

        private void VisualizeRaycast()
        {
            var ray = GetRaycastRay();
            bool hasHit = RaycastPanelOrEnvironment(ray, out var hit) || hit.status == EnvironmentRaycastHitStatus.HitPointOccluded;
            bool hasNormal = hit.normalConfidence > 0f;
            _raycastVisualizationLine.enabled = hasHit;
            _raycastVisualizationNormal.gameObject.SetActive(hasHit && hasNormal);
            if (hasHit)
            {
                _raycastVisualizationLine.SetPosition(0, ray.origin);
                _raycastVisualizationLine.SetPosition(1, hit.point);

                if (hasNormal)
                {
                    _raycastVisualizationNormal.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                }
            }

        }

        private bool RaycastPanelOrEnvironment(Ray ray, out EnvironmentRaycastHit envHit)
        {
            if (Physics.Raycast(ray, out var physicsHit) && physicsHit.transform == _trackPlatform)
            {
                envHit = new EnvironmentRaycastHit
                {
                    status = EnvironmentRaycastHitStatus.Hit,
                    point = physicsHit.point,
                    normal = physicsHit.normal,
                    normalConfidence = 1f
                };
                return true;
            }
            bool envHitResult = _raycastManager.Raycast(ray, out envHit);
            _currentEnvHitStatus = envHit.status;
            return envHitResult;
        }

        private void UpdateSpatialAnchorTrackingState()
        {
            bool isTracked = _spatialAnchor != null && _spatialAnchor.Localized;
            //_panelSprite.color = isTracked ? Color.white : Color.red;
            //_trackingLostLabel.SetActive(!isTracked);
            if (_spatialAnchor != null && _spatialAnchor.Localized && !isTracked)
            {
                RestoreSpatialAnchorTracking();
            }
        }

        private async void RestoreSpatialAnchorTracking()
        {
            if (!_isRestoringAnchorTracking)
            {
                _isRestoringAnchorTracking = true;
                await RestoreTracking();
                _isRestoringAnchorTracking = false;
            }
            async ValueTask RestoreTracking()
            {
                Fusion.Assert.Always(_spatialAnchor);
                var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>(1);
                var loadResult = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new[] { _spatialAnchor.Uuid }, unboundAnchors);
                if (!loadResult.Success)
                {
                    Debug.LogError($"LoadUnboundAnchorsAsync() failed {loadResult.Status}.");
                    return;
                }
                if (unboundAnchors.Count != 0)
                {
                    Debug.LogError($"LoadUnboundAnchorsAsync() unexpected count:{unboundAnchors.Count}.");
                    return;
                }
                await Task.Yield();
                if (_spatialAnchor.Localized)
                {
                    Debug.Log("Spatial Anchor tracking was restored successfully.");
                }
            }
        }

        private class RollingAverage
        {
            private List<Vector3> _normals;
            private int _currentRollingAverageIndex;

            public Vector3 UpdateRollingAverage(Vector3 current)
            {
                if (_normals == null)
                {
                    const int filterSize = 10;
                    _normals = Enumerable.Repeat(current, filterSize).ToList();
                }
                _currentRollingAverageIndex++;
                _normals[_currentRollingAverageIndex % _normals.Count] = current;
                Vector3 result = default;
                foreach (var normal in _normals)
                {
                    result += normal;
                }
                return result.normalized;
            }
        }
    }
}
