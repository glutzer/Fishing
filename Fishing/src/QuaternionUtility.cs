using OpenTK.Mathematics;
using System;

namespace Fishing;

public static class QuaternionUtility
{
    public static Quaternion FromToRotation(Vector3 from, Vector3 to)
    {
        from = Vector3.Normalize(from);
        to = Vector3.Normalize(to);

        float dot = Vector3.Dot(from, to);

        // If the vectors are already the same, return identity quaternion.
        if (dot > 0.9999f)
            return Quaternion.Identity;

        // If the vectors are opposite, find an arbitrary perpendicular vector.
        if (dot < -0.9999f)
        {
            Vector3 perpendicular = Vector3.Cross(from, new Vector3(1f, 0f, 0f));
            if (perpendicular.Length < 0.01f)
                perpendicular = Vector3.Cross(from, new Vector3(0f, 0f, 1f));

            perpendicular = Vector3.Normalize(perpendicular);
            return Quaternion.FromAxisAngle(perpendicular, MathF.PI);
        }

        // Compute rotation axis and angle.
        Vector3 axis = Vector3.Normalize(Vector3.Cross(from, to));
        float angle = MathF.Acos(dot); // Angle between vectors.

        return Quaternion.FromAxisAngle(axis, angle);
    }
}