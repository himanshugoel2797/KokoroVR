layout(local_size_x = 512, local_size_y = 6, local_size_z = 1) in;

//read in two triangles, determine visibility and if visible, place in the queue
//compaction should ideally also sort front to back

uniform vec3 eyePos;

struct block_info_t {
	vec4 o;
    uvec4 vbuf_hndl;
};

layout(std430, binding = 0) buffer BlockInfos_t {
    block_info_t v[];
} BlockInfo;

layout(std430, binding = 1) buffer IndexBuffer_t {
    uint i[];
} IndexBuffer;

void main(){
    uint _idx = gl_WorkGroupID.x;
    uint tri_off = gl_LocalInvocationIndex * 6;

    //read the 4 quad indices
    uint idx0 = IndexBuffer.i[tri_off + 0];
    uint idx1 = IndexBuffer.i[tri_off + 1];
    uint idx2 = IndexBuffer.i[tri_off + 2];
    uint idx3 = IndexBuffer.i[tri_off + 4];

    //read the vertices to compute the center of the quad
    uint ver_idx = bitfieldExtract(idx0, 0, 16);
	uint norm_v = bitfieldExtract(idx0, 24, 8);
	vec3 normal = vec3( bitfieldExtract(norm_v, 0, 2), bitfieldExtract(norm_v, 2, 2), bitfieldExtract(norm_v, 4, 2) ) - 1.0f;

	vec3 v0 = imageLoad(uimageBuffer(BlockInfo.v[_idx].vbuf_hndl.xy), int(bitfieldExtract(idx0, 0, 16))).xyz;
	vec3 v1 = imageLoad(uimageBuffer(BlockInfo.v[_idx].vbuf_hndl.xy), int(bitfieldExtract(idx1, 0, 16))).xyz;
	vec3 v2 = imageLoad(uimageBuffer(BlockInfo.v[_idx].vbuf_hndl.xy), int(bitfieldExtract(idx2, 0, 16))).xyz;
	vec3 v3 = imageLoad(uimageBuffer(BlockInfo.v[_idx].vbuf_hndl.xy), int(bitfieldExtract(idx3, 0, 16))).xyz;

    vec3 vc = (v0 + v1 + v2 + v3) * 0.25f + BlockInfo.v[_idx].o.xyz;
    vec3 vDir = dot((vc - eyePos), normal);

    //extract the normal and emit if dot product suggests the face is visible
    //if vDir >= 0, cull
}