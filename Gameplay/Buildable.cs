using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    public enum BuildableType {

        Floor=0,
        Anywhere=5,
        GridFloor=10,
        GridAnywhere=15

    }

    /// <summary>
    /// Buildable is the base script for objects that can be placed on the map and have a build mode (transparent version of model that follow mouse)
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    public class Buildable : MonoBehaviour
    {
        [Header("Buildable")]
        public BuildableType type;
        public float build_distance = 2f;
        public float grid_size = 1f;

        [Header("Build Obstacles")]
        public LayerMask obstacle_layer = 1; //Cant build on top of those obstacles
        public float build_obstacle_radius = 0.4f; //Can't build if obstacles within radius
        public float build_ground_dist = 0.4f; //Ground must be at least this distance on all 4 sides to build (prevent building on a wall or in the air)

        [Header("FX")]
        public AudioClip build_audio;
        public GameObject build_fx;

        public UnityAction onBuild;

        protected Selectable selectable;
        protected Destructible destruct; //Can be nulls
        protected UniqueID unique_id; //Can be nulls

        private bool building_mode = false; //Building mode means player is selecting where to build it, but it doesnt really exists yet
        private bool position_set = false;
        private Color prev_color = Color.white;

        private List<Collider> colliders = new List<Collider>();
        private List<MeshRenderer> renders = new List<MeshRenderer>();
        private List<Material> materials = new List<Material>();
        private List<Material> materials_transparent = new List<Material>();
        private List<Color> materials_color = new List<Color>();

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();
            renders.AddRange(GetComponentsInChildren<MeshRenderer>());

            foreach (MeshRenderer render in renders)
            {
                foreach (Material material in render.sharedMaterials)
                {
                    Material material_normal = new Material(material);
                    Material material_trans = new Material(material);
                    MaterialTool.ChangeRenderMode(material_trans, BlendMode.Fade);
                    materials.Add(material_normal);
                    materials_transparent.Add(material_trans);
                    materials_color.Add(material.color);
                }
            }

            foreach (Collider collide in GetComponentsInChildren<Collider>())
            {
                if (collide.enabled && !collide.isTrigger)
                {
                    colliders.Add(collide);
                }
            }
        }

        void Start()
        {

        }

        void Update()
        {
            if (building_mode)
            {
                PlayerControls constrols = PlayerControls.Get();
                PlayerControlsMouse mouse = PlayerControlsMouse.Get();

                if (!position_set)
                {
                    if (mouse.IsUsingMouse() || Mathf.Abs(constrols.GetRotateCam()) > 0.1f)
                    {
                        transform.position = mouse.GetPointingPos();
                        transform.rotation = TheCamera.Get().GetFacingRotation();

                        if (type == BuildableType.Anywhere)
                            transform.position = FindBuildPosition(mouse.GetPointingPos());
                        if(type == BuildableType.GridFloor)
                            transform.position = FindGridPosition(mouse.GetPointingPos());
                        if (type == BuildableType.GridAnywhere)
                            transform.position = FindBuildPosition(FindGridPosition(mouse.GetPointingPos()));
                    }
                }

                bool can_build = CheckIfCanBuild();
                Color color = can_build ? Color.white : Color.red * 0.9f;
                SetModelColor(new Color(color.r, color.g, color.b, 0.5f), !can_build);

            }
        }

        public void StartBuild()
        {
            building_mode = true;
            position_set = false;

            selectable.enabled = false;
            if (destruct)
                destruct.enabled = false;

            foreach (Collider collide in colliders)
                collide.isTrigger = true;

            if (TheGame.IsMobile()) //Hide on mobile
            {
                foreach (MeshRenderer mesh in renders)
                    mesh.enabled = false;
            }
        }

        public void SetBuildPosition(Vector3 pos)
        {
            if (building_mode && !position_set)
            {
                position_set = true;
                transform.position = pos;

                if (type == BuildableType.Anywhere)
                    transform.position = FindBuildPosition(pos);

                if (type == BuildableType.GridFloor)
                    transform.position = FindGridPosition(pos);

                if (type == BuildableType.GridAnywhere)
                    transform.position = FindBuildPosition(FindGridPosition(pos));

                foreach (MeshRenderer mesh in renders)
                    mesh.enabled = true;
            }
        }

        public void FinishBuild()
        {
            gameObject.SetActive(true);
            building_mode = false;
            position_set = true;

            foreach (Collider collide in colliders)
                collide.isTrigger = false;
            foreach (MeshRenderer mesh in renders)
                mesh.enabled = true;

            selectable.enabled = true;
            if (destruct)
            {
                destruct.enabled = true;
            }

            SetModelColor(Color.white, false);

            if (build_fx != null)
                Instantiate(build_fx, transform.position, Quaternion.identity);
            
            if (onBuild != null)
                onBuild.Invoke();
        }

        private void SetModelColor(Color color, bool replace)
        {
            if (color != prev_color)
            {
                int index = 0;
                foreach (MeshRenderer render in renders)
                {
                    Material[] mesh_materials = render.sharedMaterials;
                    for (int i = 0; i < mesh_materials.Length; i++)
                    {
                        if (index < materials.Count && index < materials_transparent.Count)
                        {
                            Material mesh_mat = mesh_materials[i];
                            Material ref_mat = color.a < 0.99f ? materials_transparent[index] : materials[index];
                            ref_mat.color = materials_color[index] * color;
                            if (replace)
                                ref_mat.color = color; 
                            if (ref_mat != mesh_mat)
                                mesh_materials[i] = ref_mat;
                        }
                        index++;
                    }
                    render.sharedMaterials = mesh_materials;
                }
            }

            prev_color = color;
        }

        //Check if possible to build at current position
        public bool CheckIfCanBuild()
        {
            if (type == BuildableType.Anywhere || type == BuildableType.GridAnywhere)
                return !CheckObstacleGround();
            return !CheckIfOverlap() && CheckIfFlatGround() && !CheckObstacleGround();
        }

        //Check if overlaping another object (cant build)
        public bool CheckIfOverlap()
        {
            List<Collider> overlap_colliders = new List<Collider>();
            int this_layer = 1 << gameObject.layer;
            int layer_mask = obstacle_layer | this_layer;

            //Check collision with bounding box
            foreach (Collider collide in colliders) {
                Collider[] over = Physics.OverlapBox(transform.position, collide.bounds.extents, Quaternion.identity, layer_mask);
                foreach (Collider overlap in over)
                {
                    if(!overlap.isTrigger)
                        overlap_colliders.Add(overlap);
                }
            }

            //Check collision with radius (includes triggers)
            if (build_obstacle_radius > 0.01f)
            {
                Collider[] over = Physics.OverlapSphere(transform.position, build_obstacle_radius, layer_mask);
                overlap_colliders.AddRange(over);
            }

            //Check collision list
            foreach (Collider overlap in overlap_colliders)
            {
                if (overlap != null)
                {
                    PlayerCharacter player = overlap.GetComponent<PlayerCharacter>();
                    Buildable buildable = overlap.GetComponentInParent<Buildable>();
                    if (player == null && buildable != this) //Dont overlap with player and dont overlap with itself
                        return true;
                }
            }

            return false;
        }

        //Check if there is a flat floor underneath (can't build a steep cliff)
        public bool CheckIfFlatGround()
        {
            Vector3 center = transform.position + Vector3.up * build_ground_dist;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (build_ground_dist + build_ground_dist);

            LayerMask mask = ~0; //Everything
            RaycastHit h1, h2, h3, h4;
            bool f1 = Physics.Raycast(p1, dir, out h1, dir.magnitude, mask, QueryTriggerInteraction.Ignore);
            bool f2 = Physics.Raycast(p2, dir, out h2, dir.magnitude, mask, QueryTriggerInteraction.Ignore);
            bool f3 = Physics.Raycast(p3, dir, out h3, dir.magnitude, mask, QueryTriggerInteraction.Ignore);
            bool f4 = Physics.Raycast(p4, dir, out h4, dir.magnitude, mask, QueryTriggerInteraction.Ignore);

            return f1 && f2 && f3 && f4;
        }

        //Check if ground is valid layer
        public bool CheckObstacleGround()
        {
            Vector3 center = transform.position + Vector3.up * build_ground_dist;
            Vector3 dir = Vector3.down * (build_ground_dist + build_ground_dist);
            RaycastHit h1;
            bool f1 = Physics.Raycast(center, dir, out h1, dir.magnitude, obstacle_layer.value, QueryTriggerInteraction.Ignore);
            return f1 && h1.collider.GetComponentInParent<Buildable>() != this;
        }

        //Find the height of buildable (mostly for Anywhere mode) to build on top of other things
        public Vector3 FindBuildPosition(Vector3 pos)
        {
            float offset = 10f;
            Vector3 center = pos + Vector3.up * offset;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (offset + build_ground_dist);

            LayerMask mask = ~0; //Everything
            RaycastHit h1, h2, h3, h4;
            bool f1 = Physics.Raycast(p1, dir, out h1, dir.magnitude, mask, QueryTriggerInteraction.Ignore);
            bool f2 = Physics.Raycast(p2, dir, out h2, dir.magnitude, mask, QueryTriggerInteraction.Ignore);
            bool f3 = Physics.Raycast(p3, dir, out h3, dir.magnitude, mask, QueryTriggerInteraction.Ignore);
            bool f4 = Physics.Raycast(p4, dir, out h4, dir.magnitude, mask, QueryTriggerInteraction.Ignore);

            Vector3 dist_dir = Vector3.zero;
            int nb = 0;
            if (f1 && h1.collider.GetComponentInParent<PlayerCharacter>() == null) { dist_dir += Vector3.down * h1.distance; nb++; }
            if (f2 && h2.collider.GetComponentInParent<PlayerCharacter>() == null) { dist_dir += Vector3.down * h2.distance; nb++; }
            if (f3 && h3.collider.GetComponentInParent<PlayerCharacter>() == null) { dist_dir += Vector3.down * h3.distance; nb++; }
            if (f4 && h4.collider.GetComponentInParent<PlayerCharacter>() == null) { dist_dir += Vector3.down * h4.distance; nb++; }

            if (nb > 0)
                return center + (dist_dir / nb);
            return pos;
        }

        public Vector3 FindGridPosition(Vector3 pos) {
            if (grid_size >= 0.1f)
            {
                float x = Mathf.RoundToInt(pos.x / grid_size) * grid_size;
                float z = Mathf.RoundToInt(pos.z / grid_size) * grid_size;
                return new Vector3(x, pos.y, z);

            }
            return pos;
        }

        public bool IsBuilding()
        {
            return building_mode;
        }

        public Destructible GetDestructible()
        {
            return destruct; //May be null
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        public string GetUID()
        {
            if (unique_id != null)
                return unique_id.unique_id;
            return "";
        }
    }

}