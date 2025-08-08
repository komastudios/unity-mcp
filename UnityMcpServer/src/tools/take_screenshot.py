from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, Optional, Tuple
from unity_connection import get_unity_connection
import base64
from io import BytesIO
from PIL import Image as PILImage

def register_take_screenshot_tools(mcp: FastMCP):
    """Register screenshot tools with the MCP server."""

    @mcp.tool()
    def take_screenshot(
        ctx: Context,
        view: Optional[str] = None,  # 'scene' or 'game', None for auto-detect
        width: Optional[int] = None,  # Optional custom width
        height: Optional[int] = None,  # Optional custom height
        max_size: Optional[int] = None,  # Optional max dimension (width or height)
        save_to_path: Optional[str] = None,  # Optional path to save screenshot locally
        compress: bool = True,  # Whether to compress if > 1MB
        format: str = "png"  # Output format: 'png' or 'jpeg'
    ) -> Dict[str, Any]:
        """Takes a screenshot of the Unity editor Scene or Game view.
        
        Args:
            view: Which view to capture ('scene' or 'game'). If None, defaults to:
                  - 'game' if in play mode
                  - 'scene' if in edit mode
            width: Optional width to resize the screenshot to
            height: Optional height to resize the screenshot to
            max_size: Optional max dimension - image will be scaled down if either dimension exceeds this
            save_to_path: Optional path to save the screenshot in Unity project
            compress: Whether to compress the image if it exceeds 1MB
            format: Output format ('png' or 'jpeg')
            
        Returns:
            Dictionary containing:
            - image: Image object with the screenshot data
            - metadata: Dict with view type, dimensions, play mode status, etc.
        """
        try:
            # Prepare parameters
            params = {
                "view": view,
                "width": width,
                "height": height,
                "maxSize": max_size,
                "savePath": save_to_path,
                "format": format
            }
            params = {k: v for k, v in params.items() if v is not None}
            
            # Get Unity connection
            connection = get_unity_connection()
            if not connection:
                raise ConnectionError("Failed to get Unity connection. Is the editor running?")
            
            # Send command to Unity (use registry handler name)
            response = connection.send_command("HandleScreenshotTool", params)
            
            if not response.get("success"):
                raise Exception(response.get("error", "Failed to take screenshot"))
            
            # Get the base64 encoded image data
            image_data_base64 = response.get("data", {}).get("imageData")
            if not image_data_base64:
                raise Exception("No image data received from Unity")
            
            # Decode base64 to bytes
            image_bytes = base64.b64decode(image_data_base64)
            
            # Get image metadata from response
            response_data = response.get("data", {})
            actual_view = response_data.get("view", "unknown")
            is_play_mode = response_data.get("isPlayMode", False)
            image_width = response_data.get("width", 0)
            image_height = response_data.get("height", 0)
            original_size = len(image_bytes)
            
            # Process image if needed (compression or max_size)
            need_processing = (compress and original_size > 1024 * 1024) or max_size
            
            if need_processing:
                pil_image = PILImage.open(BytesIO(image_bytes))
                
                # Convert to RGB if necessary (for JPEG)
                if format.lower() == "jpeg" and pil_image.mode in ('RGBA', 'LA', 'P'):
                    background = PILImage.new('RGB', pil_image.size, (255, 255, 255))
                    if pil_image.mode == 'P':
                        pil_image = pil_image.convert('RGBA')
                    background.paste(pil_image, mask=pil_image.split()[-1] if pil_image.mode == 'RGBA' else None)
                    pil_image = background
                
                # Apply max_size if specified by user
                if max_size and (pil_image.width > max_size or pil_image.height > max_size):
                    pil_image.thumbnail((max_size, max_size), PILImage.Resampling.LANCZOS)
                # Otherwise apply default max dimension if compressing
                elif compress and original_size > 1024 * 1024:
                    max_dimension = 2048
                    if pil_image.width > max_dimension or pil_image.height > max_dimension:
                        pil_image.thumbnail((max_dimension, max_dimension), PILImage.Resampling.LANCZOS)
                
                # Save with compression
                output = BytesIO()
                if format.lower() == "jpeg":
                    pil_image.save(output, format="JPEG", quality=85, optimize=True)
                else:
                    pil_image.save(output, format="PNG", optimize=True)
                
                image_bytes = output.getvalue()
                
                # Update dimensions if image was resized
                image_width = pil_image.width
                image_height = pil_image.height
                
                # Log compression result
                compressed_size = len(image_bytes)
                print(f"Screenshot compressed from {original_size/1024:.1f}KB to {compressed_size/1024:.1f}KB")
            
            # Build metadata dictionary
            metadata = {
                "view": actual_view,
                "isPlayMode": is_play_mode,
                "width": image_width,
                "height": image_height,
                "format": format.lower(),
                "originalSizeBytes": original_size,
                "finalSizeBytes": len(image_bytes),
                "wasCompressed": original_size != len(image_bytes)
            }
            
            # Add save path if provided
            if save_to_path:
                metadata["savedToPath"] = response_data.get("savedPath", save_to_path)
            
            # Return image data as base64 string instead of Image object to avoid serialization issues
            return {
                "success": True,
                "image_data": base64.b64encode(image_bytes).decode('utf-8'),
                "metadata": metadata
            }
            
        except Exception as e:
            raise Exception(f"Failed to take screenshot: {str(e)}")