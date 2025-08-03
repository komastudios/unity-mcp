from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, List, Optional
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_networking_tools(mcp: FastMCP):
    """Register all networking and multiplayer management tools with the MCP server."""

    @mcp.tool()
    def manage_networking(
        ctx: Context,
        action: str,
        network_object_name: str = None,
        server_settings: Dict[str, Any] = None,
        client_settings: Dict[str, Any] = None,
        rpc_settings: Dict[str, Any] = None,
        sync_settings: Dict[str, Any] = None,
        room_settings: Dict[str, Any] = None,
        player_settings: Dict[str, Any] = None,
        connection_settings: Dict[str, Any] = None,
        page_size: int = 50,
        page_number: int = 1
    ) -> Dict[str, Any]:
        """
        Comprehensive networking and multiplayer management tool for Unity.
        
        Actions:
        - setup_server: Setup multiplayer server
        - setup_client: Setup multiplayer client
        - create_network_object: Create networked GameObject
        - add_network_identity: Add NetworkIdentity to GameObject
        - create_rpc: Create Remote Procedure Call
        - sync_variable: Setup synchronized variable
        - create_room: Create multiplayer room/lobby
        - join_room: Join multiplayer room
        - leave_room: Leave multiplayer room
        - send_message: Send network message
        - list_connections: Get all network connections
        - get_network_stats: Get networking statistics
        - disconnect_client: Disconnect specific client
        """
        
        try:
            # Get Unity connection
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            # Prepare command parameters
            params = {
                "action": action,
                "network_object_name": network_object_name,
                "server_settings": server_settings or {
                    "port": 7777,
                    "max_connections": 100,
                    "timeout": 30,
                    "tick_rate": 60,
                    "compression": True,
                    "encryption": False
                },
                "client_settings": client_settings or {
                    "server_address": "127.0.0.1",
                    "port": 7777,
                    "timeout": 10,
                    "auto_reconnect": True,
                    "reconnect_delay": 5.0
                },
                "rpc_settings": rpc_settings or {
                    "rpc_name": "DefaultRPC",
                    "target": "All",
                    "reliable": True,
                    "buffered": False,
                    "parameters": []
                },
                "sync_settings": sync_settings or {
                    "variable_name": "syncVar",
                    "sync_mode": "Observers",
                    "send_rate": 20,
                    "interpolate": True
                },
                "room_settings": room_settings or {
                    "room_name": "DefaultRoom",
                    "max_players": 10,
                    "is_visible": True,
                    "is_open": True,
                    "custom_properties": {}
                },
                "player_settings": player_settings or {
                    "player_name": "Player",
                    "custom_properties": {},
                    "is_master_client": False
                },
                "connection_settings": connection_settings or {
                    "connection_id": 0,
                    "disconnect_reason": "Manual"
                },
                "page_size": page_size,
                "page_number": page_number
            }
            
            # Send command to Unity
            result = unity_conn.send_command("manage_networking", params)
            
            # Cache the result if it's a list operation
            if action in ["list_connections", "get_network_stats"] and result.get("success"):
                cache = get_cache()
                cache_id = f"networking_{action}_{hash(str(params))}"
                cache.set(cache_id, result, expire_time=300)  # 5 minutes
                result["cache_id"] = cache_id
            
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in networking management: {str(e)}"
            }

    @mcp.tool()
    def network_transport_operations(
        ctx: Context,
        action: str,
        transport_type: str = "Unity Netcode",
        transport_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Network transport layer operations.
        
        Actions:
        - setup_transport: Configure network transport
        - start_host: Start as host (server + client)
        - start_server: Start dedicated server
        - start_client: Start client connection
        - stop_network: Stop all networking
        - get_transport_info: Get transport layer information
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"transport_{action}",
                "transport_type": transport_type,
                "transport_settings": transport_settings or {
                    "connection_data": {
                        "address": "127.0.0.1",
                        "port": 7777,
                        "server_listen_address": "0.0.0.0"
                    },
                    "timeout_settings": {
                        "connect_timeout": 10000,
                        "disconnect_timeout": 30000,
                        "heartbeat_timeout": 500
                    },
                    "performance_settings": {
                        "max_payload_size": 1024,
                        "send_queue_capacity": 512,
                        "receive_queue_capacity": 512
                    }
                }
            }
            
            result = unity_conn.send_command("manage_networking", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in transport operations: {str(e)}"
            }

    @mcp.tool()
    def network_object_operations(
        ctx: Context,
        action: str,
        object_name: str = None,
        network_settings: Dict[str, Any] = None,
        ownership_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Network object management operations.
        
        Actions:
        - spawn_network_object: Spawn networked object
        - despawn_network_object: Despawn networked object
        - change_ownership: Change object ownership
        - request_ownership: Request object ownership
        - list_network_objects: List all networked objects
        - get_object_info: Get network object information
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"network_object_{action}",
                "object_name": object_name,
                "network_settings": network_settings or {
                    "spawn_with_observers": True,
                    "destroy_with_scene": True,
                    "dont_destroy_with_owner": False,
                    "auto_object_parent_sync": True
                },
                "ownership_settings": ownership_settings or {
                    "owner_client_id": 0,
                    "transfer_ownership": True
                }
            }
            
            result = unity_conn.send_command("manage_networking", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in network object operations: {str(e)}"
            }

    @mcp.tool()
    def rpc_operations(
        ctx: Context,
        action: str,
        rpc_name: str = None,
        rpc_parameters: List[Any] = None,
        target_settings: Dict[str, Any] = None,
        delivery_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Remote Procedure Call (RPC) operations.
        
        Actions:
        - create_rpc: Create new RPC method
        - call_rpc: Execute RPC call
        - register_rpc_handler: Register RPC message handler
        - unregister_rpc_handler: Unregister RPC handler
        - list_rpcs: List all available RPCs
        - get_rpc_stats: Get RPC performance statistics
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"rpc_{action}",
                "rpc_name": rpc_name,
                "rpc_parameters": rpc_parameters or [],
                "target_settings": target_settings or {
                    "target_type": "All",
                    "client_ids": [],
                    "exclude_owner": False
                },
                "delivery_settings": delivery_settings or {
                    "delivery_method": "Reliable",
                    "group_id": 0,
                    "defer_mode": "None"
                }
            }
            
            result = unity_conn.send_command("manage_networking", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in RPC operations: {str(e)}"
            }

    @mcp.tool()
    def lobby_operations(
        ctx: Context,
        action: str,
        lobby_settings: Dict[str, Any] = None,
        player_data: Dict[str, Any] = None,
        matchmaking_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Lobby and matchmaking operations.
        
        Actions:
        - create_lobby: Create new lobby/room
        - join_lobby: Join existing lobby
        - leave_lobby: Leave current lobby
        - update_lobby: Update lobby settings
        - list_lobbies: List available lobbies
        - start_matchmaking: Start matchmaking process
        - cancel_matchmaking: Cancel matchmaking
        - kick_player: Kick player from lobby
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"lobby_{action}",
                "lobby_settings": lobby_settings or {
                    "lobby_name": "DefaultLobby",
                    "max_players": 8,
                    "is_private": False,
                    "password": "",
                    "game_mode": "Default",
                    "map": "DefaultMap",
                    "custom_properties": {}
                },
                "player_data": player_data or {
                    "player_name": "Player",
                    "player_id": "",
                    "custom_properties": {},
                    "ready_status": False
                },
                "matchmaking_settings": matchmaking_settings or {
                    "skill_level": 1000,
                    "region": "auto",
                    "game_mode": "Default",
                    "max_wait_time": 60
                }
            }
            
            result = unity_conn.send_command("manage_networking", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in lobby operations: {str(e)}"
            }

    @mcp.tool()
    def network_diagnostics(
        ctx: Context,
        action: str,
        diagnostic_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Network diagnostics and monitoring.
        
        Actions:
        - get_network_stats: Get comprehensive network statistics
        - get_connection_quality: Get connection quality metrics
        - run_latency_test: Run network latency test
        - run_bandwidth_test: Run bandwidth test
        - get_packet_loss: Get packet loss statistics
        - monitor_network: Start/stop network monitoring
        """
        
        try:
            unity_conn = get_unity_connection()
            if not unity_conn:
                return {"success": False, "message": "Failed to connect to Unity"}
            
            params = {
                "action": f"diagnostics_{action}",
                "diagnostic_settings": diagnostic_settings or {
                    "monitor_duration": 30,
                    "sample_rate": 1.0,
                    "include_detailed_stats": True,
                    "log_to_file": False
                }
            }
            
            result = unity_conn.send_command("manage_networking", params)
            return result
            
        except Exception as e:
            return {
                "success": False,
                "message": f"Error in network diagnostics: {str(e)}"
            }