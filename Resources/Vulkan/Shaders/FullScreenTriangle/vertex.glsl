layout(location = 0) out vec2 uv;
layout(location = 1) out vec4 color;

layout(set = 0, binding = 0, std140) uniform GlobalDrawParams {
  mat4 proj;
  mat4 view;
  mat4 vp;
  mat4 ivp;
  mat4 prev_view;
  mat4 prev_vp;
  mat4 prev_ivp;
  vec4 prev_campos;
  vec4 prev_camup;
  vec4 prev_camdir;
  vec4 campos;
  vec4 camup;
  vec4 camdir;
}
globalParams;

vec2 positions[3] = vec2[](vec2(0.0, -0.5), vec2(0.5, 0.5), vec2(-0.5, 0.5));

void main() {
  float x = -1.0f + float((gl_VertexIndex & 1) << 2);
  float y = -1.0f + float((gl_VertexIndex & 2) << 1);

  gl_Position = vec4(x, y, 0.5f, 1.0f);

  color = globalParams.camdir;
  uv = (vec2(x, y) + vec2(1)) * 0.5f;
}