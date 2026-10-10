using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Build WebGL pronto para GitHub Pages (gzip + fallback, sem cabecalhos especiais).
public static class WebBuilder
{
    [MenuItem("TranceHero/Build WebGL")]
    public static void BuildWeb()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.runInBackground = true;
        PlayerSettings.productName = "TranceHero";
        PlayerSettings.companyName = "TranceHero";

        var opts = new BuildPlayerOptions
        {
            scenes = new string[] { "Assets/Scenes/Main.unity" },
            locationPathName = "WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[TranceHero] WebGL: " + report.summary.result +
                  " em " + report.summary.outputPath +
                  " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
