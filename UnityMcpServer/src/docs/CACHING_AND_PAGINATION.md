# Caching and Pagination Implementation

This document describes the technical implementation of the caching and pagination system in the Unity MCP Server.

## Overview

The caching system was implemented to handle MCP's token limitations when Unity returns large responses. Instead of failing when responses exceed limits, the system automatically caches large data and returns UUID references.

## Architecture

### Components

1. **cache_manager.py** - Core caching logic
   - `ResponseCache` class: Manages cached responses with expiration
   - UUID-based storage in memory
   - JQ filtering support via `python-jq` library
   - Automatic expiration after 30 minutes

2. **fetch_cached_response.py** - MCP tool for cache retrieval
   - Provides multiple actions: get, get_page, filter, info, list
   - Handles pagination requests
   - Applies JQ filters with optional result caching

3. **Tool Integration** - Automatic caching in response-heavy tools
   - `manage_scene.py`: Caches large scene hierarchies
   - `manage_gameobject.py`: Caches bulk find operations
   - `read_console.py`: Caches extensive console logs

## Implementation Details

### Automatic Caching Trigger

Tools estimate response size using token calculation:

```python
def estimate_tokens(data):
    """Estimate token count for data."""
    json_str = json.dumps(data, separators=(',', ':'))
    return len(json_str) // 4  # Rough estimate: 1 token ≈ 4 characters
```

When estimated tokens exceed 20,000, the response is cached:

```python
if estimated_tokens > 20000:
    cache = get_cache()
    metadata = {
        "tool": "manage_scene",
        "params": params,
        "size_bytes": len(json_str.encode('utf-8')),
        "estimated_tokens": estimated_tokens
    }
    cache_id = cache.add(data, metadata)
    
    return {
        "success": True,
        "cached": True,
        "cache_id": cache_id,
        "message": f"Response too large ({estimated_tokens} tokens). Data has been cached.",
        "data": {
            "cache_id": cache_id,
            "size_kb": len(json_str.encode('utf-8')) / 1024,
            "estimated_tokens": estimated_tokens,
            "usage_hint": "Use fetch_cached_response tool to retrieve the data"
        }
    }
```

### Pagination Algorithm

The pagination system intelligently handles different data structures:

#### For Arrays
```python
if isinstance(data, list):
    total_items = len(data)
    avg_item_size = total_bytes / total_items
    items_per_page = max(1, int(page_size_bytes / avg_item_size))
    
    start_idx = (page - 1) * items_per_page
    end_idx = min(start_idx + items_per_page, total_items)
    paginated_data = data[start_idx:end_idx]
```

#### For Objects with Array Fields
```python
elif isinstance(data, dict):
    # Find array fields (like 'hierarchy', 'items', etc.)
    array_fields = [(k, v) for k, v in data.items() if isinstance(v, list)]
    
    if array_fields:
        field_name, field_data = array_fields[0]
        # Paginate the array while preserving other fields
        paginated_data = {k: v for k, v in data.items() if k != field_name}
        paginated_data[field_name] = field_data[start_idx:end_idx]
        paginated_data['_pagination'] = {
            'field': field_name,
            'start_index': start_idx,
            'end_index': end_idx
        }
```

### JQ Filtering

JQ filters are applied using the `python-jq` library:

```python
def apply_jq_filter(self, cache_id: str, jq_filter: str, cache_result: bool = True):
    data = self.get(cache_id)
    
    # Apply JQ filter
    jq_program = jq.compile(jq_filter)
    filtered_data = jq_program.input(data).all()
    
    # If result is large and caching requested
    if cache_result and self._should_cache_result(filtered_data):
        # Cache the filtered result
        metadata = {
            "source_cache_id": cache_id,
            "jq_filter": jq_filter,
            "filtered_from": original_metadata.get("tool", "unknown")
        }
        new_cache_id = self.add(filtered_data, metadata)
        return new_cache_id
    
    return filtered_data
```

### Cache Expiration

Cached entries expire after 30 minutes:

```python
def add(self, data: Any, metadata: Optional[Dict[str, Any]] = None) -> str:
    cache_id = str(uuid.uuid4())
    expires_at = datetime.now() + timedelta(minutes=30)
    
    self._cache[cache_id] = {
        'data': data,
        'json_str': json.dumps(data, separators=(',', ':')),
        'metadata': metadata or {},
        'created_at': datetime.now(),
        'expires_at': expires_at,
        'access_count': 0
    }
```

## Usage Patterns

### Pattern 1: Direct Cache Access
```python
# Response indicates caching
response = {"cached": True, "cache_id": "uuid-here"}

# Retrieve full data (if small enough)
data = fetch_cached_response(cache_id="uuid-here", action="get")
```

### Pattern 2: Filtered Access
```python
# Extract specific fields without loading full data
names = fetch_cached_response(
    cache_id="uuid-here",
    action="filter",
    jq_filter=".hierarchy[].name"
)
```

### Pattern 3: Paginated Processing
```python
# Get first page info
info = fetch_cached_response(cache_id="uuid-here", action="info")
page_count = info["data"]["metadata"]["total_pages"]

# Process each page
for page_num in range(1, page_count + 1):
    page_data = fetch_cached_response(
        cache_id="uuid-here",
        action="get_page",
        page=page_num,
        page_size_kb=50
    )
    # Process page_data["data"]
```

### Pattern 4: Chained Filtering
```python
# Apply complex filter that returns large result
filtered_id = fetch_cached_response(
    cache_id="original-uuid",
    action="filter",
    jq_filter=".hierarchy | map(select(.componentNames | contains([\"Collider\"])))",
    cache_filtered_result=True
)

# If result was cached, apply another filter
if filtered_id["data"]["cached"]:
    names = fetch_cached_response(
        cache_id=filtered_id["data"]["cache_id"],
        action="filter",
        jq_filter=".[].name"
    )
```

## Performance Considerations

1. **Memory Usage**: All caches are held in memory. Large scenes may consume significant RAM.

2. **Token Estimation**: The 1:4 character-to-token ratio is approximate. Actual token counts may vary.

3. **JQ Performance**: Complex JQ filters on large datasets may take time. Consider caching filtered results.

4. **Pagination Overhead**: Each page request parses the full JSON. For very large datasets, consider using filters instead.

## Future Improvements

1. **Persistent Caching**: Store caches to disk for recovery after server restart
2. **Compression**: Compress cached JSON data to reduce memory usage
3. **Streaming**: Implement streaming for very large responses
4. **Smart Pagination**: Detect natural boundaries (e.g., complete objects) for cleaner page breaks
5. **Cache Warming**: Pre-cache common large queries
6. **LRU Eviction**: Implement least-recently-used eviction when memory limit reached

## Error Handling

The system handles various error cases:

- **Cache miss**: Returns error if cache_id not found or expired
- **Invalid JQ**: Returns error message with JQ syntax error details
- **Pagination bounds**: Returns empty data for out-of-range pages
- **Memory limits**: Currently unbounded (future improvement needed)

## Testing

Test scenarios covered:
1. Responses under threshold (no caching)
2. Responses over threshold (automatic caching)
3. Pagination with various page sizes
4. JQ filters returning small results
5. JQ filters returning large results (recursive caching)
6. Cache expiration after 30 minutes
7. Invalid cache IDs and malformed requests