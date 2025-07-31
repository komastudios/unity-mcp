"""
Cache manager for large JSON responses from Unity MCP tools.
Provides UUID-based storage, pagination, and JQ filtering capabilities.
"""

import json
import uuid
import time
from typing import Dict, Any, Optional, Union, List
from datetime import datetime, timedelta
import jq


class ResponseCache:
    """Manages cached responses with UUID keys and expiration."""
    
    def __init__(self, max_size_mb: int = 100, expiration_minutes: int = 30):
        self._cache: Dict[str, Dict[str, Any]] = {}
        self._max_size_mb = max_size_mb
        self._expiration_minutes = expiration_minutes
        self._total_size_bytes = 0
        
    def add(self, data: Any, metadata: Optional[Dict[str, Any]] = None) -> str:
        """
        Add data to cache and return UUID key.
        
        Args:
            data: The data to cache (will be JSON serialized)
            metadata: Optional metadata about the cached item
            
        Returns:
            UUID string for retrieving the cached data
        """
        # Generate UUID
        cache_id = str(uuid.uuid4())
        
        # Serialize data to get size
        json_str = json.dumps(data)
        data_size = len(json_str.encode('utf-8'))
        
        # Check if we need to clean up old entries
        self._cleanup_expired()
        self._ensure_size_limit(data_size)
        
        # Store in cache
        self._cache[cache_id] = {
            'data': data,
            'json_str': json_str,
            'size_bytes': data_size,
            'created_at': datetime.now(),
            'expires_at': datetime.now() + timedelta(minutes=self._expiration_minutes),
            'metadata': metadata or {},
            'access_count': 0
        }
        
        self._total_size_bytes += data_size
        
        return cache_id
    
    def get(self, cache_id: str) -> Optional[Any]:
        """Retrieve cached data by UUID."""
        if cache_id not in self._cache:
            return None
            
        entry = self._cache[cache_id]
        
        # Check expiration
        if datetime.now() > entry['expires_at']:
            self._remove_entry(cache_id)
            return None
            
        # Update access count and timestamp
        entry['access_count'] += 1
        entry['last_accessed'] = datetime.now()
        
        return entry['data']
    
    def get_json_string(self, cache_id: str) -> Optional[str]:
        """Get the pre-serialized JSON string for a cached item."""
        if cache_id not in self._cache:
            return None
            
        entry = self._cache[cache_id]
        
        # Check expiration
        if datetime.now() > entry['expires_at']:
            self._remove_entry(cache_id)
            return None
            
        return entry['json_str']
    
    def get_page(self, cache_id: str, page: int = 1, page_size_bytes: int = 1024 * 1024) -> Optional[Dict[str, Any]]:
        """
        Get a paginated portion of cached data.
        
        Args:
            cache_id: UUID of cached data
            page: Page number (1-based)
            page_size_bytes: Maximum bytes per page
            
        Returns:
            Dict with page data, metadata, and pagination info
        """
        json_str = self.get_json_string(cache_id)
        if not json_str:
            return None
            
        # Parse the JSON to work with structured data
        data = self.get(cache_id)
        if not data:
            return None
            
        # Calculate pagination based on string representation
        total_bytes = len(json_str.encode('utf-8'))
        
        # For structured data pagination, we'll paginate based on top-level items
        # This works well for arrays and objects with array fields
        paginated_data = None
        items_per_page = None
        total_items = None
        
        # Check if data is a list
        if isinstance(data, list):
            total_items = len(data)
            # Estimate items per page based on average item size
            if total_items > 0:
                avg_item_size = total_bytes / total_items
                items_per_page = max(1, int(page_size_bytes / avg_item_size))
                
                start_idx = (page - 1) * items_per_page
                end_idx = min(start_idx + items_per_page, total_items)
                
                if start_idx < total_items:
                    paginated_data = data[start_idx:end_idx]
                else:
                    paginated_data = []
                    
        # Check if data is a dict with a main array field (like 'hierarchy', 'items', etc.)
        elif isinstance(data, dict):
            # Find the largest array field
            array_fields = [(k, v) for k, v in data.items() if isinstance(v, list)]
            
            if array_fields:
                # Use the first array field found
                field_name, field_data = array_fields[0]
                total_items = len(field_data)
                
                if total_items > 0:
                    avg_item_size = total_bytes / total_items
                    items_per_page = max(1, int(page_size_bytes / avg_item_size))
                    
                    start_idx = (page - 1) * items_per_page
                    end_idx = min(start_idx + items_per_page, total_items)
                    
                    if start_idx < total_items:
                        # Return a copy of the data with paginated array
                        paginated_data = {k: v for k, v in data.items() if k != field_name}
                        paginated_data[field_name] = field_data[start_idx:end_idx]
                        paginated_data['_pagination'] = {
                            'field': field_name,
                            'start_index': start_idx,
                            'end_index': end_idx
                        }
                    else:
                        paginated_data = {k: v for k, v in data.items() if k != field_name}
                        paginated_data[field_name] = []
            else:
                # No array fields, return the whole object if it fits
                if total_bytes <= page_size_bytes and page == 1:
                    paginated_data = data
                else:
                    paginated_data = {"message": "Data structure not suitable for pagination"}
        
        # Calculate total pages based on items
        if total_items is not None and items_per_page is not None:
            total_pages = (total_items + items_per_page - 1) // items_per_page
        else:
            total_pages = 1
            
        return {
            'cache_id': cache_id,
            'page': page,
            'total_pages': total_pages,
            'page_size_bytes': page_size_bytes,
            'total_bytes': total_bytes,
            'total_items': total_items,
            'items_per_page': items_per_page,
            'has_next': page < total_pages,
            'has_previous': page > 1,
            'data': paginated_data,
            'is_complete': total_pages == 1
        }
    
    def apply_jq_filter(self, cache_id: str, jq_filter: str, cache_result: bool = True) -> Union[str, Any]:
        """
        Apply a JQ filter to cached data.
        
        Args:
            cache_id: UUID of cached data
            jq_filter: JQ filter expression
            cache_result: If True and result is large, cache it and return new UUID
            
        Returns:
            Filtered data or UUID of cached filtered data
        """
        data = self.get(cache_id)
        if data is None:
            raise ValueError(f"Cache ID {cache_id} not found or expired")
            
        try:
            # Apply JQ filter
            jq_program = jq.compile(jq_filter)
            filtered_data = jq_program.input(data).all()
            
            # If single result, unwrap from list
            if len(filtered_data) == 1:
                filtered_data = filtered_data[0]
            
            # Check size of filtered result
            filtered_json = json.dumps(filtered_data)
            filtered_size = len(filtered_json.encode('utf-8'))
            
            # If result is large and caching is requested, cache it
            if cache_result and filtered_size > 100 * 1024:  # 100KB threshold
                metadata = {
                    'source_cache_id': cache_id,
                    'jq_filter': jq_filter,
                    'filtered_from': self._cache[cache_id]['metadata'].get('tool', 'unknown')
                }
                new_cache_id = self.add(filtered_data, metadata)
                return new_cache_id
            else:
                return filtered_data
                
        except Exception as e:
            raise ValueError(f"JQ filter error: {str(e)}")
    
    def get_info(self, cache_id: str) -> Optional[Dict[str, Any]]:
        """Get metadata about a cached item without retrieving the data."""
        if cache_id not in self._cache:
            return None
            
        entry = self._cache[cache_id]
        
        return {
            'cache_id': cache_id,
            'size_bytes': entry['size_bytes'],
            'size_mb': round(entry['size_bytes'] / (1024 * 1024), 2),
            'created_at': entry['created_at'].isoformat(),
            'expires_at': entry['expires_at'].isoformat(),
            'access_count': entry['access_count'],
            'metadata': entry['metadata']
        }
    
    def list_cached(self) -> List[Dict[str, Any]]:
        """List all cached items with their metadata."""
        items = []
        for cache_id in self._cache:
            info = self.get_info(cache_id)
            if info:
                items.append(info)
        return sorted(items, key=lambda x: x['created_at'], reverse=True)
    
    def _cleanup_expired(self):
        """Remove expired entries from cache."""
        now = datetime.now()
        expired_ids = [
            cache_id for cache_id, entry in self._cache.items()
            if now > entry['expires_at']
        ]
        for cache_id in expired_ids:
            self._remove_entry(cache_id)
    
    def _ensure_size_limit(self, new_size: int):
        """Ensure cache doesn't exceed size limit by removing oldest entries."""
        max_bytes = self._max_size_mb * 1024 * 1024
        
        while self._total_size_bytes + new_size > max_bytes and self._cache:
            # Remove least recently accessed item
            oldest_id = min(
                self._cache.keys(),
                key=lambda k: self._cache[k].get('last_accessed', self._cache[k]['created_at'])
            )
            self._remove_entry(oldest_id)
    
    def _remove_entry(self, cache_id: str):
        """Remove an entry from cache and update total size."""
        if cache_id in self._cache:
            self._total_size_bytes -= self._cache[cache_id]['size_bytes']
            del self._cache[cache_id]


# Global cache instances
_cache_instances: Dict[str, ResponseCache] = {}


def get_cache(name: str = 'default') -> ResponseCache:
    """Get a named cache instance."""
    if name not in _cache_instances:
        _cache_instances[name] = ResponseCache()
    
    return _cache_instances[name]