namespace Fishing3;

public class WormTaskAttribute : ClassAttribute
{
    public float priority;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="priority">Higher priority first.</param>
    public WormTaskAttribute(float priority = 0f)
    {
        this.priority = priority;
    }
}