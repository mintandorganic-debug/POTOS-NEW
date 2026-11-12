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
        GameLog.Write(
            "SteamAvatarExtractor",
            $"Awake existingInstance={(Instance != null ? Instance.name : "NULL")}");

        if (Instance != null)
        {
            GameLog.Write(
                "SteamAvatarExtractor",
                "Awake duplicate instance destroyed");

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        avatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);

        GameLog.Write(
            "SteamAvatarExtractor",
            "Awake instance registered, AvatarImageLoaded callback created");
    }

    public void GetAvatar(CSteamID steamId, Action<Sprite> callback)
    {
        ulong id = steamId.m_SteamID;

        GameLog.Write(
            "SteamAvatarExtractor",
            $"GetAvatar steamId={id} hasCallback={callback != null} cacheSize={avatarCache.Count} pending={pendingCallbacks.Count}");

        if (avatarCache.TryGetValue(id, out Sprite sprite))
        {
            GameLog.Write(
                "SteamAvatarExtractor",
                $"GetAvatar cache hit steamId={id}");

            callback?.Invoke(sprite);
            return;
        }

        int imageId = SteamFriends.GetLargeFriendAvatar(steamId);

        GameLog.Write(
            "SteamAvatarExtractor",
            $"GetAvatar steamId={id} imageId={imageId}");

        if (imageId > 0)
        {
            sprite = LoadSprite(imageId);

            if (sprite != null)
            {
                avatarCache[id] = sprite;

                GameLog.Write(
                    "SteamAvatarExtractor",
                    $"GetAvatar loaded and cached steamId={id}");

                callback?.Invoke(sprite);
                return;
            }
        }

        GameLog.Write(
            "SteamAvatarExtractor",
            $"GetAvatar avatar not ready, callback stored as pending steamId={id}");

        pendingCallbacks[id] = callback;
    }

    private void OnAvatarLoaded(AvatarImageLoaded_t data)
    {
        ulong id = data.m_steamID.m_SteamID;

        GameLog.Write(
            "SteamAvatarExtractor",
            $"OnAvatarLoaded steamId={id} imageId={data.m_iImage} width={data.m_iWide} height={data.m_iTall}");

        Sprite sprite = LoadSprite(data.m_iImage);

        if (sprite == null)
        {
            GameLog.Write(
                "SteamAvatarExtractor",
                $"OnAvatarLoaded failed to build sprite steamId={id}");

            return;
        }

        avatarCache[id] = sprite;

        if (pendingCallbacks.TryGetValue(id, out Action<Sprite> callback))
        {
            GameLog.Write(
                "SteamAvatarExtractor",
                $"OnAvatarLoaded invoking pending callback steamId={id}");

            callback?.Invoke(sprite);
            pendingCallbacks.Remove(id);
        }
    }

    private Sprite LoadSprite(int imageId)
    {
        GameLog.Write(
            "SteamAvatarExtractor",
            $"LoadSprite imageId={imageId}");

        if (!SteamUtils.GetImageSize(imageId, out uint width, out uint height))
        {
            GameLog.Write(
                "SteamAvatarExtractor",
                $"LoadSprite GetImageSize failed imageId={imageId}");

            return null;
        }

        byte[] bytes = new byte[width * height * 4];

        if (!SteamUtils.GetImageRGBA(imageId, bytes, bytes.Length))
        {
            GameLog.Write(
                "SteamAvatarExtractor",
                $"LoadSprite GetImageRGBA failed imageId={imageId} size={width}x{height}");

            return null;
        }

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);

        texture.LoadRawTextureData(bytes);
        texture.Apply();

        FlipTexture(texture);

        GameLog.Write(
            "SteamAvatarExtractor",
            $"LoadSprite sprite created imageId={imageId} size={width}x{height}");

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            Vector2.one * 0.5f);
    }

    private void FlipTexture(Texture2D texture)
    {
        GameLog.Write(
            "SteamAvatarExtractor",
            $"FlipTexture texture={(texture != null ? texture.name : "NULL")}");

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
