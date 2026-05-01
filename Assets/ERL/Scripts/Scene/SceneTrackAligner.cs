using UnityEngine;
using Fusion;
using Meta.XR.MRUtilityKit;

namespace Assets.CryptoKartz.Scripts
{
    public class SceneTrackAligner : NetworkBehaviour
    {
        [Header("Target Object")]
        [Tooltip("Drag the Scene Object here. If using a prefab, find it by Tag in Start().")]
        public NetworkObject trackSceneObject;

        [Header("Placement Settings")]
        public MRUKAnchor.SceneLabels validSurfaces = MRUKAnchor.SceneLabels.TABLE | MRUKAnchor.SceneLabels.BED;
        public float edgePadding = 0.05f;

        public override void Spawned()
        {
            // Only run this for the local player (Input Authority)
            // We don't want to run this for remote players' avatars
            if (Object.HasInputAuthority)
            {
                // We wait a moment to ensure MRUK has loaded the room
                StartCoroutine(AlignTrackRoutine());
            }
        }

        private System.Collections.IEnumerator AlignTrackRoutine()
        {
            // Wait until MRUK is initialized and a room is loaded
            while (MRUK.Instance == null || MRUK.Instance.GetCurrentRoom() == null)
            {
                yield return new WaitForSeconds(0.5f);
            }

            AlignTrackToClosestSurface();
        }

        public void AlignTrackToClosestSurface()
        {
            // 1. Validate Scene Object
            if (trackSceneObject == null)
            {
                // Fallback: Find by Tag if reference is missing
                GameObject foundObj = GameObject.FindGameObjectWithTag("TrackPlatform");
                if (foundObj != null) trackSceneObject = foundObj.GetComponent<NetworkObject>();
            }

            if (trackSceneObject == null)
            {
                Debug.LogError("SceneTrackAligner: Could not find the Track Network Object!");
                return;
            }

            // 2. Get MRUK Room
            var room = MRUK.Instance.GetCurrentRoom();

            // 3. Find closest Table/Bed
            MRUKAnchor closestAnchor = GetClosestAnchor(room);
            if (closestAnchor == null)
            {
                Debug.LogError("SceneTrackAligner: No Table or Bed found in this room.");
                return;
            }

            // 4. Calculate Max Scale & Rotation
            // We measure the track's default size (assuming it is currently at scale 1,1,1 or we cache it)
            Bounds defaultBounds = GetRendererBounds(trackSceneObject.gameObject);

            FitResult fit = CalculateBestFit(closestAnchor, defaultBounds);

            // 5. Apply Transform LOCALLY
            // Since we removed NetworkTransform, this change only happens on this client.
            Transform trackTx = trackSceneObject.transform;

            trackTx.position = closestAnchor.transform.position;

            // Combine anchor rotation with our calculated 90-degree offset
            trackTx.rotation = closestAnchor.transform.rotation * Quaternion.Euler(0, fit.rotationY, 0);

            trackTx.localScale = fit.scale;

            Debug.Log($"Placed Local Track on {closestAnchor.Label} with scale {fit.scale}");
        }

        // --- Helper Logic ---

        private MRUKAnchor GetClosestAnchor(MRUKRoom room)
        {
            MRUKAnchor closest = null;
            float minDist = float.MaxValue;
            Vector3 userPos = Camera.main.transform.position;

            foreach (var anchor in room.Anchors)
            {
                if ((anchor.Label & validSurfaces) == 0) continue;
                if (anchor.PlaneRect == null) continue;

                float d = Vector3.Distance(userPos, anchor.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = anchor;
                }
            }
            return closest;
        }

        struct FitResult { public Vector3 scale; public float rotationY; }

        private FitResult CalculateBestFit(MRUKAnchor anchor, Bounds prefabBounds)
        {
            // Surface Dimensions
            Vector2 surfaceSize = anchor.PlaneRect.Value.size;
            float surfaceW = Mathf.Max(0.1f, surfaceSize.x - edgePadding * 2);
            float surfaceH = Mathf.Max(0.1f, surfaceSize.y - edgePadding * 2);

            // Object Dimensions (Prevent divide by zero)
            float prefabW = Mathf.Max(0.01f, prefabBounds.size.x);
            float prefabD = Mathf.Max(0.01f, prefabBounds.size.z);

            // Fit A: Align Width to Width
            float scaleX_A = surfaceW / prefabW;
            float scaleY_A = surfaceH / prefabD;
            float scaleA = Mathf.Min(scaleX_A, scaleY_A);

            // Fit B: Rotate 90 degrees (Width to Depth)
            float scaleX_B = surfaceW / prefabD;
            float scaleY_B = surfaceH / prefabW;
            float scaleB = Mathf.Min(scaleX_B, scaleY_B);

            if (scaleB > scaleA)
            {
                return new FitResult { scale = Vector3.one * scaleB, rotationY = 90f };
            }
            return new FitResult { scale = Vector3.one * scaleA, rotationY = 0f };
        }

        private Bounds GetRendererBounds(GameObject obj)
        {
            // Reset scale temporarily to get "native" bounds
            Vector3 oldScale = obj.transform.localScale;
            obj.transform.localScale = Vector3.one;

            Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(obj.transform.position, Vector3.zero);
            if (rends.Length > 0)
            {
                b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                {
                    b.Encapsulate(rends[i].bounds);
                }
            }

            // Restore scale
            obj.transform.localScale = oldScale;
            return b;
        }
    }
}