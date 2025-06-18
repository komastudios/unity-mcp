# Domain Reload Tool Documentation

## Overview

The `trigger_domain_reload` tool provides a way to programmatically trigger Unity's domain reload, script compilation, and asset refresh operations through the MCP interface. This tool is essential for automated workflows that need to ensure scripts are compiled and the domain is reloaded after making changes to C# files.

## Features

- **Multiple reload actions**: Choose between asset refresh, script compilation, domain reload, or a combination
- **Synchronous/Asynchronous execution**: Wait for completion or trigger and continue
- **Compilation monitoring**: Track compilation progress and capture errors
- **Log streaming**: Capture and filter compilation logs
- **Timeout protection**: Configurable timeout to prevent hanging operations

## Usage

### Basic Example

```python
# Trigger a full compile and reload (default behavior)
result = await mcp__unityMCP__trigger_domain_reload(
    action="compile_and_reload"
)
```

### Parameters

- **action** (string): The operation to perform
  - `"refresh_assets"`: Refresh the asset database only
  - `"compile_scripts"`: Request script compilation only
  - `"domain_reload"`: Force domain reload only
  - `"compile_and_reload"`: Full compile and reload (default)

- **wait_for_completion** (boolean): Whether to wait for the operation to complete
  - Default: `true`
  - When `false`, returns immediately after initiating the operation

- **include_logs** (boolean): Include compilation logs in the response
  - Default: `true`
  - Only applies when `wait_for_completion` is `true`

- **log_level** (string): Minimum log level to include
  - `"all"`: Include all logs (default)
  - `"warning"`: Include warnings and errors only
  - `"error"`: Include errors only

- **timeout** (number): Maximum seconds to wait for completion
  - Default: `300` (5 minutes)
  - Maximum: `600` (10 minutes)

### Response Format

#### Success Response
```json
{
  "success": true,
  "status": "completed",
  "compilation_succeeded": true,
  "duration": 12.5,
  "was_already_compiling": false,
  "had_to_refresh": true,
  "message": "Domain reload completed successfully",
  "compilation_errors": [],
  "logs": [
    "[10:30:45] Compilation started",
    "[10:30:57] Assembly compiled: Assembly-CSharp.dll",
    "[10:30:57] Compilation finished"
  ]
}
```

#### Error Response with Compilation Errors
```json
{
  "success": true,
  "status": "failed",
  "compilation_succeeded": false,
  "duration": 8.3,
  "message": "Compilation failed with errors",
  "compilation_errors": [
    {
      "type": "Error",
      "message": "';' expected",
      "file": "Assets/Scripts/Player.cs",
      "line": 42,
      "column": 15
    }
  ]
}
```

## Example Workflows

### 1. After Creating/Modifying Scripts

```python
# Create or modify a script
await mcp__unityMCP__manage_script(
    action="create",
    name="NewFeature",
    path="Assets/Scripts/",
    contents="public class NewFeature : MonoBehaviour { }"
)

# Trigger compilation and wait for results
result = await mcp__unityMCP__trigger_domain_reload(
    action="compile_and_reload",
    wait_for_completion=True,
    include_logs=True
)

if not result["compilation_succeeded"]:
    print("Compilation failed!")
    for error in result["compilation_errors"]:
        print(f"{error['file']}({error['line']}): {error['message']}")
```

### 2. Quick Asset Refresh

```python
# Just refresh assets without full compilation
await mcp__unityMCP__trigger_domain_reload(
    action="refresh_assets",
    wait_for_completion=True,
    include_logs=False
)
```

### 3. Background Compilation

```python
# Start compilation in the background
await mcp__unityMCP__trigger_domain_reload(
    action="compile_scripts",
    wait_for_completion=False
)

# Do other work while Unity compiles...

# Later, check if compilation is done
editor_state = await mcp__unityMCP__manage_editor(action="get_state")
if not editor_state["data"]["isCompiling"]:
    print("Compilation complete!")
```

## Integration with CI/CD

The domain reload tool is particularly useful in CI/CD pipelines:

```python
# CI/CD build script example
async def build_project():
    # 1. Update scripts
    await update_all_scripts()
    
    # 2. Compile and check for errors
    result = await mcp__unityMCP__trigger_domain_reload(
        action="compile_and_reload",
        wait_for_completion=True,
        timeout=600  # 10 minutes for large projects
    )
    
    if not result["compilation_succeeded"]:
        raise BuildError("Compilation failed", result["compilation_errors"])
    
    # 3. Run tests
    await run_unity_tests()
    
    # 4. Build player
    await build_player()
```

## Performance Considerations

1. **Compilation Time**: Large projects may take several minutes to compile
2. **Memory Usage**: Domain reload clears and recreates all static data
3. **Editor State**: Some editor windows may reset during domain reload
4. **Asset Import**: Asset refresh can trigger reimport of modified assets

## Troubleshooting

### Common Issues

1. **Timeout Errors**
   - Increase the `timeout` parameter for large projects
   - Consider breaking up large script changes

2. **Compilation Loops**
   - Check for circular dependencies
   - Ensure no scripts are generating other scripts during compilation

3. **Missing Logs**
   - Ensure `include_logs` is `true`
   - Check Unity console for additional error details

### Debug Mode

For detailed debugging, use:

```python
result = await mcp__unityMCP__trigger_domain_reload(
    action="compile_and_reload",
    wait_for_completion=True,
    include_logs=True,
    log_level="all",
    timeout=300
)

# Print all logs
if "logs" in result:
    for log in result["logs"]:
        print(log)
```

## Best Practices

1. **Always check compilation status** before proceeding with other operations
2. **Use appropriate timeouts** based on project size
3. **Filter logs** to relevant level to reduce response size
4. **Handle compilation errors** gracefully in automated workflows
5. **Avoid frequent domain reloads** as they can impact editor performance

## Related Tools

- `manage_script`: Create and modify C# scripts
- `manage_asset`: Import and manage assets
- `read_console`: Read Unity console messages
- `manage_editor`: Check editor state (e.g., `isCompiling`)