# Unity MCP Server

A Model Context Protocol (MCP) server that enables AI assistants to interact with Unity Editor through a TCP bridge. This server provides tools for scene management, GameObject manipulation, asset operations, and more.

## Features

- **Scene Management**: Query and modify Unity scenes, hierarchies, and GameObjects
- **Asset Operations**: Create, modify, and search for Unity assets
- **Console Access**: Read Unity console logs and errors
- **Automatic Response Caching**: Large responses are automatically cached with UUID references
- **Pagination Support**: Retrieve cached data in manageable chunks
- **JQ Filtering**: Apply JQ queries to cached JSON data
- **Real-time Unity Integration**: Direct communication with Unity Editor via TCP

## Installation

1. Install the Unity package from `Packages/com.justinpbarnett.unity-mcp/`
2. Install Python dependencies:
   ```bash
   cd Tools/UnityMcpServer
   pip install -r requirements.txt
   ```
3. The Unity Editor will automatically start the TCP bridge on port 6400 when loaded

## Available Tools

### manage_scene
Manage Unity scenes with hierarchy queries, filtering, and automatic response optimization.

```python
# Get scene hierarchy with automatic depth adjustment
manage_scene(action="get_hierarchy", max_depth=3, auto_adjust_depth=True)

# Filter objects by name, tag, or components
manage_scene(
    action="get_hierarchy",
    name_filter="Player",
    tag_filter="Enemy",
    component_filters=["Rigidbody", "Collider"],
    traversal_order="breadth_first"
)
```

### manage_gameobject
Create, modify, find, and manage GameObjects and their components.

```python
# Find GameObjects
manage_gameobject(action="find", search_term="Player", find_all=True)

# Create GameObject with components
manage_gameobject(
    action="create",
    name="MyObject",
    position=[0, 1, 0],
    components_to_add=["Rigidbody", "BoxCollider"]
)

# Modify component properties
manage_gameobject(
    action="modify",
    target="MyObject",
    component_properties={
        "Rigidbody": {"mass": 10.0, "useGravity": True},
        "BoxCollider": {"size": [2, 2, 2]}
    }
)
```

### manage_asset
Perform asset operations including import, create, modify, and search.

```python
# Search for assets
manage_asset(action="search", path="Assets/", search_pattern="*.prefab")

# Create material
manage_asset(
    action="create",
    path="Materials/MyMaterial.mat",
    asset_type="Material",
    properties={"color": [1, 0, 0, 1], "shader": "Standard"}
)
```

### read_console
Read Unity Editor console messages with filtering options.

```python
# Get all console messages
read_console(action="get", types=["error", "warning", "log"], count=100)

# Get errors with stack traces
read_console(
    action="get",
    types=["error"],
    format="detailed",
    include_stacktrace=True
)
```

### execute_menu_item
Execute Unity Editor menu commands programmatically.

```python
# Save the project
execute_menu_item(menu_path="File/Save Project")

# Enter play mode
execute_menu_item(menu_path="Edit/Play")
```

### take_screenshot
Capture screenshots of Scene or Game view.

```python
# Take game view screenshot
take_screenshot(view="game", format="png", max_size=1920)

# Take scene view screenshot with compression
take_screenshot(view="scene", compress=True, save_to_path="Assets/Screenshots/scene.png")
```

## Caching System

Large responses are automatically cached to handle MCP token limits. When a response exceeds the threshold, the server returns a cache reference instead:

```json
{
  "success": true,
  "cached": true,
  "cache_id": "440afb48-3d0c-4c46-a3a8-a5c0c970ae1a",
  "message": "Response too large (34230 tokens). Data has been cached.",
  "data": {
    "cache_id": "440afb48-3d0c-4c46-a3a8-a5c0c970ae1a",
    "size_kb": 133,
    "estimated_tokens": 34230,
    "usage_hint": "Use fetch_cached_response tool to retrieve the data"
  }
}
```

### fetch_cached_response
Retrieve and manipulate cached data with various options.

#### List all cached responses
```python
fetch_cached_response(action="list")
```

#### Get cache information
```python
fetch_cached_response(
    cache_id="440afb48-3d0c-4c46-a3a8-a5c0c970ae1a",
    action="info"
)
```

#### Paginated retrieval
```python
# Get first page (10KB chunks)
fetch_cached_response(
    cache_id="440afb48-3d0c-4c46-a3a8-a5c0c970ae1a",
    action="get_page",
    page=1,
    page_size_kb=10
)

# Response includes pagination metadata
{
  "page": 1,
  "total_pages": 17,
  "total_items": 33,
  "items_per_page": 2,
  "has_next": true,
  "has_previous": false,
  "data": {...}  # Paginated subset
}
```

#### JQ filtering
```python
# Count objects in hierarchy
fetch_cached_response(
    cache_id="440afb48-3d0c-4c46-a3a8-a5c0c970ae1a",
    action="filter",
    jq_filter=".hierarchy | length"
)

# Find specific objects
fetch_cached_response(
    cache_id="440afb48-3d0c-4c46-a3a8-a5c0c970ae1a",
    action="filter",
    jq_filter='.hierarchy[] | select(.name | contains("Player"))'
)

# Complex filtering with automatic result caching
fetch_cached_response(
    cache_id="440afb48-3d0c-4c46-a3a8-a5c0c970ae1a",
    action="filter",
    jq_filter=".hierarchy | map(select(.children | length > 0))",
    cache_filtered_result=True  # Cache if result is large
)
```

## Usage Examples

### Example 1: Exploring Scene Hierarchy
```python
# 1. Get scene hierarchy (may be cached if large)
response = manage_scene(action="get_hierarchy", max_depth=5)

if response["cached"]:
    cache_id = response["cache_id"]
    
    # 2. Check how many root objects
    count = fetch_cached_response(
        cache_id=cache_id,
        action="filter",
        jq_filter=".hierarchy | length"
    )
    
    # 3. Find all objects with Rigidbody components
    physics_objects = fetch_cached_response(
        cache_id=cache_id,
        action="filter",
        jq_filter='.hierarchy[] | .. | select(.componentNames? and (.componentNames | contains(["Rigidbody"])))'
    )
    
    # 4. Get paginated view if needed
    first_page = fetch_cached_response(
        cache_id=cache_id,
        action="get_page",
        page=1,
        page_size_kb=50
    )
```

### Example 2: Batch GameObject Operations
```python
# Find all enemies
enemies = manage_gameobject(
    action="find",
    search_term="Enemy",
    tag="Enemy",
    find_all=True
)

# If response is cached due to size
if enemies.get("cached"):
    cache_id = enemies["cache_id"]
    
    # Get enemy names using JQ
    enemy_names = fetch_cached_response(
        cache_id=cache_id,
        action="filter",
        jq_filter=".[].name"
    )
```

### Example 3: Asset Search with Pagination
```python
# Search all prefabs
prefabs = manage_asset(
    action="search",
    path="Assets/",
    search_pattern="*.prefab"
)

# If many results, they'll be paginated automatically
if prefabs.get("pagination"):
    total_pages = prefabs["pagination"]["total_pages"]
    
    # Process each page
    for page in range(1, total_pages + 1):
        page_data = manage_asset(
            action="search",
            path="Assets/",
            search_pattern="*.prefab",
            page_number=page,
            page_size=50
        )
        # Process page_data["assets"]
```

## Configuration

### Cache Settings
- **Expiration**: Cached responses expire after 30 minutes
- **Token Threshold**: Responses over 20,000 estimated tokens are cached
- **Page Size**: Default pagination size is 1MB, configurable per request

### Performance Optimization
- Use `max_depth` and `max_objects` to limit hierarchy queries
- Apply filters (`name_filter`, `tag_filter`) to reduce response size
- Use `traversal_order="breadth_first"` for better object distribution
- Enable `auto_adjust_depth=True` for automatic response optimization

## Troubleshooting

### Large Response Errors
If you encounter "response exceeds maximum allowed tokens" errors:
1. The response should automatically cache (check for `cached: true`)
2. Use the provided `cache_id` with `fetch_cached_response`
3. Apply JQ filters to extract only needed data
4. Use pagination for sequential processing

### Connection Issues
- Ensure Unity Editor is running and the MCP Bridge is loaded
- Check Unity console for "[MCP] Listening on port 6400" message
- Verify no firewall blocking local TCP port 6400

### Cache Management
- Cached responses expire after 30 minutes
- Use `action="list"` to see all active caches
- Cache IDs are UUIDs that remain valid until expiration

## Architecture

```
Unity Editor
    ↓ (TCP Port 6400)
Unity MCP Bridge (C#)
    ↓ (JSON Protocol)
Unity MCP Server (Python)
    ↓ (MCP Protocol)
AI Assistant
```

The caching layer sits between the Unity MCP Server and AI Assistant, intercepting large responses and storing them with UUID references for later retrieval.