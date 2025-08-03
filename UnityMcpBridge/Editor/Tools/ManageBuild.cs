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
        public static object HandleCommand(JObject parameters)
        {
            return HandleBuildCommand(parameters.ToString());
        }

        public static string HandleBuildCommand(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                var action = data.GetValueOrDefault("action", "").ToString();

                return action switch
                {
                    "create_build" => CreateBuild(data),
                    "configure_build" => ConfigureBuild(data),
                    "list_builds" => ListBuilds(),
                    "get_build_info" => GetBuildInfo(data),
                    "build_project" => BuildProject(data),
                    "get_build_log" => GetBuildLog(data),
                    "validate_build" => ValidateBuild(data),
                    _ => JsonConvert.SerializeObject(new { error = $"Unknown action: {action}" })
                };
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        public static string HandleAssetBundleCommand(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                var action = data.GetValueOrDefault("action", "").ToString();

                return action switch
                {
                    "create_bundle" => CreateAssetBundle(data),
                    "add_assets" => AddAssetsToBundle(data),
                    "remove_assets" => RemoveAssetsFromBundle(data),
                    "build_bundles" => BuildAssetBundles(data),
                    "list_bundles" => ListAssetBundles(),
                    "get_bundle_info" => GetAssetBundleInfo(data),
                    "delete_bundle" => DeleteAssetBundle(data),
                    "validate_bundles" => ValidateAssetBundles(),
                    _ => JsonConvert.SerializeObject(new { error = $"Unknown action: {action}" })
                };
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        public static string HandleBuildPipelineCommand(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                var action = data.GetValueOrDefault("action", "").ToString();

                return action switch
                {
                    "create_pipeline" => CreateBuildPipeline(data),
                    "modify_pipeline" => ModifyBuildPipeline(data),
                    "run_pipeline" => RunBuildPipeline(data),
                    "list_pipelines" => ListBuildPipelines(),
                    "get_pipeline_info" => GetBuildPipelineInfo(data),
                    "delete_pipeline" => DeleteBuildPipeline(data),
                    "validate_pipeline" => ValidateBuildPipeline(data),
                    "get_pipeline_logs" => GetBuildPipelineLogs(data),
                    _ => JsonConvert.SerializeObject(new { error = $"Unknown action: {action}" })
                };
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        public static string HandleDeploymentCommand(string jsonData)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                var action = data.GetValueOrDefault("action", "").ToString();

                return action switch
                {
                    "deploy_build" => DeployBuild(data),
                    "configure_deployment" => ConfigureDeployment(data),
                    "list_deployments" => ListDeployments(),
                    "get_deployment_status" => GetDeploymentStatus(data),
                    "rollback_deployment" => RollbackDeployment(data),
                    "validate_deployment" => ValidateDeployment(data),
                    "get_deployment_logs" => GetDeploymentLogs(data),
                    _ => JsonConvert.SerializeObject(new { error = $"Unknown action: {action}" })
                };
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string CreateBuild(Dictionary<string, object> data)
        {
            try
            {
                var buildTarget = ParseBuildTarget(data.GetValueOrDefault("build_target", "").ToString());
                var buildPath = data.GetValueOrDefault("build_path", "").ToString();
                var scenes = data.ContainsKey("scenes") ? 
                    JsonConvert.DeserializeObject<string[]>(data["scenes"].ToString()) : 
                    EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

                var buildOptions = BuildOptions.None;
                if (data.ContainsKey("development_build") && bool.Parse(data["development_build"].ToString()))
                    buildOptions |= BuildOptions.Development;
                if (data.ContainsKey("script_debugging") && bool.Parse(data["script_debugging"].ToString()))
                    buildOptions |= BuildOptions.AllowDebugging;

                var buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = buildPath,
                    target = buildTarget,
                    options = buildOptions
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                
                return JsonConvert.SerializeObject(new
                {
                    success = report.summary.result == BuildResult.Succeeded,
                    result = report.summary.result.ToString(),
                    totalTime = report.summary.totalTime.TotalSeconds,
                    totalSize = report.summary.totalSize,
                    outputPath = report.summary.outputPath,
                    platform = report.summary.platform.ToString()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string ConfigureBuild(Dictionary<string, object> data)
        {
            try
            {
                // Configure build settings
                if (data.ContainsKey("company_name"))
                    PlayerSettings.companyName = data["company_name"].ToString();
                if (data.ContainsKey("product_name"))
                    PlayerSettings.productName = data["product_name"].ToString();
                if (data.ContainsKey("version"))
                    PlayerSettings.bundleVersion = data["version"].ToString();

                return JsonConvert.SerializeObject(new { success = true, message = "Build settings configured" });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string ListBuilds()
        {
            try
            {
                var builds = new List<object>();
                
                // Get available build targets
                foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
                {
                    if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                    {
                        builds.Add(new
                        {
                            target = target.ToString(),
                            supported = true,
                            group = BuildPipeline.GetBuildTargetGroup(target).ToString()
                        });
                    }
                }

                return JsonConvert.SerializeObject(new { builds });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string GetBuildInfo(Dictionary<string, object> data)
        {
            try
            {
                var buildTarget = ParseBuildTarget(data.GetValueOrDefault("build_target", "").ToString());
                
                var info = new
                {
                    target = buildTarget.ToString(),
                    group = BuildPipeline.GetBuildTargetGroup(buildTarget).ToString(),
                    supported = BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget),
                    scenes = EditorBuildSettings.scenes.Select(s => new { path = s.path, enabled = s.enabled }).ToArray(),
                    playerSettings = new
                    {
                        companyName = PlayerSettings.companyName,
                        productName = PlayerSettings.productName,
                        version = PlayerSettings.bundleVersion
                    }
                };

                return JsonConvert.SerializeObject(info);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string BuildProject(Dictionary<string, object> data)
        {
            return CreateBuild(data); // Same as create_build
        }

        private static string GetBuildLog(Dictionary<string, object> data)
        {
            try
            {
                // Placeholder for build log retrieval
                return JsonConvert.SerializeObject(new 
                { 
                    message = "Build log retrieval not yet implemented",
                    logs = new string[] { "Build log functionality requires custom implementation" }
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
            }
        }

        private static string ValidateBuild(Dictionary<string, object> data)
        {
            try
            {
                var issues = new List<string>();
                
                // Check if scenes are set
                if (EditorBuildSettings.scenes.Length == 0)
                    issues.Add("No scenes configured for build");
                
                // Check if build target is supported
                var buildTarget = ParseBuildTarget(data.GetValueOrDefault("build_target", "").ToString());
                if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget))
                    issues.Add($"Build target {buildTarget} is not supported");

                return JsonConvert.SerializeObject(new 
                { 
                    valid = issues.Count == 0,
                    issues = issues.ToArray()
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { error = e.Message });
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