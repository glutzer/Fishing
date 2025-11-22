#version 330 core
#extension GL_ARB_explicit_attrib_location : enable
#extension GL_ARB_shading_language_420pack : require

// Example of a standard way of doing opaque shading with shadows.

layout(location = 0) in vec3 vertexIn;
layout(location = 1) in vec2 uvIn;
layout(location = 2) in vec3 normalIn;

uniform mat4 modelMatrix;

uniform vec3 offset; // Offset of the end point to the start (pole tip).
uniform float droop; // Droop power.
uniform float minY;

#include vertexflagbits.ash

layout(std140) uniform renderGlobals {
  mat4 viewMatrix;
  mat4 perspectiveMatrix;
  mat4 orthographicMatrix;
  mat4 perspectiveViewMatrix;
};

uniform mat4 mvpMatrix;

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

  if (offset.y > 0) {
    vec4 originY = modelMatrix * vec4(0, 0.02, 0, 1);
    worldPos.y = max(originY.y, worldPos.y);
  }
  gl_Position = mvpMatrix * vec4(pointMid, 1.0);
}