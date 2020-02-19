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

layout(std430, binding = 2) restrict buffer DrawCMDs_t {
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
    vec3 aabb_min = src_DrawCMDs.cmds[drawIdx].aabb_min.xyz;
    vec3 aabb_max = src_DrawCMDs.cmds[drawIdx].aabb_max.xyz;
    vec4 persp_v[8];

    persp_v[0] = GlobalParams.prev_vp[eyeIdx] * vec4(aabb_min.x, aabb_min.y, aabb_min.z, 1);
    persp_v[1] = GlobalParams.prev_vp[eyeIdx] * vec4(aabb_min.x, aabb_min.y, aabb_max.z, 1);
    persp_v[2] = GlobalParams.prev_vp[eyeIdx] * vec4(aabb_min.x, aabb_max.y, aabb_min.z, 1);
    persp_v[3] = GlobalParams.prev_vp[eyeIdx] * vec4(aabb_min.x, aabb_max.y, aabb_max.z, 1);
    persp_v[4] = GlobalParams.prev_vp[eyeIdx] * vec4(aabb_max.x, aabb_min.y, aabb_min.z, 1);
    persp_v[5] = GlobalParams.prev_vp[eyeIdx] * vec4(aabb_max.x, aabb_min.y, aabb_max.z, 1);
    persp_v[6] = GlobalParams.prev_vp[eyeIdx] * vec4(aabb_max.x, aabb_max.y, aabb_min.z, 1);
    persp_v[7] = GlobalParams.prev_vp[eyeIdx] * vec4(aabb_max.x, aabb_max.y, aabb_max.z, 1);
    
    persp_v[0].xyz /= persp_v[0].w;

    vec2 rect[2];
    float inst_dpth;
    rect[0] = persp_v[0].xy;
    rect[1] = persp_v[0].xy;
    inst_dpth = persp_v[0].z;
    for (int i = 1; i < 8; i++){
        persp_v[i].xyz /= persp_v[i].w;
        rect[0] = min(rect[0], persp_v[i].xy);
        rect[1] = max(rect[1], persp_v[i].xy);
        inst_dpth = max(inst_dpth, persp_v[i].z);
    }
    rect[0] = rect[0] * 0.5f + 0.5f;
    rect[1] = rect[1] * 0.5f + 0.5f;
    //inst_dpth = inst_dpth;
    
    //rect[0] = clamp(rect[0], 0, 1);
    //rect[1] = clamp(rect[1], 0, 1);
    //inst_dpth = clamp(inst_dpth, 0, 1);

    vec2 vsz = (rect[1] - rect[0]) * vec2(GlobalParams.eyePos.w /*Width*/, GlobalParams.eyeUp.w /*Height*/);
    float lod = clamp( ceil( log2( max(vsz.x, vsz.y) * 0.5f ) ), 0, MIP_COUNT - 1 );
    
    vec4 samples;
    //sampler2D hiz_sampler = sampler2D(HiZLayers.mip_tex.xy);
    //samples.x = textureLod(hiz_sampler, vec2(rect[0].x, rect[0].y), lod).x;
    //samples.y = textureLod(hiz_sampler, vec2(rect[0].x, rect[1].y), lod).x;
    //samples.z = textureLod(hiz_sampler, vec2(rect[1].x, rect[1].y), lod).x;
    //samples.w = textureLod(hiz_sampler, vec2(rect[1].x, rect[0].y), lod).x;
    image2D hiz = image2D(HiZLayers.mips[0][int(lod)].xy);
    ivec2 im_sz = imageSize(hiz);
    samples.x = imageLoad(hiz, ivec2(rect[0].x * im_sz.x, rect[0].y * im_sz.y)).x;
    samples.y = imageLoad(hiz, ivec2(rect[0].x * im_sz.x, rect[1].y * im_sz.y)).x;
    samples.z = imageLoad(hiz, ivec2(rect[1].x * im_sz.x, rect[1].y * im_sz.y)).x;
    samples.w = imageLoad(hiz, ivec2(rect[1].x * im_sz.x, rect[0].y * im_sz.y)).x;
    float sampledDepth = min(min(samples.x, samples.y), min(samples.z, samples.w));
    
    //image2D hiz0 = image2D(HiZLayers.mips[0][0].xy);
    //im_sz = ivec2(1024);//imageSize(hiz0);
    
    //for (int i = 0; i < 8; i++)
    //    imageStore(hiz0, ivec2((persp_v[i].x * 0.5f + 0.5f) * im_sz.x, (persp_v[i].y * 0.5f + 0.5f) * im_sz.y), vec4(sampledDepth * 50, inst_dpth * 50, 0, 1)).x;
    
    //if any corner is greater than the Hi-Z value, this draw is visible 
    //if(inst_dpth < 0.9f)return;
    src_DrawCMDs.cmds[drawIdx].count *= (inst_dpth > sampledDepth) ? 1 : 0;
}