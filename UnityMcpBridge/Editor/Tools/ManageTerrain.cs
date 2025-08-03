using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageTerrain
    {
        public static object HandleCommand(JObject parameters)
        {
            try
            {
                string action = parameters["action"]?.ToString() ?? "";
                
                return action switch
                {
                    "create_terrain" => CreateTerrain(parameters),
                    "modify_terrain" => ModifyTerrain(parameters),
                    "sculpt_terrain" => SculptTerrain(parameters),
                    "paint_texture" => PaintTexture(parameters),
                    "place_trees" => PlaceTrees(parameters),
                    "place_grass" => PlaceGrass(parameters),
                    "configure_wind" => ConfigureWind(parameters),
                    "bake_terrain" => BakeTerrain(parameters),
                    "list_terrains" => ListTerrains(parameters),
                    "get_terrain_info" => GetTerrainInfo(parameters),
                    "delete_terrain" => DeleteTerrain(parameters),
                    "heightmap_import_heightmap" => ImportHeightmap(parameters),
                    "heightmap_export_heightmap" => ExportHeightmap(parameters),
                    "heightmap_set_heightmap" => SetHeightmap(parameters),
                    "heightmap_get_heightmap" => GetHeightmap(parameters),
                    "heightmap_generate_noise" => GenerateNoiseHeightmap(parameters),
                    "streaming_setup_streaming" => SetupStreaming(parameters),
                    "streaming_create_terrain_group" => CreateTerrainGroup(parameters),
                    "streaming_load_terrain_chunk" => LoadTerrainChunk(parameters),
                    "streaming_unload_terrain_chunk" => UnloadTerrainChunk(parameters),
                    "streaming_get_streaming_status" => GetStreamingStatus(parameters),
                    _ => Response.Error($"Unknown terrain action: {action}")
                };
            }
            catch (Exception e)
            {
                return Response.Error($"Error in terrain management: {e.Message}");
            }
        }

        private static object CreateTerrain(JObject parameters)
        {
            try
            {
                string terrainName = parameters["terrain_name"]?.ToString() ?? "NewTerrain";
                var position = JsonConvert.DeserializeObject<float[]>(parameters["position"]?.ToString() ?? "[0,0,0]");
                var size = JsonConvert.DeserializeObject<float[]>(parameters["size"]?.ToString() ?? "[100,30,100]");
                int heightmapRes = parameters["heightmap_resolution"]?.ToObject<int>() ?? 513;
                int detailRes = parameters["detail_resolution"]?.ToObject<int>() ?? 1024;

                // Create terrain data
                TerrainData terrainData = new TerrainData();
                terrainData.name = terrainName + "_Data";
                terrainData.heightmapResolution = heightmapRes;
                terrainData.size = new Vector3(size[0], size[1], size[2]);
                terrainData.SetDetailResolution(detailRes, 16);

                // Create terrain GameObject
                GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
                terrainGO.name = terrainName;
                terrainGO.transform.position = new Vector3(position[0], position[1], position[2]);

                // Get terrain component
                Terrain terrain = terrainGO.GetComponent<Terrain>();

                // Save terrain data as asset
                string assetPath = $"Assets/TerrainData/{terrainName}_Data.asset";
                AssetDatabase.CreateAsset(terrainData, assetPath);
                AssetDatabase.SaveAssets();

                return Response.Success($"Terrain '{terrainName}' created successfully.", new
                {
                    name = terrainName,
                    id = terrainGO.GetInstanceID(),
                    position = SerializeVector3(terrainGO.transform.position),
                    size = SerializeVector3(terrainData.size),
                    heightmap_resolution = terrainData.heightmapResolution,
                    detail_resolution = terrainData.detailResolution,
                    asset_path = assetPath
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create terrain: {e.Message}");
            }
        }

        private static object ModifyTerrain(JObject parameters)
        {
            try
            {
                string terrainName = parameters["terrain_name"]?.ToString() ?? "";
                
                Terrain terrain = FindTerrainByName(terrainName);
                if (terrain == null)
                {
                    return Response.Error($"Terrain '{terrainName}' not found.");
                }

                // Modify position if provided
                if (parameters.ContainsKey("position"))
                {
                    var position = JsonConvert.DeserializeObject<float[]>(parameters["position"].ToString());
                    terrain.transform.position = new Vector3(position[0], position[1], position[2]);
                }

                // Modify size if provided
                if (parameters.ContainsKey("size"))
                {
                    var size = JsonConvert.DeserializeObject<float[]>(parameters["size"].ToString());
                    terrain.terrainData.size = new Vector3(size[0], size[1], size[2]);
                }

                EditorUtility.SetDirty(terrain);
                EditorUtility.SetDirty(terrain.terrainData);

                return Response.Success($"Terrain '{terrainName}' modified successfully.", new
                {
                    name = terrainName,
                    position = SerializeVector3(terrain.transform.position),
                    size = SerializeVector3(terrain.terrainData.size)
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify terrain: {e.Message}");
            }
        }

        private static object SculptTerrain(JObject parameters)
        {
            try
            {
                string terrainName = parameters["terrain_name"]?.ToString() ?? "";
                float brushSize = parameters["brush_size"]?.ToObject<float>() ?? 10.0f;
                float brushStrength = parameters["brush_strength"]?.ToObject<float>() ?? 0.5f;
                float heightValue = parameters["height_value"]?.ToObject<float>() ?? 0.0f;
                var position = JsonConvert.DeserializeObject<float[]>(parameters["position"]?.ToString() ?? "[0,0]");

                Terrain terrain = FindTerrainByName(terrainName);
                if (terrain == null)
                {
                    return Response.Error($"Terrain '{terrainName}' not found.");
                }

                TerrainData terrainData = terrain.terrainData;
                int heightmapWidth = terrainData.heightmapResolution;
                int heightmapHeight = terrainData.heightmapResolution;

                // Convert world position to heightmap coordinates
                Vector3 terrainPos = terrain.transform.position;
                Vector3 terrainSize = terrainData.size;
                
                int x = Mathf.RoundToInt((position[0] - terrainPos.x) / terrainSize.x * heightmapWidth);
                int y = Mathf.RoundToInt((position[1] - terrainPos.z) / terrainSize.z * heightmapHeight);

                // Calculate brush area
                int brushRadius = Mathf.RoundToInt(brushSize);
                int startX = Mathf.Max(0, x - brushRadius);
                int endX = Mathf.Min(heightmapWidth, x + brushRadius);
                int startY = Mathf.Max(0, y - brushRadius);
                int endY = Mathf.Min(heightmapHeight, y + brushRadius);

                // Get current heights
                float[,] heights = terrainData.GetHeights(startX, startY, endX - startX, endY - startY);

                // Apply brush effect
                for (int i = 0; i < heights.GetLength(0); i++)
                {
                    for (int j = 0; j < heights.GetLength(1); j++)
                    {
                        float distance = Vector2.Distance(new Vector2(startX + i, startY + j), new Vector2(x, y));
                        if (distance <= brushRadius)
                        {
                            float falloff = 1.0f - (distance / brushRadius);
                            float targetHeight = heightValue / terrainSize.y;
                            heights[i, j] = Mathf.Lerp(heights[i, j], targetHeight, brushStrength * falloff);
                        }
                    }
                }

                // Apply heights back to terrain
                terrainData.SetHeights(startX, startY, heights);
                EditorUtility.SetDirty(terrainData);

                return Response.Success($"Terrain '{terrainName}' sculpted successfully.", new
                {
                    brush_position = position,
                    brush_size = brushSize,
                    brush_strength = brushStrength,
                    height_value = heightValue
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to sculpt terrain: {e.Message}");
            }
        }

        private static object PaintTexture(JObject parameters)
        {
            try
            {
                string terrainName = parameters["terrain_name"]?.ToString() ?? "";
                int textureIndex = parameters["texture_index"]?.ToObject<int>() ?? 0;
                var position = JsonConvert.DeserializeObject<float[]>(parameters["position"]?.ToString() ?? "[0,0]");
                float brushSize = parameters["brush_size"]?.ToObject<float>() ?? 10.0f;
                float brushStrength = parameters["brush_strength"]?.ToObject<float>() ?? 0.5f;
                string texturePath = parameters["texture_path"]?.ToString() ?? "";
                int layerIndex = parameters["layer_index"]?.ToObject<int>() ?? 0;

                Terrain terrain = FindTerrainByName(terrainName);
                if (terrain == null)
                {
                    return Response.Error($"Terrain '{terrainName}' not found.");
                }

                // Load texture
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null)
                {
                    return Response.Error($"Texture not found at path: {texturePath}");
                }

                TerrainData terrainData = terrain.terrainData;
                
                // Create or update terrain layer
                TerrainLayer[] terrainLayers = terrainData.terrainLayers;
                if (layerIndex >= terrainLayers.Length)
                {
                    Array.Resize(ref terrainLayers, layerIndex + 1);
                }

                if (terrainLayers[layerIndex] == null)
                {
                    terrainLayers[layerIndex] = new TerrainLayer();
                }

                terrainLayers[layerIndex].diffuseTexture = texture;
                terrainData.terrainLayers = terrainLayers;

                EditorUtility.SetDirty(terrainData);

                return Response.Success($"Texture painted on terrain '{terrainName}' at layer {layerIndex}.", new
                {
                    terrain_name = terrainName,
                    texture_path = texturePath,
                    layer_index = layerIndex
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to paint texture: {e.Message}");
            }
        }

        private static object PlaceTrees(JObject parameters)
        {
            try
            {
                string terrainName = parameters["terrain_name"]?.ToString() ?? "";
                string prefabPath = parameters["prefab_path"]?.ToString() ?? "";
                var positions = JsonConvert.DeserializeObject<float[][]>(parameters["positions"]?.ToString() ?? "[]");
                int count = parameters["count"]?.ToObject<int>() ?? 1;

                Terrain terrain = FindTerrainByName(terrainName);
                if (terrain == null)
                {
                    return Response.Error($"Terrain '{terrainName}' not found.");
                }

                GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePrefabPath);
                if (treePrefab == null)
                {
                    return Response.Error($"Tree prefab not found at path: {treePrefabPath}");
                }

                TerrainData terrainData = terrain.terrainData;
                
                // Add tree prototype if not exists
                List<TreePrototype> treePrototypes = terrainData.treePrototypes.ToList();
                int prototypeIndex = treePrototypes.FindIndex(tp => tp.prefab == treePrefab);
                
                if (prototypeIndex == -1)
                {
                    TreePrototype newPrototype = new TreePrototype();
                    newPrototype.prefab = treePrefab;
                    treePrototypes.Add(newPrototype);
                    terrainData.treePrototypes = treePrototypes.ToArray();
                    prototypeIndex = treePrototypes.Count - 1;
                }

                // Place trees randomly based on density
                List<TreeInstance> treeInstances = terrainData.treeInstances.ToList();
                int treesToPlace = Mathf.RoundToInt(density * 100);

                for (int i = 0; i < treesToPlace; i++)
                {
                    TreeInstance tree = new TreeInstance();
                    tree.prototypeIndex = prototypeIndex;
                    tree.position = new Vector3(UnityEngine.Random.value, 0, UnityEngine.Random.value);
                    tree.widthScale = UnityEngine.Random.Range(0.8f, 1.2f);
                    tree.heightScale = UnityEngine.Random.Range(0.8f, 1.2f);
                    tree.rotation = UnityEngine.Random.Range(0, 2 * Mathf.PI);
                    
                    treeInstances.Add(tree);
                }

                terrainData.treeInstances = treeInstances.ToArray();
                EditorUtility.SetDirty(terrainData);

                return Response.Success($"Trees placed on terrain '{terrainName}'.", new
                {
                    terrain_name = terrainName,
                    tree_prefab = treePrefabPath,
                    trees_placed = treesToPlace,
                    total_trees = treeInstances.Count
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to place trees: {e.Message}");
            }
        }

        private static object PlaceGrass(JObject parameters)
        {
            return Response.Error("Grass placement not yet implemented");
        }

        private static object ConfigureWind(JObject parameters)
        {
            return Response.Error("Wind configuration not yet implemented");
        }

        private static object BakeTerrain(JObject parameters)
        {
            return Response.Error("Terrain baking not yet implemented");
        }

        private static object ListTerrains(JObject parameters)
        {
            try
            {
                Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
                var terrainList = terrains.Select(t => new
                {
                    name = t.name,
                    id = t.GetInstanceID(),
                    position = SerializeVector3(t.transform.position),
                    size = SerializeVector3(t.terrainData.size),
                    heightmap_resolution = t.terrainData.heightmapResolution,
                    detail_resolution = t.terrainData.detailResolution
                }).ToArray();

                return Response.Success($"Found {terrains.Length} terrain(s).", new
                {
                    count = terrains.Length,
                    terrains = terrainList
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to list terrains: {e.Message}");
            }
        }

        private static object GetTerrainInfo(JObject parameters)
        {
            try
            {
                string terrainName = parameters["terrain_name"]?.ToString() ?? "";
                
                Terrain terrain = FindTerrainByName(terrainName);
                if (terrain == null)
                {
                    return Response.Error($"Terrain '{terrainName}' not found.");
                }

                var terrainInfo = new
                {
                    name = terrain.name,
                    id = terrain.GetInstanceID(),
                    position = SerializeVector3(terrain.transform.position),
                    size = SerializeVector3(terrain.terrainData.size),
                    heightmap_resolution = terrain.terrainData.heightmapResolution,
                    detail_resolution = terrain.terrainData.detailResolution,
                    texture_count = terrain.terrainData.terrainLayers?.Length ?? 0,
                    tree_prototype_count = terrain.terrainData.treePrototypes?.Length ?? 0,
                    detail_prototype_count = terrain.terrainData.detailPrototypes?.Length ?? 0
                };

                return Response.Success($"Retrieved info for terrain '{terrainName}'.", terrainInfo);
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get terrain info: {e.Message}");
            }
        }

        private static object DeleteTerrain(JObject parameters)
        {
            try
            {
                string terrainName = parameters["terrain_name"]?.ToString() ?? "";
                
                Terrain terrain = FindTerrainByName(terrainName);
                if (terrain == null)
                {
                    return Response.Error($"Terrain '{terrainName}' not found.");
                }

                // Delete the terrain GameObject
                Object.DestroyImmediate(terrain.gameObject);

                return Response.Success($"Terrain '{terrainName}' deleted successfully.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to delete terrain: {e.Message}");
            }
        }

        // Heightmap operations
        private static object ImportHeightmap(JObject parameters)
        {
            return Response.Error("Heightmap import not yet implemented");
        }

        private static object ExportHeightmap(JObject parameters)
        {
            return Response.Error("Heightmap export not yet implemented");
        }

        private static object SetHeightmap(JObject parameters)
        {
            return Response.Error("Set heightmap not yet implemented");
        }

        private static object GetHeightmap(JObject parameters)
        {
            return Response.Error("Get heightmap not yet implemented");
        }

        private static object GenerateNoiseHeightmap(JObject parameters)
        {
            return Response.Error("Generate noise heightmap not yet implemented");
        }

        // Streaming operations
        private static object SetupStreaming(JObject parameters)
        {
            return Response.Error("Streaming setup not yet implemented");
        }

        private static object CreateTerrainGroup(JObject parameters)
        {
            return Response.Error("Create terrain group not yet implemented");
        }

        private static object LoadTerrainChunk(JObject parameters)
        {
            return Response.Error("Load terrain chunk not yet implemented");
        }

        private static object UnloadTerrainChunk(JObject parameters)
        {
            return Response.Error("Unload terrain chunk not yet implemented");
        }

        private static object GetStreamingStatus(JObject parameters)
        {
            return Response.Error("Get streaming status not yet implemented");
        }

        // Helper methods
        private static Terrain FindTerrainByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
            return terrains.FirstOrDefault(t => t.name == name);
        }

        private static object SerializeVector3(Vector3 vector)
        {
            return new { x = vector.x, y = vector.y, z = vector.z };
        }
    }
}