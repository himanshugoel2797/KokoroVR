layout(std430, push_constant) uniform PushConstants {
  vec4 normal;
  uint idx0;
  uint idx1;
  uint idx2;
  float radius;
  uint off;
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

layout(set = 0, binding = 2, std430) buffer HeightField {
  uint16_t h[];
} heights;

void main() {
  // gl_VertexIndex
  uvec2 vpos_u = uvec2((gl_VertexIndex >> 16) & 0xffff, gl_VertexIndex & 0xffff);
  vec2 vpos = vec2(vpos_u) -
              2048.0f * 0.5f;
  vpos /= 2048.0f;
  vpos *= constants.radius * 2;
  vec4 vert = vec4(0, 0, 0, 1);
  vert[constants.idx0] += vpos.x;
  vert[constants.idx1] += vpos.y;
  vert.xyz += constants.normal.xyz * constants.radius;

  float height_val = heights.h[constants.off + vpos_u.y * 2049 + vpos_u.x] / 4096.0f;
  vert.xyz = normalize(vert.xyz) * (constants.radius + height_val * 250.0f);

  vec4 vert_trans = globalParams.vp * vert;
  vert_trans.z = (vert_trans.z + vert_trans.w) * 0.5f;

  gl_Position = vert_trans;
  uv = vec2(height_val); // vert.xz / 2049.0f;
}