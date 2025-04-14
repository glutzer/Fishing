using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Fishing3;

public class LeviathanRenderer : EntityShapeRenderer
{
    public LeviathanRenderer(Entity entity, ICoreClientAPI api) : base(entity, api)
    {
    }

    public override void DoRender3DOpaque(float dt, bool isShadowPass)
    {
        if (isSpectator) return;

        NewModelMatrix(entity, dt, isShadowPass);
        Vec3d camPos = capi.World.Player.Entity.CameraPos;
        OriginPos.Set((float)(entity.Pos.X - camPos.X), (float)(entity.Pos.InternalY - camPos.Y), (float)(entity.Pos.Z - camPos.Z));

        if (isShadowPass)
        {
            DoRender3DAfterOIT(dt, true);
        }
    }

    public void NewModelMatrix(Entity entity, float dt, bool isShadowPass)
    {
        Mat4f.Identity(ModelMat);

        EntityPlayer entityPlayer = capi.World.Player.Entity;
        Mat4f.Translate(ModelMat, ModelMat, (float)(entity.Pos.X - entityPlayer.CameraPos.X), (float)(entity.Pos.InternalY - entityPlayer.CameraPos.Y), (float)(entity.Pos.Z - entityPlayer.CameraPos.Z));

        float rotX = entity.Properties.Client.Shape?.rotateX ?? 0;
        float rotY = entity.Properties.Client.Shape?.rotateY ?? 0;
        float rotZ = entity.Properties.Client.Shape?.rotateZ ?? 0;

        /*
        if (!isShadowPass)
        {
            updateStepPitch(dt);
        }
        */

        double[] quat = Quaterniond.Create();

        //float yaw = entity.Pos.Yaw + ((rotY + 90) * GameMath.DEG2RAD);
        float yaw = entity.Pos.Yaw;

        Quaterniond.RotateY(quat, quat, yaw);
        Quaterniond.RotateX(quat, quat, entity.Pos.Pitch + (rotX * GameMath.DEG2RAD));
        Quaterniond.RotateZ(quat, quat, entity.Pos.Roll + (rotZ * GameMath.DEG2RAD));

        Quaterniond.RotateY(quat, quat, yangle);
        Quaterniond.RotateX(quat, quat, xangle);
        Quaterniond.RotateZ(quat, quat, zangle);

        float[] qf = new float[quat.Length];
        for (int i = 0; i < quat.Length; i++) qf[i] = (float)quat[i];
        Mat4f.Mul(ModelMat, ModelMat, Mat4f.FromQuat(Mat4f.Create(), qf));

        float scale = entity.Properties.Client.Size;
        Mat4f.Scale(ModelMat, ModelMat, new float[] { scale, scale, scale });

        // Center entity at 0.5f.
        Mat4f.Translate(ModelMat, ModelMat, -0.5f, 0, -0.5f);
    }
}