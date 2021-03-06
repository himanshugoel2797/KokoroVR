layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct draw_cmd_t{
    uint count;
    uint instanceCount;
    uint firstIndex;
    uint baseVertex;
    uint baseInstance;
};

layout(std430, binding = 6) restrict coherent buffer OutDrawCMDs_t {
    uint x;
    uint drawCalls;
    uint z;
    uint drawCalls1;
    draw_cmd_t cmds[];
} O_DrawCMDs;

layout(std430, binding = 5) restrict readonly buffer DrawCMDs_t {
    uint x;
    uint drawCalls;
    uint z;
    uint drawCalls1;
    draw_cmd_t cmds[];
} DrawCMDs;

void main(){
    //indirect dispatch using DrawCMDs.drawCalls
    uint cnt = DrawCMDs.cmds[gl_GlobalInvocationID.x].count;
    bool laneActive = cnt != 0;
    uint64_t mask_v = ballotARB(laneActive);
	uvec2 mask_v_unpacked = unpackUint2x32(mask_v);
	uvec2 mask_v_unpacked_masked = unpackUint2x32(mask_v & gl_SubGroupLtMaskARB);
	int mask_v_bitcnt = int(bitCount(mask_v_unpacked.x) + bitCount(mask_v_unpacked.y));
    int oSlot = int(bitCount(mask_v_unpacked_masked.x) + bitCount(mask_v_unpacked_masked.y));

    uint shared_slot;
    if(gl_LocalInvocationID.x == 0){
        shared_slot = atomicAdd(O_DrawCMDs.drawCalls1, mask_v_bitcnt);
    }
    shared_slot = readFirstInvocationARB(shared_slot);

    if(laneActive){
        O_DrawCMDs.cmds[shared_slot + oSlot].count = cnt;
        O_DrawCMDs.cmds[shared_slot + oSlot].instanceCount = DrawCMDs.cmds[gl_GlobalInvocationID.x].instanceCount;
        O_DrawCMDs.cmds[shared_slot + oSlot].firstIndex = DrawCMDs.cmds[gl_GlobalInvocationID.x].firstIndex;
        O_DrawCMDs.cmds[shared_slot + oSlot].baseVertex = DrawCMDs.cmds[gl_GlobalInvocationID.x].baseVertex;
        O_DrawCMDs.cmds[shared_slot + oSlot].baseInstance = DrawCMDs.cmds[gl_GlobalInvocationID.x].baseInstance;
    }
}