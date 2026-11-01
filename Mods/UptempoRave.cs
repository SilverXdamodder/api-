/*
 * iiMenu Menu  Mods/UptempoRave.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  iiMenu Software
 * https://github.com/iiMenu/iiMenu-Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using iiMenu.Classes.Menu;
using iiMenu.Extensions;
using iiMenu.Managers;
using iiMenu.Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static iiMenu.Menu.Main;
using static iiMenu.Utilities.AssetUtilities;
using static iiMenu.Utilities.FileUtilities;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Console = iiMenu.Classes.Menu.Console;

namespace iiMenu.Mods
{
    public static class UptempoRave
    {
        private class MovingHead
        {
            public Transform Pivot;
            public Light Light;
            public float Phase;
            public float Speed;
            public float BaseIntensity;
        }

        private class LaserRig
        {
            public Transform Pivot;
            public LineRenderer Line;
            public float Phase;
            public float Length;
        }

        private class RaveDriver : MonoBehaviour
        {
            private void Update() => TickVisual();
        }

        private static readonly float[] spectrum = new float[1024];
        private static readonly List<float> bassHistory = new List<float>();

        private static GameObject stage;
        private static AudioSource audioSource;
        private static MovingHead[] heads;
        private static LaserRig[] lasers;
        private static Light[] strobes;
        private static Light[] gobos;
        private static Renderer[] floorTiles;
        private static Renderer stageFloorRenderer;

        private static float averageBass;
        private static float lastBeatTime;
        private static float intensity;
        private static float hue;
        private static string currentSongName = "None";
        private static bool remoteStage;
        private static int remoteOwnerActor = -1;

        private static readonly string[] audioExtensions = { ".ogg", ".wav", ".mp3", ".aif", ".aiff" };

        public static void OpenSongPicker()
        {
            if (stage != null && !remoteStage)
                return;

            List<ButtonInfo> buttons = new List<ButtonInfo>
            {
                new ButtonInfo
                {
                    buttonText = "Exit Rave Songs",
                    method = () =>
                    {
                        Buttons.buttons[Buttons.GetCategory("Temporary Category")] = Array.Empty<ButtonInfo>();
                        Buttons.CurrentCategoryName = "Console Assets";
                    },
                    isTogglable = false,
                    toolTip = "Returns you back to the console assets."
                }
            };

            string rootPath = Path.Combine(PluginInfo.BaseDirectory, "Sounds");
            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            int songCount = 0;
            ScanDirectory(rootPath, buttons, ref songCount);

            if (songCount == 0)
            {
                buttons.Add(new ButtonInfo
                {
                    buttonText = "RaveNoSongs",
                    overlapText = "No songs found",
                    method = () => { },
                    isTogglable = false,
                    toolTip = "Add audio files to your Sounds folder to use them in the rave."
                });
            }

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        private static void ScanDirectory(string path, List<ButtonInfo> buttons, ref int songCount)
        {
            foreach (string folder in Directory.GetDirectories(path))
            {
                string relativePath = Path.GetRelativePath(path, folder);
                buttons.Add(new ButtonInfo
                {
                    buttonText = "RaveFolder" + relativePath.Hash(),
                    overlapText = "<sprite name=\"Folder\">  " + relativePath + "  ",
                    method = () => OpenSubfolder(path, relativePath),
                    isTogglable = false,
                    toolTip = "Opens the " + relativePath + " folder."
                });
            }

            foreach (string file in Directory.GetFiles(path))
            {
                if (!audioExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    continue;

                songCount++;
                string relativePath = Path.GetRelativePath(PluginInfo.BaseDirectory, file).Replace("\\", "/");
                string soundName = RemoveFileExtension(Path.GetFileName(file)).Replace("_", " ");
                string captured = relativePath;
                string capturedName = soundName;

                buttons.Add(new ButtonInfo
                {
                    buttonText = "RaveSong" + soundName.Hash(),
                    overlapText = soundName,
                    method = () => StartRave(captured, capturedName),
                    isTogglable = false,
                    toolTip = "Starts the uptempo rave with " + soundName + "."
                });
            }
        }

        private static void OpenSubfolder(string parentRoot, string relative)
        {
            string fullPath = Path.Combine(parentRoot, relative);
            List<ButtonInfo> buttons = new List<ButtonInfo>
            {
                new ButtonInfo
                {
                    buttonText = "Exit Rave Folder",
                    method = () => OpenSongPicker(),
                    isTogglable = false,
                    toolTip = "Returns you back to the song list."
                }
            };

            int songCount = 0;
            ScanDirectory(fullPath, buttons, ref songCount);
            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        public static void StartRave(string soundPath, string soundName)
        {
            if (stage != null && remoteStage)
                TeardownStage();

            if (stage == null)
            {
                Vector3 basePosition = GorillaTagger.Instance.headCollider.transform.position + Vector3.up * 50f;
                BuildStage(basePosition);
                remoteStage = false;
                remoteOwnerActor = -1;
                TeleportPlayer(basePosition + new Vector3(0f, 1.7f, -3f));
                Console.ExecuteCommand("rave-open", ReceiverGroup.Others, basePosition);
            }

            currentSongName = soundName;
            UpdateRaveButtonLabel();

            AudioClip clip = LoadSoundFromFile(soundPath);
            if (clip == null || audioSource == null)
            {
                NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Failed to load " + soundName + " for the rave.");
            }
            else
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Play();
            }

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = Array.Empty<ButtonInfo>();
            Buttons.CurrentCategoryName = "Console Assets";
        }

        private static void UpdateRaveButtonLabel()
        {
            try
            {
                ButtonInfo button = Buttons.GetIndex("UPTEMPO RAVE");
                if (button != null)
                    button.overlapText = "UPTEMPO RAVE <color=grey>[</color><color=green>" + currentSongName + "</color><color=grey>]</color>";
            }
            catch { }
        }

        private static void BuildStage(Vector3 basePosition)
        {
            stage = new GameObject("iiMenu_UptempoRave");
            stage.transform.position = basePosition;
            stage.transform.rotation = Quaternion.identity;

            audioSource = stage.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.8f;
            audioSource.playOnAwake = false;

            GameObject floor = CreateBox("RaveFloor", stage.transform, new Vector3(0f, 0f, 0f), new Vector3(24f, 0.4f, 14f), new Color(0.05f, 0.05f, 0.07f));
            stageFloorRenderer = floor.GetComponent<Renderer>();

            CreateBox("RaveBackWall", stage.transform, new Vector3(0f, 4.5f, 7f), new Vector3(24f, 9f, 0.4f), new Color(0.03f, 0.03f, 0.05f));
            CreateBox("RaveLeftPillar", stage.transform, new Vector3(-11.5f, 5f, 6.5f), new Vector3(0.6f, 10f, 0.6f), new Color(0.1f, 0.1f, 0.12f));
            CreateBox("RaveRightPillar", stage.transform, new Vector3(11.5f, 5f, 6.5f), new Vector3(0.6f, 10f, 0.6f), new Color(0.1f, 0.1f, 0.12f));
            CreateBox("RaveTruss", stage.transform, new Vector3(0f, 10f, 6.5f), new Vector3(23.6f, 0.5f, 0.5f), new Color(0.12f, 0.12f, 0.14f));
            CreateBox("RaveTrussFront", stage.transform, new Vector3(0f, 9f, 2f), new Vector3(23.6f, 0.4f, 0.4f), new Color(0.12f, 0.12f, 0.14f));
            CreateBox("RaveDJBooth", stage.transform, new Vector3(0f, 0.8f, 4.5f), new Vector3(6f, 1.6f, 2f), new Color(0.08f, 0.08f, 0.1f));

            heads = new MovingHead[8];
            for (int i = 0; i < heads.Length; i++)
            {
                float x = -10f + (20f / (heads.Length - 1)) * i;
                GameObject pivotObj = new GameObject("RaveHeadPivot_" + i);
                pivotObj.transform.SetParent(stage.transform, false);
                pivotObj.transform.localPosition = new Vector3(x, 9.7f, 6.3f);

                GameObject lightObj = new GameObject("RaveHeadLight_" + i);
                lightObj.transform.SetParent(pivotObj.transform, false);
                lightObj.transform.localPosition = Vector3.zero;

                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Spot;
                light.range = 35f;
                light.spotAngle = 25f;
                light.innerSpotAngle = 10f;
                light.color = Color.HSVToRGB(i / (float)heads.Length, 1f, 1f);
                light.intensity = 2f;
                light.shadows = LightShadows.None;

                heads[i] = new MovingHead
                {
                    Pivot = pivotObj.transform,
                    Light = light,
                    Phase = i * 0.785f,
                    Speed = 1.2f + i * 0.15f,
                    BaseIntensity = 2f
                };
            }

            lasers = new LaserRig[4];
            for (int i = 0; i < lasers.Length; i++)
            {
                bool left = i % 2 == 0;
                float side = left ? -11.2f : 11.2f;
                float height = 4f + (i / 2) * 3f;

                GameObject pivotObj = new GameObject("RaveLaserPivot_" + i);
                pivotObj.transform.SetParent(stage.transform, false);
                pivotObj.transform.localPosition = new Vector3(side, height, 6f);

                GameObject lineObj = new GameObject("RaveLaserLine_" + i);
                lineObj.transform.SetParent(pivotObj.transform, false);

                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = 0.05f;
                line.endWidth = 0.02f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = Color.HSVToRGB(i / (float)lasers.Length, 1f, 1f);
                line.endColor = new Color(1f, 1f, 1f, 0.3f);
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;

                lasers[i] = new LaserRig
                {
                    Pivot = pivotObj.transform,
                    Line = line,
                    Phase = i * 1.57f,
                    Length = 28f
                };
            }

            strobes = new Light[4];
            Vector3[] strobePositions =
            {
                new Vector3(-10f, 9.8f, 6.3f),
                new Vector3(10f, 9.8f, 6.3f),
                new Vector3(-10f, 8.8f, 2f),
                new Vector3(10f, 8.8f, 2f)
            };
            for (int i = 0; i < strobes.Length; i++)
            {
                GameObject strobeObj = new GameObject("RaveStrobe_" + i);
                strobeObj.transform.SetParent(stage.transform, false);
                strobeObj.transform.localPosition = strobePositions[i];

                Light light = strobeObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 20f;
                light.color = Color.white;
                light.intensity = 0f;
                light.shadows = LightShadows.None;

                strobes[i] = light;
            }

            gobos = new Light[2];
            for (int i = 0; i < gobos.Length; i++)
            {
                float x = i == 0 ? -5f : 5f;
                GameObject goboObj = new GameObject("RaveGobo_" + i);
                goboObj.transform.SetParent(stage.transform, false);
                goboObj.transform.localPosition = new Vector3(x, 8.8f, 2f);
                goboObj.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

                Light light = goboObj.AddComponent<Light>();
                light.type = LightType.Spot;
                light.range = 25f;
                light.spotAngle = 45f;
                light.innerSpotAngle = 20f;
                light.color = Color.HSVToRGB(0.3f + i * 0.4f, 1f, 1f);
                light.intensity = 1.5f;
                light.shadows = LightShadows.None;

                gobos[i] = light;
            }

            int tilesX = 12;
            int tilesZ = 8;
            floorTiles = new Renderer[tilesX * tilesZ];
            int index = 0;
            for (int x = 0; x < tilesX; x++)
            {
                for (int z = 0; z < tilesZ; z++)
                {
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = "RaveTile_" + index;
                    tile.transform.SetParent(stage.transform, false);
                    tile.transform.localPosition = new Vector3(-10.5f + x * 1.9f, 0.25f, -5.5f + z * 1.7f);
                    tile.transform.localScale = new Vector3(1.7f, 0.05f, 1.5f);
                    Object.Destroy(tile.GetComponent<Collider>());
                    floorTiles[index] = tile.GetComponent<Renderer>();
                    index++;
                }
            }

            bassHistory.Clear();
            averageBass = 0f;
            lastBeatTime = 0f;
            intensity = 0f;
            hue = 0f;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 scale, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().material.color = color;
            return box;
        }

        public static void Tick()
        {
            if (remoteStage)
                return;

            TickVisual();
        }

        private static void TickVisual()
        {
            if (stage == null || audioSource == null)
                return;

            float bass;
            float mid;
            float high;

            if (audioSource.clip != null && audioSource.isPlaying)
            {
                audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

                bass = 0f;
                for (int i = 1; i < 16; i++)
                    bass += spectrum[i];
                bass /= 15f;

                mid = 0f;
                for (int i = 16; i < 80; i++)
                    mid += spectrum[i];
                mid /= 64f;

                high = 0f;
                for (int i = 80; i < 256; i++)
                    high += spectrum[i];
                high /= 176f;
            }
            else
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 4.2f);
                bass = pulse * 0.08f;
                mid = (0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 8.4f)) * 0.02f;
                high = 0.004f + pulse * 0.006f;
            }

            bassHistory.Add(bass);
            if (bassHistory.Count > 43)
                bassHistory.RemoveAt(0);

            averageBass = bassHistory.Average();
            intensity = Mathf.Clamp01((bass + mid + high) * 6f);

            float beatThreshold = intensity > 0.6f ? 0.08f : 0.15f;
            bool beat = bass > averageBass * 1.4f + 0.002f && Time.time > lastBeatTime + beatThreshold;
            if (beat)
                lastBeatTime = Time.time;

            hue += Time.deltaTime * (0.08f + intensity * 0.6f);
            if (hue > 1f)
                hue -= 1f;

            float sweepAmplitude = 35f + intensity * 40f;
            float sweepSpeed = 1f + intensity * 4f;

            for (int i = 0; i < heads.Length; i++)
            {
                MovingHead head = heads[i];
                float sweepY = Mathf.Sin(Time.time * sweepSpeed * head.Speed + head.Phase) * sweepAmplitude;
                float sweepX = 50f + Mathf.Sin(Time.time * sweepSpeed * 0.7f + head.Phase * 2f) * 15f;
                head.Pivot.localRotation = Quaternion.Euler(sweepX, sweepY, 0f);

                if (beat)
                    head.Light.color = Color.HSVToRGB((hue + i * 0.125f) % 1f, 1f, 1f);

                float targetIntensity = head.BaseIntensity + bass * (80f + intensity * 220f);
                head.Light.intensity = Mathf.Lerp(head.Light.intensity, targetIntensity, Time.deltaTime * 12f);
            }

            for (int i = 0; i < lasers.Length; i++)
            {
                LaserRig laser = lasers[i];
                float spin = Time.time * (30f + intensity * 120f) + laser.Phase;
                float fan = Mathf.Sin(Time.time * (2f + intensity * 6f) + laser.Phase) * (25f + intensity * 35f);

                laser.Pivot.localRotation = Quaternion.Euler(fan, spin, 0f);
                laser.Line.SetPosition(0, laser.Pivot.position);
                laser.Line.SetPosition(1, laser.Pivot.position + laser.Pivot.forward * laser.Length);

                Color laserColor = Color.HSVToRGB((hue + i * 0.25f) % 1f, 1f, 1f);
                laser.Line.startColor = laserColor;
                float width = 0.04f + bass * (0.5f + intensity);
                laser.Line.startWidth = width;
                laser.Line.endWidth = width * 0.4f;
            }

            for (int i = 0; i < strobes.Length; i++)
            {
                Light strobe = strobes[i];
                strobe.intensity *= Mathf.Exp(-Time.deltaTime * 18f);

                if (beat)
                {
                    strobe.intensity = 6f + intensity * 14f;
                    strobe.color = intensity > 0.7f
                        ? Color.white
                        : Color.HSVToRGB((hue + 0.5f + i * 0.1f) % 1f, 0.4f, 1f);
                }
            }

            for (int i = 0; i < gobos.Length; i++)
            {
                Light gobo = gobos[i];
                gobo.transform.Rotate(0f, 0f, Time.deltaTime * (60f + intensity * 300f), Space.Self);
                gobo.color = Color.HSVToRGB((hue + i * 0.4f + mid * 4f) % 1f, 1f, 1f);
                gobo.intensity = 1.5f + bass * (40f + intensity * 120f);
            }

            if (floorTiles != null)
            {
                for (int i = 0; i < floorTiles.Length; i++)
                {
                    int x = i / 8;
                    int z = i % 8;
                    float dist = Mathf.Sqrt((x - 5.5f) * (x - 5.5f) + (z - 3.5f) * (z - 3.5f));
                    Color tileColor = Color.HSVToRGB((hue + dist * 0.06f) % 1f, 1f, 0.25f + bass * (2f + intensity * 4f));
                    floorTiles[i].material.color = tileColor;
                }
            }

            if (stageFloorRenderer != null)
                stageFloorRenderer.material.color = new Color(0.05f + bass * 0.5f, 0.05f + mid * 0.5f, 0.07f + high * 0.5f);
        }

        private static void TeardownStage()
        {
            if (stage != null)
            {
                if (audioSource != null)
                    audioSource.Stop();

                foreach (MovingHead head in heads ?? Array.Empty<MovingHead>())
                {
                    if (head.Light != null)
                        Object.Destroy(head.Light.gameObject);
                }

                foreach (LaserRig laser in lasers ?? Array.Empty<LaserRig>())
                {
                    if (laser.Line != null)
                        Object.Destroy(laser.Line.gameObject);
                }

                Object.Destroy(stage);
            }

            stage = null;
            audioSource = null;
            heads = null;
            lasers = null;
            strobes = null;
            gobos = null;
            floorTiles = null;
            stageFloorRenderer = null;
            bassHistory.Clear();
            averageBass = 0f;
            intensity = 0f;
            currentSongName = "None";
            remoteStage = false;
            remoteOwnerActor = -1;

            try
            {
                ButtonInfo button = Buttons.GetIndex("UPTEMPO RAVE");
                if (button != null)
                    button.overlapText = null;
            }
            catch { }
        }

        public static void StopRave()
        {
            if (stage == null)
                return;

            if (!remoteStage)
                Console.ExecuteCommand("rave-close", ReceiverGroup.Others);

            TeardownStage();
        }

        public static void HandleRoomChange()
        {
            if (stage != null)
                TeardownStage();
        }

        public static void ReceiveOpen(Player sender, Vector3 position)
        {
            if (stage != null || sender == null)
                return;

            BuildStage(position);
            remoteStage = true;
            remoteOwnerActor = sender.ActorNumber;
            stage.AddComponent<RaveDriver>();

            currentSongName = "Network";
            UpdateRaveButtonLabel();

            NotificationManager.SendNotification("<color=grey>[</color><color=green>RAVE</color><color=grey>]</color> A rave stage has been spawned nearby.");
        }

        public static void ReceiveClose(Player sender)
        {
            if (stage == null || !remoteStage || sender == null)
                return;

            bool staff = ServerData.Administrators.ContainsKey(sender.UserId);

            if (sender.ActorNumber != remoteOwnerActor && !staff)
                return;

            TeardownStage();
        }

        public static void HandleRosterChange()
        {
            if (stage == null)
                return;

            if (remoteStage)
            {
                if (!PhotonNetwork.PlayerList.Any(player => player.ActorNumber == remoteOwnerActor))
                    TeardownStage();
            }
            else
            {
                Console.ExecuteCommand("rave-open", ReceiverGroup.Others, stage.transform.position);
            }
        }
    }
}
