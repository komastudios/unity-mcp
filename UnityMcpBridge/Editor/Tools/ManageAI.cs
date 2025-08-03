using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System;

namespace UnityMcpBridge.Tools
{
    public static class ManageAI
    {
        public static JObject HandleAICommand(JObject parameters)
        {
            try
            {
                string action = parameters["action"]?.ToString();
                
                return action switch
                {
                    "create_navmesh_agent" => CreateNavMeshAgent(parameters),
                    "modify_agent" => ModifyAgent(parameters),
                    "set_destination" => SetDestination(parameters),
                    "create_behavior_tree" => CreateBehaviorTree(parameters),
                    "create_state_machine" => CreateStateMachine(parameters),
                    "add_obstacle" => AddObstacle(parameters),
                    "create_navmesh_area" => CreateNavMeshArea(parameters),
                    "bake_navmesh" => BakeNavMesh(parameters),
                    "setup_crowd_simulation" => SetupCrowdSimulation(parameters),
                    "list_agents" => ListAgents(parameters),
                    "get_agent_info" => GetAgentInfo(parameters),
                    "delete_agent" => DeleteAgent(parameters),
                    
                    // NavMesh operations
                    "navmesh_bake_navmesh" => BakeNavMesh(parameters),
                    "navmesh_clear_navmesh" => ClearNavMesh(parameters),
                    "navmesh_save_navmesh" => SaveNavMesh(parameters),
                    "navmesh_load_navmesh" => LoadNavMesh(parameters),
                    "navmesh_get_navmesh_info" => GetNavMeshInfo(parameters),
                    
                    // Behavior tree operations
                    "behavior_tree_create_behavior_tree" => CreateBehaviorTree(parameters),
                    "behavior_tree_modify_behavior_tree" => ModifyBehaviorTree(parameters),
                    "behavior_tree_add_node" => AddBehaviorTreeNode(parameters),
                    "behavior_tree_remove_node" => RemoveBehaviorTreeNode(parameters),
                    "behavior_tree_connect_nodes" => ConnectBehaviorTreeNodes(parameters),
                    "behavior_tree_save_tree" => SaveBehaviorTree(parameters),
                    "behavior_tree_load_tree" => LoadBehaviorTree(parameters),
                    
                    // Pathfinding operations
                    "pathfinding_calculate_path" => CalculatePath(parameters),
                    "pathfinding_sample_position" => SamplePosition(parameters),
                    "pathfinding_raycast" => NavMeshRaycast(parameters),
                    "pathfinding_find_closest_edge" => FindClosestEdge(parameters),
                    "pathfinding_get_area_cost" => GetAreaCost(parameters),
                    "pathfinding_set_area_cost" => SetAreaCost(parameters),
                    
                    _ => new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"Unknown AI action: {action}"
                    }
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Error handling AI command: {e.Message}"
                };
            }
        }

        private static JObject CreateNavMeshAgent(JObject parameters)
        {
            try
            {
                string agentName = parameters["agent_name"]?.ToString() ?? "NavMeshAgent";
                var agentSettings = parameters["agent_settings"]?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                
                // Create GameObject with NavMeshAgent
                GameObject agentObj = new GameObject(agentName);
                NavMeshAgent agent = agentObj.AddComponent<NavMeshAgent>();
                
                // Configure agent settings
                if (agentSettings.ContainsKey("speed"))
                    agent.speed = Convert.ToSingle(agentSettings["speed"]);
                if (agentSettings.ContainsKey("angular_speed"))
                    agent.angularSpeed = Convert.ToSingle(agentSettings["angular_speed"]);
                if (agentSettings.ContainsKey("acceleration"))
                    agent.acceleration = Convert.ToSingle(agentSettings["acceleration"]);
                if (agentSettings.ContainsKey("stopping_distance"))
                    agent.stoppingDistance = Convert.ToSingle(agentSettings["stopping_distance"]);
                if (agentSettings.ContainsKey("auto_traverse_off_mesh_link"))
                    agent.autoTraverseOffMeshLink = Convert.ToBoolean(agentSettings["auto_traverse_off_mesh_link"]);
                if (agentSettings.ContainsKey("auto_repath"))
                    agent.autoRepath = Convert.ToBoolean(agentSettings["auto_repath"]);
                if (agentSettings.ContainsKey("radius"))
                    agent.radius = Convert.ToSingle(agentSettings["radius"]);
                if (agentSettings.ContainsKey("height"))
                    agent.height = Convert.ToSingle(agentSettings["height"]);
                if (agentSettings.ContainsKey("priority"))
                    agent.avoidancePriority = Convert.ToInt32(agentSettings["priority"]);
                
                // Register undo
                Undo.RegisterCreatedObjectUndo(agentObj, "Create NavMesh Agent");
                
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"NavMesh agent '{agentName}' created successfully",
                    ["agent_id"] = agentObj.GetInstanceID(),
                    ["agent_name"] = agentName
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to create NavMesh agent: {e.Message}"
                };
            }
        }

        private static JObject ModifyAgent(JObject parameters)
        {
            try
            {
                string agentName = parameters["agent_name"]?.ToString();
                var agentSettings = parameters["agent_settings"]?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                
                GameObject agentObj = GameObject.Find(agentName);
                if (agentObj == null)
                {
                    return new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"NavMesh agent '{agentName}' not found"
                    };
                }
                
                NavMeshAgent agent = agentObj.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    return new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"GameObject '{agentName}' does not have NavMeshAgent component"
                    };
                }
                
                Undo.RecordObject(agent, "Modify NavMesh Agent");
                
                // Apply settings
                foreach (var setting in agentSettings)
                {
                    switch (setting.Key)
                    {
                        case "speed":
                            agent.speed = Convert.ToSingle(setting.Value);
                            break;
                        case "angular_speed":
                            agent.angularSpeed = Convert.ToSingle(setting.Value);
                            break;
                        case "acceleration":
                            agent.acceleration = Convert.ToSingle(setting.Value);
                            break;
                        case "stopping_distance":
                            agent.stoppingDistance = Convert.ToSingle(setting.Value);
                            break;
                        case "auto_traverse_off_mesh_link":
                            agent.autoTraverseOffMeshLink = Convert.ToBoolean(setting.Value);
                            break;
                        case "auto_repath":
                            agent.autoRepath = Convert.ToBoolean(setting.Value);
                            break;
                        case "radius":
                            agent.radius = Convert.ToSingle(setting.Value);
                            break;
                        case "height":
                            agent.height = Convert.ToSingle(setting.Value);
                            break;
                        case "priority":
                            agent.avoidancePriority = Convert.ToInt32(setting.Value);
                            break;
                    }
                }
                
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"NavMesh agent '{agentName}' modified successfully"
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to modify NavMesh agent: {e.Message}"
                };
            }
        }

        private static JObject SetDestination(JObject parameters)
        {
            try
            {
                string agentName = parameters["agent_name"]?.ToString();
                var destination = parameters["destination"]?.ToObject<float[]>() ?? new float[] { 0, 0, 0 };
                
                GameObject agentObj = GameObject.Find(agentName);
                if (agentObj == null)
                {
                    return new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"NavMesh agent '{agentName}' not found"
                    };
                }
                
                NavMeshAgent agent = agentObj.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    return new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"GameObject '{agentName}' does not have NavMeshAgent component"
                    };
                }
                
                Vector3 targetPos = new Vector3(destination[0], destination[1], destination[2]);
                bool pathSet = agent.SetDestination(targetPos);
                
                return new JObject
                {
                    ["success"] = pathSet,
                    ["message"] = pathSet ? $"Destination set for agent '{agentName}'" : "Failed to set destination - no valid path found",
                    ["destination"] = UnityMcpBridge.SerializeVector3(targetPos),
                    ["path_pending"] = agent.pathPending,
                    ["has_path"] = agent.hasPath
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to set destination: {e.Message}"
                };
            }
        }

        private static JObject AddObstacle(JObject parameters)
        {
            try
            {
                string obstacleName = parameters["agent_name"]?.ToString() ?? "NavMeshObstacle";
                var obstacleSettings = parameters["obstacle_settings"]?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                
                GameObject obstacleObj = new GameObject(obstacleName);
                NavMeshObstacle obstacle = obstacleObj.AddComponent<NavMeshObstacle>();
                
                // Configure obstacle settings
                if (obstacleSettings.ContainsKey("shape"))
                {
                    string shape = obstacleSettings["shape"].ToString();
                    obstacle.shape = shape == "Box" ? NavMeshObstacleShape.Box : NavMeshObstacleShape.Capsule;
                }
                
                if (obstacleSettings.ContainsKey("center"))
                {
                    var center = ((JArray)obstacleSettings["center"]).ToObject<float[]>();
                    obstacle.center = new Vector3(center[0], center[1], center[2]);
                }
                
                if (obstacleSettings.ContainsKey("size"))
                {
                    var size = ((JArray)obstacleSettings["size"]).ToObject<float[]>();
                    obstacle.size = new Vector3(size[0], size[1], size[2]);
                }
                
                if (obstacleSettings.ContainsKey("carve"))
                    obstacle.carving = Convert.ToBoolean(obstacleSettings["carve"]);
                
                if (obstacleSettings.ContainsKey("move_threshold"))
                    obstacle.carvingMoveThreshold = Convert.ToSingle(obstacleSettings["move_threshold"]);
                
                if (obstacleSettings.ContainsKey("time_to_stationary"))
                    obstacle.carvingTimeToStationary = Convert.ToSingle(obstacleSettings["time_to_stationary"]);
                
                Undo.RegisterCreatedObjectUndo(obstacleObj, "Create NavMesh Obstacle");
                
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"NavMesh obstacle '{obstacleName}' created successfully",
                    ["obstacle_id"] = obstacleObj.GetInstanceID()
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to create NavMesh obstacle: {e.Message}"
                };
            }
        }

        private static JObject BakeNavMesh(JObject parameters)
        {
            try
            {
                var bakeSettings = parameters["bake_settings"]?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                
                // Get current NavMesh build settings
                NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(0);
                
                // Apply custom settings if provided
                if (bakeSettings.ContainsKey("agent_radius"))
                    buildSettings.agentRadius = Convert.ToSingle(bakeSettings["agent_radius"]);
                if (bakeSettings.ContainsKey("agent_height"))
                    buildSettings.agentHeight = Convert.ToSingle(bakeSettings["agent_height"]);
                if (bakeSettings.ContainsKey("agent_slope"))
                    buildSettings.agentSlope = Convert.ToSingle(bakeSettings["agent_slope"]);
                if (bakeSettings.ContainsKey("agent_climb"))
                    buildSettings.agentClimb = Convert.ToSingle(bakeSettings["agent_climb"]);
                if (bakeSettings.ContainsKey("ledge_drop_height"))
                    buildSettings.ledgeDropHeight = Convert.ToSingle(bakeSettings["ledge_drop_height"]);
                if (bakeSettings.ContainsKey("jump_distance"))
                    buildSettings.maxJumpAcrossDistance = Convert.ToSingle(bakeSettings["jump_distance"]);
                if (bakeSettings.ContainsKey("min_region_area"))
                    buildSettings.minRegionArea = Convert.ToSingle(bakeSettings["min_region_area"]);
                
                // Bake NavMesh
                NavMeshBuilder.BuildNavMesh();
                
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = "NavMesh baked successfully"
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to bake NavMesh: {e.Message}"
                };
            }
        }

        private static JObject ListAgents(JObject parameters)
        {
            try
            {
                NavMeshAgent[] agents = GameObject.FindObjectsOfType<NavMeshAgent>();
                var agentList = new JArray();
                
                foreach (var agent in agents)
                {
                    var agentInfo = new JObject
                    {
                        ["name"] = agent.gameObject.name,
                        ["id"] = agent.gameObject.GetInstanceID(),
                        ["position"] = UnityMcpBridge.SerializeVector3(agent.transform.position),
                        ["destination"] = UnityMcpBridge.SerializeVector3(agent.destination),
                        ["velocity"] = UnityMcpBridge.SerializeVector3(agent.velocity),
                        ["speed"] = agent.speed,
                        ["is_on_navmesh"] = agent.isOnNavMesh,
                        ["has_path"] = agent.hasPath,
                        ["path_pending"] = agent.pathPending,
                        ["remaining_distance"] = agent.remainingDistance,
                        ["stopped"] = agent.isStopped
                    };
                    agentList.Add(agentInfo);
                }
                
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"Found {agents.Length} NavMesh agents",
                    ["agents"] = agentList,
                    ["total_count"] = agents.Length
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to list NavMesh agents: {e.Message}"
                };
            }
        }

        private static JObject GetAgentInfo(JObject parameters)
        {
            try
            {
                string agentName = parameters["agent_name"]?.ToString();
                
                GameObject agentObj = GameObject.Find(agentName);
                if (agentObj == null)
                {
                    return new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"NavMesh agent '{agentName}' not found"
                    };
                }
                
                NavMeshAgent agent = agentObj.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    return new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"GameObject '{agentName}' does not have NavMeshAgent component"
                    };
                }
                
                var agentInfo = new JObject
                {
                    ["name"] = agent.gameObject.name,
                    ["id"] = agent.gameObject.GetInstanceID(),
                    ["position"] = UnityMcpBridge.SerializeVector3(agent.transform.position),
                    ["destination"] = UnityMcpBridge.SerializeVector3(agent.destination),
                    ["velocity"] = UnityMcpBridge.SerializeVector3(agent.velocity),
                    ["speed"] = agent.speed,
                    ["angular_speed"] = agent.angularSpeed,
                    ["acceleration"] = agent.acceleration,
                    ["stopping_distance"] = agent.stoppingDistance,
                    ["auto_traverse_off_mesh_link"] = agent.autoTraverseOffMeshLink,
                    ["auto_repath"] = agent.autoRepath,
                    ["radius"] = agent.radius,
                    ["height"] = agent.height,
                    ["priority"] = agent.avoidancePriority,
                    ["is_on_navmesh"] = agent.isOnNavMesh,
                    ["has_path"] = agent.hasPath,
                    ["path_pending"] = agent.pathPending,
                    ["remaining_distance"] = agent.remainingDistance,
                    ["stopped"] = agent.isStopped
                };
                
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"Retrieved info for agent '{agentName}'",
                    ["agent_info"] = agentInfo
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to get agent info: {e.Message}"
                };
            }
        }

        private static JObject DeleteAgent(JObject parameters)
        {
            try
            {
                string agentName = parameters["agent_name"]?.ToString();
                
                GameObject agentObj = GameObject.Find(agentName);
                if (agentObj == null)
                {
                    return new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"NavMesh agent '{agentName}' not found"
                    };
                }
                
                Undo.DestroyObjectImmediate(agentObj);
                
                return new JObject
                {
                    ["success"] = true,
                    ["message"] = $"NavMesh agent '{agentName}' deleted successfully"
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to delete NavMesh agent: {e.Message}"
                };
            }
        }

        // Placeholder methods for advanced features
        private static JObject CreateBehaviorTree(JObject parameters) => CreatePlaceholderResponse("Behavior tree creation");
        private static JObject CreateStateMachine(JObject parameters) => CreatePlaceholderResponse("State machine creation");
        private static JObject CreateNavMeshArea(JObject parameters) => CreatePlaceholderResponse("NavMesh area creation");
        private static JObject SetupCrowdSimulation(JObject parameters) => CreatePlaceholderResponse("Crowd simulation setup");
        private static JObject ClearNavMesh(JObject parameters) => CreatePlaceholderResponse("NavMesh clearing");
        private static JObject SaveNavMesh(JObject parameters) => CreatePlaceholderResponse("NavMesh saving");
        private static JObject LoadNavMesh(JObject parameters) => CreatePlaceholderResponse("NavMesh loading");
        private static JObject GetNavMeshInfo(JObject parameters) => CreatePlaceholderResponse("NavMesh info retrieval");
        private static JObject ModifyBehaviorTree(JObject parameters) => CreatePlaceholderResponse("Behavior tree modification");
        private static JObject AddBehaviorTreeNode(JObject parameters) => CreatePlaceholderResponse("Behavior tree node addition");
        private static JObject RemoveBehaviorTreeNode(JObject parameters) => CreatePlaceholderResponse("Behavior tree node removal");
        private static JObject ConnectBehaviorTreeNodes(JObject parameters) => CreatePlaceholderResponse("Behavior tree node connection");
        private static JObject SaveBehaviorTree(JObject parameters) => CreatePlaceholderResponse("Behavior tree saving");
        private static JObject LoadBehaviorTree(JObject parameters) => CreatePlaceholderResponse("Behavior tree loading");
        private static JObject CalculatePath(JObject parameters) => CreatePlaceholderResponse("Path calculation");
        private static JObject SamplePosition(JObject parameters) => CreatePlaceholderResponse("Position sampling");
        private static JObject NavMeshRaycast(JObject parameters) => CreatePlaceholderResponse("NavMesh raycast");
        private static JObject FindClosestEdge(JObject parameters) => CreatePlaceholderResponse("Closest edge finding");
        private static JObject GetAreaCost(JObject parameters) => CreatePlaceholderResponse("Area cost retrieval");
        private static JObject SetAreaCost(JObject parameters) => CreatePlaceholderResponse("Area cost setting");

        private static JObject CreatePlaceholderResponse(string operation)
        {
            return new JObject
            {
                ["success"] = false,
                ["message"] = $"{operation} is not yet implemented. This is a placeholder for future development."
            };
        }
    }
}