#version 330 core
#extension GL_ARB_explicit_attrib_location : enable
#extension GL_ARB_shading_language_420pack : require

layout(location = 0) in vec3 vertexIn;
layout(location = 1) in vec2 uvIn;
layout(location = 2) in vec3 normalIn;
layout(location = 3) in vec4 colorIn;

uniform mat4 modelMatrix;

layout(std140, binding = 1) uniform renderGlobals {
  mat4 viewMatrix;
  mat4 perspectiveMatrix;
  mat4 orthographicMatrix;
};

out vec2 uv;
out vec4 color;

void main() {
  vec3 vert = vertexIn;

  gl_Position = perspectiveMatrix * viewMatrix * modelMatrix * vec4(vert, 1.0);

  uv = uvIn;
  color = colorIn;
}