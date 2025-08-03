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

                return new JObject { ["builds"] = builds };
            }
            catch (Exception e)
            {
                return new JObject { ["error"] = e.Message };
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
        private static string CreateAssetBundle(Dictionary<string, object> data)
        {
            try
            {
                var bundleName = data.GetValueOrDefault("bundle_name", "").ToString();
                var assets = data.ContainsKey("assets") ? 
                    JsonConvert.DeserializeObject<string[]>(data["assets"].ToString()) : new string[0];

                foreach (var assetPath in assets)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    if (importer != null)
                    {
                        importer.assetBundleName = bundleName;
                    }
                }

                return JsonConvert.SerializeObject(new 
                { 
                    success = true, 
                    bundleName = bundleName,
                    assetsCount = assets.Length
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string AddAssetsToBundle(Dictionary<string, object> data)
        {
            return CreateAssetBundle(data); // Same logic
        }

        private static string RemoveAssetsFromBundle(Dictionary<string, object> data)
        {
            try
            {
                var assets = data.ContainsKey("assets") ? 
                    JsonConvert.DeserializeObject<string[]>(data["assets"].ToString()) : new string[0];

                foreach (var assetPath in assets)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    if (importer != null)
                    {
                        importer.assetBundleName = "";
                    }
                }

                return JsonConvert.SerializeObject(new 
                { 
                    success = true,
                    removedAssetsCount = assets.Length
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string BuildAssetBundles(Dictionary<string, object> data)
        {
            try
            {
                var buildPath = data.GetValueOrDefault("build_path", "Assets/StreamingAssets").ToString();
                var buildTarget = ParseBuildTarget(data.GetValueOrDefault("build_target", EditorUserBuildSettings.activeBuildTarget.ToString()).ToString());

                if (!Directory.Exists(buildPath))
                    Directory.CreateDirectory(buildPath);

                var manifest = BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.None, buildTarget);

                return JsonConvert.SerializeObject(new 
                { 
                    success = manifest != null,
                    buildPath = buildPath,
                    bundles = manifest?.GetAllAssetBundles() ?? new string[0]
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string ListAssetBundles()
        {
            try
            {
                var bundleNames = AssetDatabase.GetAllAssetBundleNames();
                var bundles = bundleNames.Select(name => new
                {
                    name = name,
                    assets = AssetDatabase.GetAssetPathsFromAssetBundle(name)
                }).ToArray();

                return JsonConvert.SerializeObject(new { bundles });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string GetAssetBundleInfo(Dictionary<string, object> data)
        {
            try
            {
                var bundleName = data.GetValueOrDefault("bundle_name", "").ToString();
                var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);

                return JsonConvert.SerializeObject(new 
                { 
                    bundleName = bundleName,
                    assets = assets,
                    assetCount = assets.Length
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string DeleteAssetBundle(Dictionary<string, object> data)
        {
            try
            {
                var bundleName = data.GetValueOrDefault("bundle_name", "").ToString();
                AssetDatabase.RemoveAssetBundleName(bundleName, true);

                return JsonConvert.SerializeObject(new 
                { 
                    success = true,
                    message = $"Asset bundle '{bundleName}' deleted"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string ValidateAssetBundles()
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

                return JsonConvert.SerializeObject(new 
                { 
                    valid = issues.Count == 0,
                    issues = issues.ToArray(),
                    bundleCount = bundleNames.Length
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        // Placeholder methods for build pipeline and deployment
        private static string CreateBuildPipeline(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Build pipeline creation not yet implemented" });
        }

        private static string ModifyBuildPipeline(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Build pipeline modification not yet implemented" });
        }

        private static string RunBuildPipeline(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Build pipeline execution not yet implemented" });
        }

        private static string ListBuildPipelines()
        {
            return JsonConvert.SerializeObject(new { message = "Build pipeline listing not yet implemented" });
        }

        private static string GetBuildPipelineInfo(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Build pipeline info not yet implemented" });
        }

        private static string DeleteBuildPipeline(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Build pipeline deletion not yet implemented" });
        }

        private static string ValidateBuildPipeline(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Build pipeline validation not yet implemented" });
        }

        private static string GetBuildPipelineLogs(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Build pipeline logs not yet implemented" });
        }

        private static string DeployBuild(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Build deployment not yet implemented" });
        }

        private static string ConfigureDeployment(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Deployment configuration not yet implemented" });
        }

        private static string ListDeployments()
        {
            return JsonConvert.SerializeObject(new { message = "Deployment listing not yet implemented" });
        }

        private static string GetDeploymentStatus(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Deployment status not yet implemented" });
        }

        private static string RollbackDeployment(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Deployment rollback not yet implemented" });
        }

        private static string ValidateDeployment(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Deployment validation not yet implemented" });
        }

        private static string GetDeploymentLogs(Dictionary<string, object> data)
        {
            return JsonConvert.SerializeObject(new { message = "Deployment logs not yet implemented" });
        }

        private static BuildTarget ParseBuildTarget(string targetString)
        {
            if (Enum.TryParse<BuildTarget>(targetString, true, out var target))
                return target;
            return EditorUserBuildSettings.activeBuildTarget;
        }
    }
}