// Output data ; will be interpolated for each fragment.
//out vec3 normal;
out vec3 pos;
flat out uint vox_v;
flat out uint vox_idx;

// Values that stay constant for the whole mesh.
uniform int eyeIdx;

struct block_info_t {
	vec4 o;
};

struct draw_cmd_t {
	uint count;
	uint instance_cnt;
	uint base_index;
	uint zr0;
	uint base_instance;
	uint index_block;
	uint vbo_idx;
	uint base_vertex;
    vec4 aabb_min;
    vec4 aabb_max;
};

layout(std430, binding = 1) readonly buffer BlockInfos_t {
    block_info_t v[];
} BlockInfo;

layout(std430, binding = 2) readonly buffer DrawCMDs_t {
	uint drawCount;
	uint pd0;
	uint pd1;
	uint pd2;
    draw_cmd_t v[];
} DrawCMDs;

layout(std140, binding = 0) uniform GlobalParams_t {
	mat4 proj[EYECOUNT];
	mat4 view[EYECOUNT];
	mat4 vp[EYECOUNT];
	mat4 ivp[EYECOUNT];
	mat4 prev_view[EYECOUNT];
	mat4 prev_vp[EYECOUNT];
	mat4 prev_ivp[EYECOUNT];
	uvec4 infoBindings[EYECOUNT];
	uvec4 depthBindings[EYECOUNT];
	vec4 prev_eyePos;
	vec4 prev_eyeUp;
	vec4 prev_eyeDir;
	vec4 eyePos;
	vec4 eyeUp;
	vec4 eyeDir;
} GlobalParams;

layout(std140, binding = 1) uniform VertexBuffers_t {
	uvec4 v[16];
} VertexBuffers;


void main(){
	uint ibo_block = DrawCMDs.v[gl_DrawID].index_block;
	uint vbo_idx = DrawCMDs.v[gl_DrawID].vbo_idx;
	uint ver_idx = bitfieldExtract(gl_VertexID, 0, 16);
	vox_v = bitfieldExtract(gl_VertexID, 16, 16);
	vox_idx = ibo_block;
	
	vec3 vs_pos = imageLoad(uimageBuffer(VertexBuffers.v[vbo_idx].xy), int(DrawCMDs.v[gl_DrawID].base_vertex + ver_idx)).xyz;
	vec3 face_pos = vs_pos.xyz + BlockInfo.v[ibo_block].o.xyz;
	pos = face_pos - GlobalParams.eyePos.xyz;
	//normal = vec3( bitfieldExtract(norm_v, 0, 2), bitfieldExtract(norm_v, 2, 2), bitfieldExtract(norm_v, 4, 2) ) - 1.0f;
	gl_Position =  GlobalParams.vp[eyeIdx] * vec4(face_pos, 1);
}