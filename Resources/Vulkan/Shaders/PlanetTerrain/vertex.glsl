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
  vec2 vpos = vec2((gl_VertexIndex >> 16) & 0xffff, gl_VertexIndex & 0xffff);
  vec4 vert = vec4(vpos.x, 0, vpos.y, 1);
  gl_Position = globalParams.vp * vert;
  uv = vert.xz / 2049.0f;
}