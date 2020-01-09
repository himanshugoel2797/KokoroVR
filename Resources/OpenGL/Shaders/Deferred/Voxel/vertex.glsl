// Output data ; will be interpolated for each fragment.
out vec3 normal;
out vec3 pos;
flat out uint vox_v;

// Values that stay constant for the whole mesh.
uniform mat4 ViewProj;

struct block_info_t {
	vec4 o;
	uvec4 vbuf_hndl;
};

layout(std430, binding = 1) buffer BlockInfos_t {
    block_info_t v[];
} BlockInfo;

void main(){
	int _idx = gl_BaseVertex;
	int ver_idx = int((gl_VertexID - gl_BaseVertex) & 0xffff);
	vox_v = uint(((gl_VertexID - gl_BaseVertex) >> 16) & 0xff);
	int norm_v = int((gl_VertexID - gl_BaseVertex) >> 24);

	vec3 vs_pos = imageLoad(uimageBuffer(BlockInfo.v[_idx].vbuf_hndl.xy), ver_idx).xyz;
	vec3 face_pos = vs_pos.xyz + BlockInfo.v[_idx].o.xyz;
	pos = face_pos;
	normal = vec3( norm_v & 3, (norm_v >> 2) & 3, norm_v >> 4 ) - 1.0f;
	gl_Position =  ViewProj * vec4(face_pos, 1);
}