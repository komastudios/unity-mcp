using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityMcpBridge.Editor.Tools
{
    public static class TriggerDomainReload
    {
        // Session ID that survives domain reloads
        private static readonly string SESSION_ID;
        private const string SESSION_ID_KEY = "UnityMcpBridge.SessionId";
        private const string JOBS_KEY_PREFIX = "UnityMcpBridge.DomainReloadJob.";
        
        // In-memory state (lost on domain reload)
        private static readonly object lockObj = new object();
        private static Dictionary<string, DomainReloadJob> activeJobs = new Dictionary<string, DomainReloadJob>();
        private static bool handlersRegistered = false;
        
        // Static constructor - runs when domain loads
        static TriggerDomainReload()
        {
            // Check if we have an existing session ID (we're being reloaded)
            string existingSessionId = SessionState.GetString(SESSION_ID_KEY, null);
            
            if (!string.IsNullOrEmpty(existingSessionId))
            {
                // We've been reloaded - restore state
                SESSION_ID = existingSessionId;
                Debug.Log($"[TriggerDomainReload] Restored after domain reload. Session: {SESSION_ID}");
                RestoreJobsFromSessionState();
            }
            else
            {
                // Fresh start - create new session
                SESSION_ID = Guid.NewGuid().ToString();
                SessionState.SetString(SESSION_ID_KEY, SESSION_ID);
                Debug.Log($"[TriggerDomainReload] Fresh start. New session: {SESSION_ID}");
            }
            
            // Always register for updates to process jobs
            EditorApplication.update += ProcessJobs;
        }
        
        [Serializable]
        private class DomainReloadJob
        {
            public string jobId;
            public string action;
            public bool includeLogs;
            public string logLevel;
            public float timeout;
            public double startTime;
            public string status = "running"; // running, completed, failed, timeout
            public bool compilationSucceeded = true;
            public List<string> compilationLogs = new List<string>();
            public List<CompilationError> compilationErrors = new List<CompilationError>();
            public string message = "";
            public bool wasCompiling;
            public bool hadToRefresh;
            
            // Track if compilation events occurred
            public bool compilationStarted = false;
            public bool compilationFinished = false;
        }
        
        [Serializable]
        private class CompilationError
        {
            public string type;
            public string message;
            public string file;
            public int line;
            public int column;
        }
        
        public static object HandleCommand(JObject parameters)
        {
            try
            {
                string action = parameters["action"]?.ToString() ?? "compile_and_reload";
                bool waitForCompletion = parameters["waitForCompletion"]?.ToObject<bool>() ?? true;
                bool includeLogs = parameters["includeLogs"]?.ToObject<bool>() ?? true;
                string logLevel = parameters["logLevel"]?.ToString() ?? "all";
                float timeout = parameters["timeout"]?.ToObject<float>() ?? 300f;
                
                // Special handling for status polling
                if (action == "get_status")
                {
                    string jobId = parameters["jobId"]?.ToString();
                    if (string.IsNullOrEmpty(jobId))
                    {
                        return new
                        {
                            success = false,
                            error = "Missing jobId parameter for get_status action"
                        };
                    }
                    
                    return GetJobStatus(jobId);
                }

                // Create a new job
                string newJobId = Guid.NewGuid().ToString();
                var job = new DomainReloadJob
                {
                    jobId = newJobId,
                    action = action,
                    includeLogs = includeLogs,
                    logLevel = logLevel,
                    timeout = timeout,
                    startTime = EditorApplication.timeSinceStartup,
                    wasCompiling = EditorApplication.isCompiling
                };

                // Store job in memory and persistent storage
                lock (lockObj)
                {
                    activeJobs[newJobId] = job;
                }
                SaveJobToSessionState(job);
                
                // Register handlers if needed
                if (!handlersRegistered && includeLogs)
                {
                    RegisterCompilationHandlers();
                    handlersRegistered = true;
                }

                // ALWAYS require confirmation for asset-heavy operations (security: cannot be overridden via TCP)
                bool needsConfirmation = action == "refresh_assets" || action == "compile_and_reload";
                
                if (needsConfirmation)
                {
                    string dialogTitle = "Confirm Asset Refresh";
                    string dialogMessage = action == "refresh_assets" 
                        ? "Asset refresh can take a long time for large projects.\n\nDo you want to proceed with refreshing all assets?"
                        : "Full compile and reload includes asset refresh which can take a long time for large projects.\n\nDo you want to proceed?";
                    
                    bool userConfirmed = EditorUtility.DisplayDialog(
                        dialogTitle,
                        dialogMessage,
                        "Yes, Proceed",
                        "Cancel"
                    );
                    
                    if (!userConfirmed)
                    {
                        // User cancelled - clean up and return
                        lock (lockObj)
                        {
                            activeJobs.Remove(newJobId);
                        }
                        
                        return new
                        {
                            success = false,
                            error = "User cancelled the operation",
                            message = "Asset refresh was cancelled by user"
                        };
                    }
                }
                
                // Perform the requested action
                Debug.Log($"[TriggerDomainReload] Starting job {newJobId} - Action: {action}");
                
                switch (action)
                {
                    case "refresh_assets":
                        AssetDatabase.Refresh();
                        job.hadToRefresh = true;
                        Debug.Log("Asset database refresh initiated.");
                        break;
                        
                    case "compile_scripts":
                        CompilationPipeline.RequestScriptCompilation();
                        Debug.Log("Script compilation requested.");
                        break;
                        
                    case "domain_reload":
                        EditorUtility.RequestScriptReload();
                        Debug.Log("Domain reload requested.");
                        break;
                        
                    case "compile_and_reload":
                    default:
                        AssetDatabase.Refresh();
                        job.hadToRefresh = true;
                        CompilationPipeline.RequestScriptCompilation();
                        EditorUtility.RequestScriptReload();
                        Debug.Log("Full compile and reload initiated.");
                        break;
                }
                
                // Save job state after action
                SaveJobToSessionState(job);

                // Return immediately with job ID
                return new
                {
                    success = true,
                    data = new
                    {
                        jobId = newJobId,
                        status = "initiated",
                        message = $"Domain reload job '{newJobId}' started. Poll with action='get_status' and jobId='{newJobId}'",
                        sessionId = SESSION_ID,
                        wasCompiling = job.wasCompiling,
                        hadToRefresh = job.hadToRefresh
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in TriggerDomainReload: {ex.Message}\n{ex.StackTrace}");
                return new
                {
                    success = false,
                    error = ex.Message,
                    message = "Failed to trigger domain reload"
                };
            }
        }
        
        private static object GetJobStatus(string jobId)
        {
            DomainReloadJob job = null;
            
            // Try to get from memory first
            lock (lockObj)
            {
                activeJobs.TryGetValue(jobId, out job);
            }
            
            // If not in memory, try to restore from session state
            if (job == null)
            {
                string jobJson = SessionState.GetString(JOBS_KEY_PREFIX + jobId, null);
                if (!string.IsNullOrEmpty(jobJson))
                {
                    try
                    {
                        job = JsonConvert.DeserializeObject<DomainReloadJob>(jobJson);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to deserialize job {jobId}: {ex.Message}");
                    }
                }
            }
            
            if (job == null)
            {
                return new
                {
                    success = false,
                    error = $"Job '{jobId}' not found. It may have expired or been cleaned up."
                };
            }
            
            // Build response
            var responseData = new Dictionary<string, object>
            {
                ["jobId"] = job.jobId,
                ["status"] = job.status,
                ["compilationSucceeded"] = job.compilationSucceeded,
                ["duration"] = job.status == "running" 
                    ? EditorApplication.timeSinceStartup - job.startTime 
                    : EditorApplication.timeSinceStartup - job.startTime,
                ["wasCompiling"] = job.wasCompiling,
                ["hadToRefresh"] = job.hadToRefresh,
                ["message"] = job.message,
                ["sessionId"] = SESSION_ID
            };
            
            // Add compilation errors if any
            if (job.compilationErrors.Count > 0)
            {
                responseData["compilationErrors"] = job.compilationErrors;
            }
            
            // Add logs if requested and available
            if (job.includeLogs && job.compilationLogs.Count > 0)
            {
                var filteredLogs = FilterLogs(job.compilationLogs, job.logLevel);
                if (filteredLogs.Count > 0)
                {
                    responseData["logs"] = filteredLogs;
                }
            }
            
            // Clean up completed jobs after they've been retrieved
            if (job.status != "running")
            {
                lock (lockObj)
                {
                    activeJobs.Remove(jobId);
                }
                // Keep in SessionState for a while in case of re-polling
            }
            
            return new
            {
                success = true,
                data = responseData
            };
        }
        
        private static void ProcessJobs()
        {
            List<DomainReloadJob> jobsToUpdate = new List<DomainReloadJob>();
            
            lock (lockObj)
            {
                foreach (var job in activeJobs.Values)
                {
                    if (job.status == "running")
                    {
                        jobsToUpdate.Add(job);
                    }
                }
            }
            
            foreach (var job in jobsToUpdate)
            {
                // Check if compilation is complete
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    // Check if we were waiting for compilation to start
                    if (job.action != "refresh_assets" && !job.compilationStarted)
                    {
                        // Give it a bit more time for compilation to start
                        var elapsed = EditorApplication.timeSinceStartup - job.startTime;
                        if (elapsed < 2.0) // Wait up to 2 seconds for compilation to start
                        {
                            continue;
                        }
                    }
                    
                    // Compilation is done (or never started)
                    job.status = job.compilationSucceeded ? "completed" : "failed";
                    job.message = job.compilationSucceeded 
                        ? "Domain reload completed successfully" 
                        : "Compilation failed with errors";
                    
                    Debug.Log($"[TriggerDomainReload] Job {job.jobId} completed with status: {job.status}");
                    SaveJobToSessionState(job);
                }
                else
                {
                    // Still compiling - check timeout
                    var elapsed = EditorApplication.timeSinceStartup - job.startTime;
                    if (elapsed > job.timeout)
                    {
                        job.status = "timeout";
                        job.message = $"Operation timed out after {job.timeout} seconds";
                        Debug.Log($"[TriggerDomainReload] Job {job.jobId} timed out");
                        SaveJobToSessionState(job);
                    }
                }
            }
        }
        
        private static void SaveJobToSessionState(DomainReloadJob job)
        {
            try
            {
                string jobJson = JsonConvert.SerializeObject(job);
                SessionState.SetString(JOBS_KEY_PREFIX + job.jobId, jobJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save job {job.jobId} to SessionState: {ex.Message}");
            }
        }
        
        private static void RestoreJobsFromSessionState()
        {
            // Look for any persisted jobs
            var restoredCount = 0;
            var keys = new List<string>();
            
            // SessionState doesn't provide enumeration, so we'll have to clean up completed jobs periodically
            // For now, we'll just log that we've been restored
            Debug.Log($"[TriggerDomainReload] Session restored. Jobs will be loaded on demand.");
        }
        
        private static void RegisterCompilationHandlers()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            Application.logMessageReceived += OnLogMessageReceived;
        }
        
        private static void OnCompilationStarted(object obj)
        {
            Debug.Log("[TriggerDomainReload] Compilation started");
            
            lock (lockObj)
            {
                foreach (var job in activeJobs.Values)
                {
                    if (job.status == "running")
                    {
                        job.compilationStarted = true;
                        job.compilationLogs.Add($"[{DateTime.Now:HH:mm:ss}] Compilation started");
                        SaveJobToSessionState(job);
                    }
                }
            }
        }
        
        private static void OnCompilationFinished(object obj)
        {
            Debug.Log("[TriggerDomainReload] Compilation finished");
            
            lock (lockObj)
            {
                foreach (var job in activeJobs.Values)
                {
                    if (job.status == "running")
                    {
                        job.compilationFinished = true;
                        job.compilationLogs.Add($"[{DateTime.Now:HH:mm:ss}] Compilation finished");
                        SaveJobToSessionState(job);
                    }
                }
            }
        }
        
        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            lock (lockObj)
            {
                foreach (var job in activeJobs.Values)
                {
                    if (job.status == "running" && job.includeLogs)
                    {
                        job.compilationLogs.Add($"[{DateTime.Now:HH:mm:ss}] Assembly compiled: {assemblyPath}");
                        
                        foreach (var message in messages)
                        {
                            if (message.type == CompilerMessageType.Error)
                            {
                                job.compilationSucceeded = false;
                                job.compilationErrors.Add(new CompilationError
                                {
                                    type = message.type.ToString(),
                                    message = message.message,
                                    file = message.file,
                                    line = message.line,
                                    column = message.column
                                });
                                job.compilationLogs.Add($"[ERROR] {message.file}({message.line},{message.column}): {message.message}");
                            }
                            else if (message.type == CompilerMessageType.Warning)
                            {
                                job.compilationLogs.Add($"[WARNING] {message.file}({message.line},{message.column}): {message.message}");
                            }
                        }
                        
                        SaveJobToSessionState(job);
                    }
                }
            }
        }
        
        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                lock (lockObj)
                {
                    foreach (var job in activeJobs.Values)
                    {
                        if (job.status == "running" && job.includeLogs)
                        {
                            job.compilationLogs.Add($"[{type}] {logString}");
                            if (!string.IsNullOrEmpty(stackTrace))
                            {
                                job.compilationLogs.Add($"Stack trace: {stackTrace}");
                            }
                            SaveJobToSessionState(job);
                        }
                    }
                }
            }
        }
        
        private static List<string> FilterLogs(List<string> logs, string logLevel)
        {
            if (logLevel == "all")
            {
                return new List<string>(logs);
            }
            
            var filtered = new List<string>();
            foreach (var log in logs)
            {
                bool include = false;
                
                switch (logLevel)
                {
                    case "error":
                        include = log.Contains("[ERROR]") || log.Contains("[Exception]");
                        break;
                    case "warning":
                        include = log.Contains("[ERROR]") || log.Contains("[Exception]") || log.Contains("[WARNING]");
                        break;
                }
                
                if (include)
                {
                    filtered.Add(log);
                }
            }
            
            return filtered;
        }
    }
}