#version 330 core
#extension GL_ARB_explicit_attrib_location : enable
#extension GL_ARB_shading_language_420pack : require

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
layout(location = 2) in vec4 colorIn;
layout(location = 3) in int flags;
layout(location = 4) in float damageEffectIn;
layout(location = 5) in int jointId;

uniform mat4 modelMatrix;
uniform mat4 playerViewMatrix;

uniform int addRenderFlags;

out vec2 uv;
out vec3 worldNormal;
out vec3 eyeVector;

#include vertexflagbits.ash

layout(std140) uniform Animation {
  mat4 values[MAXANIMATEDELEMENTS]; // MAXANIMATEDELEMENTS constant is defined
                                    // during game engine shader loading. Is
                                    // there there by default?
}
ElementTransforms;

layout(std140, binding = 1) uniform renderGlobals {
  mat4 viewMatrix;
  mat4 perspectiveMatrix;
  mat4 orthographicMatrix;
};

void main() {
  int renderFlags = flags | addRenderFlags;

  mat4 animModelMat = modelMatrix * ElementTransforms.values[jointId];
  vec4 worldPos = animModelMat * vec4(vertexPositionIn, 1.0);
  vec4 mvPosition = playerViewMatrix * worldPos;
  gl_Position = perspectiveMatrix * mvPosition;

  vec3 normalIn = unpackNormal(renderFlags);

  worldNormal = normalize(mat3(playerViewMatrix * animModelMat) * normalIn);
  eyeVector = normalize(mvPosition.xyz);

  uv = uvIn;
}