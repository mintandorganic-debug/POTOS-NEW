using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

public class SteamAvatarExtractor : MonoBehaviour
{
    public static SteamAvatarExtractor Instance { get; private set; }

    private Callback<AvatarImageLoaded_t> avatarLoaded;

    private readonly Dictionary<ulong, Sprite> avatarCache = new();
    private readonly Dictionary<ulong, Action<Sprite>> pendingCallbacks = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        avatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);
    }

    public void GetAvatar(CSteamID steamId, Action<Sprite> callback)
    {
        ulong id = steamId.m_SteamID;

        if (avatarCache.TryGetValue(id, out Sprite sprite))
        {
            callback?.Invoke(sprite);
            return;
        }

        int imageId = SteamFriends.GetLargeFriendAvatar(steamId);

        if (imageId > 0)
        {
            sprite = LoadSprite(imageId);

            if (sprite != null)
            {
                avatarCache[id] = sprite;
                callback?.Invoke(sprite);
                return;
            }
        }

        pendingCallbacks[id] = callback;
    }

    private void OnAvatarLoaded(AvatarImageLoaded_t data)
    {
        ulong id = data.m_steamID.m_SteamID;

        Sprite sprite = LoadSprite(data.m_iImage);

        if (sprite == null)
            return;

        avatarCache[id] = sprite;

        if (pendingCallbacks.TryGetValue(id, out Action<Sprite> callback))
        {
            callback?.Invoke(sprite);
            pendingCallbacks.Remove(id);
        }
    }

    private Sprite LoadSprite(int imageId)
    {
        if (!SteamUtils.GetImageSize(imageId, out uint width, out uint height))
            return null;

        byte[] bytes = new byte[width * height * 4];

        if (!SteamUtils.GetImageRGBA(imageId, bytes, bytes.Length))
            return null;

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);

        texture.LoadRawTextureData(bytes);
        texture.Apply();

        FlipTexture(texture);

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            Vector2.one * 0.5f);
    }

    private void FlipTexture(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();

        int width = texture.width;
        int height = texture.height;

        Color[] flipped = new Color[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            Array.Copy(
                pixels,
                y * width,
                flipped,
                (height - y - 1) * width,
                width);
        }

        texture.SetPixels(flipped);
        texture.Apply();
    }
}