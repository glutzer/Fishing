using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

namespace Fishing;

public static class FishingLineRenderer
{
    private static MeshHandle fishingLineMesh = null!;
    private static Texture defaultLineTexture = null!;

    public static void OnStart()
    {
        CreateCrossFishingLine(0.01f);
        defaultLineTexture = Texture.Create("fishing:textures/lines/linen.png", false, true);
    }

    /// <summary>
    /// Render a line.
    /// Standard droop for poles is 1f.
    /// </summary>
    public static void RenderLine(Vector3d startPos, Vector3d endPos, float droopLevel, Texture texture)
    {
        ShaderProgramBase? currentShader = ShaderProgramBase.CurrentShaderProgram;
        Vec4f lightRGBs = MainAPI.Capi.World.BlockAccessor.GetLightRGBs((int)startPos.X, (int)startPos.Y, (int)startPos.Z);
        NuttyShader lineShader = NuttyShaderRegistry.Get("fishingline");
        lineShader.Use();
        lineShader.Uniform("droop", droopLevel);
        lineShader.BindTexture(texture, "tex2d");

        // For murkiness.
        DefaultShaderUniforms shaderUniforms = ScreenManager.Platform.ShaderUniforms;
        lineShader.Uniform("zNear", shaderUniforms.ZNear);
        lineShader.Uniform("zFar", shaderUniforms.ZFar);
        lineShader.UnderwaterEffects();

        lineShader.ObsoleteUniform("rgbaLightIn", lightRGBs);

        if (startPos.Z == endPos.Z)
        {
            endPos.Z += 0.001f;
        }

        if (startPos.X == endPos.X)
        {
            endPos.X += 0.001f;
        }

        Vector3d offset = MainAPI.CameraPosition - new Vector3d(MainAPI.Capi.World.Player.Entity.CameraPos.X, MainAPI.Capi.World.Player.Entity.CameraPos.Y, MainAPI.Capi.World.Player.Entity.CameraPos.Z);
        lineShader.Uniform("modelMatrix", RenderTools.CameraRelativeTranslation(endPos + offset));

        lineShader.Uniform("offset", (Vector3)(startPos - endPos));

        lineShader.UniformMatrix("offsetViewMatrix", MainAPI.Capi.Render.CameraMatrixOriginf);

        lineShader.ShadowUniforms();
        lineShader.LightUniforms(true);

        RenderTools.DisableCulling();
        RenderTools.RenderMesh(fishingLineMesh);
        RenderTools.EnableCulling();

        currentShader?.Use();
    }

    public static void RenderLineShadow(Vector3d startPos, Vector3d endPos, float droopLevel)
    {
        ShaderProgramBase? currentShader = ShaderProgramBase.CurrentShaderProgram;

        NuttyShader lineShader = NuttyShaderRegistry.Get("fishinglineshadow");
        lineShader.Use();
        lineShader.Uniform("droop", droopLevel);

        if (startPos.Z == endPos.Z)
        {
            endPos.Z += 0.001f;
        }

        if (startPos.X == endPos.X)
        {
            endPos.X += 0.001f;
        }

        lineShader.Uniform("offset", (Vector3)(startPos - endPos));

        Matrixf modelMatrix = new Matrixf()
                .Translate(endPos.X - MainAPI.Capi.World.Player.Entity.CameraPos.X, endPos.Y - MainAPI.Capi.World.Player.Entity.CameraPos.Y, endPos.Z - MainAPI.Capi.World.Player.Entity.CameraPos.Z);
        float[] array = Mat4f.Mul(modelMatrix.Values, MainAPI.Capi.Render.CurrentModelviewMatrix, modelMatrix.Values);
        Mat4f.Mul(array, MainAPI.Capi.Render.CurrentProjectionMatrix, array);
        lineShader.UniformMatrix("mvpMatrix", array);

        RenderTools.DisableCulling();
        RenderTools.RenderMesh(fishingLineMesh);
        RenderTools.EnableCulling();

        currentShader?.Use();
    }

    public static void OnEnd()
    {
        fishingLineMesh?.Dispose();
        fishingLineMesh = null!;

        defaultLineTexture?.Dispose();
        defaultLineTexture = null!;
    }

    private static void CreateCrossFishingLine(float width)
    {
        MeshInfo<StandardVertex> side1 = new(6, 6);

        for (int i = 0; i < 21; i++)
        {
            float progress = i / 20f;

            side1.AddVertex(new StandardVertex(new Vector3(-width, width, 0), new Vector2(progress, 0.2f), default, default));
            side1.AddVertex(new StandardVertex(new Vector3(width, -width, 0), new Vector2(progress, -0.2f), default, default));
        }

        for (int i = 0; i < 20; i++)
        {
            int offset = 2 * i;

            side1.AddIndex(offset);
            side1.AddIndex(offset + 3);
            side1.AddIndex(offset + 2);
            side1.AddIndex(offset);
            side1.AddIndex(offset + 1);
            side1.AddIndex(offset + 3);
        }

        MeshInfo<StandardVertex> side2 = new(6, 6);

        for (int i = 0; i < 21; i++)
        {
            float progress = i / 20f;

            side2.AddVertex(new StandardVertex(new Vector3(-width, -width, 0), new Vector2(progress, 0.2f), default, default));
            side2.AddVertex(new StandardVertex(new Vector3(width, width, 0), new Vector2(progress, -0.2f), default, default));
        }

        for (int i = 0; i < 20; i++)
        {
            int offset = 2 * i;

            side2.AddIndex(offset);
            side2.AddIndex(offset + 3);
            side2.AddIndex(offset + 2);
            side2.AddIndex(offset);
            side2.AddIndex(offset + 1);
            side2.AddIndex(offset + 3);
        }

        side1.AddMeshData(side2);

        fishingLineMesh = side1.Upload();
    }
}