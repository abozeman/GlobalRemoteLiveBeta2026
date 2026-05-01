using Assets.CryptoKartz.Scripts.Managers;
using cryptokartz.Scripts.Car;
using Fusion;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using static Unity.Collections.Unicode;

public class GhostCarSpawnManager : NetworkBehaviour
{

    int _levelId;
    int _colorId;
    string _vid;
    GameObject _trackPlatform;

    public override void Spawned()
    {
        


        if (!Runner.IsServer)
        {
            var cdn = Object.GetComponent<CarDataNetwork>();
            _levelId = cdn.LevelId;
            _colorId = cdn.ColorId;
            string objFind = $"TrackPlatformPlacementTool/TrackPlatformPlacementTarget/TrackPlatformContainer/RaceTrackShell{_levelId}";
            _trackPlatform = GameObject.Find(objFind);
            transform.SetParent(_trackPlatform.transform, false);
            transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
        }
        else
        {
            var cdn = Object.GetComponent<CarDataNetwork>();
            _levelId = cdn.LevelId;
            _vid = $"echoghostracer{_levelId}";
            cdn.Vid = _vid;
            var mts = FindAnyObjectByType<MasterTelemetrySubscriberV2>();
            mts.AddCar(_vid, transform.gameObject);

        }
    }
}
