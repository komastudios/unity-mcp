using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Tools
{
    public static class ManageAI
    {
        /// <summary>
        /// Helper method to safely serialize Vector3 objects to avoid circular reference issues
        /// </summary>
        private static JObject SerializeVector3(Vector3 vector)
        {
            return new JObject
            {
                ["x"] = vector.x,
                ["y"] = vector.y,
                ["z"] = vector.z
            };
        }

        public static object HandleCommand(JObject parameters)
        {
            return HandleAICommand(parameters);
        }

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
                var agentSettings = parameters["agent_settings"]?.ToObject<JObject>() ?? new JObject();
                
                // Create GameObject with NavMeshAgent
                GameObject agentObj = new GameObject(agentName);
                NavMeshAgent agent = agentObj.AddComponent<NavMeshAgent>();
                
                // Configure agent settings
                if (agentSettings["speed"] != null)
                    agent.speed = agentSettings["speed"].ToObject<float>();
                if (agentSettings["angular_speed"] != null)
                    agent.angularSpeed = agentSettings["angular_speed"].ToObject<float>();
                if (agentSettings["acceleration"] != null)
                    agent.acceleration = agentSettings["acceleration"].ToObject<float>();
                if (agentSettings["stopping_distance"] != null)
                    agent.stoppingDistance = agentSettings["stopping_distance"].ToObject<float>();
                if (agentSettings["auto_traverse_off_mesh_link"] != null)
                    agent.autoTraverseOffMeshLink = agentSettings["auto_traverse_off_mesh_link"].ToObject<bool>();
                if (agentSettings["auto_repath"] != null)
                    agent.autoRepath = agentSettings["auto_repath"].ToObject<bool>();
                if (agentSettings["radius"] != null)
                    agent.radius = agentSettings["radius"].ToObject<float>();
                if (agentSettings["height"] != null)
                    agent.height = agentSettings["height"].ToObject<float>();
                if (agentSettings["priority"] != null)
                    agent.avoidancePriority = agentSettings["priority"].ToObject<int>();
                
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
                var agentSettings = parameters["agent_settings"]?.ToObject<JObject>() ?? new JObject();
                
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
                            agent.speed = setting.Value.ToObject<float>();
                            break;
                        case "angular_speed":
                            agent.angularSpeed = setting.Value.ToObject<float>();
                            break;
                        case "acceleration":
                            agent.acceleration = setting.Value.ToObject<float>();
                            break;
                        case "stopping_distance":
                            agent.stoppingDistance = setting.Value.ToObject<float>();
                            break;
                        case "auto_traverse_off_mesh_link":
                            agent.autoTraverseOffMeshLink = setting.Value.ToObject<bool>();
                            break;
                        case "auto_repath":
                            agent.autoRepath = setting.Value.ToObject<bool>();
                            break;
                        case "radius":
                            agent.radius = setting.Value.ToObject<float>();
                            break;
                        case "height":
                            agent.height = setting.Value.ToObject<float>();
                            break;
                        case "priority":
                            agent.avoidancePriority = setting.Value.ToObject<int>();
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
                    ["destination"] = SerializeVector3(targetPos),
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
                string obstacleName = parameters["obstacle_name"]?.ToString() ?? "NavMeshObstacle";
                var obstacleSettings = parameters["obstacle_settings"]?.ToObject<JObject>() ?? new JObject();
                
                GameObject obstacleObj = new GameObject(obstacleName);
                NavMeshObstacle obstacle = obstacleObj.AddComponent<NavMeshObstacle>();
                
                // Configure obstacle settings
                if (obstacleSettings["shape"] != null)
                {
                    string shape = obstacleSettings["shape"].ToString();
                    obstacle.shape = shape == "Box" ? NavMeshObstacleShape.Box : NavMeshObstacleShape.Capsule;
                }
                
                if (obstacleSettings["center"] != null)
                {
                    var center = obstacleSettings["center"].ToObject<float[]>();
                    obstacle.center = new Vector3(center[0], center[1], center[2]);
                }
                
                if (obstacleSettings["size"] != null)
                {
                    var size = obstacleSettings["size"].ToObject<float[]>();
                    obstacle.size = new Vector3(size[0], size[1], size[2]);
                }
                
                if (obstacleSettings["carve"] != null)
                    obstacle.carving = obstacleSettings["carve"].ToObject<bool>();
                
                if (obstacleSettings["move_threshold"] != null)
                    obstacle.carvingMoveThreshold = obstacleSettings["move_threshold"].ToObject<float>();
                
                if (obstacleSettings["time_to_stationary"] != null)
                    obstacle.carvingTimeToStationary = obstacleSettings["time_to_stationary"].ToObject<float>();
                
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
                var bakeSettings = parameters["bake_settings"]?.ToObject<JObject>() ?? new JObject();
                
                // Get current NavMesh build settings
                NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(0);
                
                // Apply custom settings if provided
                if (bakeSettings["agent_radius"] != null)
                    buildSettings.agentRadius = bakeSettings["agent_radius"].ToObject<float>();
                if (bakeSettings["agent_height"] != null)
                    buildSettings.agentHeight = bakeSettings["agent_height"].ToObject<float>();
                if (bakeSettings["agent_slope"] != null)
                    buildSettings.agentSlope = bakeSettings["agent_slope"].ToObject<float>();
                if (bakeSettings["agent_climb"] != null)
                    buildSettings.agentClimb = bakeSettings["agent_climb"].ToObject<float>();
                if (bakeSettings["ledge_drop_height"] != null)
                    buildSettings.ledgeDropHeight = bakeSettings["ledge_drop_height"].ToObject<float>();
                if (bakeSettings["jump_distance"] != null)
                    buildSettings.maxJumpAcrossDistance = bakeSettings["jump_distance"].ToObject<float>();
                if (bakeSettings["min_region_area"] != null)
                    buildSettings.minRegionArea = bakeSettings["min_region_area"].ToObject<float>();
                
                // Bake NavMesh
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                
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
                        ["position"] = SerializeVector3(agent.transform.position),
                        ["destination"] = SerializeVector3(agent.destination),
                        ["velocity"] = SerializeVector3(agent.velocity),
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
                    ["position"] = SerializeVector3(agent.transform.position),
                    ["destination"] = SerializeVector3(agent.destination),
                    ["velocity"] = SerializeVector3(agent.velocity),
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