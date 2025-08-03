import json
from ..unity_connection import get_unity_connection

async def send_command_to_unity(tool, data):
    connection = get_unity_connection()
    if not connection.sock:
        return {"success": False, "error": "Not connected to Unity"}

    command = {
        "tool": tool,
        "data": data
    }

    try:
        connection.sock.sendall(json.dumps(command).encode('utf-8'))
        response_data = connection.receive_full_response(connection.sock)
        return json.loads(response_data.decode('utf-8'))
    except Exception as e:
        return {"success": False, "error": str(e)}