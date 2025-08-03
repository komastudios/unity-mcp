"""
Unity Performance and Profiling Management Tool

This tool provides comprehensive performance monitoring, profiling, and optimization
capabilities for Unity projects, including memory profiling, CPU analysis, and performance metrics.
"""

import json
from typing import Dict, List, Any, Optional
from mcp import types
from mcp.server import Server

def register_performance_tools(server: Server):
    """Register all performance and profiling management tools"""
    
    @server.tool()
    async def manage_performance(
        action: str,
        profiler_target: Optional[str] = None,
        duration: Optional[float] = None,
        sample_rate: Optional[int] = None,
        categories: Optional[List[str]] = None,
        output_path: Optional[str] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity performance monitoring and profiling operations.
        
        Actions:
        - start_profiling: Start performance profiling session
        - stop_profiling: Stop current profiling session
        - get_profile_data: Get profiling data and metrics
        - analyze_performance: Analyze performance bottlenecks
        - list_profilers: List available profilers and tools
        - configure_profiler: Configure profiler settings
        - export_profile: Export profiling data to file
        - clear_profile_data: Clear profiling data
        
        Args:
            action: The performance action to perform
            profiler_target: Target for profiling (CPU, Memory, Rendering, etc.)
            duration: Duration of profiling session in seconds
            sample_rate: Sampling rate for profiling data
            categories: Categories to profile (scripts, rendering, audio, etc.)
            output_path: Path to export profiling data
        """
        
        try:
            # Prepare command data
            command_data = {
                "action": action,
                "profiler_target": profiler_target,
                "duration": duration,
                "sample_rate": sample_rate,
                "categories": categories or [],
                "output_path": output_path,
                **kwargs
            }
            
            # Send command to Unity
            from ..core.unity_bridge import send_command_to_unity
            result = await send_command_to_unity("HandleManagePerformance", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"Performance management result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text", 
                text=f"Error in performance management: {str(e)}"
            )]
    
    @server.tool()
    async def memory_profiling_operations(
        action: str,
        snapshot_name: Optional[str] = None,
        comparison_snapshot: Optional[str] = None,
        filter_type: Optional[str] = None,
        threshold: Optional[int] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity memory profiling and analysis operations.
        
        Actions:
        - take_memory_snapshot: Take a memory snapshot
        - compare_snapshots: Compare two memory snapshots
        - analyze_memory_leaks: Analyze potential memory leaks
        - get_memory_usage: Get current memory usage statistics
        - list_snapshots: List all memory snapshots
        - delete_snapshot: Delete a memory snapshot
        - export_snapshot: Export snapshot data
        - optimize_memory: Get memory optimization suggestions
        
        Args:
            action: The memory profiling action to perform
            snapshot_name: Name for the memory snapshot
            comparison_snapshot: Snapshot to compare against
            filter_type: Filter by object type or category
            threshold: Memory threshold for analysis
        """
        
        try:
            command_data = {
                "action": action,
                "snapshot_name": snapshot_name,
                "comparison_snapshot": comparison_snapshot,
                "filter_type": filter_type,
                "threshold": threshold,
                **kwargs
            }
            
            from ..unity_bridge import send_command_to_unity
            result = await send_command_to_unity("memory_profiling_operations", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"Memory profiling operation result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text",
                text=f"Error in memory profiling operations: {str(e)}"
            )]
    
    @server.tool()
    async def cpu_profiling_operations(
        action: str,
        thread_filter: Optional[str] = None,
        function_filter: Optional[str] = None,
        min_time_threshold: Optional[float] = None,
        call_stack_depth: Optional[int] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity CPU profiling and performance analysis operations.
        
        Actions:
        - start_cpu_profiling: Start CPU profiling session
        - stop_cpu_profiling: Stop CPU profiling session
        - get_cpu_usage: Get current CPU usage statistics
        - analyze_hotspots: Analyze CPU performance hotspots
        - get_call_stack: Get detailed call stack information
        - profile_function: Profile specific function performance
        - get_thread_usage: Get per-thread CPU usage
        - optimize_cpu: Get CPU optimization suggestions
        
        Args:
            action: The CPU profiling action to perform
            thread_filter: Filter by specific thread
            function_filter: Filter by function name pattern
            min_time_threshold: Minimum time threshold for analysis
            call_stack_depth: Depth of call stack to analyze
        """
        
        try:
            command_data = {
                "action": action,
                "thread_filter": thread_filter,
                "function_filter": function_filter,
                "min_time_threshold": min_time_threshold,
                "call_stack_depth": call_stack_depth,
                **kwargs
            }
            
            from ..unity_bridge import send_command_to_unity
            result = await send_command_to_unity("cpu_profiling_operations", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"CPU profiling operation result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text",
                text=f"Error in CPU profiling operations: {str(e)}"
            )]
    
    @server.tool()
    async def rendering_profiling_operations(
        action: str,
        render_target: Optional[str] = None,
        quality_level: Optional[int] = None,
        resolution: Optional[str] = None,
        frame_count: Optional[int] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity rendering performance profiling operations.
        
        Actions:
        - start_render_profiling: Start rendering profiling session
        - stop_render_profiling: Stop rendering profiling session
        - get_render_stats: Get rendering performance statistics
        - analyze_draw_calls: Analyze draw call optimization
        - profile_shaders: Profile shader performance
        - get_gpu_usage: Get GPU usage statistics
        - analyze_batching: Analyze batching efficiency
        - optimize_rendering: Get rendering optimization suggestions
        
        Args:
            action: The rendering profiling action to perform
            render_target: Specific render target to profile
            quality_level: Quality settings level for profiling
            resolution: Screen resolution for profiling
            frame_count: Number of frames to profile
        """
        
        try:
            command_data = {
                "action": action,
                "render_target": render_target,
                "quality_level": quality_level,
                "resolution": resolution,
                "frame_count": frame_count,
                **kwargs
            }
            
            from ..unity_bridge import send_command_to_unity
            result = await send_command_to_unity("rendering_profiling_operations", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"Rendering profiling operation result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text",
                text=f"Error in rendering profiling operations: {str(e)}"
            )]
    
    @server.tool()
    async def performance_benchmarking(
        action: str,
        benchmark_type: Optional[str] = None,
        test_duration: Optional[float] = None,
        target_fps: Optional[int] = None,
        stress_test: Optional[bool] = None,
        **kwargs
    ) -> List[types.TextContent]:
        """
        Manage Unity performance benchmarking and stress testing operations.
        
        Actions:
        - run_benchmark: Run performance benchmark tests
        - create_benchmark: Create custom benchmark configuration
        - compare_benchmarks: Compare benchmark results
        - stress_test: Run stress testing scenarios
        - get_benchmark_results: Get benchmark test results
        - export_benchmark: Export benchmark data
        - validate_performance: Validate performance against targets
        - generate_report: Generate performance report
        
        Args:
            action: The benchmarking action to perform
            benchmark_type: Type of benchmark (CPU, GPU, Memory, etc.)
            test_duration: Duration of benchmark test
            target_fps: Target FPS for performance validation
            stress_test: Whether to run stress testing
        """
        
        try:
            command_data = {
                "action": action,
                "benchmark_type": benchmark_type,
                "test_duration": test_duration,
                "target_fps": target_fps,
                "stress_test": stress_test,
                **kwargs
            }
            
            from ..unity_bridge import send_command_to_unity
            result = await send_command_to_unity("performance_benchmarking", command_data)
            
            return [types.TextContent(
                type="text",
                text=f"Performance benchmarking result: {json.dumps(result, indent=2)}"
            )]
            
        except Exception as e:
            return [types.TextContent(
                type="text",
                text=f"Error in performance benchmarking: {str(e)}"
            )]