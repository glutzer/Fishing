#version 330 core

in vec2 uv;
in vec4 color;

out vec4 fragColor;

void main() {
  fragColor = vec4(1.0);
  fragColor.a = 0.5;
}