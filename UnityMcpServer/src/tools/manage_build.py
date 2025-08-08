"""
Unity Build and Deployment Management Tool

This tool provides comprehensive build and deployment management for Unity projects,
including build configuration, platform-specific builds, asset bundling, and deployment automation.
"""

import json
from typing import Dict, List, Any, Optional
from mcp import types
from mcp.server import Server

def register_build_tools(server: Server):
    """Register all build and deployment management tools"""
    
    @server.tool()
    async def manage_build(
        action: str = None,
        build_target: Optional[str] = None,
        build_path: Optional[str] = None,
        build_options: Optional[Dict[str, Any]] = None,
        scenes: Optional[List[str]] = None,
        development_build: Optional[bool] = None,
        script_debugging: Optional[bool] = None,
        compression: Optional[str] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity build operations and configurations.
        
        Actions:
        - create_build: Create a new build with specified settings
        - configure_build: Configure build settings and options
        - list_builds: List available build configurations
        - get_build_info: Get information about a specific build
        - delete_build: Delete a build configuration
        - build_project: Execute the build process
        - get_build_log: Get build log information
        - validate_build: Validate build settings and requirements
        
        Args:
            action: The build action to perform
            build_target: Target platform (Standalone, Android, iOS, WebGL, etc.)
            build_path: Output path for the build
            build_options: Additional build options and settings
            scenes: List of scenes to include in the build
            development_build: Whether to create a development build
            script_debugging: Enable script debugging
            compression: Compression method for the build
        """
        
        try:
            # Prepare command data
            command_data = {
                "action": action,
                "build_target": build_target,
                "build_path": build_path,
                "build_options": build_options or {},
                "scenes": scenes or [],
                "development_build": development_build,
                "script_debugging": script_debugging,
                "compression": compression,
                **kwargs
            }
            
            # Send command to Unity
            from ..core.unity_bridge import send_command_to_unity
            # Unity bridge expects 'HandleManageBuild' as the type in the registry
            result = await send_command_to_unity("HandleManageBuild", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"Build management result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text", 
                text=f"Error in build management: {str(e)}"
            )]
    
    @server.tool()
    async def asset_bundle_operations(
        action: str,
        bundle_name: Optional[str] = None,
        assets: Optional[List[str]] = None,
        build_path: Optional[str] = None,
        build_target: Optional[str] = None,
        compression: Optional[str] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity Asset Bundle operations.
        
        Actions:
        - create_bundle: Create a new asset bundle
        - add_assets: Add assets to an existing bundle
        - remove_assets: Remove assets from a bundle
        - build_bundles: Build all asset bundles
        - list_bundles: List all asset bundles
        - get_bundle_info: Get information about a specific bundle
        - delete_bundle: Delete an asset bundle
        - validate_bundles: Validate asset bundle dependencies
        
        Args:
            action: The asset bundle action to perform
            bundle_name: Name of the asset bundle
            assets: List of asset paths to include
            build_path: Output path for built bundles
            build_target: Target platform for bundles
            compression: Compression method for bundles
        """
        
        try:
            command_data = {
                "action": action,
                "bundle_name": bundle_name,
                "assets": assets or [],
                "build_path": build_path,
                "build_target": build_target,
                "compression": compression,
                **kwargs
            }
            
            from ..core.unity_bridge import send_command_to_unity
            result = await send_command_to_unity("HandleManageBuild", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"Asset bundle operation result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text",
                text=f"Error in asset bundle operations: {str(e)}"
            )]
    
    @server.tool()
    async def build_pipeline_operations(
        action: str,
        pipeline_name: Optional[str] = None,
        steps: Optional[List[Dict[str, Any]]] = None,
        triggers: Optional[List[str]] = None,
        environment: Optional[Dict[str, str]] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity build pipeline and automation.
        
        Actions:
        - create_pipeline: Create a new build pipeline
        - modify_pipeline: Modify an existing pipeline
        - run_pipeline: Execute a build pipeline
        - list_pipelines: List all build pipelines
        - get_pipeline_info: Get information about a pipeline
        - delete_pipeline: Delete a build pipeline
        - validate_pipeline: Validate pipeline configuration
        - get_pipeline_logs: Get pipeline execution logs
        
        Args:
            action: The pipeline action to perform
            pipeline_name: Name of the build pipeline
            steps: List of pipeline steps and configurations
            triggers: List of pipeline triggers (manual, git, schedule)
            environment: Environment variables for the pipeline
        """
        
        try:
            command_data = {
                "action": action,
                "pipeline_name": pipeline_name,
                "steps": steps or [],
                "triggers": triggers or [],
                "environment": environment or {},
                **kwargs
            }
            
            from ..core.unity_bridge import send_command_to_unity
            result = await send_command_to_unity("HandleManageBuild", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"Build pipeline operation result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text",
                text=f"Error in build pipeline operations: {str(e)}"
            )]
    
    @server.tool()
    async def deployment_operations(
        action: str,
        deployment_target: Optional[str] = None,
        build_path: Optional[str] = None,
        credentials: Optional[Dict[str, str]] = None,
        deployment_config: Optional[Dict[str, Any]] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity project deployment operations.
        
        Actions:
        - deploy_build: Deploy a build to specified target
        - configure_deployment: Configure deployment settings
        - list_deployments: List deployment configurations
        - get_deployment_status: Get status of a deployment
        - rollback_deployment: Rollback to previous deployment
        - validate_deployment: Validate deployment configuration
        - get_deployment_logs: Get deployment logs
        
        Args:
            action: The deployment action to perform
            deployment_target: Target platform/service (Steam, App Store, Google Play, etc.)
            build_path: Path to the build to deploy
            credentials: Deployment credentials and authentication
            deployment_config: Deployment-specific configuration
        """
        
        try:
            command_data = {
                "action": action,
                "deployment_target": deployment_target,
                "build_path": build_path,
                "credentials": credentials or {},
                "deployment_config": deployment_config or {},
                **kwargs
            }
            
            from ..core.unity_bridge import send_command_to_unity
            result = await send_command_to_unity("HandleManageBuild", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"Deployment operation result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text",
                text=f"Error in deployment operations: {str(e)}"
            )]
    
    return manage_build