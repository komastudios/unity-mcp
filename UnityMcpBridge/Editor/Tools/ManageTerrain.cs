using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace UnityMcpBridge.Tools
{
    public static class ManageTerrain
    {
        [UnityMcpCommand("manage_terrain")]
        public static Response HandleTerrainCommand(Dictionary<string, object> parameters)
        {
            try
            {
                string action = parameters.GetValueOrDefault("action", "").ToString();
                
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

        private static Response CreateTerrain(Dictionary<string, object> parameters)
        {
            try
            {
                string terrainName = parameters.GetValueOrDefault("terrain_name", "NewTerrain").ToString();
                var position = JsonConvert.DeserializeObject<float[]>(parameters.GetValueOrDefault("position", "[0,0,0]").ToString());
                var size = JsonConvert.DeserializeObject<float[]>(parameters.GetValueOrDefault("size", "[100,30,100]").ToString());
                int heightmapRes = Convert.ToInt32(parameters.GetValueOrDefault("heightmap_resolution", 513));
                int detailRes = Convert.ToInt32(parameters.GetValueOrDefault("detail_resolution", 1024));

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

        private static Response ModifyTerrain(Dictionary<string, object> parameters)
        {
            try
            {
                string terrainName = parameters.GetValueOrDefault("terrain_name", "").ToString();
                
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

        private static Response SculptTerrain(Dictionary<string, object> parameters)
        {
            try
            {
                string terrainName = parameters.GetValueOrDefault("terrain_name", "").ToString();
                float brushSize = Convert.ToSingle(parameters.GetValueOrDefault("brush_size", 10.0f));
                float brushStrength = Convert.ToSingle(parameters.GetValueOrDefault("brush_strength", 0.5f));
                float heightValue = Convert.ToSingle(parameters.GetValueOrDefault("height_value", 0.0f));
                var position = JsonConvert.DeserializeObject<float[]>(parameters.GetValueOrDefault("position", "[0,0]").ToString());

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

        private static Response PaintTexture(Dictionary<string, object> parameters)
        {
            try
            {
                string terrainName = parameters.GetValueOrDefault("terrain_name", "").ToString();
                string texturePath = parameters.GetValueOrDefault("texture_path", "").ToString();
                int layerIndex = Convert.ToInt32(parameters.GetValueOrDefault("layer_index", 0));

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

        private static Response PlaceTrees(Dictionary<string, object> parameters)
        {
            try
            {
                string terrainName = parameters.GetValueOrDefault("terrain_name", "").ToString();
                string treePrefabPath = parameters.GetValueOrDefault("tree_prefab_path", "").ToString();
                float density = Convert.ToSingle(parameters.GetValueOrDefault("tree_density", 0.5f));

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

        private static Response PlaceGrass(Dictionary<string, object> parameters)
        {
            // Implementation for grass placement
            return Response.Success("Grass placement functionality not yet implemented.");
        }

        private static Response ConfigureWind(Dictionary<string, object> parameters)
        {
            // Implementation for wind configuration
            return Response.Success("Wind configuration functionality not yet implemented.");
        }

        private static Response BakeTerrain(Dictionary<string, object> parameters)
        {
            // Implementation for terrain baking
            return Response.Success("Terrain baking functionality not yet implemented.");
        }

        private static Response ListTerrains(Dictionary<string, object> parameters)
        {
            try
            {
                Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
                
                var terrainList = terrains.Select(terrain => new
                {
                    name = terrain.name,
                    id = terrain.GetInstanceID(),
                    position = SerializeVector3(terrain.transform.position),
                    size = SerializeVector3(terrain.terrainData.size),
                    heightmap_resolution = terrain.terrainData.heightmapResolution,
                    detail_resolution = terrain.terrainData.detailResolution,
                    tree_count = terrain.terrainData.treeInstanceCount,
                    layer_count = terrain.terrainData.terrainLayers?.Length ?? 0
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

        private static Response GetTerrainInfo(Dictionary<string, object> parameters)
        {
            try
            {
                string terrainName = parameters.GetValueOrDefault("terrain_name", "").ToString();
                
                Terrain terrain = FindTerrainByName(terrainName);
                if (terrain == null)
                {
                    return Response.Error($"Terrain '{terrainName}' not found.");
                }

                TerrainData terrainData = terrain.terrainData;

                return Response.Success($"Terrain '{terrainName}' information retrieved.", new
                {
                    name = terrain.name,
                    id = terrain.GetInstanceID(),
                    position = SerializeVector3(terrain.transform.position),
                    size = SerializeVector3(terrainData.size),
                    heightmap_resolution = terrainData.heightmapResolution,
                    detail_resolution = terrainData.detailResolution,
                    tree_count = terrainData.treeInstanceCount,
                    layer_count = terrainData.terrainLayers?.Length ?? 0,
                    tree_prototypes = terrainData.treePrototypes?.Length ?? 0,
                    detail_prototypes = terrainData.detailPrototypes?.Length ?? 0
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get terrain info: {e.Message}");
            }
        }

        private static Response DeleteTerrain(Dictionary<string, object> parameters)
        {
            try
            {
                string terrainName = parameters.GetValueOrDefault("terrain_name", "").ToString();
                
                Terrain terrain = FindTerrainByName(terrainName);
                if (terrain == null)
                {
                    return Response.Error($"Terrain '{terrainName}' not found.");
                }

                UnityEngine.Object.DestroyImmediate(terrain.gameObject);

                return Response.Success($"Terrain '{terrainName}' deleted successfully.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to delete terrain: {e.Message}");
            }
        }

        // Heightmap operations
        private static Response ImportHeightmap(Dictionary<string, object> parameters)
        {
            return Response.Success("Heightmap import functionality not yet implemented.");
        }

        private static Response ExportHeightmap(Dictionary<string, object> parameters)
        {
            return Response.Success("Heightmap export functionality not yet implemented.");
        }

        private static Response SetHeightmap(Dictionary<string, object> parameters)
        {
            return Response.Success("Set heightmap functionality not yet implemented.");
        }

        private static Response GetHeightmap(Dictionary<string, object> parameters)
        {
            return Response.Success("Get heightmap functionality not yet implemented.");
        }

        private static Response GenerateNoiseHeightmap(Dictionary<string, object> parameters)
        {
            return Response.Success("Generate noise heightmap functionality not yet implemented.");
        }

        // Streaming operations
        private static Response SetupStreaming(Dictionary<string, object> parameters)
        {
            return Response.Success("Terrain streaming setup functionality not yet implemented.");
        }

        private static Response CreateTerrainGroup(Dictionary<string, object> parameters)
        {
            return Response.Success("Create terrain group functionality not yet implemented.");
        }

        private static Response LoadTerrainChunk(Dictionary<string, object> parameters)
        {
            return Response.Success("Load terrain chunk functionality not yet implemented.");
        }

        private static Response UnloadTerrainChunk(Dictionary<string, object> parameters)
        {
            return Response.Success("Unload terrain chunk functionality not yet implemented.");
        }

        private static Response GetStreamingStatus(Dictionary<string, object> parameters)
        {
            return Response.Success("Get streaming status functionality not yet implemented.");
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