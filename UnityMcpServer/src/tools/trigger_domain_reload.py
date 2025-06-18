"""Tool for triggering domain reload and script compilation in Unity."""

from typing import Dict, Any
import asyncio
import time
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection
import logging

logger = logging.getLogger(__name__)

def register_domain_reload_tools(mcp: FastMCP):
    """Register domain reload tool with the MCP server."""
    
    @mcp.tool()
    def trigger_domain_reload(
        ctx: Context,
        action: str = "compile_and_reload",
        wait_for_completion: bool = True,
        include_logs: bool = True,
        log_level: str = "all",
        timeout: float = 300,
        job_id: str = None
    ) -> Dict[str, Any]:
        """Triggers domain reload, script compilation, or asset refresh in Unity.
        
        Args:
            action: Operation to perform:
                    - 'refresh_assets': Refresh asset database
                    - 'compile_scripts': Request script compilation
                    - 'domain_reload': Force full domain reload
                    - 'compile_and_reload': Compile scripts then reload domain (default)
                    - 'get_status': Get status of a running job (requires job_id)
            wait_for_completion: Whether to wait for operation to complete (default: True)
                                When False, returns immediately with a job_id for polling
            include_logs: Include compilation logs in response (default: True)
            log_level: Minimum log level to include ('all', 'warning', 'error')
            timeout: Maximum seconds to wait for completion (default: 300)
            job_id: Job ID for status polling (only used with action='get_status')
            
        Returns:
            Dictionary with:
                - success: Whether the operation completed successfully
                - For new jobs: jobId, status='initiated', sessionId
                - For status checks: jobId, status, compilationErrors, logs, duration
        """
        try:
            connection = get_unity_connection()
            
            # Prepare command parameters
            params = {
                "action": action,
                "waitForCompletion": wait_for_completion,
                "includeLogs": include_logs,
                "logLevel": log_level,
                "timeout": timeout
            }
            
            # Add job_id for status polling
            if job_id is not None:
                params["jobId"] = job_id
            
            # Special case: if wait_for_completion is True and action is not get_status,
            # we need to implement our own polling loop
            if wait_for_completion and action != "get_status":
                logger.info(f"Triggering domain reload with action: {action} (synchronous)")
                
                # First, start the job
                params["waitForCompletion"] = False  # Force async on Unity side
                result = connection.send_command("trigger_domain_reload", params)
                
                if not result.get("success", False):
                    return {
                        "success": False,
                        "error": result.get("error", "Unknown error occurred"),
                        "message": result.get("message", "Failed to trigger domain reload")
                    }
                
                # Extract job ID from response
                data = result.get("data", {})
                job_id = data.get("jobId")
                
                if not job_id:
                    return {
                        "success": False,
                        "error": "No job ID returned from Unity",
                        "message": "Failed to start domain reload job"
                    }
                
                logger.info(f"Domain reload job started: {job_id}")
                
                # Now poll for completion
                poll_interval = 0.5  # Poll every 500ms
                elapsed = 0.0
                
                while elapsed < timeout:
                    # Wait before polling
                    time.sleep(poll_interval)
                    elapsed += poll_interval
                    
                    # Poll for status
                    status_params = {
                        "action": "get_status",
                        "jobId": job_id
                    }
                    
                    try:
                        status_result = connection.send_command("trigger_domain_reload", status_params)
                    except Exception as e:
                        # Connection might be lost during domain reload
                        logger.warning(f"Lost connection during polling (expected during domain reload): {e}")
                        time.sleep(2)  # Wait a bit longer before retrying
                        elapsed += 2
                        
                        # Try to reconnect
                        try:
                            connection = get_unity_connection()
                            continue
                        except:
                            logger.warning("Failed to reconnect, will keep trying...")
                            continue
                    
                    if not status_result.get("success", False):
                        # If we can't get status, it might be due to domain reload
                        logger.warning(f"Failed to get job status: {status_result.get('error', 'Unknown error')}")
                        continue
                    
                    status_data = status_result.get("data", {})
                    job_status = status_data.get("status", "unknown")
                    
                    # Check if job is complete
                    if job_status != "running":
                        # Job is done, return the full status
                        response = {
                            "success": status_data.get("compilationSucceeded", True),
                            "status": job_status,
                            "duration": status_data.get("duration", elapsed),
                            "message": status_data.get("message", "")
                        }
                        
                        # Add optional fields if present
                        if "compilationErrors" in status_data:
                            response["compilation_errors"] = status_data["compilationErrors"]
                        if "logs" in status_data:
                            response["logs"] = status_data["logs"]
                        if "wasCompiling" in status_data:
                            response["was_already_compiling"] = status_data["wasCompiling"]
                        if "hadToRefresh" in status_data:
                            response["had_to_refresh"] = status_data["hadToRefresh"]
                        
                        logger.info(f"Domain reload job {job_id} completed with status: {job_status}")
                        return response
                
                # Timeout reached
                return {
                    "success": False,
                    "status": "timeout",
                    "duration": elapsed,
                    "message": f"Operation timed out after {timeout} seconds",
                    "job_id": job_id
                }
                
            else:
                # Async mode or status polling - just pass through to Unity
                logger.info(f"Sending domain reload command: {action}")
                result = connection.send_command("trigger_domain_reload", params)
                
                if not result.get("success", False):
                    return {
                        "success": False,
                        "error": result.get("error", "Unknown error occurred"),
                        "message": result.get("message", "Failed to execute domain reload command")
                    }
                
                # Extract and format response data
                data = result.get("data", {})
                
                # For new jobs (async mode)
                if "jobId" in data and action != "get_status":
                    return {
                        "success": True,
                        "job_id": data.get("jobId"),
                        "status": data.get("status", "initiated"),
                        "message": data.get("message", "Domain reload job started"),
                        "session_id": data.get("sessionId"),
                        "was_already_compiling": data.get("wasCompiling", False),
                        "had_to_refresh": data.get("hadToRefresh", False)
                    }
                
                # For status checks
                else:
                    response = {
                        "success": data.get("compilationSucceeded", True),
                        "status": data.get("status", "unknown"),
                        "duration": data.get("duration", 0),
                        "message": data.get("message", "")
                    }
                    
                    # Add optional fields if present
                    if "jobId" in data:
                        response["job_id"] = data["jobId"]
                    if "compilationErrors" in data:
                        response["compilation_errors"] = data["compilationErrors"]
                    if "logs" in data:
                        response["logs"] = data["logs"]
                    if "wasCompiling" in data:
                        response["was_already_compiling"] = data["wasCompiling"]
                    if "hadToRefresh" in data:
                        response["had_to_refresh"] = data["hadToRefresh"]
                    if "sessionId" in data:
                        response["session_id"] = data["sessionId"]
                    
                    return response
            
        except ConnectionError as e:
            logger.error(f"Unity connection error: {e}")
            return {
                "success": False,
                "error": f"Unity connection error: {str(e)}",
                "message": "Failed to connect to Unity Editor"
            }
        except Exception as e:
            logger.error(f"Unexpected error in trigger_domain_reload: {e}")
            return {
                "success": False,
                "error": str(e),
                "message": "An unexpected error occurred"
            }