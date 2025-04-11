#version 330 core

uniform sampler2D tex2d;

in vec2 texCoord;
out vec4 outColor;

void main(void) { outColor = texture(tex2d, texCoord); }