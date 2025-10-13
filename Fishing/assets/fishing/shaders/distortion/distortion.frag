#version 330 core

// Current time in seconds.
uniform float time;

// Resolution of display.
uniform vec2 resolution;

// Primary framebuffer texture.
uniform sampler2D primary;
uniform sampler2D depth;
uniform sampler2D tex2d;
uniform int useTexture;
uniform float strength = 1.0;

uniform int useFresnel;
uniform vec4 fresnelColor = vec4(1.0);
uniform float fresnelStrength = 1.0;

out vec4 fragColor;

in vec2 uv;
in vec3 worldNormal;
in vec3 eyeVector;

float random(in vec2 st) {
  return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

float noise(in vec2 st) {
  vec2 i = floor(st);
  vec2 f = fract(st);

  float a = random(i);
  float b = random(i + vec2(1.0, 0.0));
  float c = random(i + vec2(0.0, 1.0));
  float d = random(i + vec2(1.0, 1.0));

  vec2 u = f * f * (3.0 - 2.0 * f);

  return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

float fresnel(vec3 eyeVector, vec3 worldNormal, float power) {
  float fresnelFactor = abs(dot(eyeVector, worldNormal));

  float inversefresnelFactor = 1.0 - fresnelFactor;

  return pow(inversefresnelFactor, power);
}

void main() {
  vec2 uv = gl_FragCoord.xy / resolution;

  // Depth test fail.
  if (gl_FragCoord.z > texture(depth, uv).r)
    discard;

  float iorRed = 1.0 / 1.20;
  float iorGreen = 1.0 / 1.05;
  float iorBlue = 1.0 / 1.10;

  vec3 color = vec3(0.0);

  vec3 refractVecR = refract(eyeVector, worldNormal, iorRed);
  vec3 refractVecG = refract(eyeVector, worldNormal, iorGreen);
  vec3 refractVecB = refract(eyeVector, worldNormal, iorBlue);

  float uRefractPower = 0.15 * strength;
  float uChromaticAberration = 0.5 * strength;

  if (useTexture == 1) {
    float alpha = texture(tex2d, uv).a;
    if (alpha < 0.01)
      discard;

    uRefractPower *= alpha;
    uChromaticAberration *= alpha;
  }

  for (int i = 0; i < 16; i++) {
    float slide = float(i) / float(16) * 0.05;

    color.r +=
        texture2D(primary, clamp(uv + refractVecR.xy * (uRefractPower + slide) *
                                          uChromaticAberration,
                                 0.0, 1.0))
            .r;
    color.g +=
        texture2D(primary, clamp(uv + refractVecG.xy * (uRefractPower + slide) *
                                          uChromaticAberration,
                                 0.0, 1.0))
            .g;
    color.b +=
        texture2D(primary, clamp(uv + refractVecB.xy * (uRefractPower + slide) *
                                          uChromaticAberration,
                                 0.0, 1.0))
            .b;
  }

  color /= float(16);

  // Invisibility fresnel effect, not needed.
  if (useFresnel == 1) {
    float f = fresnel(eyeVector, worldNormal, 1.0);
    float originalAverage = (color.r + color.b + color.g) / 3.0;
    color += f * originalAverage * fresnelColor.rgb * fresnelColor.a;
  }

  fragColor = vec4(color, 1.0);
}