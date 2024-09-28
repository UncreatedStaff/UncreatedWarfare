namespace Uncreated.Warfare.Util;

/// <summary>
/// Utilities for transforming from map to world coordinates and vice-versa.
/// </summary>
public static class CartographyUtility
{
    private static Matrix4x4 _worldToMap = Matrix4x4.identity;
    private static Matrix4x4 _mapToWorld = Matrix4x4.identity;
    private static Vector2Int _mapImageSize = new Vector2Int(Level.MEDIUM_SIZE, Level.MEDIUM_SIZE);

    /// <summary>
    /// Transforms coordinates from normalized world position (x, y, z) to map position [-1 to 1] (x, y, 0).
    /// </summary>
    /// <remarks>Use <see cref="Matrix4x4.MultiplyPoint3x4"/> to transform a point.</remarks>
    public static ref Matrix4x4 WorldToMap => ref _worldToMap;

    /// <summary>
    /// Transforms coordinates from normalized map position [-1 to 1] (x, y, 0) to world position (x, y, z).
    /// </summary>
    /// <remarks>Use <see cref="Matrix4x4.MultiplyPoint3x4"/> to transform a point.</remarks>
    public static ref Matrix4x4 MapToWorld => ref _mapToWorld;

    /// <summary>
    /// The size in pixels of the outputted image.
    /// </summary>
    public static Vector2Int MapImageSize => _mapImageSize;

    /// <summary>
    /// The size of the rectangular area that gets captured in world coordiantes.
    /// </summary>
    /// <remarks>This shouldn't be assumed to be axis-aligned.</remarks>
    public static Vector2 CaptureAreaSize { get; private set; }

    /// <summary>
    /// Converts normalized map coordinates [-1 to 1] to pixel map coodinates [0 to <see cref="MapImageSize"/>], where (0, 0) is lower left of the image.
    /// </summary>
    public static Vector2 DenormalizeMapCoordinates(Vector2 mapCoordinates)
    {
        mapCoordinates.x = mapCoordinates.x / 2f + 0.5f;
        mapCoordinates.y = mapCoordinates.y / 2f + 0.5f;

        mapCoordinates.x *= _mapImageSize.x;
        mapCoordinates.y *= _mapImageSize.y;
        return mapCoordinates;
    }

    /// <summary>
    /// Converts pixel map coodinates [0 to <see cref="MapImageSize"/>] to normalized map coordinates [-1 to 1].
    /// </summary>
    public static Vector2 NormalizeMapCoordinates(Vector2 mapPixelCoodinates)
    {
        mapPixelCoodinates.x /= _mapImageSize.x;
        mapPixelCoodinates.y /= _mapImageSize.y;

        mapPixelCoodinates.x = (mapPixelCoodinates.x - 0.5f) * 2;
        mapPixelCoodinates.y = (mapPixelCoodinates.y - 0.5f) * 2;
        return mapPixelCoodinates;
    }

    /// <summary>
    /// Projects a world coodinate to the world coordiate of a point on a flat 'war table' type barricade, given the x and y size and 3D offset of the table.
    /// </summary>
    public static Matrix4x4 ProjectWorldToMapBarricade(BarricadeDrop drop, Vector3 platformOffset, Vector2 platformSize)
    {
        Vector3 scale = default;
        scale.x = platformSize.x / 2f;
        scale.y = platformSize.y / -2f;

        // fit map into platform
        Vector2 captureSize = CaptureAreaSize;

        float xRatio = platformSize.x / captureSize.x,
              yRatio = platformSize.y / captureSize.y;

        if (xRatio > yRatio)
            scale.x *= yRatio / xRatio;
        else
            scale.y *= xRatio / yRatio;

        Matrix4x4 normalizedToBarricade = Matrix4x4.TRS(
            drop.model.position + platformOffset,
            drop.model.rotation,
            scale
        );

        return normalizedToBarricade * _worldToMap;
    }

    internal static void Init(int level)
    {
        Level.onPrePreLevelLoaded -= Init;

        CartographyVolume? cartoVolume = CartographyVolumeManager.Get().GetMainVolume();

        if (cartoVolume == null)
        {
            // tested on Gulf of Aqaba 09/28/2024
            int levelSize = Level.size;
            int levelBorder = Level.border;

            float captureSize = levelSize - levelBorder * 2;

            _mapToWorld = Matrix4x4.TRS(
                default,
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(captureSize / 2f, captureSize / 2f, 0f)
            );

            // matrices with zero scales can't be inverted automatically apparently
            _worldToMap = Matrix4x4.TRS(
                default,
                Quaternion.Euler(270f, 0f, 0f), 
                new Vector3(2f / captureSize, 0f, 2f / captureSize)
            );

            _mapImageSize = new Vector2Int(levelSize, levelSize);

            CaptureAreaSize = new Vector2(captureSize, captureSize);
        }
        else
        {
            Vector3 position = cartoVolume.transform.position;
            Quaternion rotation = cartoVolume.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
            Vector3 scale = cartoVolume.transform.localScale;

            _mapToWorld = Matrix4x4.TRS(
                position,
                rotation,
                new Vector3(scale.x / 2f, scale.z / 2f, scale.y / 2f)
            );

            _worldToMap = _mapToWorld.inverse;

            // todo id prefer if this were scaled zero on the y axis but inverse doesn't work properly and i cant figure out how to manually reconstruct it
            // Vector3 scale2 = new Vector3(2f / scale.x, 0f, 2f / scale.z);
            // 
            // _worldToMap = Matrix4x4.TRS(
            //     new Vector3(-position.x * scale2.x, 0f, position.z * scale2.z),
            //     Quaternion.LookRotation(cartoVolume.transform.InverseTransformDirection(Vector3.up)),
            //     scale2
            // );

            Vector3 boundsSize = cartoVolume.CalculateLocalBounds().size;

            _mapImageSize = new Vector2Int(Mathf.CeilToInt(boundsSize.x), Mathf.CeilToInt(boundsSize.z));

            CaptureAreaSize = new Vector2(boundsSize.x, boundsSize.z);
        }
    }
}