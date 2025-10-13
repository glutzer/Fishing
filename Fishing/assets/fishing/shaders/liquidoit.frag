#version 330 core
#extension GL_ARB_shading_language_420pack : enable

layout(location = 0) out vec4 outAccu;
layout(location = 1) out vec4 outReveal;
layout(location = 2) out vec4 outGlow;

in vec2 uv;
in vec4 colorOut;
in float fogAmount;
in vec4 rgbaFog;

in float glowLevel;

in vec3 hitPos;

uniform vec3 cubePosition;
uniform vec3 cubeScale;

uniform sampler2D tex2d;
uniform vec4 uniformColor = vec4(1.0);

#include fogandlight.fsh

bool rayAABBFast(vec3 rayDir, vec3 minB, vec3 maxB, out float floatMin,
                 out float floatMax) {
  floatMin = -99999;
  floatMax = 99999;

  vec3 tMin = minB / rayDir;
  vec3 tMax = maxB / rayDir;

  vec3 tSmaller = min(tMin, tMax);
  vec3 tBigger = max(tMin, tMax);

  floatMin = max(floatMin, max(tSmaller[0], max(tSmaller[1], tSmaller[2])));
  floatMax = min(floatMax, min(tBigger[0], min(tBigger[1], tBigger[2])));

  return floatMin < floatMax;
}

void drawPixel(vec4 colorA) {
  float weight =
      colorA.a *
      clamp(0.03 / (1e-5 + pow(gl_FragCoord.z / 200, 4.0)), 1e-2, 3e3);

  outAccu = vec4(colorA.rgb * colorA.a, colorA.a) * weight; // Half weight.

  outReveal.r = colorA.a;

  // glowLevel from fogandlight (0-1).

  outGlow = vec4(glowLevel, 0, 0, colorA.a);
}

float map(float value, float originalMin, float originalMax, float newMin,
          float newMax) {
  return (value - originalMin) / (originalMax - originalMin) *
             (newMax - newMin) +
         newMin;
}

void main() {
  vec3 ray = hitPos * 2.0;
  float tMin;
  float tMax;

  bool intersects = rayAABBFast(ray, cubePosition - cubeScale / 2,
                                cubePosition + cubeScale / 2, tMin, tMax);

  if (!intersects)
    discard;

  vec3 entryPoint = ray * tMin;
  vec3 exitPoint = ray * tMax;

  float absorptionValue =
      1.0 / uniformColor.a; // 1 block at full opacity, 10 blocks at 0.1.

  absorptionValue *= 0.3;

  float absorptionCoefficient = 1.0 / absorptionValue;

  float volume = length(entryPoint - exitPoint);
  float attenuation = exp(-absorptionCoefficient * volume);
  float alpha = max(1.0 - attenuation, 0);

  alpha = uniformColor.a * 0.75 + alpha * (1.0 - uniformColor.a * 0.75);

  vec4 uColor = uniformColor;
  uColor.a = alpha;

  vec4 color = colorOut * uColor;

  drawPixel(applyFogAndShadow(color, fogAmount));
}