namespace Fishing;

public class ConfigFishing
{
    public static ConfigFishing Loaded { get; set; } = new ConfigFishing();

    public float BASE_BITE_TIME = 60f;
    public float BASE_REEL_STRENGTH = 5f;
    public float FISH_SATIETY_MULTIPLIER = 1f;

    public bool ENABLE_CATCHABLE_FLOTSAM = true;
    public bool ENABLE_CATCHABLE_ITEMS = true;
}
