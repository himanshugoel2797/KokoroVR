// Output data ; will be interpolated for each fragment.
out vec3 normal;
out vec3 pos;
flat out uint vox_v;

// Values that stay constant for the whole mesh.
uniform mat4 View;
uniform mat4 Proj;

struct block_info_t {
	vec4 o;
};

layout(std430, binding = 1) buffer BlockInfos_t {
    block_info_t v[];
} BlockInfo;

void main(){
	FETCH_CODE_BLOCK

	vec3 n_unnorm = ((vs_pos.xyz & 192) - 64) / 64.0f;
	vec3 vs_pos_e = (vs_pos.xyz & 63);

	vec3 face_pos = vs_pos_e.xyz + BlockInfo.v[_idx].o.xyz;
	normal = n_unnorm;
    pos = face_pos;
	vox_v = uint(vs_pos.w);
	gl_Position =  Proj * View * vec4(face_pos, 1);

}