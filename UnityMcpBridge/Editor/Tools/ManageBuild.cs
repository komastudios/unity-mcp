using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Tools
{
    public static class ManageBuild
    {
        public static JObject HandleCommand(JObject parameters)
        {
            return HandleBuildCommand(parameters);
        }

        public static JObject HandleBuildCommand(JObject parameters)
        {
            try
            {
                string action = parameters["action"]?.ToString();

                return action switch
                {
                    "create_build" => CreateBuild(parameters),
                    "configure_build" => ConfigureBuild(parameters),
                    "list_builds" => ListBuilds(),
                    "get_build_info" => GetBuildInfo(parameters),
                    "build_project" => BuildProject(parameters),
                    "get_build_log" => GetBuildLog(parameters),
                    "validate_build" => ValidateBuild(parameters),
                    _ => new JObject { ["error"] = $"Unknown action: {action}" }
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        public static JObject HandleAssetBundleCommand(JObject parameters)
        {
            try
            {
                string action = parameters["action"]?.ToString();

                return action switch
                {
                    "create_bundle" => CreateAssetBundle(parameters),
                    "add_assets" => AddAssetsToBundle(parameters),
                    "remove_assets" => RemoveAssetsFromBundle(parameters),
                    "build_bundles" => BuildAssetBundles(parameters),
                    "list_bundles" => ListAssetBundles(),
                    "get_bundle_info" => GetAssetBundleInfo(parameters),
                    "delete_bundle" => DeleteAssetBundle(parameters),
                    "validate_bundles" => ValidateAssetBundles(),
                    _ => new JObject { ["error"] = $"Unknown action: {action}" }
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        public static JObject HandleBuildPipelineCommand(JObject parameters)
        {
            try
            {
                string action = parameters["action"]?.ToString();

                return action switch
                {
                    "create_pipeline" => CreateBuildPipeline(parameters),
                    "modify_pipeline" => ModifyBuildPipeline(parameters),
                    "run_pipeline" => RunBuildPipeline(parameters),
                    "list_pipelines" => ListBuildPipelines(),
                    "get_pipeline_info" => GetBuildPipelineInfo(parameters),
                    "delete_pipeline" => DeleteBuildPipeline(parameters),
                    "validate_pipeline" => ValidateBuildPipeline(parameters),
                    "get_pipeline_logs" => GetBuildPipelineLogs(parameters),
                    _ => new JObject { ["error"] = $"Unknown action: {action}" }
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        public static JObject HandleDeploymentCommand(JObject parameters)
        {
            try
            {
                string action = parameters["action"]?.ToString();

                return action switch
                {
                    "deploy_build" => DeployBuild(parameters),
                    "configure_deployment" => ConfigureDeployment(parameters),
                    "list_deployments" => ListDeployments(),
                    "get_deployment_status" => GetDeploymentStatus(parameters),
                    "rollback_deployment" => RollbackDeployment(parameters),
                    "validate_deployment" => ValidateDeployment(parameters),
                    "get_deployment_logs" => GetDeploymentLogs(parameters),
                    _ => new JObject { ["error"] = $"Unknown action: {action}" }
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject CreateBuild(JObject parameters)
        {
            try
            {
                var buildTarget = ParseBuildTarget(parameters["build_target"]?.ToString() ?? "");
                var buildPath = parameters["build_path"]?.ToString() ?? "";
                var scenes = parameters["scenes"] != null ? 
                    parameters["scenes"].ToObject<string[]>() : 
                    EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

                var buildOptions = BuildOptions.None;
                if (parameters["development_build"]?.ToObject<bool>() ?? false)
                    buildOptions |= BuildOptions.Development;
                if (parameters["script_debugging"]?.ToObject<bool>() ?? false)
                    buildOptions |= BuildOptions.AllowDebugging;

                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = buildPath,
                    target = buildTarget,
                    options = buildOptions
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                
                return new JObject
                {
                    ["success"] = report.summary.result == BuildResult.Succeeded,
                    ["result"] = report.summary.result.ToString(),
                    ["totalTime"] = report.summary.totalTime.TotalSeconds,
                    ["totalSize"] = report.summary.totalSize,
                    ["outputPath"] = report.summary.outputPath,
                    ["platform"] = report.summary.platform.ToString()
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject ConfigureBuild(JObject parameters)
        {
            try
            {
                // Configure build settings
                if (parameters["company_name"] != null)
                    PlayerSettings.companyName = parameters["company_name"].ToString();
                if (parameters["product_name"] != null)
                    PlayerSettings.productName = parameters["product_name"].ToString();
                if (parameters["version"] != null)
                    PlayerSettings.bundleVersion = parameters["version"].ToString();

                return new JObject { ["success"] = true, ["message"] = "Build settings configured" };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject ListBuilds()
        {
            try
            {
                var builds = new JArray();
                
                // Get available build targets
                foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
                {
                    if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                    {
                        builds.Add(new JObject
                        {
                            ["target"] = target.ToString(),
                            ["supported"] = true,
                            ["group"] = BuildPipeline.GetBuildTargetGroup(target).ToString()
                        });
                    }
                }

                return new JObject { ["success"] = true, ["builds"] = builds };
            }
            catch (Exception e)
            {
                return new JObject { ["success"] = false, ["error"] = e.Message };
            }
        }

        private static JObject GetBuildInfo(JObject parameters)
        {
            try
            {
                var buildTarget = ParseBuildTarget(parameters["build_target"]?.ToString() ?? "");
                
                var info = new JObject
                {
                    ["target"] = buildTarget.ToString(),
                    ["group"] = BuildPipeline.GetBuildTargetGroup(buildTarget).ToString(),
                    ["supported"] = BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget),
                    ["scenes"] = new JArray(EditorBuildSettings.scenes.Select(s => new JObject { ["path"] = s.path, ["enabled"] = s.enabled })),
                    ["playerSettings"] = new JObject
                    {
                        ["companyName"] = PlayerSettings.companyName,
                        ["productName"] = PlayerSettings.productName,
                        ["version"] = PlayerSettings.bundleVersion
                    }
                };

                return info;
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject BuildProject(JObject parameters)
        {
            return CreateBuild(parameters); // Same as create_build
        }

        private static JObject GetBuildLog(JObject parameters)
        {
            try
            {
                // Placeholder for build log retrieval
                return new JObject 
                { 
                    ["message"] = "Build log retrieval not yet implemented",
                    ["logs"] = new JArray("Build log functionality requires custom implementation")
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject ValidateBuild(JObject parameters)
        {
            try
            {
                var issues = new JArray();
                
                // Check if scenes are set
                if (EditorBuildSettings.scenes.Length == 0)
                    issues.Add("No scenes configured for build");
                
                // Check if build target is supported
                var buildTarget = ParseBuildTarget(parameters["build_target"]?.ToString() ?? "");
                if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget))
                    issues.Add($"Build target {buildTarget} is not supported");

                return new JObject 
                { 
                    ["valid"] = issues.Count == 0,
                    ["issues"] = issues
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        // Asset Bundle Methods
        private static JObject CreateAssetBundle(JObject data)
        {
            try
            {
                var bundleName = data["bundle_name"]?.ToString() ?? "";
                var assets = data["assets"]?.ToObject<string[]>() ?? new string[0];

                foreach (var assetPath in assets)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    if (importer != null)
                    {
                        importer.assetBundleName = bundleName;
                    }
                }

                return new JObject
                { 
                    ["success"] = true, 
                    ["bundleName"] = bundleName,
                    ["assetsCount"] = assets.Length
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject AddAssetsToBundle(JObject data)
        {
            return CreateAssetBundle(data); // Same logic
        }

        private static JObject RemoveAssetsFromBundle(JObject data)
        {
            try
            {
                var assets = data["assets"]?.ToObject<string[]>() ?? new string[0];

                foreach (var assetPath in assets)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    if (importer != null)
                    {
                        importer.assetBundleName = "";
                    }
                }

                return new JObject
                { 
                    ["success"] = true,
                    ["removedAssetsCount"] = assets.Length
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject BuildAssetBundles(JObject data)
        {
            try
            {
                var buildPath = data["build_path"]?.ToString() ?? "Assets/StreamingAssets";
                var buildTarget = ParseBuildTarget(data["build_target"]?.ToString() ?? EditorUserBuildSettings.activeBuildTarget.ToString());

                if (!Directory.Exists(buildPath))
                    Directory.CreateDirectory(buildPath);

                var manifest = BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.None, buildTarget);

                return new JObject
                { 
                    ["success"] = manifest != null,
                    ["buildPath"] = buildPath,
                    ["bundles"] = new JArray(manifest?.GetAllAssetBundles() ?? new string[0])
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject ListAssetBundles()
        {
            try
            {
                var bundleNames = AssetDatabase.GetAllAssetBundleNames();
                var bundles = new JArray();
                
                foreach (var name in bundleNames)
                {
                    bundles.Add(new JObject
                    {
                        ["name"] = name,
                        ["assets"] = new JArray(AssetDatabase.GetAssetPathsFromAssetBundle(name))
                    });
                }

                return new JObject { ["bundles"] = bundles };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject GetAssetBundleInfo(JObject data)
        {
            try
            {
                var bundleName = data["bundle_name"]?.ToString() ?? "";
                var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);

                return new JObject
                { 
                    ["bundleName"] = bundleName,
                    ["assets"] = new JArray(assets),
                    ["assetCount"] = assets.Length
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject DeleteAssetBundle(JObject data)
        {
            try
            {
                var bundleName = data["bundle_name"]?.ToString() ?? "";
                AssetDatabase.RemoveAssetBundleName(bundleName, true);

                return new JObject
                { 
                    ["success"] = true,
                    ["message"] = $"Asset bundle '{bundleName}' deleted"
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        private static JObject ValidateAssetBundles()
        {
            try
            {
                var issues = new List<string>();
                var bundleNames = AssetDatabase.GetAllAssetBundleNames();

                foreach (var bundleName in bundleNames)
                {
                    var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
                    if (assets.Length == 0)
                        issues.Add($"Bundle '{bundleName}' has no assets");
                }

                return new JObject
                { 
                    ["valid"] = issues.Count == 0,
                    ["issues"] = new JArray(issues.ToArray()),
                    ["bundleCount"] = bundleNames.Length
                };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
            }
        }

        // Placeholder methods for build pipeline and deployment
        private static JObject CreateBuildPipeline(JObject data)
        {
            return new JObject { ["message"] = "Build pipeline creation not yet implemented" };
        }

        private static JObject ModifyBuildPipeline(JObject data)
        {
            return new JObject { ["message"] = "Build pipeline modification not yet implemented" };
        }

        private static JObject RunBuildPipeline(JObject data)
        {
            return new JObject { ["message"] = "Build pipeline execution not yet implemented" };
        }

        private static JObject ListBuildPipelines()
        {
            return new JObject { ["message"] = "Build pipeline listing not yet implemented" };
        }

        private static JObject GetBuildPipelineInfo(JObject data)
        {
            return new JObject { ["message"] = "Build pipeline info not yet implemented" };
        }

        private static JObject DeleteBuildPipeline(JObject data)
        {
            return new JObject { ["message"] = "Build pipeline deletion not yet implemented" };
        }

        private static JObject ValidateBuildPipeline(JObject data)
        {
            return new JObject { ["message"] = "Build pipeline validation not yet implemented" };
        }

        private static JObject GetBuildPipelineLogs(JObject data)
        {
            return new JObject { ["message"] = "Build pipeline logs not yet implemented" };
        }

        private static JObject DeployBuild(JObject data)
        {
            return new JObject { ["message"] = "Build deployment not yet implemented" };
        }

        private static JObject ConfigureDeployment(JObject data)
        {
            return new JObject { ["message"] = "Deployment configuration not yet implemented" };
        }

        private static JObject ListDeployments()
        {
            return new JObject { ["message"] = "Deployment listing not yet implemented" };
        }

        private static JObject GetDeploymentStatus(JObject data)
        {
            return new JObject { ["message"] = "Deployment status not yet implemented" };
        }

        private static JObject RollbackDeployment(JObject data)
        {
            return new JObject { ["message"] = "Deployment rollback not yet implemented" };
        }

        private static JObject ValidateDeployment(JObject data)
        {
            return new JObject { ["message"] = "Deployment validation not yet implemented" };
        }

        private static JObject GetDeploymentLogs(JObject data)
        {
            return new JObject { ["message"] = "Deployment logs not yet implemented" };
        }

        private static BuildTarget ParseBuildTarget(string targetString)
        {
            if (Enum.TryParse<BuildTarget>(targetString, true, out var target))
                return target;
            return EditorUserBuildSettings.activeBuildTarget;
        }
    }
}