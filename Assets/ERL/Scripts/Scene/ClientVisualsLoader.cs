using Fusion;
using UnityEngine;

public class ClientVisualsLoader : NetworkBehaviour
{
    [SerializeField] private GameObject visualsPrefab;

    // The "Safety Lock" - holds the reference to the created graphics
    private GameObject _spawnedVisuals;

    public override void Spawned()
    {
        // 1. SERVER REJECTION
        // If this is the dedicated server, abort immediately.
        if (Runner.IsServer && Runner.GameMode == GameMode.Server) return;

        // 2. DUPLICATE CHECK (The Fix)
        // Check if we already created the visuals for THIS specific object.
        if (_spawnedVisuals != null)
        {
            return;
        }

        // 3. CHILD CHECK
        // Sometimes Unity Reloads confuse things. Check if we already have a child.
        if (transform.childCount > 0)
        {
            // Optional: Destroy existing children if you want a hard reset
            // But usually, we just attach to what is there.
            _spawnedVisuals = transform.GetChild(0).gameObject;
            return;
        }

        // 4. SPAWN
        // If we get here, we are a Client, and we are "empty". 
        // Load the graphics.
        _spawnedVisuals = Instantiate(visualsPrefab, transform.position, transform.rotation, transform);
    }
}