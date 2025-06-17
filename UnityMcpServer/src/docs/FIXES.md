# Unity MCP Server Fixes

## Issue 1: Component Properties Validation Error

**Problem**: When using `set_component_property` action, the MCP server expected all properties to be nested dictionaries, but Unity components often have simple property values (strings, numbers, booleans).

**Error**:
```
Error executing tool manage_gameobject: 3 validation errors for manage_gameobjectArguments
component_properties.text
  Input should be a valid dictionary [type=dict_type, input_value='Vehicle Connection', input_type=str]
```

**Fix**: Added `manage_gameobject_fix.py` that automatically wraps component properties under the component name for the `set_component_property` action. This ensures compatibility between the Python validation and Unity's expectations.

**Example Usage**:
```python
# Before fix (would fail):
manage_gameobject(
    action="set_component_property",
    target="MyGameObject",
    component_name="TextMeshProUGUI",
    component_properties={"text": "Hello World", "fontSize": 24}
)

# After fix (automatically handled):
# The properties are wrapped as: {"TextMeshProUGUI": {"text": "Hello World", "fontSize": 24}}
```

## Issue 2: Oversized get_hierarchy Response

**Problem**: The `get_hierarchy` action in `manage_scene` returned the entire scene hierarchy recursively, which could result in responses exceeding 300-500KB for complex scenes, causing the AI agent to fail processing.

**Fix**: 
1. Created `ManageSceneFix.cs` with depth-limited hierarchy traversal
2. Added parameters to control hierarchy depth and component inclusion
3. Default depth limit set to 2 levels to keep responses manageable

**New Parameters**:
- `maxDepth` (int, default: 2): Maximum depth to traverse the hierarchy
- `includeComponents` (bool, default: false): Whether to include component names

**Example Usage**:
```python
# Get shallow hierarchy (2 levels)
manage_scene(action="get_hierarchy")

# Get deeper hierarchy if needed
manage_scene(action="get_hierarchy", maxDepth=4, includeComponents=True)
```

**Additional Feature**: Added `GetSceneObjectsList` method for paginated flat list of all scene objects as an alternative to hierarchical view.

## Installation

1. The Python fix is automatically loaded when the MCP server starts
2. The Unity fix requires recompiling the Unity project (happens automatically when Unity detects the new files)

## Issue 3: MCP Token Limit Exceeded for Large Responses

**Problem**: MCP framework has a token limit (25,000) that would cause failures when Unity returned large responses, even with depth limiting. Complex scenes could still exceed this limit.

**Fix**: Implemented automatic response caching system with UUID references and retrieval tools.

**Components**:
1. **Automatic Caching**: Responses over 20,000 estimated tokens are cached automatically
2. **UUID References**: Large responses return a cache ID instead of the data
3. **fetch_cached_response Tool**: New tool to retrieve cached data with pagination and filtering
4. **JQ Filtering**: Apply JQ queries to extract specific data without loading full response

**Example Usage**:
```python
# Large response automatically cached
response = manage_scene(action="get_hierarchy", max_depth=5)
# Returns: {"cached": true, "cache_id": "uuid-here", "size_kb": 133}

# Retrieve with pagination
page1 = fetch_cached_response(
    cache_id="uuid-here",
    action="get_page",
    page=1,
    page_size_kb=50
)

# Or use JQ filtering
objects_with_colliders = fetch_cached_response(
    cache_id="uuid-here",
    action="filter",
    jq_filter='.hierarchy[] | select(.componentNames | contains(["Collider"]))'
)
```

**Additional Improvements**:
- **Breadth-First Traversal**: Better object distribution across pages
- **Smart Pagination**: Automatically calculates items per page based on size
- **Filter Caching**: Large filter results can be cached with new UUIDs
- **Cache Expiration**: Automatic cleanup after 30 minutes

See `docs/CACHING_AND_PAGINATION.md` for detailed technical documentation.

## Prevention

To prevent similar issues in the future:
1. Always validate response sizes before sending from Unity
2. Add pagination or depth limits to any recursive data structures
3. Consider the difference between Python's strict typing and C#'s flexible property system when designing APIs
4. Implement caching strategies for potentially large responses
5. Provide filtering mechanisms to extract only needed data