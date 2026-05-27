using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.CarSystems.Components;
using Assets.Scripts.CarSystems.Ui;
using Assets.Scripts.Entities;
using Assets.Scripts.System;
using Assets.Scripts.System.Fileparsers;
using UnityEngine;

namespace Assets.Scripts.CarSystems
{
    public class WeaponsController
    {
        private int _activeGroup;
        private readonly Weapon[] _weapons;
        private readonly AudioSource _weaponAudio;
        private readonly AudioClip _weaponEmptySound;
        private readonly AudioClip _weaponBrokenSound;
        private readonly Car _car;
        private readonly List<Weapon> _firingWeapons;
        private readonly List<int> _weaponGroups;
        private readonly WeaponsPanel _panel;

        public WeaponsController(Car car, Vcf vcf, Transform firstPersonTransform)
        {
            _car = car;
            int weaponCount = vcf.Weapons.Count;
            _weapons = new Weapon[weaponCount];
            _weaponAudio = car.gameObject.AddComponent<AudioSource>();
            _weaponAudio.volume = 0.5f;
            AudioCategorySource weaponAudioCategory = _weaponAudio.gameObject.GetComponent<AudioCategorySource>() ?? _weaponAudio.gameObject.AddComponent<AudioCategorySource>();
            weaponAudioCategory.Category = AudioCategory.Sfx;
            weaponAudioCategory.BaseVolume = 0.5f;
            weaponAudioCategory.UpdateVolume();
            _weaponEmptySound = CacheManager.Instance.GetAudioClip("cammo.gpw");
            _weaponBrokenSound = CacheManager.Instance.GetAudioClip("cwstat.gpw");
            _firingWeapons = new List<Weapon>(5);
            _weaponGroups = new List<int>();

            List<VcfWeapon> weaponsList = vcf.Weapons;
            weaponsList.Sort((x, y) =>
            {
                int compare = x.RearFacing.CompareTo(y.RearFacing);
                if (compare != 0)
                {
                    return compare;
                }

                return x.Gdf.WeaponGroup.CompareTo(y.Gdf.WeaponGroup);
            });


            int seperatorIndex = -1;
            for (int i = 0; i < weaponCount; ++i)
            {
                AudioClip fireSound = CacheManager.Instance.GetAudioClip(weaponsList[i].Gdf.SoundName);
                _weapons[i] = new Weapon(weaponsList[i].Gdf, weaponsList[i].Transform)
                {
                    FireSound = fireSound,
                    RearFacing = weaponsList[i].RearFacing,
                    Index = i
                };

                if (_weapons[i].RearFacing)
                {
                    if (seperatorIndex == -1)
                    {
                        seperatorIndex = i;
                    }

                    _weapons[i].WeaponGroupOffset += 100;
                }
                
                if (!_weaponGroups.Contains(_weapons[i].WeaponGroupOffset))
                {
                    _weaponGroups.Add(_weapons[i].WeaponGroupOffset);
                }
            }

            if (firstPersonTransform != null)
            {
                _panel = new WeaponsPanel(firstPersonTransform);
                _panel.SetWeaponCount(weaponCount);
                for (int i = 0; i < weaponCount; ++i)
                {
                    _panel.SetWeaponHealthGroup(i, 0);
                    _panel.SetWeaponAmmoCount(i, _weapons[i].Ammo);
                    _panel.TryGetWeaponSprites(weaponsList[i].Gdf, out _weapons[i].OnSprite, out _weapons[i].OffSprite);
                }

                if (seperatorIndex != -1)
                {
                    _panel.SeparatorIndex = seperatorIndex;
                }

                if (_weaponGroups.Count > 0)
                {
                    _panel.UpdateActiveWeaponGroup(_weaponGroups[_activeGroup], _weapons);
                }
            }
        }

        internal void StopFiring()
        {
            _weaponAudio.Stop();
        }

        public void CycleWeapon()
        {
            if (_weaponGroups.Count < 2)
            {
                return;
            }

            _activeGroup = ++_activeGroup % _weaponGroups.Count;

            if (_panel != null)
            {
                _panel.UpdateActiveWeaponGroup(_weaponGroups[_activeGroup], _weapons);
            }
        }
        
        public void Fire(int weaponIndex)
        {
            _firingWeapons.Clear();
            if (weaponIndex == -1)
            {
                int weaponGroup = _weaponGroups[_activeGroup];
                for (int i = 0; i < _weapons.Length; ++i)
                {
                    Weapon weapon = _weapons[i];
                    if (weapon.WeaponGroupOffset != weaponGroup)
                    {
                        continue;
                    }

                    _firingWeapons.Add(weapon);
                }
            }
            else
            {
                if (_weapons.Length <= weaponIndex)
                {
                    return;
                }

                _firingWeapons.Add(_weapons[weaponIndex]);
            }

            for (int i = 0; i < _firingWeapons.Count; ++i)
            {
                Weapon weapon = _firingWeapons[i];

                if (weapon.Health <= 0)
                {
                    _weaponAudio.PlayIfNotAlreadyPlaying(_weaponBrokenSound);
                    continue;
                }

                if (weapon.Ammo == 0)
                {
                    _weaponAudio.PlayIfNotAlreadyPlaying(_weaponEmptySound);
                    continue;
                }

                if (weapon.Firing || Time.time - weapon.LastFireTime < weapon.Gdf.FiringRate)
                {
                    continue;
                }

                if (weapon.Gdf.FireAmount > 1)
                {
                    _car.StartCoroutine(BurstFire(weapon));
                }
                else
                {
                    FireWeapon(weapon);
                }
            }
        }

        private void FireWeapon(Weapon weapon)
        {
            GetProjectileSpawn(weapon, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            GameObject projObj = UnityEngine.Object.Instantiate(weapon.ProjectilePrefab, position, rotation);
            projObj.transform.localScale = Vector3.Scale(projObj.transform.localScale, scale);

            Projectile projectile = projObj.GetComponent<Projectile>();
            projectile.Velocity = weapon.Gdf.BulletVelocity;
            projectile.Damage = weapon.Gdf.Damage;
            projectile.Owner = _car.transform;
            projObj.SetActive(true);

            weapon.LastFireTime = Time.time;
            _weaponAudio.PlayIfNotAlreadyPlaying(weapon.FireSound);
            --weapon.Ammo;

            if (_panel != null)
            {
                _panel.SetWeaponAmmoCount(weapon.Index, weapon.Ammo);
            }
        }

        private void GetProjectileSpawn(Weapon weapon, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = weapon.Transform.position;
            rotation = weapon.Transform.rotation;
            scale = Vector3.one;

            if (weapon.Gdf.ObjectOffsets != null && weapon.Gdf.ObjectOffsets.Length > 0)
            {
                SdfPart worldOffset = GetProjectileOffset(weapon);
                if (worldOffset != null)
                {
                    position += weapon.Transform.rotation * worldOffset.Position;
                    rotation *= Quaternion.LookRotation(worldOffset.Forward.normalized, worldOffset.Up.normalized);
                    scale = new Vector3(worldOffset.Right.magnitude, worldOffset.Up.magnitude, worldOffset.Forward.magnitude);
                }
            }

            if (weapon.Gdf.Projectile != null)
            {
                position += rotation * weapon.Gdf.Projectile.Position;
                rotation *= Quaternion.LookRotation(weapon.Gdf.Projectile.Forward.normalized, weapon.Gdf.Projectile.Up.normalized);
                Vector3 projectileScale = new Vector3(
                    weapon.Gdf.Projectile.Right.magnitude,
                    weapon.Gdf.Projectile.Up.magnitude,
                    weapon.Gdf.Projectile.Forward.magnitude);
                scale = Vector3.Scale(scale, projectileScale);
            }
        }

        private SdfPart GetProjectileOffset(Weapon weapon)
        {
            if (weapon.Gdf.ObjectOffsets == null)
            {
                return null;
            }

            switch (weapon.MeshType)
            {
                case HardpointMeshType.Top:
                    return weapon.Gdf.ObjectOffsets.Length > 0 ? weapon.Gdf.ObjectOffsets[0] : null;
                case HardpointMeshType.Side:
                    return weapon.Gdf.ObjectOffsets.Length > 1 ? weapon.Gdf.ObjectOffsets[1] : null;
                case HardpointMeshType.Turret:
                    return weapon.Gdf.ObjectOffsets.Length > 2 ? weapon.Gdf.ObjectOffsets[2] : null;
                case HardpointMeshType.Inside:
                    return weapon.Gdf.ObjectOffsets.Length > 3 ? weapon.Gdf.ObjectOffsets[3] : null;
                default:
                    return weapon.Gdf.ObjectOffsets.Length > 0 ? weapon.Gdf.ObjectOffsets[0] : null;
            }
        }

        private IEnumerator BurstFire(Weapon weapon)
        {
            weapon.Firing = true;
            for (int i = 0; i < weapon.Gdf.FireAmount; ++i)
            {
                if (!_car.Alive || weapon.Health <= 0 || weapon.Ammo <= 0)
                {
                    weapon.Firing = false;
                    yield break;
                }

                FireWeapon(weapon);
                yield return weapon.BurstWait;
            }

            yield return weapon.ReloadWait;
            weapon.Firing = false;
        }
    }
}
