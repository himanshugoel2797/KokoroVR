layout(location = 0) out vec2 uv;

layout(set = 0, location = 0, std140) uniform GlobalDrawParams {
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

layout(set = 0, location = 1) uniform sampler2D HeightMap;
layout(set = 0, location = 2, std140) uniform TerrainTileInfo {
  vec4 position[];
}
tileInfo;

layout(set = 0, location = 3) uniform samplerBuffer TileVertices;

void main() {
  // gl_VertexIndex
  vec4 vert = vec4(texelFetch(TileVertices, gl_VertexIndex).xy, 0, 1);
  gl_Position = globalParams.vp * vert;
  uv = vec2(vert.x / 129.0f, vert.y / 129.0f);
}