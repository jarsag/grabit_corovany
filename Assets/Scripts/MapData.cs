using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace MapLoader
{
    [Serializable]
    public class Manifest
    {
        public string alpha_atlas;
        public List<string> alpha_masks;
        public List<float> ambient;
        public Dictionary<string, Area> areas;
        public List<float> background_color;
        public Dictionary<string, Building> buildings;
        public List<Placement> placements;
        public int map_height_tiles;
        public int map_width_tiles;
        public string map_name;
    }

    [Serializable]
    public class Area
    {
        public List<int> color;
        public List<int> env_color;
        public List<int> light_color;
        public List<float> light_dir;
        public int music;
        public int zone_type;
    }

    [Serializable]
    public class Building
    {
        public int attach_effect_id;
        public bool enable_env_light;
        public bool enable_point_light;
        public string filename;
        public int flag;
        public string glb;
        public bool is_really_big;
        public int obj_type;
        public float point_attenuation;
        public List<int> point_color;
        public int point_range;
        public bool shade_flag;
        public int size_flag;
        public int style;
    }

    [Serializable]
    public class Placement
    {
        public int obj_id;
        public List<float> position;
        public int rotation_y_degrees;
        public int scale;
    }
}
