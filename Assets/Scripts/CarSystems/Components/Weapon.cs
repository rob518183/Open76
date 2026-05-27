using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts.CarSystems.Components
{
    public class Weapon
    {
        public bool RearFacing;
        public I76Sprite OnSprite;
        public I76Sprite OffSprite;
        public readonly Gdf Gdf;
        public AudioClip FireSound;
        public int Index;
        public int Ammo;
        public int Health; // TODO: Handle damage to the weapon.
        public int WeaponGroupOffset;
        public HardpointMeshType MeshType;
        public bool Firing;
        public float LastFireTime;
        public readonly Transform Transform;
        public readonly WaitForSeconds BurstWait;
        public readonly WaitForSeconds ReloadWait;
        public GameObject ProjectilePrefab;

        private static Mesh _defaultProjectileMesh;

        static Weapon()
        {
            CreateDefaultProjectileMesh();
        }

        public Weapon(Gdf gdf, Transform transform)
        {
            Gdf = gdf;
            Health = gdf.Health;
            Ammo = gdf.AmmoCount;
            WeaponGroupOffset = gdf.WeaponGroup;

            LoadProjectile();
            Transform = transform;

            if (gdf.FireAmount > 1)
            {
                BurstWait = new WaitForSeconds(gdf.FiringRate);
                ReloadWait = new WaitForSeconds(gdf.BurstRate);
            }
        }

        private void LoadProjectile()
        {
            CacheManager.GeoMeshCacheEntry meshCacheEntry = CacheManager.Instance.ImportMesh(Gdf.Projectile.Name + ".geo", null, 0);

            GameObject obj = Object.Instantiate(CacheManager.Instance.ProjectilePrefab);
            obj.SetActive(false);
            obj.gameObject.name = meshCacheEntry.GeoMesh.Name;

            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                if (meshCacheEntry.Mesh != null && meshCacheEntry.Mesh.vertexCount > 0)
                {
                    meshFilter.sharedMesh = meshCacheEntry.Mesh;
                }
                else
                {
                    meshFilter.mesh = _defaultProjectileMesh;
                }

                MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                if (meshCacheEntry.Materials != null && meshCacheEntry.Materials.Length > 0)
                {
                    renderer.materials = meshCacheEntry.Materials;
                }
                else
                {
                    renderer.material = CacheManager.Instance.GetTextureMaterial(Gdf.Projectile.Name + ".map", true);
                }
            }

            MeshCollider collider = obj.GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = meshCacheEntry.Mesh;
            }

            ProjectilePrefab = obj.gameObject;
        }

        private static void CreateDefaultProjectileMesh()
        {
            const float radius = 0.03f;
            const float halfLength = 0.15f;

            Mesh mesh = new Mesh();

            Vector3[] vertices =
            {
                new Vector3(0f, 0f, halfLength),        // 0 front tip
                new Vector3(radius, 0f, 0f),            // 1 right
                new Vector3(0f, radius, 0f),            // 2 top
                new Vector3(-radius, 0f, 0f),           // 3 left
                new Vector3(0f, -radius, 0f),           // 4 bottom
                new Vector3(0f, 0f, -halfLength),       // 5 back tip
            };

            Vector2[] uvs =
            {
                new Vector2(0.5f, 1f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f)
            };

            int[] triangles =
            {
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 1,
                5, 2, 1,
                5, 3, 2,
                5, 4, 3,
                5, 1, 4
            };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _defaultProjectileMesh = mesh;
        }
    }
}
