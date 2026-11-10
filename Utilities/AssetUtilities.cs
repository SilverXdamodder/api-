/*
 * ii's Stupid Menu  Utilities/AssetUtilities.cs
 * A mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Goldentrophy Software
 * https://github.com/iiDk-the-actual/iis.Stupid.Menu
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

using iiMenu.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static iiMenu.Utilities.FileUtilities;

namespace iiMenu.Utilities
{
    public class AssetUtilities
    {
        private static AssetBundle assetBundle;
        private static void LoadAssetBundle()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{PluginInfo.ClientResourcePath}.iimenu");
            if (stream != null)
                assetBundle = AssetBundle.LoadFromStream(stream);
            else
                LogManager.LogError("Failed to load assetbundle");
        }

        public static T LoadObject<T>(string assetName) where T : UnityEngine.Object
        {
            if (assetBundle == null)
                LoadAssetBundle();

            T gameObject = UnityEngine.Object.Instantiate(assetBundle.LoadAsset<T>(assetName));
            return gameObject;
        }

        public static T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            if (assetBundle == null)
                LoadAssetBundle();

            T gameObject = assetBundle.LoadAsset(assetName) as T;
            return gameObject;
        }

        public static readonly Dictionary<string, AudioClip> audioFilePool = new Dictionary<string, AudioClip>();
        public static AudioClip LoadSoundFromFile(string fileName) // Thanks to ShibaGT for help with loading the audio from file
        {
            if (audioFilePool.TryGetValue(fileName, out AudioClip sound))
                return sound;

            string filePath = $"{GetGamePath()}/{PluginInfo.BaseDirectory}/{fileName}";
            if (!File.Exists(filePath))
            {
                LogManager.LogError("Sound file is missing: " + fileName);
                return null;
            }

            UnityWebRequest actualrequest = UnityWebRequestMultimedia.GetAudioClip($"file://{filePath}", GetAudioTypeFromFile(filePath, fileName));
            UnityWebRequestAsyncOperation newvar = actualrequest.SendWebRequest();
            while (!newvar.isDone) { }

            if (actualrequest.result != UnityWebRequest.Result.Success)
                LogManager.LogError($"Could not load {fileName}: {actualrequest.error}");
            else
                sound = DownloadHandlerAudioClip.GetContent(actualrequest);

            actualrequest.Dispose();

            // A broken file used to be cached as null, which silenced that sound until the game restarted
            if (sound == null)
                return null;

            audioFilePool[fileName] = sound;
            return sound;
        }

        /// <summary>
        /// Works out which audio container a file really holds. Some sounds are shipped with an
        /// extension that does not match their contents, and loading those by extension alone
        /// made Unity fail to decode them, so the sound stayed silent.
        /// </summary>
        private static AudioType GetAudioTypeFromFile(string filePath, string fileName)
        {
            try
            {
                byte[] header = new byte[12];
                using (FileStream stream = File.OpenRead(filePath))
                    stream.Read(header, 0, header.Length);

                if (header[0] == 'O' && header[1] == 'g' && header[2] == 'g' && header[3] == 'S')
                    return AudioType.OGGVORBIS;

                if ((header[0] == 'I' && header[1] == 'D' && header[2] == '3') || (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0))
                    return AudioType.MPEG;

                if (header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' && header[8] == 'W' && header[9] == 'A' && header[10] == 'V' && header[11] == 'E')
                    return AudioType.WAV;

                if (header[0] == 'F' && header[1] == 'O' && header[2] == 'R' && header[3] == 'M')
                    return AudioType.AIFF;
            }
            catch { }

            return GetAudioType(GetFileExtension(fileName));
        }

        private static bool HasSoundData(string filePath)
        {
            try { return new FileInfo(filePath).Length > 0; }
            catch { return false; }
        }

        private const string EmbeddedServerResourcePath = "iiMenu.Resources.Server";

        /// <summary>
        /// Reads a sound that was embedded into the plugin at build time.
        /// </summary>
        /// <param name="fileName">Path of the sound inside the server resources, such as Audio/Menu/open.ogg.</param>
        /// <returns>The raw audio bytes, or null when the sound is not embedded.</returns>
        private static byte[] ReadEmbeddedSound(string fileName)
        {
            string resourceName = $"{EmbeddedServerResourcePath}.{fileName.Replace('/', '.').Replace('\\', '.')}";

            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;

            byte[] data = new byte[stream.Length];
            int offset = 0;
            while (offset < data.Length)
            {
                int read = stream.Read(data, offset, data.Length - offset);
                if (read <= 0) break;
                offset += read;
            }

            return offset == data.Length ? data : null;
        }

        private static bool ExtractEmbeddedSound(string fileName, string filePath)
        {
            byte[] data = ReadEmbeddedSound(fileName);
            if (data == null || data.Length == 0)
                return false;

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                // ReSharper disable once AssignNullToNotNullAttribute
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(filePath, data);
            return true;
        }

        public static AudioClip LoadSoundFromURL(string resourcePath, string fileName)
        {
            string filePath = $"{PluginInfo.BaseDirectory}/{fileName}";

            // A failed download used to leave an empty file behind, and the existence check below then
            // kept that sound silent for good. Throw broken files away so they are fetched again.
            if (File.Exists(filePath) && !HasSoundData(filePath))
            {
                audioFilePool.Remove(fileName);
                try { File.Delete(filePath); } catch { }
            }

            if (File.Exists(filePath)) return LoadSoundFromFile(fileName);

            // The menu's own sounds are embedded in the plugin, so they still work when the download
            // host is unreachable or has stopped serving the files.
            if (ExtractEmbeddedSound(fileName, filePath)) return LoadSoundFromFile(fileName);

            LogManager.Log("Downloading " + fileName);
            try
            {
                using WebClient stream = new WebClient();
                stream.DownloadFile(resourcePath, filePath);
            }
            catch (Exception ex)
            {
                LogManager.LogError($"Could not download {fileName}: {ex.Message}");
                try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
                return null;
            }

            if (!File.Exists(filePath) || !HasSoundData(filePath))
            {
                LogManager.LogError($"Downloaded {fileName} but it was empty");
                try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
                return null;
            }

            return LoadSoundFromFile(fileName);
        }

        public static readonly Dictionary<string, Texture2D> textureResourceDictionary = new Dictionary<string, Texture2D>();
        public static Texture2D LoadTextureFromResource(string resourcePath)
        {
            if (textureResourceDictionary.TryGetValue(resourcePath, out Texture2D existingTexture))
                return existingTexture;

            Texture2D texture = new Texture2D(2, 2);

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
            if (stream != null)
            {
                byte[] fileData = new byte[stream.Length];
                // ReSharper disable once MustUseReturnValue
                stream.Read(fileData, 0, (int)stream.Length);
                texture.LoadImage(fileData);
            }
            else
                LogManager.LogError("Failed to load texture from resource: " + resourcePath);

            textureResourceDictionary[resourcePath] = texture;

            return texture;
        }

        public static readonly Dictionary<string, Texture2D> textureUrlDictionary = new Dictionary<string, Texture2D>();
        public static Texture2D LoadTextureFromURL(string resourcePath, string fileName)
        {
            if (textureUrlDictionary.TryGetValue(resourcePath, out Texture2D existingTexture))
                return existingTexture;

            string filePath = $"{PluginInfo.BaseDirectory}/{fileName}";
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                // ReSharper disable once AssignNullToNotNullAttribute
                Directory.CreateDirectory(directory);

            if (!File.Exists(filePath))
            {
                LogManager.Log("Downloading " + fileName);
                WebClient stream = new WebClient();
                stream.DownloadFile(resourcePath, filePath);
            }

            Texture2D texture = LoadTextureFromFile(fileName);

            textureUrlDictionary[resourcePath] = texture;

            return texture;
        }

        public static readonly Dictionary<string, Texture2D> textureFileDirectory = new Dictionary<string, Texture2D>();
        public static Texture2D LoadTextureFromFile(string fileName)
        {
            if (textureFileDirectory.TryGetValue(fileName, out Texture2D existingTexture))
                return existingTexture;

            string filePath = $"{PluginInfo.BaseDirectory}/{fileName}";
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            Texture2D texture = new Texture2D(2, 2);

            byte[] bytes = File.ReadAllBytes(filePath);
            texture.LoadImage(bytes);

            textureFileDirectory[fileName] = texture;

            return texture;
        }
    }
}