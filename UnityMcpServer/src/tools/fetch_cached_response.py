from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, Optional
from cache_manager import get_cache

def register_fetch_cached_response_tools(mcp: FastMCP):
    """Register cache management tools with the MCP server."""
    
    @mcp.tool()
    def fetch_cached_response(
        ctx: Context,
        cache_id: str = None,
        action: str = "get",
        page: int = 1,
        page_size_kb: int = 1024,
        jq_filter: str = None,
        cache_filtered_result: bool = True
    ) -> Dict[str, Any]:
        """Fetch cached response data with pagination or JQ filtering.
        
        Args:
            cache_id: UUID of the cached response
            action: Operation to perform ('get', 'get_page', 'filter', 'info', 'list')
            page: Page number for pagination (1-based)
            page_size_kb: Page size in KB for pagination
            jq_filter: JQ filter expression to apply to cached data
            cache_filtered_result: Whether to cache large filtered results
            
        Returns:
            Dictionary with requested data or operation result
        """
        cache = get_cache()
        
        try:
            if action == "list":
                # List all cached items
                items = cache.list_cached()
                return {
                    "success": True,
                    "message": f"Found {len(items)} cached items",
                    "data": {
                        "count": len(items),
                        "total_size_mb": sum(item['size_mb'] for item in items),
                        "items": items
                    }
                }
                
            # For non-list actions, cache_id is required
            if not cache_id:
                return {
                    "success": False,
                    "message": f"cache_id is required for action '{action}'"
                }
                
            elif action == "info":
                # Get info about a specific cached item
                info = cache.get_info(cache_id)
                if info is None:
                    return {
                        "success": False,
                        "message": f"Cache ID {cache_id} not found or expired"
                    }
                return {
                    "success": True,
                    "message": "Cache info retrieved",
                    "data": info
                }
                
            elif action == "get":
                # Get full cached data
                data = cache.get(cache_id)
                if data is None:
                    return {
                        "success": False,
                        "message": f"Cache ID {cache_id} not found or expired"
                    }
                    
                # Check size before returning
                import json
                data_str = json.dumps(data)
                size_kb = len(data_str.encode('utf-8')) / 1024
                
                if size_kb > 100:  # Still too large
                    return {
                        "success": False,
                        "message": f"Cached data is still too large ({size_kb:.1f} KB). Use pagination or filtering.",
                        "data": {
                            "cache_id": cache_id,
                            "size_kb": size_kb,
                            "suggestions": {
                                "use_pagination": "Set action='get_page' with appropriate page_size_kb",
                                "use_filter": "Set action='filter' with a JQ expression to extract specific data"
                            }
                        }
                    }
                    
                return {
                    "success": True,
                    "message": "Cached data retrieved",
                    "data": data
                }
                
            elif action == "get_page":
                # Get paginated data
                page_size_bytes = page_size_kb * 1024
                page_data = cache.get_page(cache_id, page, page_size_bytes)
                
                if page_data is None:
                    return {
                        "success": False,
                        "message": f"Failed to get page {page} for cache ID {cache_id}"
                    }
                    
                return {
                    "success": True,
                    "message": f"Retrieved page {page} of {page_data['total_pages']}",
                    "data": page_data
                }
                
            elif action == "filter":
                # Apply JQ filter
                if not jq_filter:
                    return {
                        "success": False,
                        "message": "JQ filter expression is required for filter action"
                    }
                    
                try:
                    result = cache.apply_jq_filter(cache_id, jq_filter, cache_filtered_result)
                    
                    # Check if result is a UUID (cached)
                    if isinstance(result, str) and len(result) == 36 and result.count('-') == 4:
                        return {
                            "success": True,
                            "message": "Filtered result was large and has been cached",
                            "data": {
                                "cached": True,
                                "cache_id": result,
                                "jq_filter": jq_filter,
                                "hint": "Use the returned cache_id to fetch the filtered data"
                            }
                        }
                    else:
                        return {
                            "success": True,
                            "message": "JQ filter applied successfully",
                            "data": {
                                "cached": False,
                                "result": result,
                                "jq_filter": jq_filter
                            }
                        }
                        
                except ValueError as e:
                    return {
                        "success": False,
                        "message": str(e),
                        "data": {
                            "cache_id": cache_id,
                            "jq_filter": jq_filter
                        }
                    }
                    
            else:
                return {
                    "success": False,
                    "message": f"Unknown action: {action}. Valid actions: get, get_page, filter, info, list"
                }
                
        except Exception as e:
            return {
                "success": False,
                "message": f"Error processing cached response: {str(e)}"
            }