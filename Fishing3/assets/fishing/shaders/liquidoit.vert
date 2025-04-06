#version 330 core

#extension GL_ARB_explicit_attrib_location : enable
#extension GL_ARB_shading_language_420pack : require

layout(location = 0) in vec3 vertexIn;
layout(location = 1) in vec2 uvIn;

uniform mat4 modelMatrix;

layout(std140, binding = 1) uniform renderGlobals {
  mat4 viewMatrix;
  mat4 perspectiveMatrix;
  mat4 orthographicMatrix;
};

out vec2 uv;
out vec4 colorOut;

uniform mat4 offsetViewMatrix;

uniform vec4 rgbaFogIn;
out vec4 rgbaFog;

// Glow value from 0-255 (liquid glow * 255).
uniform int glowAmount = 0;

uniform vec4 rgbaLightIn;
uniform vec3 rgbaAmbientIn;
uniform float fogMinIn;
uniform float fogDensityIn;
out float fogAmount;

#include vertexflagbits.ash
#include shadowcoords.vsh
#include fogandlight.vsh

void main() {
  vec3 vert = vertexIn;

  vec4 worldPos = modelMatrix * vec4(vert, 1.0);
  gl_Position = perspectiveMatrix * offsetViewMatrix * worldPos;

  uv = uvIn;

  // Bright green ambient color?
  float glowFloat = glowAmount / 255.0;
  vec3 mixedAmbient = mix(rgbaAmbientIn, vec3(1.0), glowFloat);

  colorOut = applyLight(mixedAmbient, rgbaLightIn, glowAmount,
                        offsetViewMatrix * worldPos);

  // Render flag is here replaced with glowAmount, since that's the first 8 bits
  // of the flags.

  // Calc shadow map coordinates from the frag shader.
  calcShadowMapCoords(offsetViewMatrix, worldPos);

  rgbaFog = rgbaFogIn;
  fogAmount = getFogLevel(worldPos, fogMinIn, fogDensityIn);
}