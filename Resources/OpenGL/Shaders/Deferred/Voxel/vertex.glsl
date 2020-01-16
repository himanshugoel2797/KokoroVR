// Output data ; will be interpolated for each fragment.
//out vec3 normal;
out vec3 pos;
flat out uint vox_v;

// Values that stay constant for the whole mesh.
uniform mat4 ViewProj;
uniform vec3 eyePos;
uniform int curLayer;

struct block_info_t {
	vec4 o;
    uvec4 vbuf_hndl;
};

layout(std430, binding = 1) buffer BlockInfos_t {
    block_info_t v[];
} BlockInfo;


void main(){
	uint _idx = gl_BaseVertex;
	uint idx_val = gl_VertexID - gl_BaseVertex;
	uint ver_idx = bitfieldExtract(idx_val, 0, 16);
	vox_v = bitfieldExtract(idx_val, 16, 8);
	//uint norm_v = bitfieldExtract(idx_val, 24, 8);

	vec3 vs_pos = imageLoad(uimageBuffer(BlockInfo.v[_idx].vbuf_hndl.xy), int(ver_idx)).xyz;
	vec3 face_pos = vs_pos.xyz + BlockInfo.v[_idx].o.xyz;
	pos = face_pos - eyePos;
	//normal = vec3( bitfieldExtract(norm_v, 0, 2), bitfieldExtract(norm_v, 2, 2), bitfieldExtract(norm_v, 4, 2) ) - 1.0f;
	gl_Position =  ViewProj * vec4(face_pos, 1);
	gl_Layer = curLayer;
}