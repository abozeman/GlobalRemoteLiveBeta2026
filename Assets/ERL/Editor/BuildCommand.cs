using UnityEditor;
using UnityEditor.Build.Profile; // Required for BuildProfile API
using System.IO;

public class BuildCommand
{
    public static void BuildTPLServer()
    {
        // 1. Locate the "TPLServer" Build Profile asset
        // Profiles are stored as assets; we find the GUID and load it.
        string[] profileGuids = AssetDatabase.FindAssets("t:BuildProfile TPLServer");

        if (profileGuids.Length == 0)
        {
            UnityEngine.Debug.LogError("Error: Build Profile 'TPLServer' was not found in the project.");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(profileGuids[0]);
        BuildProfile tplProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);

        // 2. Configure the specific options for Unity 6 Build Profiles
        // We use BuildPlayerWithProfileOptions instead of the old BuildPlayerOptions
        var buildOptions = new BuildPlayerWithProfileOptions
        {
            buildProfile = tplProfile,
            locationPathName = @"C:\Users\antho\TPLive\tplcontainer\GameServerBuild\TP2026.exe",
            // You can still add additional options if needed, e.g., BuildOptions.Development
            options = BuildOptions.None
        };

        // 3. Execute the build using the profile-specific pipeline
        UnityEngine.Debug.Log($"Starting build using Profile: {tplProfile.name}");
        var report = BuildPipeline.BuildPlayer(buildOptions);

        // 4. Report results
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("Build Succeeded!");
        }
        else
        {
            UnityEngine.Debug.LogError("Build Failed! Check the Unity log for details.");
        }
    }
}