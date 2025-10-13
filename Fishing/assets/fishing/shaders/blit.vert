#version 330 core
#extension GL_ARB_explicit_attrib_location : enable
#extension GL_ARB_shading_language_420pack : require

layout(location = 0) in vec2 position;

out vec2 texCoord;

void main(void) {
  float x = -1.0 + float((gl_VertexID & 1) << 2);
  float y = -1.0 + float((gl_VertexID & 2) << 1);
  gl_Position = vec4(x, y, 0, 1);
  texCoord = vec2((x + 1.0) * 0.5, (y + 1.0) * 0.5);
}