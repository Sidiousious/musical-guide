using System;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace MusicalGuide;

public class CamController(Configuration configuration)
{
    private const float MaxDist = 20f;
    private const float MinDist = 1.5f;
    private int pendingMountId;
    
    public static unsafe float Distance => Cam()->Distance;

    public void SetDistance(float distance)
    {
        S.Framework.RunOnFrameworkThread(() =>
        {
            InternalSetDistance(distance);
        });
    }

    private unsafe void InternalSetDistance(float distance, int retry = 0, int retryId = 0)
    {
        S.Log.Debug($"InternalSetDistance({distance}, {retry}, {retryId})");
        if (!S.Framework.IsInFrameworkUpdateThread)
        {
            S.Log.Error("CamController not in framework update thread.");
            return;
        }

        switch (retry)
        {
            case > 0 when pendingMountId != retryId:
                S.Log.Info("Waiting for mount cancelled");
                return;
            case 0:
                pendingMountId = -1;
                break;
        }

        if (configuration.UseFurtherCameraForLargerMounts)
        {
            try
            {
                var hitboxSize = MountHitboxSize();
                S.Log.Debug($"Mount hitbox size: {hitboxSize}");
                distance = MathF.Max(MinDist, MathF.Min(MaxDist, ((hitboxSize - 1f) * 2) + distance));
            }
            catch (NotReadyException)
            {
                if (pendingMountId == -1)
                {
                    pendingMountId = Random.Shared.Next(int.MaxValue);
                    retryId = pendingMountId;
                }

                if (retry > 3)
                {
                    S.Log.Info("Did not find mount in time");
                    return;
                }
                S.Framework.RunOnTick(() =>
                {
                    InternalSetDistance(distance, retry + 1, retryId);
                }, TimeSpan.FromMilliseconds(400));
                return;
            }
        }

        S.Log.Debug($"Setting distance to {distance}");
        Cam()->Distance = distance;
    }

    private static float MountHitboxSize()
    {
        var mountId = S.ClientState.LocalPlayer?.CurrentMount?.ValueNullable?.RowId;
        if (mountId is null)
        {
            S.Log.Debug("Mount was not found");
            return 1f;
        }

        try
        {
            return S.ObjectTable.First(s => s.ObjectKind == ObjectKind.MountType).HitboxRadius;
        }
        catch (InvalidOperationException)
        {
            S.Log.Debug("Mount object was not found");
            S.Log.Debug($"Looking for {mountId} in {string.Join(',', S.ObjectTable.Select(s => s))}");
            throw new NotReadyException();
        }
    }
    
    private static unsafe Camera* Cam()
    {
        return CameraManager.Instance()->GetActiveCamera();
    }
}

internal class NotReadyException : Exception
{
}
