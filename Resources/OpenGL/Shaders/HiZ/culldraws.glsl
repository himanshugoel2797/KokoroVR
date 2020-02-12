//cull draws by checking nearest corner distance with chain
//take closest corners of each AABB, project them onto previous frame's buffers
//use this for culling provided list of draws, one draw per execution

layout(local_size_x = 64) in;

struct draw_cmd_t{
    uint count;
    uint instanceCount;
    uint firstIndex;
    uint zr0;
    uint baseInstance;
	uint index_block;
	uint vbo_idx;
	uint base_vertex;
    vec4 aabb_min;
    vec4 aabb_max;
};

layout(std140, binding = 0) uniform GlobalParams_t {
	mat4 proj[EYECOUNT];
	mat4 view[EYECOUNT];
	mat4 vp[EYECOUNT];
	mat4 prev_view[EYECOUNT];
	mat4 prev_vp[EYECOUNT];
	uvec4 infoBindings[EYECOUNT];
	uvec4 depthBindings[EYECOUNT];
	vec4 eyePos;
	vec4 eyeUp;
	vec4 eyeDir;
} GlobalParams;

layout(std430, binding = 0) restrict readonly buffer DrawCMDs_t {
    uint drawCount;
    uint p0;
    uint p1;
    uint p2;
    draw_cmd_t cmds[];
} src_DrawCMDs;

layout(std430, binding = 1) restrict writeonly buffer DrawCMDs_t {
    uint drawCount;
    uint p0;
    uint p1;
    uint p2;
    draw_cmd_t cmds[];
} dst_DrawCMDs;

uniform int eyeIdx;

int main(){
    //position = gl_GlobalInvocationID
    //Compute screen space position of each AABB corner given the previous VP matrix
    //Determine the mip level to fit 4 pixels on screen
    //if any corner is greater than the Hi-Z value, this draw is visible 
}