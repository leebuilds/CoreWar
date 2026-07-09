/// <summary>
/// Per-weapon reserve + magazine ammo pools.
/// </summary>
public struct WeaponAmmoPool
{
    public int reserve;
    public int mag;
    public int magSize;
    public int maxTotal;

    public WeaponAmmoPool(int startReserve, int startMag, int magSize, int maxTotal)
    {
        reserve = startReserve;
        mag = startMag;
        this.magSize = magSize;
        this.maxTotal = maxTotal;
    }

    public bool CanFire => mag > 0;
    public bool IsMagFull => mag >= magSize;
    public bool HasReserve => reserve > 0;
    public bool NeedsReload => !IsMagFull && HasReserve;

    public void ConsumeRound()
    {
        if (mag > 0)
        {
            mag--;
        }
    }

    public int FillMagFromReserve()
    {
        int needed = magSize - mag;
        if (needed <= 0 || reserve <= 0)
        {
            return 0;
        }

        int transferred = needed < reserve ? needed : reserve;
        mag += transferred;
        reserve -= transferred;
        return transferred;
    }

    public int LoadSingleRound()
    {
        if (mag >= magSize || reserve <= 0)
        {
            return 0;
        }

        mag++;
        reserve--;
        return 1;
    }

    public void SyncReserveFromShared(int sharedReserve)
    {
        reserve = sharedReserve;
    }

    public int FillMagFromSharedReserve(ref int sharedReserve)
    {
        int needed = magSize - mag;
        if (needed <= 0 || sharedReserve <= 0)
        {
            return 0;
        }

        int transferred = needed < sharedReserve ? needed : sharedReserve;
        mag += transferred;
        sharedReserve -= transferred;
        reserve = sharedReserve;
        return transferred;
    }
}

public static class WeaponAmmoDefaults
{
    public const int PistolMagSize = 12;
    public const int PistolMaxTotal = 162;
    public const int PistolStartReserve = 150;

    public const int AssaultRifleMagSize = 30;
    public const int AssaultRifleMaxTotal = 230;
    public const int AssaultRifleStartReserve = 200;

    public const int SmgMagSize = 25;
    public const int SmgMaxTotal = 175;
    public const int SmgStartReserve = 150;

    public const int MachinePistolMagSize = 18;
    public const int MachinePistolMaxTotal = 165;
    public const int MachinePistolStartReserve = 150;

    public const int LmgMagSize = 55;
    public const int LmgMaxTotal = 255;
    public const int LmgStartReserve = 200;

    public const int SniperMagSize = 5;
    public const int SniperMaxTotal = 45;
    public const int SniperStartReserve = 40;

    public const int HuntingRifleMagSize = 1;
    public const int HuntingRifleMaxTotal = 49;
    public const int HuntingRifleStartReserve = 48;

    public const float PistolReloadSeconds = 1.2f;
    public const float SmgReloadSeconds = 1.275f;
    public const float MachinePistolReloadSeconds = PistolReloadSeconds;
    public const float AssaultRifleReloadSeconds = 1.5f;
    public const float LmgReloadSeconds = 4.5f;
    public const float SniperReloadStartSeconds = 1.5f;
    public const float SniperRoundReloadSeconds = 0.8f;
    public const float HuntingRifleReloadSeconds = 2.1f;

    public const int AntiMaterialMagSize = 1;
    public const int AntiMaterialMaxTotal = 45;
    public const int AntiMaterialStartReserve = 40;
    public const float AntiMaterialReloadSeconds = 8f;
}
