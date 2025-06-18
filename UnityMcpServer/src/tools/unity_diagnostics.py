from typing import Dict, Any, Optional
from mcp.server.fastmcp import Context
import logging

logger = logging.getLogger("unity-mcp-server")

def register_unity_diagnostics_tools(mcp):
    """Register Unity diagnostics tools with the MCP server."""
    
    @mcp.tool()
    async def get_unity_diagnostics(ctx: Context) -> Dict[str, Any]:
        """
        Get diagnostic information about Unity Editor status and potential issues.
        
        This tool is useful when Unity connection fails or when troubleshooting
        Unity Editor issues. It analyzes Unity log files to detect:
        - Whether Unity is running
        - If Unity is in Safe Mode
        - Compiler errors preventing normal operation
        - Recent log file locations
        
        Returns:
            Dictionary containing diagnostic information:
            - unity_running: Whether Unity Editor appears to be running
            - safe_mode: Whether Unity is in Safe Mode
            - compiler_errors: List of recent compiler errors
            - log_files_found: Paths to Unity log files
            - connection_status: Current connection status to Unity
        """
        try:
            # Check if we have a log analyzer in the context
            log_analyzer = getattr(ctx, 'log_analyzer', None)
            if not log_analyzer:
                return {
                    "success": False,
                    "error": "Log analyzer not available",
                    "message": "Unity diagnostics system not initialized"
                }
            
            # Get diagnostic information
            diagnostics = log_analyzer.get_diagnostic_info()
            
            # Check connection status
            bridge = getattr(ctx, 'bridge', None)
            connection_status = "Not initialized"
            if bridge:
                try:
                    # Try to ping Unity
                    bridge.send_command("ping")
                    connection_status = "Connected"
                except:
                    connection_status = "Disconnected"
            else:
                connection_status = "No connection established"
            
            # Format the response
            result = {
                "unity_running": diagnostics['unity_running'],
                "safe_mode": diagnostics['safe_mode'],
                "safe_mode_log": diagnostics['safe_mode_log'],
                "connection_status": connection_status,
                "log_files_found": diagnostics['log_files_found'],
                "analysis_time": diagnostics['analysis_time']
            }
            
            # Add compiler errors if any
            if diagnostics['compiler_errors']:
                result['compiler_error_count'] = len(diagnostics['compiler_errors'])
                result['compiler_errors'] = []
                
                for error in diagnostics['compiler_errors'][:10]:  # Limit to 10 errors
                    error_summary = {
                        'error': error.get('line', 'Unknown error'),
                        'log_file': error.get('log_file', 'Unknown')
                    }
                    
                    if 'file' in error:
                        error_summary['script_file'] = error['file']
                        error_summary['location'] = f"Line {error.get('line_number', '?')}, Column {error.get('column', '?')}"
                    
                    if 'error_code' in error:
                        error_summary['error_code'] = error['error_code']
                    
                    result['compiler_errors'].append(error_summary)
            
            # Provide actionable advice
            if diagnostics['safe_mode']:
                result['advice'] = (
                    "Unity is in Safe Mode due to compiler errors. "
                    "Fix the compiler errors listed above and restart Unity. "
                    "The MCP Bridge cannot load while Unity is in Safe Mode."
                )
            elif not diagnostics['unity_running']:
                result['advice'] = (
                    "Unity Editor does not appear to be running. "
                    "Please start Unity Editor with your project open."
                )
            elif connection_status == "Disconnected":
                result['advice'] = (
                    "Unity is running but the MCP Bridge is not responding. "
                    "Please ensure the MCP Bridge package is installed and "
                    "check the Unity console for any errors."
                )
            
            return {
                "success": True,
                "data": result
            }
            
        except Exception as e:
            logger.error(f"Error getting Unity diagnostics: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "message": "Failed to retrieve Unity diagnostics"
            }
    
    logger.info("Unity diagnostics tools registered")