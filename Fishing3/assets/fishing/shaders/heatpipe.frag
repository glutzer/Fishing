#version 330 core

uniform sampler2D tex2d;

// Example of a standard way of doing opaque shading with shadows.

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec4 outGlow;
#if SSAOLEVEL > 0
layout(location = 2) out vec4 outGNormal;
layout(location = 3) out vec4 outGPosition;
#endif

in vec2 uvOut;
in vec4 colorOut;
in vec4 gNormal;
in vec4 cameraPos;
in vec3 normal;

in float fogAmount;
in vec4 rgbaFog;

uniform float temperature;

#include vertexflagbits.ash
#include fogandlight.fsh

void main() {
  float glowPower = clamp(clamp((temperature - 100.0), 0, 900) / 900.0, 0, 1);

  vec4 tex = texture(tex2d, uvOut);
  vec4 texColor = mix(tex * colorOut, tex * vec4(1, 0.4, 0.4, 1), glowPower);

  outColor = texColor;
  outColor = applyFogAndShadow(texColor, fogAmount);

  outGlow = vec4(glowPower, 0, 0, min(1, fogAmount + outColor.a));

#if SSAOLEVEL > 0
  outGPosition =
      vec4(cameraPos.xyz, fogAmount * 2 /*+ glowLevel*/ /*+ murkiness*/);
  outGNormal = gNormal;
#endif
}