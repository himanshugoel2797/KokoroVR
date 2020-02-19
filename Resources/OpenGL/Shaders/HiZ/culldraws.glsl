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

layout(std140, binding = 2) uniform HiZLayers_t {
    uvec4 sdpp;
    uvec4 mips[EYECOUNT][MIP_COUNT];
    uvec4 mip_tex;
} HiZLayers;

layout(std430, binding = 2) restrict coherent buffer DrawCMDs_t {
    uint drawCount;
    uint p0;
    uint p1;
    uint p2;
    draw_cmd_t cmds[];
} src_DrawCMDs;

uniform int eyeIdx;

void main(){
    //position = gl_GlobalInvocationID
    int drawIdx = int(gl_GlobalInvocationID.x);

    //Compute screen space position of each AABB corner given the previous VP matrix
    vec3 aabb[2];
    aabb[0] = src_DrawCMDs.cmds[drawIdx].aabb_min.xyz;
    aabb[1] = src_DrawCMDs.cmds[drawIdx].aabb_max.xyz;
    vec4 persp_v = GlobalParams.prev_vp[eyeIdx] * vec4(aabb[0].x, aabb[0].y, aabb[0].z, 1);
    persp_v.xyz /= persp_v.w;
    
    vec2 rect[2];
    float inst_dpth;
    rect[0] = persp_v.xy;
    rect[1] = persp_v.xy;
    inst_dpth = persp_v.z;
    for (int i = 1; i < 8; i++){
        persp_v = GlobalParams.prev_vp[eyeIdx] * vec4(aabb[i >> 2].x, aabb[(i >> 1) & 1].y, aabb[i & 1].z, 1);
        persp_v.xyz /= persp_v.w;
        rect[0] = min(rect[0], persp_v.xy);
        rect[1] = max(rect[1], persp_v.xy);
        inst_dpth = max(inst_dpth, persp_v.z);
    }
    rect[0] = rect[0] * 0.5f + 0.5f;
    rect[1] = rect[1] * 0.5f + 0.5f;

    vec2 vsz = (rect[1] - rect[0]) * vec2(GlobalParams.eyePos.w /*Width*/, GlobalParams.eyeUp.w /*Height*/);
    float lod = ceil( log2( max(vsz.x, vsz.y) * 0.5f ) );
    
    vec4 samples;
    sampler2D hiz_sampler = sampler2D(HiZLayers.mip_tex.xy);
    samples.x = textureLod(hiz_sampler, vec2(rect[0].x, rect[0].y), lod).x;
    samples.y = textureLod(hiz_sampler, vec2(rect[0].x, rect[1].y), lod).x;
    samples.z = textureLod(hiz_sampler, vec2(rect[1].x, rect[1].y), lod).x;
    samples.w = textureLod(hiz_sampler, vec2(rect[1].x, rect[0].y), lod).x;
    
    float sampledDepth = min(min(samples.x, samples.y), min(samples.z, samples.w));
    src_DrawCMDs.cmds[drawIdx].count *= int(inst_dpth >= sampledDepth);
}