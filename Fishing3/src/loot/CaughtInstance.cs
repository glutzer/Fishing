using Vintagestory.API.Common;

namespace Fishing3;

/// <summary>
/// Something that has been caught and given to the bobber.
/// </summary>
public class CaughtInstance
{
    public readonly float kg;
    public readonly float speed;
    public readonly ItemStack itemStack;
    public float stamina;

    public CaughtInstance(ItemStack itemStack, float kg = 1f, float speed = 1f, float stamina = 1f)
    {
        this.kg = kg;
        this.speed = speed;
        this.itemStack = itemStack;
        this.stamina = stamina;
    }
}