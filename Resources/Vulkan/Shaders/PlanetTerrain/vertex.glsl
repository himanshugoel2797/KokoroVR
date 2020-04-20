layout(std430, push_constant) uniform PushConstants {
  vec4 normal;
  uint idx0;
  uint idx1;
  uint idx2;
  float radius;
}
constants;

layout(location = 0) out vec2 uv;

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

layout(set = 0, binding = 1, std140) uniform TerrainTileInfo {
  vec4 position[40];
}
tileInfo;

void main() {
  // gl_VertexIndex
  vec2 vpos = vec2((gl_VertexIndex >> 16) & 0xffff, gl_VertexIndex & 0xffff) -
              128.0f * 0.5f;
  vpos /= 128.0f;
  vpos *= 200.0f;
  vec4 vert = vec4(0, 0, 0, 1);
  vert[constants.idx0] += vpos.x;
  vert[constants.idx1] += vpos.y;
  vert.xyz += constants.normal.xyz * constants.radius;

  vert.xyz = normalize(vert.xyz) * constants.radius;

  gl_Position = globalParams.vp * vert;
  uv = vec2(1); // vert.xz / 2049.0f;
}