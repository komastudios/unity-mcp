using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Handles Physics System operations including Rigidbodies, Colliders, Joints, 
    /// and Physics simulation settings.
    /// </summary>
    public static class ManagePhysics
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString().ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("Action parameter is required.");
            }

            try
            {
                switch (action)
                {
                    case "add_rigidbody":
                        return AddRigidbody(@params);
                    case "modify_rigidbody":
                        return ModifyRigidbody(@params);
                    case "add_collider":
                        return AddCollider(@params);
                    case "modify_collider":
                        return ModifyCollider(@params);
                    case "add_joint":
                        return AddJoint(@params);
                    case "modify_joint":
                        return ModifyJoint(@params);
                    case "simulate_physics":
                        return SimulatePhysics(@params);
                    case "set_physics_settings":
                        return SetPhysicsSettings(@params);
                    case "get_physics_info":
                        return GetPhysicsInfo(@params);
                    case "create_physics_material":
                        return CreatePhysicsMaterial(@params);
                    case "apply_force":
                        return ApplyForce(@params);
                    case "raycast":
                        return PerformRaycast(@params);
                    default:
                        return Response.Error($"Unknown physics action: '{action}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManagePhysics] Action '{action}' failed: {e}");
                return Response.Error($"Internal error processing physics action '{action}': {e.Message}");
            }
        }

        private static object AddRigidbody(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            try
            {
                Rigidbody rb = targetObject.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = targetObject.AddComponent<Rigidbody>();
                }

                // Set rigidbody properties
                if (@params["mass"] != null)
                    rb.mass = @params["mass"].ToObject<float>();
                if (@params["drag"] != null)
                    rb.linearDamping = @params["drag"].ToObject<float>();
                if (@params["angular_drag"] != null)
                    rb.angularDamping = @params["angular_drag"].ToObject<float>();
                if (@params["use_gravity"] != null)
                    rb.useGravity = @params["use_gravity"].ToObject<bool>();
                if (@params["is_kinematic"] != null)
                    rb.isKinematic = @params["is_kinematic"].ToObject<bool>();

                // Set constraints
                if (@params["freeze_position"] != null)
                {
                    JObject freezePos = @params["freeze_position"] as JObject;
                    RigidbodyConstraints constraints = RigidbodyConstraints.None;
                    if (freezePos["x"]?.ToObject<bool>() == true) constraints |= RigidbodyConstraints.FreezePositionX;
                    if (freezePos["y"]?.ToObject<bool>() == true) constraints |= RigidbodyConstraints.FreezePositionY;
                    if (freezePos["z"]?.ToObject<bool>() == true) constraints |= RigidbodyConstraints.FreezePositionZ;
                    rb.constraints = constraints;
                }

                if (@params["freeze_rotation"] != null)
                {
                    JObject freezeRot = @params["freeze_rotation"] as JObject;
                    RigidbodyConstraints constraints = rb.constraints;
                    if (freezeRot["x"]?.ToObject<bool>() == true) constraints |= RigidbodyConstraints.FreezeRotationX;
                    if (freezeRot["y"]?.ToObject<bool>() == true) constraints |= RigidbodyConstraints.FreezeRotationY;
                    if (freezeRot["z"]?.ToObject<bool>() == true) constraints |= RigidbodyConstraints.FreezeRotationZ;
                    rb.constraints = constraints;
                }

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"Rigidbody added/modified on '{gameObjectName}'.", new
                {
                    gameObjectName = gameObjectName,
                    mass = rb.mass,
                    drag = rb.linearDamping,
                    angularDrag = rb.angularDamping,
                    useGravity = rb.useGravity,
                    isKinematic = rb.isKinematic,
                    constraints = rb.constraints.ToString()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add rigidbody: {e.Message}");
            }
        }

        private static object ModifyRigidbody(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            Rigidbody rb = targetObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have a Rigidbody component.");
            }

            try
            {
                // Modify properties
                if (@params["mass"] != null)
                    rb.mass = @params["mass"].ToObject<float>();
                if (@params["drag"] != null)
                    rb.linearDamping = @params["drag"].ToObject<float>();
                if (@params["angular_drag"] != null)
                    rb.angularDamping = @params["angular_drag"].ToObject<float>();
                if (@params["use_gravity"] != null)
                    rb.useGravity = @params["use_gravity"].ToObject<bool>();
                if (@params["is_kinematic"] != null)
                    rb.isKinematic = @params["is_kinematic"].ToObject<bool>();

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"Rigidbody on '{gameObjectName}' modified successfully.", new
                {
                    gameObjectName = gameObjectName,
                    mass = rb.mass,
                    drag = rb.linearDamping,
                    angularDrag = rb.angularDamping,
                    useGravity = rb.useGravity,
                    isKinematic = rb.isKinematic
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify rigidbody: {e.Message}");
            }
        }

        private static object AddCollider(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            string colliderType = @params["collider_type"]?.ToString()?.ToLower() ?? "box";

            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            try
            {
                Collider collider = null;

                switch (colliderType)
                {
                    case "box":
                        collider = targetObject.GetComponent<BoxCollider>() ?? targetObject.AddComponent<BoxCollider>();
                        if (@params["size"] != null)
                        {
                            JObject size = @params["size"] as JObject;
                            ((BoxCollider)collider).size = new Vector3(
                                size["x"]?.ToObject<float>() ?? 1f,
                                size["y"]?.ToObject<float>() ?? 1f,
                                size["z"]?.ToObject<float>() ?? 1f
                            );
                        }
                        break;

                    case "sphere":
                        collider = targetObject.GetComponent<SphereCollider>() ?? targetObject.AddComponent<SphereCollider>();
                        if (@params["radius"] != null)
                            ((SphereCollider)collider).radius = @params["radius"].ToObject<float>();
                        break;

                    case "capsule":
                        collider = targetObject.GetComponent<CapsuleCollider>() ?? targetObject.AddComponent<CapsuleCollider>();
                        if (@params["radius"] != null)
                            ((CapsuleCollider)collider).radius = @params["radius"].ToObject<float>();
                        if (@params["height"] != null)
                            ((CapsuleCollider)collider).height = @params["height"].ToObject<float>();
                        break;

                    case "mesh":
                        collider = targetObject.GetComponent<MeshCollider>() ?? targetObject.AddComponent<MeshCollider>();
                        if (@params["convex"] != null)
                            ((MeshCollider)collider).convex = @params["convex"].ToObject<bool>();
                        break;

                    default:
                        return Response.Error($"Unknown collider type: {colliderType}");
                }

                // Set common properties
                if (@params["is_trigger"] != null)
                    collider.isTrigger = @params["is_trigger"].ToObject<bool>();

                if (@params["center"] != null)
                {
                    JObject center = @params["center"] as JObject;
                    Vector3 centerVector = new Vector3(
                        center["x"]?.ToObject<float>() ?? 0f,
                        center["y"]?.ToObject<float>() ?? 0f,
                        center["z"]?.ToObject<float>() ?? 0f
                    );
                    
                    // Set center based on collider type
                    if (collider is BoxCollider boxCollider)
                        boxCollider.center = centerVector;
                    else if (collider is SphereCollider sphereCollider)
                        sphereCollider.center = centerVector;
                    else if (collider is CapsuleCollider capsuleCollider)
                        capsuleCollider.center = centerVector;
                }

                // Apply physics material if specified
                if (@params["physics_material"] != null)
                {
                    string materialPath = @params["physics_material"].ToString();
                    PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
                    if (material != null)
                        collider.material = material;
                }

                EditorUtility.SetDirty(targetObject);

                // Get center based on collider type for response
                Vector3 centerVector = Vector3.zero;
                if (collider is BoxCollider boxCol)
                    centerVector = boxCol.center;
                else if (collider is SphereCollider sphereCol)
                    centerVector = sphereCol.center;
                else if (collider is CapsuleCollider capsuleCol)
                    centerVector = capsuleCol.center;

                return Response.Success($"{colliderType} collider added to '{gameObjectName}'.", new
                {
                    gameObjectName = gameObjectName,
                    colliderType = colliderType,
                    isTrigger = collider.isTrigger,
                    center = new { x = centerVector.x, y = centerVector.y, z = centerVector.z }
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add collider: {e.Message}");
            }
        }

        private static object ModifyCollider(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            Collider collider = targetObject.GetComponent<Collider>();
            if (collider == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have a Collider component.");
            }

            try
            {
                // Modify common properties
                if (@params["is_trigger"] != null)
                    collider.isTrigger = @params["is_trigger"].ToObject<bool>();

                if (@params["center"] != null)
                {
                    JObject center = @params["center"] as JObject;
                    Vector3 centerVector = new Vector3(
                        center["x"]?.ToObject<float>() ?? 0f,
                        center["y"]?.ToObject<float>() ?? 0f,
                        center["z"]?.ToObject<float>() ?? 0f
                    );

                    // Set center based on collider type
                    if (collider is BoxCollider boxCol)
                        boxCol.center = centerVector;
                    else if (collider is SphereCollider sphereCol)
                        sphereCol.center = centerVector;
                    else if (collider is CapsuleCollider capsuleCol)
                        capsuleCol.center = centerVector;
                }

                // Type-specific modifications
                if (collider is BoxCollider boxCollider && @params["size"] != null)
                {
                    JObject size = @params["size"] as JObject;
                    boxCollider.size = new Vector3(
                        size["x"]?.ToObject<float>() ?? boxCollider.size.x,
                        size["y"]?.ToObject<float>() ?? boxCollider.size.y,
                        size["z"]?.ToObject<float>() ?? boxCollider.size.z
                    );
                }
                else if (collider is SphereCollider sphereCollider && @params["radius"] != null)
                {
                    sphereCollider.radius = @params["radius"].ToObject<float>();
                }
                else if (collider is CapsuleCollider capsuleCollider)
                {
                    if (@params["radius"] != null)
                        capsuleCollider.radius = @params["radius"].ToObject<float>();
                    if (@params["height"] != null)
                        capsuleCollider.height = @params["height"].ToObject<float>();
                }

                EditorUtility.SetDirty(targetObject);

                // Get center based on collider type for response
                Vector3 centerVector = Vector3.zero;
                if (collider is BoxCollider boxCol)
                    centerVector = boxCol.center;
                else if (collider is SphereCollider sphereCol)
                    centerVector = sphereCol.center;
                else if (collider is CapsuleCollider capsuleCol)
                    centerVector = capsuleCol.center;

                return Response.Success($"Collider on '{gameObjectName}' modified successfully.", new
                {
                    gameObjectName = gameObjectName,
                    colliderType = collider.GetType().Name,
                    isTrigger = collider.isTrigger,
                    center = new { x = centerVector.x, y = centerVector.y, z = centerVector.z }
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify collider: {e.Message}");
            }
        }

        private static object AddJoint(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            string jointType = @params["joint_type"]?.ToString()?.ToLower() ?? "fixed";

            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            try
            {
                Joint joint = null;

                switch (jointType)
                {
                    case "fixed":
                        joint = targetObject.GetComponent<FixedJoint>() ?? targetObject.AddComponent<FixedJoint>();
                        break;

                    case "hinge":
                        joint = targetObject.GetComponent<HingeJoint>() ?? targetObject.AddComponent<HingeJoint>();
                        var hingeJoint = (HingeJoint)joint;
                        
                        if (@params["axis"] != null)
                        {
                            JObject axis = @params["axis"] as JObject;
                            hingeJoint.axis = new Vector3(
                                axis["x"]?.ToObject<float>() ?? 1f,
                                axis["y"]?.ToObject<float>() ?? 0f,
                                axis["z"]?.ToObject<float>() ?? 0f
                            );
                        }

                        if (@params["use_limits"] != null && @params["use_limits"].ToObject<bool>())
                        {
                            hingeJoint.useLimits = true;
                            var limits = hingeJoint.limits;
                            if (@params["min_limit"] != null)
                                limits.min = @params["min_limit"].ToObject<float>();
                            if (@params["max_limit"] != null)
                                limits.max = @params["max_limit"].ToObject<float>();
                            hingeJoint.limits = limits;
                        }
                        break;

                    case "spring":
                        joint = targetObject.GetComponent<SpringJoint>() ?? targetObject.AddComponent<SpringJoint>();
                        var springJoint = (SpringJoint)joint;
                        
                        if (@params["spring"] != null)
                            springJoint.spring = @params["spring"].ToObject<float>();
                        if (@params["damper"] != null)
                            springJoint.damper = @params["damper"].ToObject<float>();
                        if (@params["min_distance"] != null)
                            springJoint.minDistance = @params["min_distance"].ToObject<float>();
                        if (@params["max_distance"] != null)
                            springJoint.maxDistance = @params["max_distance"].ToObject<float>();
                        break;

                    case "configurable":
                        joint = targetObject.GetComponent<ConfigurableJoint>() ?? targetObject.AddComponent<ConfigurableJoint>();
                        // ConfigurableJoint has many parameters - implement as needed
                        break;

                    default:
                        return Response.Error($"Unknown joint type: {jointType}");
                }

                // Set connected body if specified
                if (@params["connected_body"] != null)
                {
                    string connectedBodyName = @params["connected_body"].ToString();
                    GameObject connectedObject = GameObject.Find(connectedBodyName);
                    if (connectedObject != null)
                    {
                        Rigidbody connectedRb = connectedObject.GetComponent<Rigidbody>();
                        if (connectedRb != null)
                            joint.connectedBody = connectedRb;
                    }
                }

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"{jointType} joint added to '{gameObjectName}'.", new
                {
                    gameObjectName = gameObjectName,
                    jointType = jointType,
                    connectedBody = joint.connectedBody?.name ?? "None"
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add joint: {e.Message}");
            }
        }

        private static object ModifyJoint(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            Joint joint = targetObject.GetComponent<Joint>();
            if (joint == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have a Joint component.");
            }

            try
            {
                // Modify connected body
                if (@params["connected_body"] != null)
                {
                    string connectedBodyName = @params["connected_body"].ToString();
                    if (connectedBodyName == "None" || string.IsNullOrEmpty(connectedBodyName))
                    {
                        joint.connectedBody = null;
                    }
                    else
                    {
                        GameObject connectedObject = GameObject.Find(connectedBodyName);
                        if (connectedObject != null)
                        {
                            Rigidbody connectedRb = connectedObject.GetComponent<Rigidbody>();
                            if (connectedRb != null)
                                joint.connectedBody = connectedRb;
                        }
                    }
                }

                // Type-specific modifications
                if (joint is SpringJoint springJoint)
                {
                    if (@params["spring"] != null)
                        springJoint.spring = @params["spring"].ToObject<float>();
                    if (@params["damper"] != null)
                        springJoint.damper = @params["damper"].ToObject<float>();
                }
                else if (joint is HingeJoint hingeJoint)
                {
                    if (@params["use_limits"] != null)
                    {
                        hingeJoint.useLimits = @params["use_limits"].ToObject<bool>();
                        if (hingeJoint.useLimits)
                        {
                            var limits = hingeJoint.limits;
                            if (@params["min_limit"] != null)
                                limits.min = @params["min_limit"].ToObject<float>();
                            if (@params["max_limit"] != null)
                                limits.max = @params["max_limit"].ToObject<float>();
                            hingeJoint.limits = limits;
                        }
                    }
                }

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"Joint on '{gameObjectName}' modified successfully.", new
                {
                    gameObjectName = gameObjectName,
                    jointType = joint.GetType().Name,
                    connectedBody = joint.connectedBody?.name ?? "None"
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify joint: {e.Message}");
            }
        }

        private static object SimulatePhysics(JObject @params)
        {
            try
            {
                float duration = @params["duration"]?.ToObject<float>() ?? 1.0f;
                float timeStep = @params["time_step"]?.ToObject<float>() ?? 0.02f;
                
                // Note: This is a simplified simulation for editor mode
                // In play mode, Physics.Simulate() would be used
                
                if (Application.isPlaying)
                {
                    Physics.Simulate(timeStep);
                    return Response.Success("Physics simulation step executed.", new
                    {
                        timeStep = timeStep,
                        mode = "PlayMode"
                    });
                }
                else
                {
                    return Response.Success("Physics simulation requested (requires Play Mode for full simulation).", new
                    {
                        duration = duration,
                        timeStep = timeStep,
                        mode = "EditorMode"
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to simulate physics: {e.Message}");
            }
        }

        private static object SetPhysicsSettings(JObject @params)
        {
            try
            {
                if (@params["gravity"] != null)
                {
                    JObject gravity = @params["gravity"] as JObject;
                    Physics.gravity = new Vector3(
                        gravity["x"]?.ToObject<float>() ?? Physics.gravity.x,
                        gravity["y"]?.ToObject<float>() ?? Physics.gravity.y,
                        gravity["z"]?.ToObject<float>() ?? Physics.gravity.z
                    );
                }

                if (@params["default_solver_iterations"] != null)
                    Physics.defaultSolverIterations = @params["default_solver_iterations"].ToObject<int>();

                if (@params["default_solver_velocity_iterations"] != null)
                    Physics.defaultSolverVelocityIterations = @params["default_solver_velocity_iterations"].ToObject<int>();

                if (@params["bounce_threshold"] != null)
                    Physics.bounceThreshold = @params["bounce_threshold"].ToObject<float>();

                if (@params["sleep_threshold"] != null)
                    Physics.sleepThreshold = @params["sleep_threshold"].ToObject<float>();

                return Response.Success("Physics settings updated successfully.", new
                {
                    gravity = new { x = Physics.gravity.x, y = Physics.gravity.y, z = Physics.gravity.z },
                    defaultSolverIterations = Physics.defaultSolverIterations,
                    defaultSolverVelocityIterations = Physics.defaultSolverVelocityIterations,
                    bounceThreshold = Physics.bounceThreshold,
                    sleepThreshold = Physics.sleepThreshold
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set physics settings: {e.Message}");
            }
        }

        private static object GetPhysicsInfo(JObject @params)
        {
            try
            {
                string gameObjectName = @params["gameobject_name"]?.ToString();
                
                if (!string.IsNullOrEmpty(gameObjectName))
                {
                    GameObject targetObject = GameObject.Find(gameObjectName);
                    if (targetObject == null)
                    {
                        return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
                    }

                    Rigidbody rb = targetObject.GetComponent<Rigidbody>();
                    Collider[] colliders = targetObject.GetComponents<Collider>();
                    Joint[] joints = targetObject.GetComponents<Joint>();

                    return Response.Success($"Physics info for '{gameObjectName}'.", new
                    {
                        gameObjectName = gameObjectName,
                        hasRigidbody = rb != null,
                        rigidbody = rb != null ? new
                        {
                            mass = rb.mass,
                            drag = rb.linearDamping,
                            angularDrag = rb.angularDamping,
                            useGravity = rb.useGravity,
                            isKinematic = rb.isKinematic,
                            velocity = new { x = rb.linearVelocity.x, y = rb.linearVelocity.y, z = rb.linearVelocity.z },
                            angularVelocity = new { x = rb.angularVelocity.x, y = rb.angularVelocity.y, z = rb.angularVelocity.z }
                        } : null,
                        colliders = colliders.Select(c => {
                            Vector3 centerVector = Vector3.zero;
                            if (c is BoxCollider boxCol)
                                centerVector = boxCol.center;
                            else if (c is SphereCollider sphereCol)
                                centerVector = sphereCol.center;
                            else if (c is CapsuleCollider capsuleCol)
                                centerVector = capsuleCol.center;
                            
                            return new
                            {
                                type = c.GetType().Name,
                                isTrigger = c.isTrigger,
                                center = new { x = centerVector.x, y = centerVector.y, z = centerVector.z }
                            };
                        }).ToArray(),
                        joints = joints.Select(j => new
                        {
                            type = j.GetType().Name,
                            connectedBody = j.connectedBody?.name ?? "None"
                        }).ToArray()
                    });
                }
                else
                {
                    // Return global physics settings
                    return Response.Success("Global physics settings.", new
                    {
                        gravity = new { x = Physics.gravity.x, y = Physics.gravity.y, z = Physics.gravity.z },
                        defaultSolverIterations = Physics.defaultSolverIterations,
                        defaultSolverVelocityIterations = Physics.defaultSolverVelocityIterations,
                        bounceThreshold = Physics.bounceThreshold,
                        sleepThreshold = Physics.sleepThreshold
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get physics info: {e.Message}");
            }
        }

        private static object CreatePhysicsMaterial(JObject @params)
        {
            string name = @params["name"]?.ToString();
            string path = @params["path"]?.ToString() ?? "Assets/PhysicsMaterials";

            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Physics material name is required.");
            }

            try
            {
                // Ensure directory exists
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string[] folders = path.Split('/');
                    string currentPath = folders[0];
                    for (int i = 1; i < folders.Length; i++)
                    {
                        string newPath = currentPath + "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = newPath;
                    }
                }

                PhysicsMaterial material = new PhysicsMaterial(name);
                
                if (@params["dynamic_friction"] != null)
                    material.dynamicFriction = @params["dynamic_friction"].ToObject<float>();
                if (@params["static_friction"] != null)
                    material.staticFriction = @params["static_friction"].ToObject<float>();
                if (@params["bounciness"] != null)
                    material.bounciness = @params["bounciness"].ToObject<float>();
                if (@params["friction_combine"] != null)
                {
                    string combineMode = @params["friction_combine"].ToString();
                    material.frictionCombine = (PhysicsMaterialCombine)Enum.Parse(typeof(PhysicsMaterialCombine), combineMode);
                }
                if (@params["bounce_combine"] != null)
                {
                    string combineMode = @params["bounce_combine"].ToString();
                    material.bounceCombine = (PhysicsMaterialCombine)Enum.Parse(typeof(PhysicsMaterialCombine), combineMode);
                }

                string fullPath = $"{path}/{name}.physicMaterial";
                AssetDatabase.CreateAsset(material, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success($"Physics material '{name}' created successfully.", new
                {
                    name = material.name,
                    path = fullPath,
                    dynamicFriction = material.dynamicFriction,
                    staticFriction = material.staticFriction,
                    bounciness = material.bounciness,
                    frictionCombine = material.frictionCombine.ToString(),
                    bounceCombine = material.bounceCombine.ToString()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create physics material: {e.Message}");
            }
        }

        private static object ApplyForce(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            Rigidbody rb = targetObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have a Rigidbody component.");
            }

            try
            {
                if (@params["force"] != null)
                {
                    JObject force = @params["force"] as JObject;
                    Vector3 forceVector = new Vector3(
                        force["x"]?.ToObject<float>() ?? 0f,
                        force["y"]?.ToObject<float>() ?? 0f,
                        force["z"]?.ToObject<float>() ?? 0f
                    );

                    string forceMode = @params["force_mode"]?.ToString() ?? "Force";
                    ForceMode mode = (ForceMode)Enum.Parse(typeof(ForceMode), forceMode);

                    if (Application.isPlaying)
                    {
                        rb.AddForce(forceVector, mode);
                        return Response.Success($"Force applied to '{gameObjectName}'.", new
                        {
                            gameObjectName = gameObjectName,
                            force = new { x = forceVector.x, y = forceVector.y, z = forceVector.z },
                            forceMode = mode.ToString(),
                            applied = true
                        });
                    }
                    else
                    {
                        return Response.Success($"Force would be applied to '{gameObjectName}' (requires Play Mode).", new
                        {
                            gameObjectName = gameObjectName,
                            force = new { x = forceVector.x, y = forceVector.y, z = forceVector.z },
                            forceMode = mode.ToString(),
                            applied = false,
                            reason = "Not in Play Mode"
                        });
                    }
                }

                return Response.Error("Force vector is required.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to apply force: {e.Message}");
            }
        }

        private static object PerformRaycast(JObject @params)
        {
            try
            {
                if (@params["origin"] == null || @params["direction"] == null)
                {
                    return Response.Error("Origin and direction are required for raycast.");
                }

                JObject origin = @params["origin"] as JObject;
                JObject direction = @params["direction"] as JObject;

                Vector3 rayOrigin = new Vector3(
                    origin["x"]?.ToObject<float>() ?? 0f,
                    origin["y"]?.ToObject<float>() ?? 0f,
                    origin["z"]?.ToObject<float>() ?? 0f
                );

                Vector3 rayDirection = new Vector3(
                    direction["x"]?.ToObject<float>() ?? 0f,
                    direction["y"]?.ToObject<float>() ?? 1f,
                    direction["z"]?.ToObject<float>() ?? 0f
                ).normalized;

                float maxDistance = @params["max_distance"]?.ToObject<float>() ?? Mathf.Infinity;
                int layerMask = @params["layer_mask"]?.ToObject<int>() ?? Physics.DefaultRaycastLayers;

                RaycastHit hit;
                bool didHit = Physics.Raycast(rayOrigin, rayDirection, out hit, maxDistance, layerMask);

                if (didHit)
                {
                    return Response.Success("Raycast hit detected.", new
                    {
                        hit = true,
                        hitObject = hit.collider.gameObject.name,
                        hitPoint = new { x = hit.point.x, y = hit.point.y, z = hit.point.z },
                        hitNormal = new { x = hit.normal.x, y = hit.normal.y, z = hit.normal.z },
                        distance = hit.distance,
                        colliderType = hit.collider.GetType().Name
                    });
                }
                else
                {
                    return Response.Success("Raycast completed - no hit.", new
                    {
                        hit = false,
                        origin = new { x = rayOrigin.x, y = rayOrigin.y, z = rayOrigin.z },
                        direction = new { x = rayDirection.x, y = rayDirection.y, z = rayDirection.z },
                        maxDistance = maxDistance
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to perform raycast: {e.Message}");
            }
        }
    }
}