layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;
//local_size = number of times the subgroup is executed
//workgroup = spread of the subgroup

//read in two triangles, determine visibility and if visible, place in the queue
//compaction should ideally also sort front to back
//divide the work across a wavefront - one atomic counter shared to handle compaction -max 64 quads
//x = 96

uniform vec3 eyePos;

struct block_info_t {
	vec4 o;
    uvec4 vbuf_hndl;
};

struct draw_cmd_t{
    uint count;
    uint instanceCount;
    uint firstIndex;
    uint baseVertex;
    uint baseInstance;
};

layout(std430, binding = 1) restrict readonly buffer BlockInfos_t {
    block_info_t v[];
} BlockInfo;

layout(std430, binding = 2) restrict readonly buffer IndexBuffer_t {
    uint i[];
} IndexBuffer;

layout(std430, binding = 3) restrict readonly buffer DrawCMDs_t {
    uint x;
    uint drawCalls;
    uint z;
    uint drawCalls1;
    draw_cmd_t cmds[];
} DrawCMDs;

layout(std430, binding = 4) restrict writeonly buffer OutIndexBuffer_t {
    uint i[];
} OutIndexBuffer;

layout(std430, binding = 5) restrict coherent buffer OutDrawCMDs_t {
    uint x;
    uint drawCalls;
    uint z;
    uint drawCalls1;
    draw_cmd_t cmds[];
} O_DrawCMDs;

//Increment O_DrawCMDs.cmds[_idx].count
//Set O_DrawCMDs.cmds[_idx + 1].firstIndex to O_DrawCMDs.cmds[_idx].count
//O_DrawCMDs.blk_cntr = atomicMax(O_DrawCMDs.blk_cntr, _idx)

void main(){
    //gl_WorkGroupID.y = gl_DrawID
    //gl_WorkGroupID.x = higher portion of index
    //index_off = (gl_WorkGroupID.x * 1024 + gl_LocalInvocationIndex) * 6
    //o_draw_idx = gl_WorkGroupID.y * 6 + gl_WorkGroupID.x
    //i_draw_idx = gl_WorkGroupID.y 
    //Each draw has 6*1024 indices, no need for a workgroup slot, each is at o_draw_idx*6*1024 offset

    //indirect dispatch using DrawCMDs.drawCalls
    uint _idx = DrawCMDs.cmds[gl_WorkGroupID.y].baseVertex;   //0 -> drawCount
    uint tri_off = DrawCMDs.cmds[gl_WorkGroupID.y].firstIndex + (gl_WorkGroupID.x * gl_WorkGroupSize.x + gl_LocalInvocationID.x) * 6;   //0 -> (36 * 1024)
    uint o_tri_off = gl_WorkGroupID.y * 6 * gl_NumWorkGroups.x * gl_WorkGroupSize.x;

    //read the 4 quad indices
    uint idx0 = IndexBuffer.i[tri_off + 0];
    uint idx1 = bitfieldExtract(IndexBuffer.i[tri_off + 1], 0, 24);
    uint idx2 = bitfieldExtract(IndexBuffer.i[tri_off + 2], 0, 24);
    uint idx3 = bitfieldExtract(IndexBuffer.i[tri_off + 3], 0, 24);
    uint idx4 = bitfieldExtract(IndexBuffer.i[tri_off + 4], 0, 24);
    uint idx5 = bitfieldExtract(IndexBuffer.i[tri_off + 5], 0, 24);

    //read the vertices to compute the center of the quad
    uimageBuffer vbuf = uimageBuffer(BlockInfo.v[_idx].vbuf_hndl.xy);
	vec3 v0 = imageLoad(vbuf, int(bitfieldExtract(idx0, 0, 16))).xyz;
    vec3 normal = vec3(bitfieldExtract(idx0, 24, 2), bitfieldExtract(idx0, 26, 2), bitfieldExtract(idx0, 28, 2)) - 1.0f;
    vec3 vc = v0 + BlockInfo.v[_idx].o.xyz; //(v0 + v1 + v2 + v3) * 0.25f + BlockInfo.v[_idx].o.xyz;
    float vDir = dot((vc - eyePos), normal);//try using precomputed normals instead, save on the vertex lookup + cross, reduce index buffer size

    O_DrawCMDs.cmds[gl_WorkGroupID.y].baseVertex = _idx;
    
    //extract the normal and emit if dot product suggests the face is visible
    //if vDir >= 0, cull
    bool laneActive = vDir < 0.0f;
    uint64_t mask_v = ballotARB(laneActive);
	uvec2 mask_v_unpacked = unpackUint2x32(mask_v);
	uvec2 mask_v_unpacked_masked = unpackUint2x32(mask_v & gl_SubGroupLtMaskARB);
	int mask_v_bitcnt = int(bitCount(mask_v_unpacked.x) + bitCount(mask_v_unpacked.y)) * 6;
    int oSlot = int(bitCount(mask_v_unpacked_masked.x) + bitCount(mask_v_unpacked_masked.y)) * 6;

    uint shared_slot;
    if(gl_LocalInvocationID.x == 0){
        shared_slot = atomicAdd(O_DrawCMDs.cmds[gl_WorkGroupID.y].count, mask_v_bitcnt);
    }
    shared_slot = readFirstInvocationARB(shared_slot);

    if(laneActive){
        OutIndexBuffer.i[o_tri_off + shared_slot + oSlot + 0] = bitfieldExtract(idx0, 0, 24);
        OutIndexBuffer.i[o_tri_off + shared_slot + oSlot + 1] = idx1;
        OutIndexBuffer.i[o_tri_off + shared_slot + oSlot + 2] = idx2;
        OutIndexBuffer.i[o_tri_off + shared_slot + oSlot + 3] = idx3;
        OutIndexBuffer.i[o_tri_off + shared_slot + oSlot + 4] = idx4;
        OutIndexBuffer.i[o_tri_off + shared_slot + oSlot + 5] = idx5;
    }
}