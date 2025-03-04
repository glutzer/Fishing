#version 330 core
#extension GL_ARB_explicit_attrib_location : enable
#extension GL_ARB_shading_language_420pack : require

// Example of a standard way of doing opaque shading with shadows.

layout(location = 0) in vec3 vertexIn;
layout(location = 1) in vec2 uvIn;

uniform mat4 modelMatrix;

out vec2 uvOut;
out vec4 colorOut;

out float fogAmount;
out vec4 rgbaFog;

uniform vec3 rgbaAmbientIn;
uniform vec4 rgbaLightIn;
uniform vec4 rgbaFogIn;
uniform float fogMinIn;
uniform float fogDensityIn;

uniform vec3 offset; // Offset of the end point to the start (pole tip).
uniform float droop; // Droop power.

#include vertexflagbits.ash
#include shadowcoords.vsh
#include fogandlight.vsh

layout(std140) uniform renderGlobals {
  mat4 viewMatrix;
  mat4 perspectiveMatrix;
  mat4 orthographicMatrix;
  mat4 perspectiveViewMatrix;
  float zNear;
  float zFar;
};

uniform mat4 offsetViewMatrix;

float catenary(float x, float d, float a) {
  return a * (cosh((x - (d / 2.0)) / a) - cosh((d / 2.0) / a));
}

mat3 rotateToVector(vec3 targetVector) {
  vec3 zAxis = normalize(targetVector);
  vec3 xAxis = normalize(cross(vec3(0.0, 1.0, 0.0), zAxis));
  vec3 yAxis = cross(zAxis, xAxis);

  return mat3(xAxis, yAxis, zAxis);
}

vec3 calcPoint(float progress, vec3 vertex) {
  float cat = catenary(progress, 1.0, 0.4);

  // y - cat * 1.0,
  return vec3(vertex.x + offset.x * progress,
              vertex.y + offset.y * progress + cat * droop,
              vertex.z + offset.z * progress);
}

void main() {
  vec3 pointA = calcPoint(uvIn.x - 0.1, vertexIn);
  vec3 pointB = calcPoint(uvIn.x + 0.1, vertexIn);
  vec3 normal = normalize(pointB - pointA);
  mat3 rotMatrix = rotateToVector(normal);
  vec3 rotatedPoint = rotMatrix * vertexIn;
  vec3 pointMid = calcPoint(uvIn.x, rotatedPoint);

  vec4 worldPos = modelMatrix * vec4(pointMid, 1.0);
  vec4 cameraPos = offsetViewMatrix * worldPos;
  gl_Position = perspectiveMatrix * cameraPos;

  // Take the view matrix and the world pos relative to the camera, send shadow
  // info to the frag.
  calcShadowMapCoords(offsetViewMatrix, worldPos);

  // Get the fog amount at the current pos.
  fogAmount = getFogLevel(worldPos, fogMinIn, fogDensityIn);

  // Apply point lights/block light.
  colorOut = applyLight(rgbaAmbientIn, rgbaLightIn, 0, cameraPos);

  // rgbaFog for fragment include.
  rgbaFog = rgbaFogIn;

  uvOut = uvIn;
  uvOut.x *= length(offset) * 2;
}