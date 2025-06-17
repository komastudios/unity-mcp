from typing import Dict, Any

def fix_component_properties(params: Dict[str, Any]) -> Dict[str, Any]:
    """
    Fix component_properties to handle both flat and nested structures.
    
    When action is 'set_component_property', the component_properties should be
    nested under the component name. This function ensures proper structure.
    """
    action = params.get('action', '').lower()
    
    if action == 'set_component_property':
        component_name = params.get('componentName')
        component_properties = params.get('componentProperties')
        
        if component_name and component_properties:
            # Check if properties are already nested under component name
            if not isinstance(component_properties.get(component_name), dict):
                # Wrap the properties under the component name
                params['componentProperties'] = {component_name: component_properties}
    
    return params