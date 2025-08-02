from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from cache_manager import get_cache

def register_manage_audio_tools(mcp: FastMCP):
    """Register all audio management tools with the MCP server."""

    @mcp.tool()
    def manage_audio(
        ctx: Context,
        action: str,
        gameobject_name: str = None,
        audiosource_settings: Dict[str, Any] = None,
        audiomixer_settings: Dict[str, Any] = None,
        audioclip_settings: Dict[str, Any] = None,
        audiolistener_settings: Dict[str, Any] = None,
        reverb_zone_settings: Dict[str, Any] = None,
        audio_3d_settings: Dict[str, Any] = None,
        playback_settings: Dict[str, Any] = None
    ) -> Dict[str, Any]:
        """
        Manage audio components and operations in Unity.
        
        Actions:
        - add_audiosource: Add AudioSource component to GameObject
        - modify_audiosource: Modify existing AudioSource properties
        - play_audio: Play audio from AudioSource
        - stop_audio: Stop audio playback
        - pause_audio: Pause audio playback
        - create_audiomixer: Create AudioMixer asset
        - modify_audiomixer: Modify AudioMixer properties
        - set_3d_audio: Configure 3D spatial audio settings
        - get_audio_info: Get audio information from GameObject
        - import_audioclip: Import AudioClip asset
        - add_audiolistener: Add AudioListener component
        - set_reverb_zone: Configure AudioReverbZone properties
        """
        try:
            connection = get_unity_connection()
            
            command_data = {
                "type": "manage_audio",
                "params": {
                    "action": action,
                    "gameobject_name": gameobject_name,
                    "audiosource_settings": audiosource_settings,
                    "audiomixer_settings": audiomixer_settings,
                    "audioclip_settings": audioclip_settings,
                    "audiolistener_settings": audiolistener_settings,
                    "reverb_zone_settings": reverb_zone_settings,
                    "audio_3d_settings": audio_3d_settings,
                    "playback_settings": playback_settings
                }
            }
            
            response = connection.send_command(command_data)
            
            # Cache the response for potential future use
            cache = get_cache()
            cache_key = f"audio_{action}_{gameobject_name or 'global'}"
            cache.set(cache_key, response, ttl=300)  # 5 minutes TTL
            
            return response
            
        except Exception as e:
            return {
                "success": False,
                "error": f"Failed to manage audio: {str(e)}"
            }